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

        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        private bool isDisposed = false;

        public EventPublisher(ILogger<EventPublisher> log, IConfigurationManager configManager, IBrokerConnectionFactory brokerConfigFactory)        {
            //this._appDbContext=appDbContext;
            this._log = log;
            this._brokerConfigFactory=brokerConfigFactory;
            this._configManager = configManager;           

        }


        public void publish(string messagePublish)
        {
            try
            {
                _log.LogInformation("Trying to publish message...");
                _publisher = _brokerConfigFactory.CreatePublisher();                
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                ITextMessage request = _brokerConfigFactory.GetTextMessageRequest(messagePublish);
                _publisher.Send(request);
                //_appDbContext.Create(new Application.Models.Event("ID3","Code1","Publish-Event","Testing Sample Insert"));
                watch.Stop();
                _log.LogInformation("Message Sent! time taken {milliseconds} ms to Topic: {topic}", watch.ElapsedMilliseconds, _configManager.GetAppSettings().OutboundTopic);
            }
            catch (Exception e)
            {
                _log.LogError("Error occurred {e}", e.StackTrace);

            }

        }

        public void publish(IMessageEISProducer messageObject)
        {
            try
            {
                //TODO check if connection is stable and up

                //  
                _publisher = _brokerConfigFactory.CreatePublisher();

                _log.LogInformation("sending object");
                EisEvent eisEvent = this.getEisEvent(messageObject);
                var watch = new System.Diagnostics.Stopwatch();
                string jsonString = JsonSerializer.Serialize(eisEvent);
                watch.Start();
                _log.LogInformation("{s}", jsonString);
                ITextMessage request = _brokerConfigFactory.GetTextMessageRequest(jsonString);
                _publisher.Send(request);
                //_appDbContext.Create(new Application.Models.Event("ID11","Code1","Publish-Event","Testing Sample Insert"));
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

       



        private EisEvent getEisEvent(IMessageEISProducer messageProducer)
        {
            EisEvent eisEvent = new EisEvent();
            eisEvent.eventID = Guid.NewGuid().ToString();
            eisEvent.eventType = messageProducer.getEventType();
            eisEvent.traceId = messageProducer.getTraceId();
            eisEvent.spanId = Guid.NewGuid().ToString();
            eisEvent.createdDate = DateTime.Now;
            eisEvent.sourceSystemName = SourceSystemName.MDM;//TODO get the name from properies
            eisEvent.payload = messageProducer.getPayLoad();
            return eisEvent;
        }


        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                _publisher.Close();
                this.isDisposed = true;
            }
        }
        #endregion
    }
}