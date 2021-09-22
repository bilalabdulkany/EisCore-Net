using EisCore.Application.Constants;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eis_core_net.src.Infrastructure.Persistence
{
    public class EventInboxOutboxDbContext
    {
        private string _databaseName;
        private ILogger<EventInboxOutboxDbContext> _log;
        private IConfiguration _configuration;
        private string HostIp;

        public int TryEventInsert(EisEvent eisEvent,string topicQueueName,AtLeastOnceDeliveryDirection direction){
            string sql="INSERT INTO EIS_EVENT_INBOX_OUTBOX(ID,EVENT_ID,TOPIC_QUEUE_NAME,EIS_EVENT, EVENT_TIMESTAMP,IN_OUT)"+
            "(SELECT CAST(@Id AS VARCHAR(50)), CAST (@eventID AS VARCHAR(50)),CAST (@topicQueueName AS VARCHAR(50)), CAST (@eisEvent AS CLOB), CURRENT_TIMESTAMP, CAST (@INOUT AS VARCHAR(3)) FROM DUAL"+
            " WHERE NOT EXISTS (SELECT 1 FROM EIS_EVENT_INBOX_OUTBOX WHERE EVENT_ID=@Id AND IN_OUT=@INOUT))";
            return 1;
        }

        //public List<EisEventInboxOutbox>
    }
}