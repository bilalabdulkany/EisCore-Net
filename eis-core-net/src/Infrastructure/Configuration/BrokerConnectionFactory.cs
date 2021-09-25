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
        private FailoverTransportFactory _failoverTransportFactory;
        private static bool IsTransportInterrupted = false;
        CustomConnectionFactory _consumerConnFactory = null;

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

            // _consumerConnFactory = new CustomConnectionFactory();
            _connecturi = new Uri(brokerUrl);
            IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(_connecturi);
            _failoverTransportFactory = new FailoverTransportFactory();

            _factory = factory;
            _ConsumerConnection = null;
            CreateProducerConnection();

        }
        private void CreateProducerConnection()
        {
            IConnection ProducerTcpConnection = null;
            ISession ProducerTcpSession = null;
            try
            {
                ProducerTcpConnection = _factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                //ProducerTcpConnection.ClientId = "Producer_Connection";

                if (ProducerTcpConnection.IsStarted)
                {
                    _log.LogInformation("producer and consumer connection started");
                }

                /* TcpConnection.ConnectionInterruptedListener += new ConnectionInterruptedListener(OnConnectionInterruptedListener);
                 TcpConnection.ConnectionResumedListener += new ConnectionResumedListener(OnConnectionResumedListener);
                 TcpConnection.ExceptionListener += new ExceptionListener(OnExceptionListener);
                 */
                ProducerTcpSession = ProducerTcpConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                _ProducerConnection = ProducerTcpConnection;
                _ProducerSession = ProducerTcpSession;

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
                if (_ConsumerConnection != null && _ConsumerConnection.IsStarted)
                {
                    return;
                }
                if (_ConsumerConnection != null)
                {
                    _log.LogInformation("_ConsumerConnection.IsStarted: " + _ConsumerConnection.IsStarted);
                    DestroyConsumerConnection();
                    _log.LogInformation("_ConsumerConnection Stopped");
                }
                _log.LogInformation("Creating new consumer broker connection");
                var brokerUrl = _configManager.GetBrokerUrl();
                _log.LogInformation("Broker - {brokerUrl}", brokerUrl);

                CustomConnectionFactory _consumerConnFactory = new CustomConnectionFactory();
                Uri connecturi = new Uri(brokerUrl);
                _ConsumerConnection = _consumerConnFactory.CreateActiveMQConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password, connecturi);
                _ConsumerConnection.ClientId = "Consumer_Connection";
                _log.LogInformation("connection created, client id set");

                _ConsumerConnection.ConnectionInterruptedListener += new ConnectionInterruptedListener(OnConnectionInterruptedListener);
                _ConsumerConnection.ConnectionResumedListener += new ConnectionResumedListener(OnConnectionResumedListener);
                _ConsumerConnection.ExceptionListener += new ExceptionListener(OnExceptionListener);



                var ConsumerTcpSession = _ConsumerConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);

                _ConsumerConnection.Start();
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
                  if (_MessagePublisher!=null)
                {
                    return _MessagePublisher;
                }
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
                _log.LogInformation("DestroyConsumerConnection - called: ");
                //   ITransport consumerTransport = _consumerConnFactory.getConsumerTransport();

                if (IsTransportInterrupted)
                {
                    //  consumerTransport.Stop();
                    _log.LogInformation("stopped transport and no further close is needed");
                }
                else
                {
                    if (_ConsumerConnection != null)
                    {
                        _log.LogInformation("_ConsumerConnection != null");
                        _ConsumerConnection.Stop();
                        _ConsumerConnection.Close();
                        _ConsumerConnection.Dispose();
                        _ConsumerConnection = null;
                        _log.LogInformation("DestroyConsumerConnection - connection disposed");
                    }
                }

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
                        _MessagePublisher=null;
                        _ProducerConnection=null;
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

        protected void OnConnectionInterruptedListener()
        {
            _log.LogInformation("Connection Interrupted.");
            IsTransportInterrupted = true;
            DestroyConsumerConnection();
        }
        protected void OnConnectionResumedListener()
        {
            IsTransportInterrupted = false;
            _log.LogInformation("Connection Resumed.");
        }
        protected void OnExceptionListener(Exception NMSException)
        {
            _log.LogInformation("On Exception Listener: {e}", NMSException.GetBaseException());
            //    CreateAsyncBrokerConnection();
            DestroyConsumerConnection();
            _ConsumerConnection = null;
        }


        private bool CheckFailoverTransport()
        {
            return true;
            /*         FailoverTransportFactory failoverTransportFactory = new FailoverTransportFactory();
                     using (ITransport transport = failoverTransportFactory.CreateTransport(_connecturi))
                     {
                         _failoverTransport = transport.Narrow(typeof(FailoverTransport)) as FailoverTransport;
                         if (_failoverTransport != null)
                         {


                             _log.LogInformation("_transport.IsConnected: " + _failoverTransport.IsConnected);
                             _log.LogInformation("_transport.IsStarted: " + _failoverTransport.IsStarted);
                             _log.LogInformation("_transport.IsStarted: " + transport.IsConnected);

                             return _failoverTransport.IsStarted;
                         }
                     }
                     return false;
                     */
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

        class CustomConnectionFactory : ConnectionFactory
        {
            ITransport _consumerTransport;

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return base.ToString();
            }

            protected override void ConfigureConnection(Connection connection)
            {
                base.ConfigureConnection(connection);
            }

            protected override Connection CreateActiveMQConnection()
            {
                return base.CreateActiveMQConnection();
            }

            protected override Connection CreateActiveMQConnection(string userName, string password)
            {
                return base.CreateActiveMQConnection(userName, password);
            }

            protected override Connection CreateActiveMQConnection(ITransport transport)
            {
                return base.CreateActiveMQConnection(transport);
            }

            public Connection CreateActiveMQConnection(string userName, string password, Uri connecturi)
            {
                Connection connection = null;
                try
                {
                    ITransport transport = TransportFactory.CreateTransport(connecturi);
                    _consumerTransport = transport;
                    connection = CreateActiveMQConnection(transport);

                    ConfigureConnection(connection);

                    connection.UserName = userName;
                    connection.Password = password;

                    if (base.ClientId != null)
                    {
                        connection.DefaultClientId = base.ClientId;
                    }

                    return connection;
                }
                catch (NMSException)
                {
                    try
                    {
                        connection.Close();
                    }
                    catch
                    {
                    }

                    throw;
                }
                catch (Exception e)
                {
                    try
                    {
                        connection.Close();
                    }
                    catch
                    {
                    }

                    throw NMSExceptionSupport.Create("Could not connect to broker URL: " + "Reason: " + e.Message, e);
                }
            }
            public ITransport getConsumerTransport()
            {
                return _consumerTransport;
            }
        }



    }
}