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
    public class QuartzKeepAliveEntryJob : IJob
    {
        private readonly IBrokerConnectionFactory _brokerConfigFactory;
        private readonly ICompetingConsumerDbContext _dbContext;
        private readonly IConfigurationManager _configManager;
        private readonly ILogger<QuartzKeepAliveEntryJob> _log;
        string testHostIp;
        public QuartzKeepAliveEntryJob(IBrokerConnectionFactory brokerConfigFactory, ICompetingConsumerDbContext dbContext, IConfigurationManager configManager, ILogger<QuartzKeepAliveEntryJob> log)
        {
            this._brokerConfigFactory = brokerConfigFactory;
            this._dbContext = dbContext;
            this._configManager = configManager;
            this._log = log;
            testHostIp= Guid.NewGuid().ToString();
            _dbContext.setHostIpAddress(testHostIp);
        }

        private Boolean stopStart = true;

        public Task Execute(IJobExecutionContext context)
        {
            Console.Out.WriteLineAsync("#########Consumer Connection Quartz Job... Cron: ["+_configManager.GetBrokerConfiguration().CronExpression+"]");

            //TODO Testing only with one System: XYZ
            var eisGroupKey = SourceSystemName.MDM + "_COMPETING_CONSUMER_GROUP";
            var refreshInterval = _configManager.GetBrokerConfiguration().RefreshInterval;

            try
            {
                //TODO put the hostIP
                var hostIP =testHostIp;// _dbContext.GetIPAddressOfServer(eisGroupKey, refreshInterval);
                var deleteResult = _dbContext.DeleteStaleEntry(eisGroupKey, refreshInterval);
                _log.LogInformation("Stale entry delete status:{r}",deleteResult.Result);
                var insertResult = _dbContext.InsertEntry(eisGroupKey);

                if (insertResult.Result == 1)
                {
                    _brokerConfigFactory.CreateConsumer();
                    _log.LogInformation("*** Consumer locked for: {ip} in group: {groupKey}", hostIP, eisGroupKey);
                }
                else
                {
                    string IpAddress = _dbContext.GetIPAddressOfServer(eisGroupKey, refreshInterval);
                    if (IpAddress != null)
                    {
                         _log.LogInformation("IsIPAddressMatchesWithGroupEntry(IpAddress): " +  IsIPAddressMatchesWithGroupEntry(IpAddress));
                        if (!IsIPAddressMatchesWithGroupEntry(IpAddress))
                        {
                            _brokerConfigFactory.DestroyConsumerConnection();
                        }
                        else
                        {
                            _brokerConfigFactory.CreateConsumer();
                            var keepAliveResult = _dbContext.KeepAliveEntry(stopStart, eisGroupKey);
                            _log.LogInformation("***Refreshing Keep Alive entry {k}", keepAliveResult.Result);
                        }
                    }
                    else
                    {
                        _brokerConfigFactory.DestroyConsumerConnection();
                        _log.LogInformation("***Connection destroyed");
                    }
                }
                return Console.Out.WriteLineAsync("exiting QuartzKeepAliveEntryJob");
            }
            catch (Exception e)
            {
                _log.LogCritical("exception when creating consumer: {e}", e.StackTrace);
                _brokerConfigFactory.DestroyConsumerConnection();
                _log.LogCritical("Consumer connection stopped on IP: ");
            }
            return Console.Out.WriteLineAsync("exception when creating consumer");
        }

        private bool IsIPAddressMatchesWithGroupEntry(string ipAddress)
        {
            return ipAddress.Equals(testHostIp);
            //TODO revert after testing
            //return ipAddress.Equals(UtilityClass.GetLocalIpAddress());           
            

        }     
    }
}