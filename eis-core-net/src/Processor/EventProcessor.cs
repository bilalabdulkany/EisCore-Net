
using System;
using System.IO;
using EisCore.Model;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using EisCore.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Apache.NMS;
using Apache.NMS.Util;

namespace EisCore
{
    public class EventProcessor : IDisposable
    {
        private bool isDisposed = false;

        private readonly ILogger<EventProcessor> _log;
        // private static readonly configurationManager _configurationManager=new configurationManager();
        private readonly ApplicationSettings _appSettings;
        readonly IConfigurationManager _configManager;
        private ISession _session;
        private IConnection _connection;
        private IDestination _destination;
        protected static ITextMessage queueMessage = null;
        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        protected static AutoResetEvent semaphore = new AutoResetEvent(false);

        private IMessageConsumer _consumer;

        public EventProcessor(ILogger<EventProcessor> log, IConfigurationManager configurationManager)
        {
            this._log = log;
            this._configManager = configurationManager;
            this._appSettings = configurationManager.GetAppSettings();
            
            _log.LogInformation("EventProcessor constructor");
            _session = _configManager.GetBrokerConfiguration().session;
            _connection = _configManager.GetBrokerConfiguration().connection;
            if (_session == null || _connection == null)
            {
                _log.LogInformation("TCP Session not available, creating one..");
                //
                if (_session == null) { throw new Exception("Session cannot be created"); }
            }
            var Queue = this._appSettings.InboundQueue;
            _destination = SessionUtil.GetQueue(_session, Queue);
            _log.LogInformation("Destination Topic: {d}", _destination);
            //this.producer = _session.CreateProducer(destination); //new SenderLink(session, "sender-link", topic);
            IMessageConsumer consumer = _session.CreateConsumer(_destination);
            _consumer = consumer;
            RunConsumerEventListener();
        }

        public void RunConsumerEventListener()
        {
            _log.LogInformation("Starting listener"); 
            _connection.Start();
            _consumer.Listener += new MessageListener(OnMessage);
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
            receivedMsg.Acknowledge();
            //_consumer.Close();
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