using System.Collections.Generic;
using event_consumer_net.Application.Model;

namespace event_consumer_net.Application.Interface
{
    public interface IIdempotentEventCheckDbContext
    {
        List<IdempotentEventCheck> FindMIdAndEventType(int mId);
        int Save(IdempotentEventCheck idempotentEventCheck);
    }
}