using System;
using Apache.NMS;

namespace EisCore.Application.Interfaces
{
    public interface IEventConsumerService
    {
         void RunConsumerEventListener(IMessageConsumer consumer);        
    }
}