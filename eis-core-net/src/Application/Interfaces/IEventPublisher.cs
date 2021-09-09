using System;

namespace EisCore.Application.Interfaces
{
    public interface IEventPublisher: IDisposable
    {
         void publish(string messagePublish);
         void publish(IMessageEISProducer messageObject);
         
    }
}