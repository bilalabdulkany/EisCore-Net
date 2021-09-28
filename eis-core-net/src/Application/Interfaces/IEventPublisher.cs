using System;

namespace EisCore.Application.Interfaces
{
    public interface IEventPublisher : IDisposable
    {
        void publish(IMessageEISProducer messageObject);

    }
}