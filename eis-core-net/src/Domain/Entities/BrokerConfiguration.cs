
using System;
using Apache.NMS;
namespace EisCore.Domain.Entities
{
    public class BrokerConfiguration
    {
        public string Protocol {get; set; }
        public string Url {get; set; }
        public string Username {get; set; }
        public string Password {get; set; }        
      
       }
}