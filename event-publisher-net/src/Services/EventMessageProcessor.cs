
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace event_publisher_net.processor
{
    public class EventMessageProcessor : IMessageProcessor
    {     

        private readonly ILogger<EventMessageProcessor> _logger;
        public EventMessageProcessor(ILogger<EventMessageProcessor> logger) {
            _logger = logger;
        }
        public void Process(Payload payload, string eventType) {
           _logger.LogInformation("inside consumer's process method");
        }
    }
}