using System;
using System.Collections.Generic;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EisCorec.Infrastructure.Services.Util
{
    public class EventConsumerUtil
    {
        private static ILogger<EventConsumerUtil> _log;

        public EventConsumerUtil(ILogger<EventConsumerUtil> log)
        {
            _log = log;
        }

        public static void ConsumeEvent(EisEvent eisEvent, string queueName, Dictionary<string, IMessageProcessor> messageProcessors, string sourceApplicationName)
        {
            IMessageProcessor messageProcessor = messageProcessors[queueName];
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