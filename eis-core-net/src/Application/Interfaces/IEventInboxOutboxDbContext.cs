using System.Collections.Generic;
using System.Threading.Tasks;
using EisCore.Application.Constants;
using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{
      public interface IEventInboxOutboxDbContext
    {
        Task<List<EisEventInboxOutbox>> GetAllUnprocessedEvents(string direction);
        Task<int> TryEventInsert(EisEvent eisEvent, string topicQueueName, string direction);
        Task<int> UpdateEventStatus(string eventId, string eventStatus);
    }

}