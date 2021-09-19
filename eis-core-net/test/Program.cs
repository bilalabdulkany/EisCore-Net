// using System;
// using System.IO;
// using System.Threading.Tasks;
// using EisCore.Application.Interfaces;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using System.Threading;
// using EisCore.Infrastructure.Configuration;
// //using Serilog;
// using EisCore.Domain.Entities;
// //using Microsoft.AspNetCore.Builder;

// namespace EisCore
// {
//     public class Program
//     {
//         public EventPublisher _publisher;
//         public EventProcessor _processor;
//         public IConfigurationManager _configManager;
//         public Microsoft.Extensions.Logging.ILogger<EventPublisher> _publisherlog;
//         public Microsoft.Extensions.Logging.ILogger<EventProcessor> _processorlog;

//         public Program(EventPublisher publisher,EventProcessor processor){
//           this._publisher= publisher;//new EventPublisher(_publisherlog,_configManager);
//           this._processor= processor;//new EventProcessor(_processorlog,_configManager);

//         }

//         static async Task Main(string[] args)
//         {
//             var builder = new ConfigurationBuilder();
            

//             BuildConfig(builder);
//             /*Log.Logger = new LoggerConfiguration()            
//             .ReadFrom.Configuration(builder.Build())
//             .Enrich.FromLogContext()
//             .WriteTo.Console()
//             .CreateLogger();
            

//             Log.Logger.Information("Application Starting");
//            */
//             var host =Host.CreateDefaultBuilder()
//             .ConfigureServices((context,services)=>
//             {
//                 services.AddSingleton<IConfigurationManager,ConfigurationManager>();
//                 services.AddSingleton<EventProcessor>();
//                 services.AddSingleton<EventPublisher>();
//                 services.AddSingleton<BrokerConfiguration>();
//                 //var appbuilder = new ApplicationBuilder((IServiceProvider)services);
//                // BuildApp(appbuilder);
               
//             })      
//             //.UseSerilog()
//             .Build();
            

//            // var svc = ActivatorUtilities.CreateInstance<ConfigurationManager>(host.Services);
//            // svc.CreateAsyncBrokerConnection();

//            var mainProgram=ActivatorUtilities.CreateInstance<Program>(host.Services);
//             mainProgram.TestProgramRun();
//             Thread.Sleep(3000);           

//         }

//         static void BuildConfig(IConfigurationBuilder builder){
//              builder.SetBasePath(Directory.GetCurrentDirectory())
//              .AddJsonFile("appsettings.json",optional:false,reloadOnChange:true)
//              .AddJsonFile("eissettings.json",optional:false,reloadOnChange:true)
//              .AddJsonFile($"eissettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")??"Production"}.json",optional:true)
//              .AddEnvironmentVariables();
            
             

//         }

//       //  static void BuildApp(IApplicationBuilder app){
//          //    app.ApplicationServices.GetService<EventProcessor>();
//         //}

//         public  void TestProgramRun()
//         {         

//          //  Log.Logger.Information("Starting to publish message to Topic...");            
//             //message=Console.ReadLine();
//             //runProgram.publisher.SendMessage("Message: " + message);
//             //this._publisher.publish("message +123");
//             var msg =new MessageProducerImpl();
//             this._publisher.publish(msg);
            
//            //Log.Logger.Information("Message Sent!");
//            this._processor.RunConsumerEventListener();
//            //Log.Logger.Information("Message Completed");
//         }

//     }
// }