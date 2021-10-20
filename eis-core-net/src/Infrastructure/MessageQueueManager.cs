using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Apache.NMS;
using EisCore.Application.Constants;
using EisCore.Application.Interfaces;
using EisCore.Application.Util;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EisCore
{
    public class MessageQueueManager : IMessageQueueManager
    {
        private readonly IBrokerConnectionFactory _brokerConnectionFactory;
        private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;
        private readonly ICompetingConsumerDbContext _dbContext;
        private readonly IConfigurationManager _configManager;
        private readonly ILogger<MessageQueueManager> _log;
        private readonly EventHandlerRegistry _eventRegistry;
        private string sourceName;
        //TODO testing purposes
        private string testHostIp;

        public MessageQueueManager(IBrokerConnectionFactory brokerConnectionFactory, IEventInboxOutboxDbContext eventINOUTDbContext, ICompetingConsumerDbContext dbContext, IConfigurationManager configManager, ILogger<MessageQueueManager> log, EventHandlerRegistry eventRegistry)
        {
            this._brokerConnectionFactory = brokerConnectionFactory;
            this._eventINOUTDbContext = eventINOUTDbContext;
            this._configManager = configManager;
            this._dbContext = dbContext;
            this._log = log;
            this._eventRegistry = eventRegistry;
            sourceName = _configManager.GetSourceSystemName();
            testHostIp = Guid.NewGuid().ToString();
            _dbContext.setHostIpAddress(testHostIp);
            //Check if any Message Processors are registered, then call the keep alive services
            ConsumerKeepAliveTask();
        }
        public Task InboxOutboxPollerTask()
        {
            if (GlobalVariables.IsCurrentIpLockedForConsumer)
            {
                ProcessAllUnprocessedInboxEvents();//Process existing events from db
                if (!GlobalVariables.IsTransportInterrupted)
                {
                    ProcessAllUnprocessedOutboxEvents();//Publish the evfents from db to the MQ
                }
                else
                {
                    _log.LogInformation("QuartzInboxOutboxPollerJob >> not locked for broker connection");
                }
            }
            return Task.CompletedTask;
        }
        private Task ProcessAllUnprocessedInboxEvents()
        {
            return Task.Run(() =>
             {
                 List<EisEventInboxOutbox> inboxEventsList = _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.IN).Result;
                 if (inboxEventsList.Count > 0)
                 {
                     var recordUpdateStatus = 0;
                     string _eventID = null;
                     _log.LogInformation("INBOX: UnprocessedInboxEvents data are available: {c}", inboxEventsList.Count);
                     foreach (var events in inboxEventsList)
                     {
                         try
                         {
                             //TODO check null
                             EisEventInboxOutbox dbEvents = events;
                             EisEvent eisEvent = JsonSerializer.Deserialize<EisEvent>(events.eisEvent);
                             _eventID = eisEvent.EventID;
                             this.ConsumeEvent(eisEvent, dbEvents.TopicQueueName);
                             recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.IN).Result;
                             _log.LogInformation("Processed {e}, with status {s}", _eventID.ToString(), recordUpdateStatus);
                         }
                         catch (Exception e)
                         {
                             _log.LogError("Exception occurred while processing > {e}", e.StackTrace);
                             recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.FAILED, AtLeastOnceDeliveryDirection.IN).Result;
                         }
                     }
                 }
                 else
                 {
                     GlobalVariables.IsUnprocessedInMessagePresent = false;
                 }
             }
             );
        }

        private Task ProcessAllUnprocessedOutboxEvents()
        {
            return Task.Run(() =>
            {
                //TODO check thread safety and use BlockingCollection<EisEventInboxOutbox> Collection
                List<EisEventInboxOutbox> outboxEventsList = _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT).Result;
                if (outboxEventsList.Count > 0)
                {
                    var recordUpdateStatus = 0;
                    string _eventID = null;
                    _log.LogInformation("OUTBOX: UnprocessedOutboxEvents data are available: {c}", outboxEventsList.Count);

                    foreach (var events in outboxEventsList)
                    {
                        try
                        {
                            //TODO check null
                            EisEventInboxOutbox dbEvents = events;
                            EisEvent eisEvent = JsonSerializer.Deserialize<EisEvent>(events.eisEvent);
                            _eventID = eisEvent.EventID;
                            QueueToPublisherTopic(eisEvent, false);
                            recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.OUT).Result;
                            _log.LogInformation("Processed {e}, with status {s}", _eventID.ToString(), recordUpdateStatus);
                        }
                        catch (Exception e)
                        {
                            _log.LogError("Exception occurred while processing > {e}", e.StackTrace);
                            recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.FAILED, AtLeastOnceDeliveryDirection.OUT).Result;
                        }
                    }
                }
                else
                {
                    GlobalVariables.IsUnprocessedOutMessagePresent = false;
                }
            });
        }

        public void QueueToPublisherTopic(EisEvent eisEvent, bool isCurrent)
        {
            if (isCurrent && !GlobalVariables.IsUnprocessedOutMessagePresent)
            {
                //****
                var OutboundTopic = _configManager.GetAppSettings().OutboundTopic;

                int recordInsertCount = _eventINOUTDbContext.TryEventInsert(eisEvent, OutboundTopic, AtLeastOnceDeliveryDirection.OUT).Result;

                if (recordInsertCount == 1)
                {
                    _log.LogInformation("OUTBOX::NEW [Insert] status: {a}", recordInsertCount);
                    //Console.WriteLine($"publish Thread={Thread.CurrentThread.ManagedThreadId} SendToQueue called");
                    _brokerConnectionFactory.QueueToPublisherTopic(eisEvent);
                    var recordUpdateStatus1 = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.OUT).Result;
                    _log.LogInformation("OUTBOX::Processed {e}, with status {s}", eisEvent.EventID.ToString(), recordUpdateStatus1);
                }
                else
                {
                    _log.LogInformation("OUTBOX::OLD record already published. insert status: {a}", recordInsertCount);
                }
            }

            if (!isCurrent)//First publish the messages in OUTBOX queue if not empty
            {
                _brokerConnectionFactory.QueueToPublisherTopic(eisEvent);
                var recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED, AtLeastOnceDeliveryDirection.OUT).Result;
                _log.LogInformation("OUTBOX::TIMER::Processed {e}, with status {s}", eisEvent.EventID.ToString(), recordUpdateStatus);
            }
            //If it is coming from timer, isCurrent=false, and GlobalVariables.IsUnprocessedOutMessagePresent is true -- do nothing - let the 
        }
        public void ConsumeEvent(EisEvent eisEvent, string queueName)
        {
            UtilityClass.ConsumeEvent(eisEvent, queueName, _eventRegistry, sourceName, _log);
        }
        public async Task ConsumerKeepAliveTask()
        {
            await Console.Out.WriteLineAsync("#########Consumer Connection Quartz Job... Cron: [" + _configManager.GetBrokerConfiguration().CronExpression + "]");
            var eisGroupKey = _configManager.GetSourceSystemName() + "_COMPETING_CONSUMER_GROUP";
            var refreshInterval = _configManager.GetBrokerConfiguration().RefreshInterval;

            try
            {
                //TODO put the hostIP
                var hostIP = testHostIp;// _dbContext.GetIPAddressOfServer(eisGroupKey, refreshInterval);
                var deleteResult = _dbContext.DeleteStaleEntry(eisGroupKey, refreshInterval);
                _log.LogInformation("Stale entry delete status:{r}", deleteResult.Result);
                var insertResult = _dbContext.InsertEntry(eisGroupKey);

                if (insertResult.Result == 1)
                {
                    _brokerConnectionFactory.CreateConsumerListener();
                    _log.LogInformation("*** Consumer locked for: {ip} in group: {groupKey}", hostIP, eisGroupKey);
                    GlobalVariables.IsCurrentIpLockedForConsumer = true;
                }
                else
                {
                    string IpAddress = _dbContext.GetIPAddressOfServer(eisGroupKey, refreshInterval);
                    if (IpAddress != null)
                    {
                        _log.LogInformation($"Current IP: [{testHostIp}]");
                        _log.LogInformation("IsIPAddressMatchesWithGroupEntry(IpAddress): " + IsIPAddressMatchesWithGroupEntry(IpAddress));
                        if (!IsIPAddressMatchesWithGroupEntry(IpAddress))
                        {
                            _brokerConnectionFactory.DestroyConsumerConnection();
                            GlobalVariables.IsCurrentIpLockedForConsumer = false;
                        }
                        else
                        {                            
                            _brokerConnectionFactory.CreateConsumerListener();
                            if (!GlobalVariables.IsTransportInterrupted)
                            {
                                var keepAliveResult = _dbContext.KeepAliveEntry(true, eisGroupKey);
                                _log.LogInformation("***Refreshing Keep Alive entry {k}", keepAliveResult.Result);
                                GlobalVariables.IsCurrentIpLockedForConsumer = true;
                            }
                            else
                            {
                                _log.LogCritical("Broker is down. Connection Interrupted");
                            }
                        }
                    }
                    else
                    {
                        _brokerConnectionFactory.DestroyConsumerConnection();
                        _log.LogInformation("***Connection destroyed");
                    }
                }
                await Console.Out.WriteLineAsync("exiting QuartzKeepAliveEntryJob");
                return;
            }
            catch (Exception e)
            {
                _log.LogCritical("exception when creating consumer: {e}", e.StackTrace);
                _brokerConnectionFactory.DestroyConsumerConnection();
                _log.LogCritical("Consumer connection stopped on IP: ");
            }
            await Console.Out.WriteLineAsync("exception when creating consumer");
            return;
        }
        private bool IsIPAddressMatchesWithGroupEntry(string ipAddress)
        {
            return ipAddress.Equals(testHostIp);
            //TODO revert after testing
            //return ipAddress.Equals(UtilityClass.GetLocalIpAddress());    
        }
    }
}