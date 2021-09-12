

using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;

namespace event_publisher_net
{  
    public class MessageProducerImpl : IMessageEISProducer
    {    

        private Payload _payload;

        public MessageProducerImpl(Payload payload)
        {
            this._payload=payload;
        }
         public Payload getPayLoad(){
             return this._payload;
         }

         public string getEventType(){
             return "SORTIE_CREATED";
         }
         public string getTraceId(){
             return "123";
         }
    }
}