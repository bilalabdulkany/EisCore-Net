using System;
using Dapper;
using event_consumer_net.Application.Interface;
using event_consumer_net.Application.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace event_consumer_net.Infrastructure.Persistence
{
  

    public class StaleEventCheckDbContext : IStaleEventCheckDbContext
    {
        private string _databaseName;
        private ILogger<StaleEventCheckDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;
        public StaleEventCheckDbContext(ILogger<StaleEventCheckDbContext> log, IConfiguration configuration)
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


        public StaleEventCheck FindMIdAndEventType(int mId, string eventType)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string sql = "SELECT ID, M_ID AS MID, EVENT_TYPE AS EVENTTYPE, CAST(EVENT_TIMESTAMP AS Datetime) as EVENTTIMESTAMP FROM STALE_EVENT_CHECK WHERE M_ID =@mId AND EVENT_TYPE=@eventType";


                    _log.LogInformation("Executing query: {sql} with variables [{a},{b}]", sql, mId, eventType);

                    StaleEventCheck staleEvent = connection.QueryFirstOrDefault<StaleEventCheck>(sql, new { mId, eventType });
                    _log.LogInformation("ID from query: [ {a} ]", (staleEvent==null)||staleEvent.Id==null?"Null":staleEvent);
                    return staleEvent;
                }
                catch (Exception e)
                {
                    _log.LogError(e.Message);
                    _log.LogError(e.StackTrace);
                }
                return null;
            }
        }

        public int Save(StaleEventCheck staleEventCheck)
        {

            string sqlite = "INSERT INTO STALE_EVENT_CHECK(ID,M_ID,EVENT_TYPE,EVENT_TIMESTAMP)" +
            " SELECT CAST(@Id AS VARCHAR(255)), CAST (@mId AS VARCHAR(255)),CAST (@EventType AS VARCHAR(255)),@EventTimestamp";

            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {

                    _log.LogInformation("Executing query: {sqlite} with variables [{Id},{mId},{EventType},{EventTimestamp}]", sqlite, staleEventCheck.Id, staleEventCheck.mId, staleEventCheck.EventType, staleEventCheck.EventTimestamp);
                    return connection.Execute(sqlite, new { staleEventCheck.Id, staleEventCheck.mId, staleEventCheck.EventType, staleEventCheck.EventTimestamp });
                }
                catch (Exception e)
                {
                    _log.LogError("Error occurred: {e}", e.Message);
                }
            }
            return 0;
        }

    }
}