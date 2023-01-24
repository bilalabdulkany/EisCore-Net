using System;
using System.Text;
using System.IO;
using EisCore.Domain.Entities;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.Util;
using EisCore.Application.Interfaces;
using EisCore.Application.Constants;

namespace EisCore
{
    public class EventPublisherService : IEventPublisherService
    {

        private readonly IConfigurationManager _configManager;
        private readonly ILogger<EventPublisherService> _log;

        private readonly IMessageQueueManager _messageQueueManager;
        private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;

        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        private bool isDisposed = false;

        public EventPublisherService(ILogger<EventPublisherService> log, IConfigurationManager configManager, IMessageQueueManager messageQueueManager, IEventInboxOutboxDbContext eventINOUTDbContext)
        {

            this._log = log;
            this._configManager = configManager;
            this._eventINOUTDbContext = eventINOUTDbContext;
            this._messageQueueManager = messageQueueManager;
        }


        public void publish(IMessageEISProducer messageObject)
        {
            try
            {               
                _log.LogInformation("sending object");
                EisEvent eisEvent = this.getEisEvent(messageObject);
                Console.WriteLine($"publish Thread={Thread.CurrentThread.ManagedThreadId} SendToQueue called");
                _messageQueueManager.QueueToPublisherTopic(eisEvent, true);//Execute method in separate thread

            }

            catch (Exception e)
            {
                _log.LogError("Error {e}", e.StackTrace);
                _log.LogCritical("Connection Listener delegation..{log}", e.GetBaseException());
            }
        }
        private EisEvent getEisEvent(IMessageEISProducer messageProducer)
        {
            EisEvent eisEvent = new EisEvent();
            eisEvent.EventID = Guid.NewGuid().ToString();
            eisEvent.EventType = messageProducer.getEventType();
            eisEvent.TraceId = messageProducer.getTraceId();
            eisEvent.SpanId = Guid.NewGuid().ToString();
            eisEvent.CreatedDate = DateTime.Now;
            eisEvent.SourceSystemName = _configManager.GetSourceSystemName();
            eisEvent.Payload = messageProducer.getPayLoad();
            return eisEvent;
        }



    }
}