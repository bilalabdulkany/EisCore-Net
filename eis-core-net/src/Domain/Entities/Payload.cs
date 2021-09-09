using System;

namespace EisCore.Domain.Entities
{   
   
    public class Payload    {   
       
        public object Content {get;set;} 

        public Payload(){
        }

        public Payload(object content)
        {
            this.Content = content;
        }
    }
}