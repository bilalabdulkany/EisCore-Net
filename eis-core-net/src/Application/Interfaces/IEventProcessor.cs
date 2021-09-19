using System;
using Apache.NMS;

namespace EisCore.Application.Interfaces
{
    public interface IEventProcessor
    {
         void RunConsumerEventListener(IMessageConsumer consumer);        
    }
}