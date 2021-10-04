using System;
using System.Collections.Generic;
using Dapper;
using event_consumer_net.Application.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace event_consumer_net.Infrastructure.Persistence
{
    public class IdempotentEventCheckDbContext
    {
        private string _databaseName;
        private ILogger<IdempotentEventCheckDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;
        public IdempotentEventCheckDbContext(ILogger<IdempotentEventCheckDbContext> log, IConfiguration configuration)
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


        public List<IdempotentEventCheck> FindMIdAndEventType(int mId)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string sql = "SELECT ID, M_ID AS MID, C_ID AS CID, EVENT_CODE AS EVENTCODE FROM IDEMPOTENT_EVENT_CHECK WHERE M_ID =@mId";


                    _log.LogDebug("Executing query: {sql} with variables [{a}]", sql, mId);

                    List<IdempotentEventCheck> idempotentEvent = connection.Query<IdempotentEventCheck>(sql, new { mId }).AsList();
                    _log.LogInformation("Number of events from query: [ {a} ]", idempotentEvent.Count);
                    return idempotentEvent;
                }
                catch (Exception e)
                {
                    _log.LogError(e.Message);
                }
                return null;
            }
        }
        public int Save(IdempotentEventCheck idempotentEventCheck)
        {

            string sqlite = "INSERT INTO IDEMPOTENT_EVENT_CHECK(ID,M_ID,C_ID,EVENT_CODE)" +
            " SELECT CAST(@Id AS VARCHAR(255)), CAST (@MId AS VARCHAR(255)),CAST (@CId AS VARCHAR(255)),CAST (@EventCode AS VARCHAR(255))";

            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {

                    _log.LogDebug("Executing query: {sqlite} with variables [{Id},{mId},{EventCode}]", sqlite, idempotentEventCheck.Id, idempotentEventCheck.MId, idempotentEventCheck.CId, idempotentEventCheck.EventCode);
                    return connection.Execute(sqlite, new { idempotentEventCheck.Id, idempotentEventCheck.MId, idempotentEventCheck.CId, idempotentEventCheck.EventCode });
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