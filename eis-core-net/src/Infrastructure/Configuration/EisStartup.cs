using Microsoft.Extensions.DependencyInjection;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;

namespace EisCore.Infrastructure.Configuration
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
