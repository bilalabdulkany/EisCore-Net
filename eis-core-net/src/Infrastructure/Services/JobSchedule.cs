using System;
using EisCore.Application.Interfaces;
using EisCore.Infrastructure.Services;

namespace EisCore
{
    public class JobSchedule
    {
        IConfigurationManager _configManager;
        public JobSchedule(IConfigurationManager configManager)
        {
            JobType = typeof(QuartzKeepAliveEntryJob);
            _configManager=configManager;            
            
        }

        public Type JobType { get; }
        public string GetCronExpression()
         { return _configManager.GetBrokerConfiguration().CronExpression; }
    }
}