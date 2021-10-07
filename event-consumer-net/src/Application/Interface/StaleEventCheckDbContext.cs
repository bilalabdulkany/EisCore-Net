using event_consumer_net.Application.Model;

namespace event_consumer_net.Application.Interface
{
     public interface IStaleEventCheckDbContext
    {
        StaleEventCheck FindMIdAndEventType(int mId, string eventType);
        int Save(StaleEventCheck staleEventCheck);
    }
}