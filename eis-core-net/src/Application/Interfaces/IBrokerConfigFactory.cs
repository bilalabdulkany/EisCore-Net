using System;
using Apache.NMS;
namespace EisCore.Application.Interfaces
{
    public interface IBrokerConfigFactory:IDisposable
    {
        void CreateBrokerConnection();
        IMessageConsumer CreateConsumer();
        IMessageProducer CreateProducer();
        ITextMessage GetTextMessageRequest(string message);


    }
}