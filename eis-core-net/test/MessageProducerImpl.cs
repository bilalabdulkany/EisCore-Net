

using EisCore.Model;

namespace EisCore
{  
    public class MessageProducerImpl : IMessageEISProducer
    {    

         public Payload getPayLoad(){
             return new Payload("Content");
         }

         public string getEventType(){
             return "Model";
         }
         public string getTraceId(){
             return "123";
         }
    }
}