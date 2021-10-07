using System;

namespace event_consumer_net.Application.Model
{
    public class StaleEventCheck
    {
        
        public StaleEventCheck()
        {
            
        }

        public StaleEventCheck(string Id, int mId, string eventType, string EventTimestamp)
        {
            this.Id = Id;
            this.mId = mId;
            this.EventType = eventType;            
            this.EventTimestamp=EventTimestamp;
        }

        public string Id {get;set;}

        public int mId{get;set;}

        public string EventType {get;set;}
        public string EventTimestamp {get;set;}
    }
}