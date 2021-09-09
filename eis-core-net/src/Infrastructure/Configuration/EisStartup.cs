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
              services.AddSingleton<IEventProcessor,EventProcessor>();
              services.AddSingleton<IEventPublisher,EventPublisher>();
              services.AddSingleton<BrokerConfiguration>();
              services.AddSingleton<EventHandlerRegistry>();
        }
    }
}
