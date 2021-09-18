using System;
using System.Threading;
using Apache.NMS;
using Apache.NMS.Util;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EisCore.Infrastructure.Configuration
{
    public class BrokerConfigFactory : IBrokerConfigFactory
    {
        private bool isDisposed = false;
        private readonly ILogger<BrokerConfigFactory> _log;
        private BrokerConfiguration _brokerConfiguration;
        private ApplicationSettings _appSettings;
        readonly IConfigurationManager _configManager;
        private ISession _ConsumerSession;
        private ISession _ProducerSession;
        public IConnection _ConsumerConnection{get;set;}
        public IConnection _ProducerConnection{get;set;}

        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        protected static AutoResetEvent semaphore = new AutoResetEvent(false);

        private IMessageConsumer _consumer;
        private IMessageProducer _publisher;
        public BrokerConfigFactory(ILogger<BrokerConfigFactory> log, IConfigurationManager configurationManager, BrokerConfiguration brokerConfig)
        {
            this._log = log;
            this._configManager = configurationManager;
            this._appSettings = configurationManager.GetAppSettings();
            // this._brokerConfiguration=brokerConfig;
            _log.LogInformation("BrokerFactory constructor");
            _brokerConfiguration = configurationManager.GetBrokerConfiguration();
            CreateBrokerConnection();
            if (_ProducerSession == null || _ProducerConnection == null
            || _ConsumerSession == null || _ConsumerConnection == null
            )
            {
                //TODO if either is null, throw exception for now
                _log.LogInformation("TCP Session not available, creating one..");
                throw new Exception("Session cannot be created");
            }
        }

        public void CreateBrokerConnection()
        {
            IConnection ConsumerTcpConnection = null;
            ISession ConsumerTcpSession = null;

            IConnection ProducerTcpConnection = null;
            ISession ProducerTcpSession = null;
            try
            {
                var brokerUrl = _configManager.GetBrokerUrl();

                _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

                Uri connecturi = new Uri(brokerUrl);
                IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(connecturi);

                ProducerTcpConnection = factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                ConsumerTcpConnection = factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);

                if (ProducerTcpConnection.IsStarted&&ConsumerTcpConnection.IsStarted)
                {
                    _log.LogInformation("producer and consumer connection started");
                }

               /* TcpConnection.ConnectionInterruptedListener += new ConnectionInterruptedListener(OnConnectionInterruptedListener);
                TcpConnection.ConnectionResumedListener += new ConnectionResumedListener(OnConnectionResumedListener);
                TcpConnection.ExceptionListener += new ExceptionListener(OnExceptionListener);
                */
                ProducerTcpSession = ProducerTcpConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                ConsumerTcpSession = ConsumerTcpConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                _ProducerConnection = ProducerTcpConnection;
                _ConsumerConnection= ConsumerTcpConnection;
                _ProducerSession = ProducerTcpSession;
                _ConsumerSession=ConsumerTcpSession;


                _log.LogInformation("##Created Publisher connection {con}", this._ProducerConnection.ToString());
                _log.LogInformation("##Created Consumer connection {con}", this._ConsumerConnection.ToString());
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
                try
                {
                    _log.LogCritical("Stopping Connection..");
                    ProducerTcpConnection.Stop();
                    ConsumerTcpConnection.Stop();
                }
                catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
                {
                    _log.LogCritical("Cannot close the connection {e2}", e2.StackTrace);
                    ProducerTcpConnection.Close();
                    ConsumerTcpConnection.Close();

                }
                finally
                {
                    ProducerTcpConnection.Dispose();
                    ConsumerTcpConnection.Dispose();

                }
            }
            catch (Exception e)
            {
                _log.LogError("Exception occurred {class}: {e}", this.ToString(), e.StackTrace);
            }
        }


        public IMessageConsumer CreateConsumer()
        {
            if (_ConsumerConnection.IsStarted)
            {
                _log.LogInformation("producer connection started");
            } else {
                _log.LogInformation("producer connection not started, starting");
                _ConsumerConnection.Start();
            }
            var Queue = this._appSettings.InboundQueue;
            var QueueDestination = SessionUtil.GetQueue(_ConsumerSession, Queue);
            _log.LogInformation("Created MessageProducer for Destination Queue: {d}", QueueDestination);
            _consumer = _ConsumerSession.CreateConsumer(QueueDestination);
            return _consumer;
        }


        public IMessageProducer CreateProducer()
        {
            var topic = _configManager.GetAppSettings().OutboundTopic;
            var TopicDestination = SessionUtil.GetTopic(_ProducerSession, topic);
            _ProducerConnection.Start();
            if (_ProducerConnection.IsStarted)
            {
                _log.LogInformation("connection started");
            }
            _publisher = _ProducerSession.CreateProducer(TopicDestination);
            _publisher.DeliveryMode = MsgDeliveryMode.Persistent;
            _publisher.RequestTimeout = receiveTimeout;
            _log.LogInformation("Created MessageProducer for Destination Topic: {d}", TopicDestination);
            return _publisher;
        }

        public ITextMessage GetTextMessageRequest(string message)
        {
            ITextMessage request = _ProducerSession.CreateTextMessage(message);
            request.NMSCorrelationID = Guid.NewGuid().ToString();
            // request.Properties["NMSXGroupID"] = "cheese";
            // request.Properties["myHeader"] = "Cheddar";
            return request;
        }

        protected void OnConnectionInterruptedListener()
        {
            _log.LogDebug("Connection Interrupted.. Trying to reconnect");
            // CreateAsyncBrokerConnection();

        }
        protected void OnConnectionResumedListener()
        {
            _log.LogDebug("Connection Resumed");
        }
        protected void OnExceptionListener(Exception NMSException)
        {
            _log.LogError("On Exception Listener: {e}", NMSException.GetBaseException());
            //    CreateAsyncBrokerConnection();
        }



        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                if (_publisher != null)
                {
                    _publisher.Close();
                }
                if (_consumer != null)
                {
                    _consumer.Close();
                }
                this.isDisposed = true;
            }
        }
        #endregion

    }
}