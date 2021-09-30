using System;
using EisCore.Application.Interfaces;

namespace EisCore.Infrastructure.Services
{
    public class InboxOutboxPollerJobSchedule : IJobSchedule
    {
        IConfigurationManager _configManager;
        public InboxOutboxPollerJobSchedule(IConfigurationManager configManager)
        {
            JobType = typeof(InboxOutboxPollerJob);

            _configManager = configManager;

        }

        public Type JobType { get; }

        public string GetCronExpression()
        { return "0/5 * 1/1 ? * *"; }
    }
}