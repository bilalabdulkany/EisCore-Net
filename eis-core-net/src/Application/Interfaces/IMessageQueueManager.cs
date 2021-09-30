using System.Threading.Tasks;
using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{
    public interface IMessageQueueManager
    {
        void ConsumeEvent(EisEvent eisEvent, string queueName);
        Task InboxOutboxPollerTask();
        void QueueToPublisherTopic(EisEvent eisEvent, bool isCurrent);
        Task KeepAliveTask();
    }
}