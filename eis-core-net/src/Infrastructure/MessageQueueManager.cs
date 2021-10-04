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
            sourceName = _configManager.GetAppSettings().Name;
            testHostIp = Guid.NewGuid().ToString();
            _dbContext.setHostIpAddress(testHostIp);
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
                 var recordUpdateStatus = 0;
                 string _eventID = null;
                 List<EisEventInboxOutbox> inboxEventsList = _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.IN).Result;
                 if (inboxEventsList.Count > 0)
                 {
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

                             recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.PROCESSED).Result;
                             _log.LogInformation("Processed {e}, with status {s}", _eventID.ToString(), recordUpdateStatus);
                         }
                         catch (Exception e)
                         {
                             _log.LogError("Exception occurred while processing > {e}", e.StackTrace);
                             recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.FAILED).Result;
                         }
                     }
                 }
                 else
                 {
                     GlobalVariables.IsUnprocessedInMessagePresent = false;
                     //this.ConsumeEvent(eisEvent, dbEvents.TopicQueueName, _eventRegistry, sourceName);
                     //_brokerConnectionFactory.CreateConsumerListener();
                     //recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.PROCESSED).Result;
                     //_log.LogInformation("Processed {e}, with status {s}", _eventID.ToString(), recordUpdateStatus);
                     //TODO get current message which is received and process without the poller
                 }
             }
             );
        }

        private Task ProcessAllUnprocessedOutboxEvents()
        {
            return Task.Run(() =>
            {
                var recordUpdateStatus = 0;
                string _eventID = null;
                //TODO check thread safety and use BlockingCollection<EisEventInboxOutbox> Collection
                List<EisEventInboxOutbox> outboxEventsList = _eventINOUTDbContext.GetAllUnprocessedEvents(AtLeastOnceDeliveryDirection.OUT).Result;
                if (outboxEventsList.Count > 0)
                {
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
                            recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.PROCESSED).Result;
                            _log.LogInformation("Processed {e}, with status {s}", _eventID.ToString(), recordUpdateStatus);
                        }
                        catch (Exception e)
                        {
                            _log.LogError("Exception occurred while processing > {e}", e.StackTrace);
                            recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(_eventID, TestSystemVariables.FAILED).Result;
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
                _brokerConnectionFactory.QueueToPublisherTopic(eisEvent);
                var recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED).Result;
                _log.LogInformation("OUTBOX::Processed {e}, with status {s}", eisEvent.EventID.ToString(), recordUpdateStatus);
            }

            if (!isCurrent)//First publish the messages in OUTBOX queue if not empty
            {
                _brokerConnectionFactory.QueueToPublisherTopic(eisEvent);
                var recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED).Result;
                _log.LogInformation("OUTBOX::TIMER::Processed {e}, with status {s}", eisEvent.EventID.ToString(), recordUpdateStatus);
            }//TODO process current events

            //If it is coming from timer, isCurrent=false, and GlobalVariables.IsUnprocessedOutMessagePresent is true -- do nothing - let the 
        }

        public void ConsumeEvent(EisEvent eisEvent, string queueName)
        {

            UtilityClass.ConsumeEvent(eisEvent, queueName, _eventRegistry, sourceName, _log);
            /*IMessageProcessor messageProcessor = _eventRegistry.GetMessageProcessor();
            if (messageProcessor == null)
            {
                _log.LogError("{app}: No message handler found for the event ID {id} in queue {queue}", sourceName, eisEvent.EventID, queueName);
                throw new Exception("No MessageProcessor found for the queue");
            }
            try
            {
                _log.LogInformation("{app}: message with event {event} received", sourceName, eisEvent);
                messageProcessor.Process(eisEvent.Payload, eisEvent.EventType);
            }
            catch (Exception e)
            {
                _log.LogError("{app}: Processing of message with id {id} failed with error {er}", sourceName, eisEvent.EventID, e.Message);
                throw new Exception($"Processing event with ID > {eisEvent.EventID} failed > {e.Message}");
            }*/
        }



        public async Task ConsumerKeepAliveTask()
        {
            await Console.Out.WriteLineAsync("#########Consumer Connection Quartz Job... Cron: [" + _configManager.GetBrokerConfiguration().CronExpression + "]");

            //TODO Testing only with one System: XYZ
            var eisGroupKey = SourceSystemName.MDM + "_COMPETING_CONSUMER_GROUP";
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
                            //bool canStart=GlobalVariables.IsCurrentIpLockedForConsumer&&!GlobalVariables.IsTransportInterrupted;
                            _brokerConnectionFactory.CreateConsumerListener();
                            var keepAliveResult = _dbContext.KeepAliveEntry(true, eisGroupKey);
                            _log.LogInformation("***Refreshing Keep Alive entry {k}", keepAliveResult.Result);
                            GlobalVariables.IsCurrentIpLockedForConsumer = true;
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