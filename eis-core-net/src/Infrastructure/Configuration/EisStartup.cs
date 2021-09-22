using Microsoft.Extensions.DependencyInjection;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using EisCore.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Quartz;
using EisCore.Infrastructure.Services;
using Quartz.Impl;
using Quartz.Spi;

namespace EisCore.Infrastructure.Configuration
{
    public static class EisStartup
    {

        public static void ConfigureServices(IServiceCollection services, IConfiguration Configuration)
        {
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
            services.AddSingleton<IBrokerConnectionFactory, BrokerConnectionFactory>();
            services.AddSingleton<IDatabaseBootstrap, DatabaseBootstrap>();              
            services.AddSingleton<IEventProcessor, EventProcessor>();
            services.AddSingleton<IEventPublisher, EventPublisher>();
            services.AddSingleton<BrokerConfiguration>();
            services.AddSingleton<EventHandlerRegistry>();
            services.AddSingleton<ICompetingConsumerDbContext,CompetingConsumerDbContext>();


            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddHostedService<QuartzHostedService>();
            // base configuration from appsettings.json
            //services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
            // if you are using persistent job store, you might want to alter some options
            services.Configure<QuartzOptions>(options =>
            {
                options.SchedulerName = "Quartz ASP.NET Core Test Scheduler";
                options.Scheduling.IgnoreDuplicates = true; // default: false
                options.Scheduling.OverWriteExistingData = true; // default: true
            });
            
            // Add the required Quartz.NET services
            services.AddSingleton<QuartzKeepAliveEntryJob>();            
            services.AddSingleton<JobSchedule>();
                //new JobSchedule( jobType: typeof(QuartzKeepAliveEntryJob),cronExpression:"0/20 * * * * ?")
            //);// run every {n} seconds
         
        }
    }
}
