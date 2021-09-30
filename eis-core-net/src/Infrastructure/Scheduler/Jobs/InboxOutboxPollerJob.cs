using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EisCore.Application.Constants;
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EisCore.Infrastructure.Services
{
    public class InboxOutboxPollerJob : IJob
    {
        private IMessageQueueManager _messageQueueManager;
        public InboxOutboxPollerJob(IMessageQueueManager messageQueueManager)
        {
            //this._log = log;
            this._messageQueueManager = messageQueueManager;
        }

        public Task Execute(IJobExecutionContext context)
        {
            Console.Out.WriteAsync("QuartzInboxOutboxPollerJob >>Executing Task");
            _messageQueueManager.InboxOutboxPollerTask();
            return Task.CompletedTask;
        }

    }
}