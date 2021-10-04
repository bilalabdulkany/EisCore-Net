namespace event_consumer_net.Infrastructure.Persistence
{
    public class StaleEventCheckDbContext
    {


         public string FindMissionIdAndEventType(int missionId, string eventType)
        {
            using (var connection = new SqliteConnection(_databaseName))
            {
                try
                {
                    string sql = "SELECT * FROM STALE_EVENT_CHECK WHERE MISSION_ID =@missionId AND EVENT_TYPE=@eventType";
                    

                    _log.LogDebug("Executing query: {sql} with variables [{a}]", sqlite, missionId, eventType);

                    string result = connection.Query<EisEventInboxOutbox>(sqlite, new { eisGroupKey, eisGroupRefreshInterval });
                    _log.LogInformation("MISSION_ID from query: [ {a} ]", result);
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