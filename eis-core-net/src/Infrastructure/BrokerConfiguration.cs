
using System;
using Apache.NMS;
namespace EisCore.Model
{
    public class BrokerConfiguration : System.IDisposable
    {
        public string Protocol {get; set; }
        public string Url {get; set; }
        public string Username {get; set; }
        public string Password {get; set; }        
        public IConnection connection{get;set;}
        public ISession session{get;set;}
       // public ReceiverLink receiver{get;set;}

        public void Dispose()
        {
            Console.WriteLine("Disposing BrokerConfig session,con objects");
            session.Close();
            connection.Close();
        }
    }
}