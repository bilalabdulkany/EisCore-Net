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
        private readonly IBrokerConnectionFactory _brokerConfigFactory;
        public QuartzJob(IServiceProvider provider, IBrokerConnectionFactory brokerConfigFactory)
        {            
            this._brokerConfigFactory = brokerConfigFactory;
        }

        private Boolean stopStart = true;

        public Task Execute(IJobExecutionContext context)
        {
            Console.Out.WriteLineAsync("#########Consumer Connection Quartz Job...");

            //TODO - create atomic bool to handle the consumer creation with the same client id..
            // _brokerConfigFactory._ConsumerConnection.Start();
            if (stopStart)
            {
                _brokerConfigFactory.CreateConsumer();
                stopStart = false;
                return Console.Out.WriteLineAsync("Consumer Connection started!");
            }
            else
            {
                _brokerConfigFactory.DestroyConsumer();
                stopStart = true;
                return Console.Out.WriteLineAsync("Consumer Connection Stopped!");
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
}