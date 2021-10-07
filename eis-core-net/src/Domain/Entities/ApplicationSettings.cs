namespace EisCore.Domain.Entities
{
    public class ApplicationSettings
    {        
            public string Name{get;set;}
            public string OutboundTopic{get;set;}
            public string InboundQueue {get;set;}
     

        public override string ToString()
        {
            return Name+" outbound: "+OutboundTopic+" inbound: "+InboundQueue;
        }
    }
}