using System;
using EisCore.Application.Interfaces;

namespace eis_core_net.src.Infrastructure.Services
{
    public class InboxOutboxPollerJobSchedule : IJobSchedule
    {
        IConfigurationManager _configManager;
        public InboxOutboxPollerJobSchedule(IConfigurationManager configManager)
        {
            JobType = typeof(QuartzInboxOutboxPollerJob);

            _configManager = configManager;

        }

        public Type JobType { get; }

        public string GetCronExpression()
        { return _configManager.GetBrokerConfiguration().CronExpression; }
    }
}