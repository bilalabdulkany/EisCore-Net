using Microsoft.Extensions.DependencyInjection;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using EisCore.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Quartz;
using EisCore.src.Infrastructure.Services;

namespace EisCore.Infrastructure.Configuration
{
    public static class EisStartup
    {

        public static void ConfigureServices(IServiceCollection services, IConfiguration Configuration)
        {
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
            //services.AddSingleton<IDatabaseBootstrap, DatabaseBootstrap>();              
            services.AddSingleton<IEventProcessor, EventProcessor>();
            services.AddSingleton<IEventPublisher, EventPublisher>();
            services.AddSingleton<BrokerConfiguration>();
            services.AddSingleton<EventHandlerRegistry>();
            //services.AddSingleton<IApplicationDbContext,ApplicationDbContext>();




            // base configuration from appsettings.json
            services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));

            // if you are using persistent job store, you might want to alter some options
            services.Configure<QuartzOptions>(options =>
            {
                options.SchedulerName = "Quartz ASP.NET Core Sample Scheduler";
                options.Scheduling.IgnoreDuplicates = true; // default: false
                options.Scheduling.OverWriteExistingData = true; // default: true
            });
            // Add the required Quartz.NET services
            services.AddQuartz(q =>
            {
                // Use a Scoped container to create jobs. I'll touch on this later
                q.UseMicrosoftDependencyInjectionScopedJobFactory();
                var jobKey = new JobKey("HelloWorldJob");

                // Register the job with the DI container
                q.AddJob<QuartzJob>(opts => opts.WithIdentity(jobKey));

                // Create a trigger for the job
                q.AddTrigger(opts => opts
                    .ForJob(jobKey) // link to the HelloWorldJob
                    .WithIdentity("HelloWorldJob-trigger") // give the trigger a unique name
                    .WithCronSchedule("0/5 * * * * ?")); // run every 5 seconds                    
            });
            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }
    }
}
