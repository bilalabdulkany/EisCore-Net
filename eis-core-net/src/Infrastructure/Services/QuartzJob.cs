using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using EisCore.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EisCore.Infrastructure.Services
{
    public class QuartzJob : IJob
    {
      //  IBrokerConfigFactory _brokerConfigFactory;
        private readonly IServiceProvider _provider;

        public QuartzJob(IServiceProvider provider)
        {
            //TODO https://andrewlock.net/using-scoped-services-inside-a-quartz-net-hosted-service-with-asp-net-core/
            this._provider = provider;
        }
        public Task Execute(IJobExecutionContext context)
        {
            using (var scope = _provider.CreateScope())
            {
               try { var brokerConnectionFactory = scope.ServiceProvider.GetService<IBrokerConfigFactory>();
                brokerConnectionFactory._ProducerConnection.Start();
               }catch(Exception e){
                   Console.WriteLine("Error occurred in connection",e.StackTrace);
               }
                return Console.Out.WriteLineAsync("Consumer Connection Started!");
                
            }


           // return Console.Out.WriteLineAsync("Greetings from HelloJob!");
        }
    }

    class ConsoleLogProvider : ILogProvider
    {
        public Logger GetLogger(string name)
        {
            return (level, func, exception, parameters) =>
            {
                if (level >= LogLevel.Info && func != null)
                {
                    Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
                }
                return true;
            };
        }

        public IDisposable OpenNestedContext(string message)
        {
            throw new NotImplementedException();
        }

        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            throw new NotImplementedException();
        }
    }
}