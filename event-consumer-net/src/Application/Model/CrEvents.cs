using EisCore.Application.Models;
using EisCore.Domain.Entities;

namespace event_consumer_net.Application.Model
{
    public class CrEvents
    {
        public string Id { get; set; }
        public string CId { get; set; }
        public string EventCode { get; set; }
        public Event Event { get; set; }
        public string Value { get; set; }
        public RecordMethod RecordMethod{ get; set; }
    }
}