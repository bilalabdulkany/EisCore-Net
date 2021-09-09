#Introduction

EIS Core library is developed to hide the complexities of asynchronous communication involved in microservices architecture
1.	Publisher component
2.	Consumer component
3.	Tracing and Auditing
4.	Active/Passive JMS Consumer implementation (Competing consumer problem solution)

#Getting Started

Given below are the list of steps to be followed to integrate EIS Core in Java applications

1.	Add the following library, versions will be communicated separately:
Eis-core-net.dll
2.	Open your Startup.cs class and the below line of code in the ConfigureServices method:

public void ConfigureServices(IServiceCollection services)
    {          
        //Add the below code
            EisStartup.ConfigureServices(services);
    }
3.	In the same Startup.cs class add the below line in the Configure method:

  ##public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {   app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "event_publisher_net v1"));
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            { endpoints.MapControllers();
            });
	        //Add below code
            app.ApplicationServices.GetService<ConfigurationManager>();
            app.ApplicationServices.GetService<EventProcessor>();
        }
