using EisCore.Application.Interfaces;

namespace EisCore.Domain.Entities
{
    public class EventHandlerRegistry
    {
        private IMessageProcessor _messageProcessor;


        public void AddMessageProcessor(IMessageProcessor messageProcessor) {
            this._messageProcessor = messageProcessor;
        }

        public IMessageProcessor GetMessageProcessor() {
            return this._messageProcessor;
        }
    }
}