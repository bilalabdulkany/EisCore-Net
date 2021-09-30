
using System;
using EisCore.Domain.Entities;
using EisCore.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Apache.NMS;

using EisCore.Application.Constants;

namespace EisCore
{
    public class EventConsumerService : IEventConsumerService
    {
        private readonly ILogger<EventConsumerService> _log;
        private readonly IConfigurationManager _configManager;
        private IEventInboxOutboxDbContext _eventINOUTDbContext;

        public EventConsumerService(ILogger<EventConsumerService> log, IConfigurationManager configurationManager,
           IEventInboxOutboxDbContext eventINOUTDbContext)
        {
            this._log = log;
            this._eventINOUTDbContext = eventINOUTDbContext;
            this._configManager = configurationManager;
        }

        public void RunConsumerEventListener(IMessageConsumer consumer)
        {
            _log.LogInformation("called RunConsumerEventListener()");
            //consumer.Listener += new MessageListener(OnMessage);

        }

    
    }
}