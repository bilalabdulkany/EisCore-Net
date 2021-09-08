using EisCore.Model;
namespace EisCore.Configuration
{
    public interface IConfigurationManager
    {
         
        string GetBrokerUrl();
        ApplicationSettings GetAppSettings();
        BrokerConfiguration GetBrokerConfiguration();
    //      void Dispose();   
        void CreateAsyncBrokerConnection();


    }
}