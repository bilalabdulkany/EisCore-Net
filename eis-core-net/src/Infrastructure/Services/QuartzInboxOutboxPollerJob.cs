using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using EisCore.Application.Constants;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using Quartz;

namespace eis_core_net.src.Infrastructure.Services
{
    public class QuartzInboxOutboxPollerJob : IJob
    {

        private readonly IBrokerConnectionFactory _brokerConfigFactory;
        private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;
        private readonly IConfigurationManager _configManager;
        private readonly ILogger<QuartzInboxOutboxPollerJob> _log;

        private readonly EventHandlerRegistry _eventRegistry;
        string sourceName;
        public QuartzInboxOutboxPollerJob(IBrokerConnectionFactory brokerConfigFactory, IEventInboxOutboxDbContext eventINOUTDbContext, IConfigurationManager configManager, ILogger<QuartzInboxOutboxPollerJob> log, EventHandlerRegistry eventRegistry)
        {
            this._brokerConfigFactory = brokerConfigFactory;
            this._eventINOUTDbContext = eventINOUTDbContext;
            this._configManager = configManager;
            this._log = log;
            this._eventRegistry = eventRegistry;
            sourceName = _configManager.GetAppSettings().Name;
        }

        public Task Execute(IJobExecutionContext context)
        {

            IEnumerable<EisEventInboxOutbox> listofEvents = _eventINOUTDbContext.GetAllUnprocessedEvents().Result;
            foreach (var events in listofEvents)
            {
                //TODO check null

                EisEventInboxOutbox dbEvents = events;
                EisEvent eisEvent = JsonSerializer.Deserialize<EisEvent>(events.eisEvent);
                this.ConsumeEvent(eisEvent, dbEvents.TopicQueueName, _eventRegistry, sourceName);
                var recordUpdateStatus = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED).Result;
                //TODO try catch exception
                _log.LogInformation("Processed {e}, with status {s}", eisEvent.EventID.ToString(), recordUpdateStatus);

            }

            _log.LogInformation("QuartzInboxOutboxPollerJob >>Executing Task");
            return Task.CompletedTask;
        }


        private void ConsumeEvent(EisEvent eisEvent, string queueName, EventHandlerRegistry eventRegistry, string sourceApplicationName)
        {
            IMessageProcessor messageProcessor = eventRegistry.GetMessageProcessor();
            if (messageProcessor == null)
            {
                _log.LogError("{app}: No message handler found for the event ID {id} in queue {queue}", sourceApplicationName, eisEvent.EventID, queueName);
                throw new Exception("No MessageProcessor found for the queue");
            }
            try
            {
                _log.LogInformation("{app}: message with event {event} received", sourceApplicationName, eisEvent);
                messageProcessor.Process(eisEvent.Payload, eisEvent.EventType);
            }
            catch (Exception e)
            {
                _log.LogError("{app}: Processing of message with id {id} failed with error {er}", sourceApplicationName, eisEvent.EventID, e.Message);
                throw new Exception($"Processing event with ID > {eisEvent.EventID} failed > {e.Message}");
            }
        }

    }
}