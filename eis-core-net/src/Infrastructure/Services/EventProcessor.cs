
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
using EisCore.Application.Constants;

namespace EisCore
{
    public class EventProcessor : IEventProcessor
    {
        private bool isDisposed = false;
        private readonly ILogger<EventProcessor> _log;
        private readonly IConfigurationManager _configManager;
        private IEventInboxOutboxDbContext _eventINOUTDbContext;
        protected static ITextMessage queueMessage = null;

        private EventHandlerRegistry _eventHandlerRegistry;
        //private IMessageConsumer _consumer;

        public EventProcessor(ILogger<EventProcessor> log, IConfigurationManager configurationManager,
           EventHandlerRegistry eventHandlerRegistry, IEventInboxOutboxDbContext eventINOUTDbContext)
        {
            this._log = log;
            this._eventHandlerRegistry = eventHandlerRegistry;
            this._eventINOUTDbContext = eventINOUTDbContext;
            this._configManager = configurationManager;
        }

        public void RunConsumerEventListener(IMessageConsumer consumer)
        {
            _log.LogInformation("called RunConsumerEventListener()");
            consumer.Listener += new MessageListener(OnMessage);
            //_log.LogInformation("Logging data",_appDbContext.Get().Result.GetEnumerator().Current);

        }

        protected void OnMessage(IMessage receivedMsg)
        {
            EisEvent eisEvent = null;
            var InboundQueue = _configManager.GetAppSettings().InboundQueue;
           //string INOUT = null;
            try
            {
                _log.LogInformation("Receiving the message inside OnMessage");
                queueMessage = receivedMsg as ITextMessage;

                _log.LogInformation("Received message with ID: {n}  ", queueMessage.NMSMessageId);
                _log.LogInformation("Received message with text: {n}  ", queueMessage.Text);

                eisEvent = JsonSerializer.Deserialize<EisEvent>(queueMessage.Text);
                //TODO check json deserializer exception handling in IN OUT BOX
                _log.LogInformation("Receiving the message: {eisEvent}", eisEvent.ToString());
                int recordInsertCount = _eventINOUTDbContext.TryEventInsert(eisEvent, InboundQueue, AtLeastOnceDeliveryDirection.IN).Result;
                _log.LogInformation("INBOX insert status: {a}", recordInsertCount);
               // INOUT = recordInsertCount == 1 ? AtLeastOnceDeliveryDirection.IN : AtLeastOnceDeliveryDirection.OUT;
                //var recordUpdateStatus = 0;
               

                //if (!INOUT.Equals(AtLeastOnceDeliveryDirection.OUT))
                {
              //      _eventHandlerRegistry.GetMessageProcessor().Process(eisEvent.Payload, eisEvent.EventType);
                   
                }
                //_log.LogInformation("IN-OUT BOX update status: {a}", recordUpdateStatus);


                receivedMsg.Acknowledge();
            }
            catch (Exception ex)
            {
                receivedMsg.Acknowledge();
                _eventINOUTDbContext.UpdateEventStatus(eisEvent.EventID, TestSystemVariables.FAILED);
                _log.LogError("exception in onMessage: {eisEvent}", ex.StackTrace);
                throw ex;
            }
        }
    }

}