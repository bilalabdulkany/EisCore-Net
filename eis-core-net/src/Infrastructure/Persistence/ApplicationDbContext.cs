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
             if(_databaseName==null)                      {
                _databaseName="Data Source=TestCore.sqlite";
            }
            HostIp = this._configuration["environment:profile"];
        }

        public async void InsertEntry(string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    var Id = Guid.NewGuid().ToString();

                    /*      await connection.ExecuteAsync("INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                              " (SELECT CAST (@Id As VARCHAR(50)), CAST(@GroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), CURRENT_TIMESTAMP FROM DUAL " +
                              "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @GroupKey))", new { Id, HostIp, eisGroupKey });
                              */
                    string sql = "INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                    " (SELECT CAST (@Id As VARCHAR(50)), CAST(@eisGroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), CURRENT_TIMESTAMP FROM DUAL " +
                    "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @eisGroupKey))";

                    string sqlite = "INSERT INTO EIS_COMPETING_CONSUMER_GROUP (ID,GROUP_KEY, HOST_IP_ADDRESS,LAST_ACCESSED_TIMESTAMP)" +
                    " SELECT CAST (@Id As VARCHAR(50)), CAST(@eisGroupKey AS VARCHAR(50)), CAST(@HostIp AS VARCHAR(255)), datetime('now','localtime') " +
                    "WHERE NOT EXISTS(SELECT 1 FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY = @eisGroupKey1)";
                    
                    var eisGroupKey1=eisGroupKey;
                    _log.LogInformation("Executing query: {sql} with variables {a},{b},{c}", sqlite,Id,eisGroupKey, HostIp, eisGroupKey);
                    await connection.ExecuteAsync(sqlite, new { Id, eisGroupKey,HostIp,eisGroupKey1 });

                }
                catch (Exception e)
                {
                    _log.LogError(e.StackTrace);
                }
            }
        }

        public async void KeepAliveEntry(bool isStarted, string eisGroupKey)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    int startStatus = isStarted ? 1 : 0;
                    _log.LogInformation("Keep Alive Entry..");

                    string sqlite = @"UPDATE EIS_COMPETING_CONSUMER_GROUP 
                    SET LAST_ACCESSED_TIMESTAMP = datetime('now','localtime') 
                    WHERE GROUP_KEY=@eisGroupKey AND HOST_IP_ADDRESS=@HostIp AND 1=@startStatus";

                    //sqlite = "UPDATE EIS_COMPETING_CONSUMER_GROUP SET LAST_ACCESSED_TIMESTAMP = datetime('now','localtime') WHERE GROUP_KEY='MDM' AND HOST_IP_ADDRESS= 'development' AND 1=1";


                    _log.LogInformation("eisGroupKey, HostIp, startStatus "+ eisGroupKey +  HostIp +  startStatus);
                    var executestatus = await connection.ExecuteAsync(sqlite, new { eisGroupKey, HostIp, startStatus });
                    //var executestatus = await connection.ExecuteAsync(sqlite);
                    _log.LogInformation("executestatus::: " + executestatus );
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

                    string sql = "DELETE FROM EIS_COMPETING_CONSUMER_GROUP WHERE  " +
                    "EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval " +
                    "AND GROUP_KEY=@eisGroupKey";

                    string sqlite = "DELETE FROM EIS_COMPETING_CONSUMER_GROUP WHERE  " +
                    "(strftime('%M','now','localtime')-strftime('%M',LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval " +
                    "AND GROUP_KEY=@eisGroupKey";
                    _log.LogInformation("Executing query: {sql} with variables {a},{b}", sqlite,eisGroupKey,eisGroupRefreshInterval);


                    connection.Execute(sqlite, new { eisGroupRefreshInterval, eisGroupKey });
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
                    string sql="SELECT HOST_IP_ADDRESS FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY=@eisGroupKey " +
                    "AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval";

                    string sqlite="SELECT HOST_IP_ADDRESS FROM EIS_COMPETING_CONSUMER_GROUP WHERE GROUP_KEY=@eisGroupKey " +
                    "AND EXTRACT(MINUTE FROM (CURRENT_TIMESTAMP - LAST_ACCESSED_TIMESTAMP))<=@eisGroupRefreshInterval";
                    _log.LogInformation("Executing query: {sql} with variables {a},{b}", sqlite,eisGroupKey);
                    return connection.QuerySingleOrDefault<string>(sqlite, new { eisGroupKey, eisGroupRefreshInterval });
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