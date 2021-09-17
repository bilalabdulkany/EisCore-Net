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
        private ISession _session;
        private IConnection _connection;
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
            if (_session == null || _connection == null)
            {
                _log.LogInformation("TCP Session not available, creating one..");
                if (_session == null) { throw new Exception("Session cannot be created"); }
            }
        }

        public void CreateBrokerConnection()
        {
            IConnection TcpConnection = null;
            ISession TcpSession = null;
            try
            {
                var brokerUrl = _configManager.GetBrokerUrl();

                _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

                Uri connecturi = new Uri(brokerUrl);

                IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(connecturi);

                TcpConnection = factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                if (TcpConnection.IsStarted)
                {
                    _log.LogInformation("connection started");

                }

                // TcpConnection.ConnectionInterruptedListener += new ConnectionInterruptedListener(OnConnectionInterruptedListener);
                // TcpConnection.ConnectionResumedListener += new ConnectionResumedListener(OnConnectionResumedListener);
                // TcpConnection.ExceptionListener += new ExceptionListener(OnExceptionListener);


                TcpSession = TcpConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                _connection = TcpConnection;
                _session = TcpSession;
                _log.LogInformation("##Created connection {con}", this._connection.ToString());
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
                try
                {
                    _log.LogCritical("Stopping Connection..");
                    TcpConnection.Stop();
                }
                catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
                {
                    _log.LogCritical("Cannot close the connection {e2}", e2.StackTrace);
                    TcpConnection.Close();
                }
                finally
                {
                    TcpConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                _log.LogError("Exception occurred {class}: {e}", this.ToString(), e.StackTrace);
            }

        }


        public IMessageConsumer CreateConsumer()
        {
            _connection.Start();
            if (_connection.IsStarted)
            {
                _log.LogInformation("connection started");
            }
            var Queue = this._appSettings.InboundQueue;
            var QueueDestination = SessionUtil.GetQueue(_session, Queue);
            _log.LogInformation("Created MessageProducer for Destination Queue: {d}", QueueDestination);
            _consumer = _session.CreateConsumer(QueueDestination);
            return _consumer;
        }


        public IMessageProducer CreateProducer()
        {
            var topic = _configManager.GetAppSettings().OutboundTopic;
            var TopicDestination = SessionUtil.GetTopic(_session, topic);
            _connection.Start();
            if (_connection.IsStarted)
            {
                _log.LogInformation("connection started");
            }
            _publisher = _session.CreateProducer(TopicDestination);
            _publisher.DeliveryMode = MsgDeliveryMode.Persistent;
            _publisher.RequestTimeout = receiveTimeout;
            _log.LogInformation("Created MessageProducer for Destination Topic: {d}", TopicDestination);
            return _publisher;
        }

        public ITextMessage GetTextMessageRequest(string message)
        {
            ITextMessage request = _session.CreateTextMessage(message);
            request.NMSCorrelationID = Guid.NewGuid().ToString();
            // request.Properties["NMSXGroupID"] = "cheese";
            // request.Properties["myHeader"] = "Cheddar";
            return request;
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