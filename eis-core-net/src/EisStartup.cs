using Microsoft.Extensions.DependencyInjection;
using EisCore.Configuration;
using EisCore.Model;

namespace EisCore
{
    public static class EisStartup
    {

        public static void ConfigureServices(IServiceCollection services)
        {
              services.AddSingleton<IConfigurationManager,ConfigurationManager>();
              services.AddSingleton<EventProcessor>();
              services.AddSingleton<EventPublisher>();
              services.AddSingleton<BrokerConfiguration>();
        }
    }
}
