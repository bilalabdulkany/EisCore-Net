using System;
using Apache.NMS;
using System.Threading.Tasks;
using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{
    public interface IBrokerConnectionFactory : IDisposable
    {      
        IMessageProducer CreatePublisher();       
        void CreateConsumerListener();
        void DestroyConsumerConnection();
        void QueueToPublisherTopic(EisEvent eisEvent);


    }
}