using System.Collections.Generic;
using System.Threading.Tasks;
using EisCore.Application.Constants;
using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{
      public interface IEventInboxOutboxDbContext
    {
        Task<IEnumerable<EisEventInboxOutbox>> GetAllUnprocessedEvents();
        Task<int> TryEventInsert(EisEvent eisEvent, string topicQueueName, string direction);
        Task<int> UpdateEventStatus(string eventId, string eventStatus);
    }

}