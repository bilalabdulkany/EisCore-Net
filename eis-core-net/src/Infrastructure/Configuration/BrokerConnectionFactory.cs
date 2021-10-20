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
using System.Threading.Tasks;
using EisCore.Application.Constants;
using System.Text.Json;
using EisCore.Infrastructure.Persistence;
using EisCore.Application.Util;

namespace EisCore.Infrastructure.Configuration
{
    public class BrokerConnectionFactory : IBrokerConnectionFactory
    {
        private bool isDisposed = false;
        private readonly ILogger<BrokerConnectionFactory> _log;
        private readonly BrokerConfiguration _brokerConfiguration;
        private readonly ApplicationSettings _appSettings;
        readonly IConfigurationManager _configManager;
        private ISession _PublisherSession;
        public IConnection _ConsumerConnection;
        public IConnection _PublisherConnection;
        private IConnectionFactory _factory;
        protected static TimeSpan receiveTimeout = TimeSpan.FromSeconds(10);
        private IMessageConsumer _MessageConsumer;
        private IMessageProducer _MessagePublisher;
        private readonly Uri _connecturi;
        //private static bool IsTransportInterrupted = true;
        ConnectionInterruptedListener interruptedListener = null;
        // ConnectionResumedListener connectionResumedListener = null;
        ExceptionListener connectionExceptionListener = null;

        private readonly IEventInboxOutboxDbContext _eventINOUTDbContext;
        private EventHandlerRegistry _eventRegistry;

        public BrokerConnectionFactory(ILogger<BrokerConnectionFactory> log,
        IConfigurationManager configurationManager, BrokerConfiguration brokerConfig,
        IEventInboxOutboxDbContext eventINOUTDbContext, EventHandlerRegistry eventHandlerRegistry)
        {
            this._log = log;
            this._configManager = configurationManager;
            this._appSettings = configurationManager.GetAppSettings();
            _brokerConfiguration = _configManager.GetBrokerConfiguration();
            this._eventRegistry = eventHandlerRegistry;
            var brokerUrl = _configManager.GetBrokerUrl();
            _log.LogInformation("BrokerConnectionFactory >> Initializing broker connections.");
            _log.LogInformation("Broker - {brokerUrl}", brokerUrl);
            _connecturi = new Uri(brokerUrl);
            IConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(_connecturi);

            _factory = factory;
            _eventINOUTDbContext = eventINOUTDbContext;
            _ConsumerConnection = null;

            interruptedListener = new ConnectionInterruptedListener(OnConnectionInterruptedListener);
            //connectionResumedListener= new ConnectionResumedListener(OnConnectionResumedListener);
            connectionExceptionListener = new ExceptionListener(OnExceptionListener);

            //CreateProducerConnection();

        }
        private void CreatePublisherConnection()
        {
            // IConnection _PublisherConnection = null;
            try
            {
                _PublisherConnection = _factory.CreateConnection(this._brokerConfiguration.Username, this._brokerConfiguration.Password);
                //ProducerTcpConnection.ClientId = "Producer_Connection";

                if (_PublisherConnection.IsStarted)
                {
                    _log.LogInformation("producer and consumer connection started");
                }

                _PublisherSession = _PublisherConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);

                _log.LogInformation("##Created Publisher connection {con}", this._PublisherConnection.ToString());
            }
            catch (Apache.NMS.ActiveMQ.ConnectionClosedException e1)
            {
                _log.LogCritical("Connection closed exception thrown while closing a connection. {e1}", e1.StackTrace);
                try
                {
                    _log.LogCritical("Stopping Connection..");
                    _PublisherConnection.Stop();
                }
                catch (Apache.NMS.ActiveMQ.ConnectionClosedException e2)
                {
                    _log.LogCritical("Cannot close the connection {e2}", e2.StackTrace);
                    _PublisherConnection.Close();
                }
                finally
                {
                    _PublisherConnection.Dispose();
                }
            }
            catch (Exception e)
            {
                _log.LogError("Exception occurred {class}: {e}", this.ToString(), e.StackTrace);
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
                CreatePublisherConnection();
                var topic = _configManager.GetAppSettings().OutboundTopic;
                var TopicDestination = SessionUtil.GetTopic(_PublisherSession, topic);
                _PublisherConnection.Start();
                if (_PublisherConnection.IsStarted)
                {
                    _log.LogInformation("connection started");
                }
                _MessagePublisher = _PublisherSession.CreateProducer(TopicDestination);
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
        private ITextMessage GetTextMessageRequest(string message)
        {
            ITextMessage request = _PublisherSession.CreateTextMessage(message);
            request.NMSCorrelationID = Guid.NewGuid().ToString();
            // request.Properties["NMSXGroupID"] = "cheese";
            // request.Properties["myHeader"] = "Cheddar";

            return request;
        }

        public void QueueToPublisherTopic(EisEvent eisEvent)
        {

            // var recordUpdateStatus = 0;
            try
            {
                string jsonString = JsonSerializer.Serialize(eisEvent);
                _MessagePublisher = CreatePublisher();
                ITextMessage request = GetTextMessageRequest(jsonString);
                /**
                When the target server receives the message it will check if that property is set, if it is, then it will check in its in memory cache if it has already received a message with that value of the header.
                 If it has received a message with the same value before then it will ignore the message.
                **/
                request.Properties["HDR_DUPLICATE_DETECTION_ID"] = eisEvent.EventID;//Duplicate message check on server's cache with Event ID. This is to prevent duplicate messages when broker is interrupted
                _log.LogInformation("{s}", jsonString);
                foreach (var item in request.Properties.Keys)
                {
                 _log.LogInformation("Properties:: {s}",item);    
                }
                _MessagePublisher.Send(request);
                //TODO add the processed status
                Console.WriteLine($"Thread={Thread.CurrentThread.ManagedThreadId} SendToQueue exiting");

                // recordUpdateStatus = _eventInboxOutboxDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED).Result;

                // _log.LogInformation("QueueToPublisherTopic: Event published to queue: {a}", eisEvent.EventID.ToString());
            }
            catch (Exception e)
            {
                _log.LogInformation("{s}", e.StackTrace);
            }

        }

        public void CreateConsumerListener()
        {
            try
            {               
                _log.LogInformation("CreateConsumer _ConsumerConnection: >> " + _ConsumerConnection + " <<");
                if (_ConsumerConnection != null)
                {
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
                _ConsumerConnection.ExceptionListener += connectionExceptionListener;
                 _log.LogInformation("Creating session"); 
                 ISession ConsumerTcpSession =null; 
                // https://stackoverflow.com/questions/4303075/activemq-starting-consumer-without-broker
                Task.Run(()=>{
                 ConsumerTcpSession = _ConsumerConnection.CreateSession(AcknowledgementMode.ClientAcknowledge);
                _log.LogInformation("Consumer Connection going to start:");                                
                _ConsumerConnection.Start();
                _log.LogInformation("start connection");
               
               
                if (_ConsumerConnection.IsStarted)
                {
                    _log.LogInformation("consumer connection started");
                     GlobalVariables.IsTransportInterrupted = false;
                }
                else
                {
                    _log.LogInformation("consumer connection not started, starting");
                }
                 
                 if(ConsumerTcpSession!=null){

                var Queue = this._appSettings.InboundQueue;
                _log.LogInformation("QUEUE:" + _appSettings);
                var QueueDestination = SessionUtil.GetQueue(ConsumerTcpSession, Queue);
                _log.LogInformation("Created MessageProducer for Destination Queue: {d}", QueueDestination);
                _MessageConsumer = ConsumerTcpSession.CreateConsumer(QueueDestination);                
                 GlobalVariables.IsTransportInterrupted = false;
                _MessageConsumer.Listener += new MessageListener(OnMessage);
                 } 
               
                 else {
                     _log.LogInformation("Consumer connection not started");
                 }
                   });
                
            }
            catch (Exception e)
            {
                _log.LogCritical("Error occurred when creating Consumer: {e}", e.StackTrace);
                DestroyConsumerConnection();
                throw e;
            }
        }


        protected void OnMessage(IMessage receivedMsg)
        {
            EisEvent eisEvent = null;
            var InboundQueue = _configManager.GetAppSettings().InboundQueue;
            try
            {
                _log.LogInformation("Receiving the message inside OnMessage");
                var queueMessage = receivedMsg as ITextMessage;

                _log.LogInformation("Received message with ID: {n}  ", queueMessage.NMSMessageId);
                _log.LogInformation("Received message with text: {n}  ", queueMessage.Text);

                eisEvent = JsonSerializer.Deserialize<EisEvent>(queueMessage.Text);
                //TODO check json deserializer exception handling in IN OUT BOX
                _log.LogInformation("Receiving the message: {eisEvent}", eisEvent.ToString());
                int recordInsertCount = _eventINOUTDbContext.TryEventInsert(eisEvent, InboundQueue, AtLeastOnceDeliveryDirection.IN).Result;
                ///If the record is new, and status is 1, then process the data
                /// 
                /// 
                if (recordInsertCount == 1)
                {
                    _log.LogInformation("INBOX::NEW [Insert] status: {a}", recordInsertCount);
                    UtilityClass.ConsumeEvent(eisEvent, InboundQueue, _eventRegistry, _configManager.GetAppSettings().Name, _log);
                    var updatedStatus = _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.PROCESSED,AtLeastOnceDeliveryDirection.IN);
                    _log.LogInformation("INBOX::NEW [Processed] status: {b}", updatedStatus);
                }
                else
                {
                    _log.LogInformation("INBOX::OLD record already exists. insert status: {a}", recordInsertCount);
                }


                receivedMsg.Acknowledge();
            }
            catch (Exception ex)
            {
                receivedMsg.Acknowledge();
                _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.FAILED,AtLeastOnceDeliveryDirection.IN);
                _log.LogError("exception in onMessage: {eisEvent}", ex.Message);
                throw ex;
            }
        }


        protected void OnConnectionInterruptedListener()
        {
            _log.LogInformation("Connection Interrupted.");
            GlobalVariables.IsTransportInterrupted = true;
            DestroyConsumerConnection();
            DestroyProducerConnection();
        }
        // protected void OnConnectionResumedListener()
        // {
        //     IsTransportInterrupted = false;
        //     _log.LogInformation("Connection Resumed.");

        // }
        protected void OnExceptionListener(Exception NMSException)
        {
            _log.LogInformation("On Exception Listener: {e}", NMSException.GetBaseException());
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

        public void DestroyConsumerConnection()
        {
            try
            {
                _log.LogInformation("DestroyConsumerConnection - called, IsTransportInterrupted: " + GlobalVariables.IsTransportInterrupted);
                //   ITransport consumerTransport = _consumerConnFactory.getConsumerTransport();

                if (GlobalVariables.IsTransportInterrupted)
                {
                    _log.LogInformation("DestroyConsumerConnection Transport already stopped");
                }
                else
                {
                    if (_ConsumerConnection != null)
                    {
                        _log.LogInformation("_ConsumerConnection != null");
                        _ConsumerConnection.ConnectionInterruptedListener -= interruptedListener;
                        //_ConsumerConnection.ConnectionResumedListener -= connectionResumedListener;
                        _ConsumerConnection.ExceptionListener -= connectionExceptionListener;
                        _ConsumerConnection.Stop();
                        _ConsumerConnection.Close();
                        _ConsumerConnection.Dispose();
                        _log.LogInformation("_ConsumerConnection started: " + _ConsumerConnection.IsStarted);
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

                if (GlobalVariables.IsTransportInterrupted)
                {
                    //  consumerTransport.Stop();
                    _log.LogInformation("DestroyProducerConnection stopped transport");
                }
                else
                {
                    _log.LogInformation("DestroyProducerConnection inside else");
                    if (_PublisherConnection != null)
                    {
                        _log.LogInformation("_ProducerConnection != null");
                        _PublisherConnection.Stop();
                        _PublisherConnection.Close();
                        _PublisherConnection.Dispose();
                        _MessagePublisher = null;
                        _log.LogInformation("DestroyProducerConnection - connection disposed");
                    }
                }
                _PublisherConnection = null;
            }
            catch (Exception ex)
            {
                _log.LogError("Error while disposing the connection", ex.StackTrace);
            }
        }

        #endregion
    }
}