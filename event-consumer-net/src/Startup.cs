using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using EisCore.Infrastructure.Configuration;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using event_consumer_net.Infrastructure.Services;
using event_consumer_net.Application.Interface;
using event_consumer_net.Infrastructure.Persistence;

namespace event_consumer_net
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "event_consumer_net", Version = "v1" });
            });           
            services.AddScoped<IMessageProcessor, EventMessageProcessor>();
            services.AddSingleton<IIdempotentEventCheckDbContext,IdempotentEventCheckDbContext>();
            services.AddSingleton<IStaleEventCheckDbContext,StaleEventCheckDbContext>();
            services.AddEisServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "event_consumer_net v1"));                
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.ApplicationServices.GetRequiredService<IMessageQueueManager>();
            app.ApplicationServices.GetRequiredService<IDatabaseBootstrap>();
            EventMessageProcessor eventProcessor = null;
            // get scoped factory
            var scopedFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            // create a scope
            using (var scope = scopedFactory.CreateScope())
            {
                // then resolve the services and execute it
                eventProcessor = (EventMessageProcessor)scope.ServiceProvider.GetRequiredService<IMessageProcessor>();
            }
            EventHandlerRegistry eventHandlerRegistry = app.ApplicationServices.GetService<EventHandlerRegistry>();
            //eventProcessor = (EventMessageProcessor)app.ApplicationServices.GetService<IMessageProcessor>();
            eventHandlerRegistry.AddMessageProcessor(eventProcessor);
        }
    }
}
