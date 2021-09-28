using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EisCore.Application.Interfaces;
using EisCore.Application.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EisCore.Infrastructure.Persistence
{
    public class CompetingConsumerDbContext : ICompetingConsumerDbContext
    {


        private string _databaseName;
        private ILogger<CompetingConsumerDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;


        public CompetingConsumerDbContext(ILogger<CompetingConsumerDbContext> log, IConfiguration configuration)
        {
            this._log = log;
            this._configuration = configuration;
            _databaseName = this._configuration["DatabaseSource"];
            if (_databaseName == null)
            {
                _databaseName = "Data Source=TestCore.sqlite";
            }

            HostIp = this._configuration["environment:profile"];
        }


        //TODO TESTING to be removed
        public void setHostIpAddress(string hostIp) {
            HostIp = hostIp;
        }
        public async Task<int> InsertEntry(string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    var Id = Guid.NewGuid().ToString();

                    string sql = "INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                    " (SELECT CAST (@Id As VARCHAR(50)), CAST(@eisGroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), CURRENT_TIMESTAMP FROM DUAL " +
                    "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @eisGroupKey))";

                    string sqlite = "INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                    " SELECT CAST (@Id As VARCHAR(50)), CAST(@eisGroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), datetime('now','localtime') " +
                    "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @eisGroupKey1)";

                    var eisGroupKey1 = eisGroupKey;
                    _log.LogDebug("Executing query: {sql} with variables [ {a},{b},{c} ]", sqlite, Id, eisGroupKey, HostIp, eisGroupKey);
                    return await connection.ExecuteAsync(sqlite, new { Id, eisGroupKey, HostIp, eisGroupKey1 });


                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);

                }
                return 0;
            }
        }

        public async Task<int> KeepAliveEntry(bool isStarted, string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    int startStatus = isStarted ? 1 : 0;
                    _log.LogInformation("Keep Alive Entry..");

                    string sql = "UPDATE EIS_COMPETING_CONSUMER_GROUP SET LAST_ACCESSED_TIMESTAMP = CURRENT_TIMESTAMP WHERE " +
                   "GROUP_KEY=@eisGroupKey AND HOST_IP_ADDRESS= @HostIp AND 1=@startStatus";

                    string sqlite = "UPDATE EIS_COMPETING_CONSUMER_GROUP SET LAST_ACCESSED_TIMESTAMP = datetime('now','localtime') WHERE " +
                   "GROUP_KEY=CAST(@eisGroupKey AS VARCHAR(50)) AND HOST_IP_ADDRESS= CAST(@HostIp AS VARCHAR(255)) AND 1=@startStatus";

                    _log.LogDebug("Executing query: {sql} with variables [ {a},{b},{c} ]", sqlite, eisGroupKey, HostIp, startStatus);

                    return await connection.ExecuteAsync(sqlite, new { eisGroupKey, HostIp, startStatus });


                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);

                }
                return 0;
            }
        }

        public async Task<int> DeleteStaleEntry(string eisGroupKey, int eisGroupRefreshInterval)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string sql = "DELETE FROM EIS_COMPETING_CONSUMER_GROUP WHERE  " +
                    "EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval " +
                    "AND GROUP_KEY=@eisGroupKey";

                    string sqlite = "DELETE FROM EIS_COMPETING_CONSUMER_GROUP WHERE  " +
                    "CAST ((julianday(datetime('now','localtime'))-julianday(last_accessed_timestamp))*24*60 as Integer)>@eisGroupRefreshInterval " +
                    "AND GROUP_KEY=@eisGroupKey";

                    _log.LogDebug("Executing query: {sql} with variables {a},{b}", sqlite, eisGroupRefreshInterval, eisGroupKey);

                    return await connection.ExecuteAsync(sqlite, new { eisGroupRefreshInterval, eisGroupKey });
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);

                }
                return 0;


            }
        }

        public string GetIPAddressOfServer(string eisGroupKey, int eisGroupRefreshInterval)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {

                    string sql = "SELECT HOST_IP_ADDRESS FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY=@eisGroupKey " +
                    "AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval";

                    string sqlite = "SELECT HOST_IP_ADDRESS FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY=@eisGroupKey " +
                    "AND CAST ((julianday(datetime('now','localtime'))-julianday(last_accessed_timestamp))*24*60 as Integer)<=@eisGroupRefreshInterval";

                    _log.LogDebug("Executing query: {sql} with variables {a},{b}", sqlite, eisGroupKey, eisGroupRefreshInterval);

                    string result = connection.QuerySingleOrDefault<string>(sqlite, new { eisGroupKey, eisGroupRefreshInterval });
                    _log.LogInformation("IP address from query: [ {a} ]", result);
                    return result;
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
                return null;
            }
        }


    }
}