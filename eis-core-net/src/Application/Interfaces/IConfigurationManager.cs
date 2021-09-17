
using EisCore.Domain.Entities;
namespace EisCore.Application.Interfaces
{
    public interface IConfigurationManager
    {
         
        string GetBrokerUrl();
        ApplicationSettings GetAppSettings();
        BrokerConfiguration GetBrokerConfiguration();
        void Dispose();   
       

    }
}