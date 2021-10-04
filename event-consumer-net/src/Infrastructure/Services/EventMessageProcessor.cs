
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using event_consumer_net.Infrastructure.Persistence;
using event_consumer_net.Application.Model;
using System.Collections.Generic;
using System.Linq;

namespace event_consumer_net.Infrastructure.Services
{
    public class EventMessageProcessor : IMessageProcessor
    {

        private readonly StaleEventCheckDbContext staleEventCheckDbContext;
        private readonly IdempotentEventCheckDbContext idempotentEventCheckDbContext;
        private readonly ILogger<EventMessageProcessor> _logger;
        public static Payload[] LastConsumerPayload;
        public EventMessageProcessor(ILogger<EventMessageProcessor> logger)
        {
            _logger = logger;
        }
        public void Process(Payload payload, string eventType)
        {
            _logger.LogInformation("inside consumer's process method");
            SortieContent payloadContent = payload.ConvertContent<SortieContent>();
            //Stale event check - to determine if timestamp of new event is older than the existing event in db
            StaleEventCheck check = staleEventCheckDbContext.FindMIdAndEventType(payloadContent.MId, eventType);
            if (check == null)
            {
                //Save StaleEventCheck to DB
                StaleEventCheck checkToSave = new StaleEventCheck("0", payloadContent.MId, eventType, payloadContent.EventTimestamp);
                staleEventCheckDbContext.Save(checkToSave);
            }
            else
            {
                if (check.EventTimestamp.Equals(payloadContent.EventTimestamp) || check.EventTimestamp.CompareTo(payloadContent.EventTimestamp) > 0)
                {//Check the new record older than the current record in the db
                    _logger.LogInformation("Payload ignored due to stalenetss::{}", payloadContent);
                    return;
                }
            }

            //Idempotent event check - to determine if the data is semantically similar rather than event IDs
            List<IdempotentEventCheck> IdempotentEventCheckListFromDb = idempotentEventCheckDbContext.FindMIdAndEventType(payloadContent.MId);
            //Set the SortieContent from the payload to the idempotentEventCheckList in memory
            List<IdempotentEventCheck> idempotentEventCheckList = GetIdempotentEventCheckList(payloadContent);
            if (IdempotentEventCheckListFromDb.Count == 0)
            {
                //Save all data to DB
                foreach (IdempotentEventCheck idempotentEventCheck in idempotentEventCheckList)
                {
                    idempotentEventCheckDbContext.Save(idempotentEventCheck);
                }
            }
            else
            {
                List<IdempotentEventCheck> both = IdempotentEventCheckListFromDb.Intersect(idempotentEventCheckList).ToList<IdempotentEventCheck>();

                if (both.Count == IdempotentEventCheckListFromDb.Count)
                {
                    _logger.LogInformation("The event is semantically similar");
                    return;
                }
                else
                {
                    _logger.LogInformation("The events are different");
                }
            }

            //Payload business logic
            LastConsumerPayload[0] = payload;
            _logger.LogInformation("Payload :: {p}", payloadContent);
        }

        private List<IdempotentEventCheck> GetIdempotentEventCheckList(SortieContent payloadContent)
        {
            if (payloadContent == null) return null;
            List<IdempotentEventCheck> IdempotentEventCheckList = new List<IdempotentEventCheck>();
            IdempotentEventCheck idempotentEventCheck;
            if (payloadContent.CrEvents != null)
            {
                foreach (CrEvents crEvents in payloadContent.CrEvents)
                {
                    idempotentEventCheck = new IdempotentEventCheck();
                    idempotentEventCheck.MId = payloadContent.MId;
                    idempotentEventCheck.CId = crEvents.CId;
                    idempotentEventCheck.EventCode = crEvents.Event.Code;
                    IdempotentEventCheckList.Add(idempotentEventCheck);
                }
            }
            return IdempotentEventCheckList;
        }
    }
}