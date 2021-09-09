using EisCore.Domain.Entities;
namespace EisCore.Application.Interfaces
{
    public interface IMessageProcessor
    {
        void Process(Payload payload, string eventType);
    }
}