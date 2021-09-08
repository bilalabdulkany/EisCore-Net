
namespace EisCore.Model
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