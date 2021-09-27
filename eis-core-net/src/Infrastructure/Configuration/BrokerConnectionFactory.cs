using System;
using System.Threading;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Transport;
using Apache.NMS.Util;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using Apache.NMS.ActiveMQ.Transport.Failover;

namespace EisCore.Infrastructure.Configuration
{
    public class BrokerConnectionFactory : IBrokerConnectionFactory
    {
        private bool isDisposed = false;
        private readonly ILogger<BrokerConnectionFactory> _log;
        private BrokerConfiguration _brokerConfiguration;
        private ApplicationSettings _appSettings;
        readonly IConfigurationManager _configManager;


        //private ISession _ConsumerSession;
        private ISession _ProducerSession;
        public IConnection _ConsumerConnection;
        public IConnection _ProducerConnection;
        private IConnectionFactory _factory;
        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        protected static AutoResetEvent semaphore = new AutoResetEvent(false);

        private IMessageConsumer _MessageConsumer;
        private IMessageProducer _MessagePublisher;

        private readonly IEventProcessor _eventProcessor;
        private FailoverTransport _failoverTransport;
        private Uri _connecturi;
        private static bool IsTransportInterrupted = false;
        ConnectionInterruptedListener interruptedListener = null;
        ConnectionResumedListener connectionResumedListener= null;
        ExceptionListener connecttonExceptionListener =null;

        public BrokerConnectionFactory(ILogger<BrokerConnectionFactory> log,
        IConfigurationManager configurationManager, BrokerConfiguration brokerConfig, IEventProcessor eventProcessor)
        {
            this._log = log;
            this._configManager = configurationManager;
            this._appSettings = configurationManager.GetAppSettings();
            // this._brokerConfiguration=brokerConfig;
            _log.LogInformation("BrokerFactory constructor");
            _brokerConfiguration = configurationManager.GetBrokerConfiguration();
            _eventProcessor = eventProcessor;
            var brokerUrl = _configManager.GetBrokerUrl();
            _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

            _connecturi = new Uri(brokerUrl);
            IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(_connecturi);

            _factory = factory;
            _ConsumerConnection = null;

            interruptedListener = new ConnectionInterruptedListener(OnConnectionInterruptedListener);
            //connectionResumedListener= new ConnectionResumedListener(OnConnectionResumedListener);
            connecttonExceptionListener = new ExceptionListener(OnExceptionListener);
        
           //CreateProducerConnection();

        }
        private void CreateProducerConnection()
        {
            IConnection ProducerTcpConnection = null;
            try
            {
                _ProducerConnection = _factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                //ProducerTcpConnection.ClientId = "Producer_Connection";

                if (_ProducerConnection.IsStarted)
                {
                    _log.LogInformation("producer and consumer connection started");
                }

                _ProducerSession = _ProducerConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                
                _log.LogInformation("##Created Publisher connection {con}", this._ProducerConnection.ToString());
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
                try
                {
                    _log.LogCritical("Stopping Connection..");
                    ProducerTcpConnection.Stop();
                }
                catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
                {
                    _log.LogCritical("Cannot close the connection {e2}", e2.StackTrace);
                    ProducerTcpConnection.Close();
                }
                finally
                {
                    ProducerTcpConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                _log.LogError("Exception occurred {class}: {e}", this.ToString(), e.StackTrace);
            }
        }

        public void CreateConsumer()
        {
            try
            {
                // if (_ConsumerConnection != null && _ConsumerConnection.IsStarted)
                // {
                //     _log.LogInformation("returning existing broker connection in create consumer");
                //     return;
                // }
                // if (_ConsumerConnection != null)
                // {
                //     _log.LogInformation("_ConsumerConnection.IsStarted: " + _ConsumerConnection.IsStarted);
                //    DestroyConsumerConnection();
                //     _log.LogInformation("_ConsumerConnection Stopped");
                // }
                _log.LogInformation("CreateConsumer _ConsumerConnection: >> " + _ConsumerConnection + " <<");
                if(_ConsumerConnection != null) {
                    return;
                }
                _log.LogInformation("Creating new consumer broker connection");
                var brokerUrl = _configManager.GetBrokerUrl();
                _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

                _ConsumerConnection = _factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                //_ConsumerConnection.ClientId = "Consumer_Connection";
                _log.LogInformation("connection created, client id set");

                _ConsumerConnection.ConnectionInterruptedListener += interruptedListener;
                //_ConsumerConnection.ConnectionResumedListener += connectionResumedListener;
                _ConsumerConnection.ExceptionListener += connecttonExceptionListener;
                
                var ConsumerTcpSession = _ConsumerConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);

                _ConsumerConnection.Start();
                IsTransportInterrupted = false;
                if (_ConsumerConnection.IsStarted)
                {
                    _log.LogInformation("consumer connection started");
                }
                else
                {
                    _log.LogInformation("consumer connection not started, starting");
                }
                var Queue = this._appSettings.InboundQueue;
                var QueueDestination = SessionUtil.GetQueue(ConsumerTcpSession, Queue);
                _log.LogInformation("Created MessageProducer for Destination Queue: {d}", QueueDestination);
                _MessageConsumer = ConsumerTcpSession.CreateConsumer(QueueDestination);
                _eventProcessor.RunConsumerEventListener(_MessageConsumer);
            }
            catch (Exception e)
            {
                _log.LogCritical("Error occurred when creating Consumer: {e}", e.StackTrace);
                DestroyConsumerConnection();
                throw e;
            }
        }
        public IMessageProducer CreatePublisher()
        {
            try
            {
                if (_MessagePublisher != null)
                {
                    return _MessagePublisher;
                }
                CreateProducerConnection();
                var topic = _configManager.GetAppSettings().OutboundTopic;
                var TopicDestination = SessionUtil.GetTopic(_ProducerSession, topic);
                _ProducerConnection.Start();
                if (_ProducerConnection.IsStarted)
                {
                    _log.LogInformation("connection started");
                }
                _MessagePublisher = _ProducerSession.CreateProducer(TopicDestination);
                _MessagePublisher.DeliveryMode = MsgDeliveryMode.Persistent;
                _MessagePublisher.RequestTimeout = receiveTimeout;
                _log.LogInformation("Created MessageProducer for Destination Topic: {d}", TopicDestination);
                return _MessagePublisher;
            }
            catch (Exception e)
            {
                _log.LogCritical("Error occurred while creating producer: " + e.StackTrace);
                DestroyProducerConnection();
                throw e;
            }
        }
        public void DestroyConsumerConnection()
        {
            try
            {
                _log.LogInformation("DestroyConsumerConnection - called, IsTransportInterrupted: " + IsTransportInterrupted);
                //   ITransport consumerTransport = _consumerConnFactory.getConsumerTransport();

                if(IsTransportInterrupted) {
                    _log.LogInformation("Transport already stopped, hence exiting");
                } else {
                    if (_ConsumerConnection != null)
                    {
                        _log.LogInformation("_ConsumerConnection != null");
                        _ConsumerConnection.ConnectionInterruptedListener -= interruptedListener;
                        //_ConsumerConnection.ConnectionResumedListener -= connectionResumedListener;
                        _ConsumerConnection.ExceptionListener -= connecttonExceptionListener;
                        _ConsumerConnection.Stop();
                        _ConsumerConnection.Close();
                        _ConsumerConnection.Dispose();
                        _log.LogInformation("_ConsumerConnection started: "+ _ConsumerConnection.IsStarted);
                        _log.LogInformation("DestroyConsumerConnection - connection disposed");
                    }
                }
                _ConsumerConnection = null;
            }
            catch (Exception ex)
            {
                _log.LogError("Error while disposing the connection", ex.StackTrace);
            }
        }
        public void DestroyProducerConnection()
        {
            try
            {
                _log.LogInformation("DestroyProducerConnection - called");

                if (IsTransportInterrupted)
                {
                    //  consumerTransport.Stop();
                    _log.LogInformation("stopped transport and no further close is needed");
                }
                else
                {
                    if (_ProducerConnection != null)
                    {
                        _log.LogInformation("_ProducerConnection != null");
                        _ProducerConnection.Stop();
                        _ProducerConnection.Close();
                        _ProducerConnection.Dispose();
                        _MessagePublisher = null;
                        _ProducerConnection = null;
                        _log.LogInformation("DestroyProducerConnection - connection disposed");
                    }
                }



            }
            catch (Exception ex)
            {
                _log.LogError("Error while disposing the connection", ex.StackTrace);
            }
        }

        public ITextMessage GetTextMessageRequest(string message)
        {
            ITextMessage request = _ProducerSession.CreateTextMessage(message);
            request.NMSCorrelationID = Guid.NewGuid().ToString();
            // request.Properties["NMSXGroupID"] = "cheese";
            // request.Properties["myHeader"] = "Cheddar";
            return request;
        }

        protected void OnConnectionInterruptedListener() {
            _log.LogInformation("Connection Interrupted.");
            IsTransportInterrupted = true;
            DestroyConsumerConnection();
        }
        // protected void OnConnectionResumedListener()
        // {
        //     IsTransportInterrupted = false;
        //     _log.LogInformation("Connection Resumed.");
          
        // }
        protected void OnExceptionListener(Exception NMSException) {
            _log.LogInformation("On Exception Listener: {e}", NMSException.GetBaseException());
            //    CreateAsyncBrokerConnection();
            //DestroyConsumerConnection();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                if (_MessagePublisher != null)
                {
                    _MessagePublisher.Close();
                }
                if (_MessageConsumer != null)
                {
                    _MessageConsumer.Close();
                }

                DestroyConsumerConnection();
                DestroyProducerConnection();
                this.isDisposed = true;
            }
        }
        #endregion
    }
}