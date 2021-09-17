
using System;
using System.IO;
using EisCore.Domain.Entities;
using EisCore.Application.Interfaces;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Apache.NMS;
using Apache.NMS.Util;

namespace EisCore
{
    public class EventProcessor : IEventProcessor
    {
        private bool isDisposed = false;
        private readonly ILogger<EventProcessor> _log;
        readonly IConfigurationManager _configManager;
        //readonly IApplicationDbContext _appDbContext;
        protected static ITextMessage queueMessage = null;

        private EventHandlerRegistry _eventHandlerRegistry;

        private IBrokerConfigFactory _brokerConfigFactory;
         private IMessageConsumer _consumer;

        public EventProcessor(ILogger<EventProcessor> log, IConfigurationManager configurationManager,
           EventHandlerRegistry eventHandlerRegistry, IBrokerConfigFactory brokerConfigFactory)
        {
            this._log = log;
            this._brokerConfigFactory=brokerConfigFactory;
           
            _eventHandlerRegistry = eventHandlerRegistry;
            RunConsumerEventListener();
        }

        public void RunConsumerEventListener()
        {
            _log.LogInformation("Starting listener"); 
            _consumer = _brokerConfigFactory.CreateConsumer();
            _consumer.Listener += new MessageListener(OnMessage);
            //_log.LogInformation("Logging data",_appDbContext.Get().Result.GetEnumerator().Current);

        }

        protected void OnMessage(IMessage receivedMsg)
        {    
            try{
            _log.LogInformation("Receiving the message inside OnMessage");
            queueMessage = receivedMsg as ITextMessage;

            _log.LogInformation("Received message with ID: {n}  ", queueMessage.NMSMessageId);
             _log.LogInformation("Received message with text: {n}  ", queueMessage.Text);

            EisEvent eisEvent = JsonSerializer.Deserialize<EisEvent>(queueMessage.Text);
            _log.LogInformation("Receiving the message: {eisEvent}", eisEvent.ToString());

            _eventHandlerRegistry.GetMessageProcessor().Process(eisEvent.payload, eisEvent.eventType);
            receivedMsg.Acknowledge();
            } catch(Exception ex) {
                receivedMsg.Acknowledge();
                _log.LogError("exception in onMessage: {eisEvent}", ex.StackTrace);
                throw ex;
            }
        }



        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                _consumer.Close();
                this.isDisposed = true;
            }
        }
        #endregion
    }

}