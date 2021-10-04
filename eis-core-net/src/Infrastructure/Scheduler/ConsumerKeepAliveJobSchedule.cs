using System;
using EisCore.Application.Interfaces;
namespace EisCore.Infrastructure.Services
{

    public class ConsumerKeepAliveJobSchedule : IJobSchedule
    {
        IConfigurationManager _configManager;
        public ConsumerKeepAliveJobSchedule(IConfigurationManager configManager)
        {
            JobType = typeof(ConsumerKeepAliveEntryPollerJob);
            _configManager = configManager;

        }

        public Type JobType { get; }
        public string GetCronExpression()
        { return _configManager.GetBrokerConfiguration().CronExpression; }
    }
}