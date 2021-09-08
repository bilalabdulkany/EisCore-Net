using System;

namespace EisCore.Model
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