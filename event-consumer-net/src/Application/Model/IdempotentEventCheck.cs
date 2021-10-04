using System;


namespace event_consumer_net.Application.Model
{
    public class IdempotentEventCheck
    {
        public string Id { get; set; }
        public int MId { get; set; }
        public string CId { get; set; }
        public string EventCode { get; set; }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || this.GetType() != obj.GetType()) return false;

            return obj is IdempotentEventCheck check &&
                   Id == check.Id &&
                   MId == check.MId &&
                   CId == check.CId &&
                   EventCode == check.EventCode;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MId, CId, EventCode);
        }
    }
}