using Microsoft.Extensions.DependencyInjection;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using EisCore.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace EisCore.Infrastructure.Configuration
{
    public static class EisStartup
    {

        public static void ConfigureServices(IServiceCollection services)
        {
              services.AddSingleton<IConfigurationManager,ConfigurationManager>();
              services.AddSingleton<IDatabaseBootstrap, DatabaseBootstrap>();              
              services.AddSingleton<IEventProcessor,EventProcessor>();
              services.AddSingleton<IEventPublisher,EventPublisher>();
              services.AddSingleton<BrokerConfiguration>();
              services.AddSingleton<EventHandlerRegistry>();
              services.AddSingleton<IApplicationDbContext,ApplicationDbContext>();                                      
        }

      

       
    }
}
