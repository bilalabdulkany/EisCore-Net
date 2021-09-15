using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EisCore;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using EisCore.Infrastructure.Configuration;
using event_publisher_net.processor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace event_publisher_net
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "event_publisher_net", Version = "v1" });
            });            

            EisStartup.ConfigureServices(services);
            services.AddSingleton<EventMessageProcessor>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "event_publisher_net v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.ApplicationServices.GetService<IConfigurationManager>();
            app.ApplicationServices.GetService<IEventProcessor>();  
            app.ApplicationServices.GetService<IDatabaseBootstrap>().Setup();        


            //TODO add in the documentation
            EventHandlerRegistry eventHandlerRegistry = app.ApplicationServices.GetService<EventHandlerRegistry>();
            EventMessageProcessor eventProcessor = app.ApplicationServices.GetService<EventMessageProcessor>();
            eventHandlerRegistry.AddMessageProcessor(eventProcessor);

        }
    }
}
