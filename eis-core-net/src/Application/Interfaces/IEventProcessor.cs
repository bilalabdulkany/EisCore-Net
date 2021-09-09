using System;

namespace EisCore.Application.Interfaces
{
    public interface IEventProcessor: IDisposable
    {
         void RunConsumerEventListener();        
    }
}