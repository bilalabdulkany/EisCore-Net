using System;
using EisCore.Application.Constants;
using EisCore.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using EisCore.Application.Interfaces;

namespace EisCore.Infrastructure.Persistence
{

    public class EventInboxOutboxDbContext : IEventInboxOutboxDbContext
    {
        private string _databaseName;
        private ILogger<EventInboxOutboxDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;


        public EventInboxOutboxDbContext(ILogger<EventInboxOutboxDbContext> log, IConfiguration configuration)
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

        public async Task<int> TryEventInsert(EisEvent eisEvent, string topicQueueName, string direction)
        {
            string sql = "INSERT INTO EIS_EVENT_INBOX_OUTBOX(ID,EVENT_ID,TOPIC_QUEUE_NAME,EIS_EVENT, EVENT_TIMESTAMP,IN_OUT)" +
            "(SELECT CAST(@Id AS VARCHAR(50)), CAST (@eventID AS VARCHAR(50)),CAST (@topicQueueName AS VARCHAR(50)), CAST (@objString AS CLOB), CURRENT_TIMESTAMP, CAST (@INOUT AS VARCHAR(3)) FROM DUAL" +
            " WHERE NOT EXISTS (SELECT 1 FROM EIS_EVENT_INBOX_OUTBOX WHERE EVENT_ID=@Id AND IN_OUT=@INOUT))";

            string sqlite = "INSERT INTO EIS_EVENT_INBOX_OUTBOX(ID,EVENT_ID,TOPIC_QUEUE_NAME,EIS_EVENT, EVENT_TIMESTAMP,IN_OUT)" +
            " SELECT CAST(@Id AS VARCHAR(50)), CAST (@EventID AS VARCHAR(50)),CAST (@topicQueueName AS VARCHAR(50)), CAST (@objString AS CLOB), datetime('now','localtime'), CAST (@direction AS VARCHAR(3))" +
            " WHERE NOT EXISTS (SELECT 1 FROM EIS_EVENT_INBOX_OUTBOX WHERE EVENT_ID=@EventID)";

            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string objString = JsonSerializer.Serialize(eisEvent);
                    var Id = Guid.NewGuid().ToString();
                    _log.LogInformation("Executing query: {sqlite} with variables [{Id},{eisEvent.EventID},{topicQueueName},{objString},{direction}]", sqlite, Id, eisEvent.EventID, topicQueueName, objString, direction);
                    return await connection.ExecuteAsync(sqlite, new { Id, eisEvent.EventID, topicQueueName, objString, direction });
                }
                catch (Exception e)
                {
                    _log.LogError("Error occurred: {e}", e.StackTrace);
                }

            }
            return 0;
        }

        public async Task<int> UpdateEventStatus(string eventId, string eventStatus)
        {
            string sql = "UPDATE EIS_EVENT_INBOX_OUTBOX SET IS_EVENT_PROCESSED=@ProcessedStatus WHERE EVENT_ID=@EventId and IN_OUT=@direction";
            string sqlite = "UPDATE EIS_EVENT_INBOX_OUTBOX SET IS_EVENT_PROCESSED=@eventStatus WHERE EVENT_ID=@eventId ";
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    _log.LogInformation("Executing query: {sqlite} with variables [{eventStatus},{eventId}]", sqlite, eventStatus, eventId);
                    return await connection.ExecuteAsync(sqlite, new { eventStatus, eventId });
                }               
                catch (Exception e)
                {
                    _log.LogError("Error occurred: {e}", e.StackTrace);
                }

            }
            return 0;

        }

        public async Task<IEnumerable<EisEventInboxOutbox>> GetAllUnprocessedEvents()
        {

            string sqlite = "SELECT ID, EVENT_ID AS EVENTID, TOPIC_QUEUE_NAME AS TOPICQUEUENAME,EIS_EVENT AS EISEVENT,EVENT_TIMESTAMP AS EVENTTIMESTAMP, IS_EVENT_PROCESSED AS ISEVENTPROCESSED, IN_OUT AS INOUT FROM EIS_EVENT_INBOX_OUTBOX WHERE IS_EVENT_PROCESSED IS NULL order by EVENT_TIMESTAMP ASC";
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    _log.LogInformation("Executing query: {sqlite} with variables]", sqlite);
                    var ListOfEvents = await connection.QueryAsync<EisEventInboxOutbox>(sqlite);
                    return ListOfEvents;
                }
                catch (Exception e)
                {
                    _log.LogError("Error occurred: {e}", e.StackTrace);
                }

            }
            return null;
        }




        //public List<EisEventInboxOutbox>
    }
}