using System;
using Apache.NMS;
namespace EisCore.Application.Interfaces
{
    public interface IBrokerConnectionFactory : IDisposable
    {
        //IConnection _ConsumerConnection { get; set; }
        //IConnection _ProducerConnection { get; set; }
        IMessageProducer CreateProducer();
        ITextMessage GetTextMessageRequest(string message);
        void CreateConsumer();
        void DestroyConsumerConnection();


    }
}