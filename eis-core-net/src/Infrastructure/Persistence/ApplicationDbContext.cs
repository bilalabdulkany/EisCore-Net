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
    public class ApplicationDbContext : IApplicationDbContext
    {


        private string _databaseName;
        private ILogger<ApplicationDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;


        public ApplicationDbContext(ILogger<ApplicationDbContext> log, IConfiguration configuration)
        {
            this._log = log;
            this._configuration = configuration;
            _databaseName = this._configuration["DatabaseSource"];
            HostIp = this._configuration["environment:profile"];
        }

        public async void InsertEntry(string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    var Id = new Guid().ToString();

                    await connection.ExecuteAsync("INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                        " (SELECT CAST (@Id As VARCHAR(50)), CAST(@GroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), CURRENT_TIMESTAMP FROM DUAL " +
                        "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @GroupKey))", new { Id, HostIp, eisGroupKey });
                    //_log.LogInformation("Inserted into db {eisevent}", eisevent);
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
            }
        }

        public void KeepAliveEntry(bool isStarted, string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string startStatus = isStarted ? "1" : "0";
                    _log.LogInformation("Keep Alive Entry..");
                    connection.Execute("UPDATE EIS_COMPETING_CONSUMER_GROUP SET LAST_ACCESSED_TIMESTAMP = CURRENT_TIMESTAMP WHERE " +
                    "GROUP_KEY=@eisGroupKey AND HOST_IP_ADDRESS= @HostIp", new { eisGroupKey, HostIp });
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
            }
        }

        public void DeleteStaleEntry(string eisGroupKey, string eisGroupRefreshInterval)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    _log.LogInformation("Deleting Record Due to stale entry...");
                    connection.Execute("DELETE FROM EIS_COMPETING_CONSUMER_GROUP WHERE  " +
                    "EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval " +
                    "AND GROUP_KEY=@eisGroupKey", new { eisGroupRefreshInterval, eisGroupKey });
                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }


            }
        }

        public string GetIPAddressOfServer(string eisGroupKey, string eisGroupRefreshInterval)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    _log.LogInformation("Quering Event table");
                    return connection.QuerySingleOrDefault<string>("SELECT HOST_IP_ADDRESS FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY=@eisGroupKey " +
                    "AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval", new { eisGroupKey, eisGroupRefreshInterval });
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