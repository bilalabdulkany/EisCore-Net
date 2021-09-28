  using System;
  using System.IO;

namespace EisCore.Domain.Entities
{
  
    
        public class EisEvent 
    {
       
        public string EventID{get;set;}
       
        public string EventType{get;set;}
      
        public DateTime CreatedDate {get;set;}
     
        public string SourceSystemName{get;set;}
       
        public string TraceId {get;set;}
        
        public string SpanId {get;set;}
        
        public Payload Payload {get;set;}
        
        //TODO - Method Chaining
    }
}