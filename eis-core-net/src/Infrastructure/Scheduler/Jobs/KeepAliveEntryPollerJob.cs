using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;
using EisCore.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using EisCore.Application.Constants;
using Microsoft.Extensions.Logging;
using EisCore.Application.Util;

namespace EisCore.Infrastructure.Services
{

    [DisallowConcurrentExecution]
    public class KeepAliveEntryPollerJob : IJob
    {
        //private readonly ILogger<KeepAliveEntryPollerJob> _log;
        private IMessageQueueManager _messageQueueManager;        
        public KeepAliveEntryPollerJob(IMessageQueueManager messageQueueManager)
        {
          
            this._messageQueueManager = messageQueueManager;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            await this._messageQueueManager.KeepAliveTask();

        }


    }
}