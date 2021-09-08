  using System;
  using System.IO;

namespace EisCore.Model
{
  
    
        public class EisEvent 
    {
       
        public string eventID{get;set;}
       
        public string eventType{get;set;}
      
        public DateTime createdDate {get;set;}
     
        public string sourceSystemName{get;set;}
       
        public string traceId {get;set;}
        
        public string spanId {get;set;}
        
        public Payload payload {get;set;}
        
        //TODO - Method Chaining
    }
}