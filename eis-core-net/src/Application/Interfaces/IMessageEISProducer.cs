using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{   
   
    public interface IMessageEISProducer
    { 
      
        Payload getPayLoad();       
         string getEventType();
         
         string getTraceId();
    }

/*    static class GetDefaultTraceID
{
    public static string FormattedNameDefault()
    {
        return Guid.NewGuid().ToString();
    }
}*/

}