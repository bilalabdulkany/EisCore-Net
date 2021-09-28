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
    public class EventPublisher : IEventPublisher
    {

        private Apache.NMS.IMessageProducer _publisher;
        private readonly IConfigurationManager _configManager;
        //readonly IApplicationDbContext _appDbContext;
        private readonly ILogger<EventPublisher> _log;

        private readonly IBrokerConnectionFactory _brokerConfigFactory;
        private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;

        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        private bool isDisposed = false;

        public EventPublisher(ILogger<EventPublisher> log, IConfigurationManager configManager, IBrokerConnectionFactory brokerConfigFactory, IEventInboxOutboxDbContext eventINOUTDbContext)
        {
            //this._appDbContext=appDbContext;
            this._log = log;
            this._brokerConfigFactory = brokerConfigFactory;
            this._configManager = configManager;
            this._eventINOUTDbContext = eventINOUTDbContext;
        }


        public void publish(IMessageEISProducer messageObject)
        {
            try
            {
                //TODO check if connection is stable and up
                //  
                _log.LogInformation("sending object");
                EisEvent eisEvent = this.getEisEvent(messageObject);
                var OutboundTopic = _configManager.GetAppSettings().OutboundTopic;
                var watch = new System.Diagnostics.Stopwatch();
                string jsonString = JsonSerializer.Serialize(eisEvent);
                watch.Start();
                _log.LogInformation("{s}", jsonString);

                int recordInsertCount = _eventINOUTDbContext.TryEventInsert(eisEvent, OutboundTopic, AtLeastOnceDeliveryDirection.OUT).Result;

                Console.WriteLine($"publish Thread={Thread.CurrentThread.ManagedThreadId} SendToQueue called");
                
                Task.Run(() => SendToQueue(jsonString));

                watch.Stop();
                _log.LogInformation("Message Sent! time taken {milliseconds} ms to Topic: {topic}", watch.ElapsedMilliseconds, _configManager.GetAppSettings().OutboundTopic);
            }

            catch (Exception e)
            {
                _log.LogError("Error {e}", e.StackTrace);
                _log.LogCritical("Connection Listener delegation..{log}", e.GetBaseException());
                // _brokerConfigFactory.CreateBrokerConnection();
            }
        }


        public void SendToQueue(string eisEvent)
        {
            _publisher =  _brokerConfigFactory.CreatePublisher();
            ITextMessage request = _brokerConfigFactory.GetTextMessageRequest(eisEvent);
            _publisher.Send(request);
            Console.WriteLine($"Thread={Thread.CurrentThread.ManagedThreadId} SendToQueue exiting");
        }


        private EisEvent getEisEvent(IMessageEISProducer messageProducer)
        {
            EisEvent eisEvent = new EisEvent();
            eisEvent.EventID = Guid.NewGuid().ToString();
            eisEvent.EventType = messageProducer.getEventType();
            eisEvent.TraceId = messageProducer.getTraceId();
            eisEvent.SpanId = Guid.NewGuid().ToString();
            eisEvent.CreatedDate = DateTime.Now;
            eisEvent.SourceSystemName = SourceSystemName.MDM;//TODO get the name from properies
            eisEvent.Payload = messageProducer.getPayLoad();
            return eisEvent;
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                if (_publisher != null)
                    _publisher.Close();
                this.isDisposed = true;
            }
        }
        #endregion
    }
}