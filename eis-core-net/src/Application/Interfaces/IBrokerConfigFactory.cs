using System;
using Apache.NMS;
namespace EisCore.Application.Interfaces
{
    public interface IBrokerConfigFactory:IDisposable
    {
        void CreateBrokerConnection();
        IConnection _ConsumerConnection{get;set;}
        IConnection _ProducerConnection{get;set;}
        IMessageConsumer CreateConsumer();
        IMessageProducer CreateProducer();
        ITextMessage GetTextMessageRequest(string message);

        


    }
}