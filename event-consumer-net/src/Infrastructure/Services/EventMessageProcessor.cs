
using EisCore.Application.Interfaces;
using EisCore.Domain.Entities;
using Microsoft.Extensions.Logging;
using event_consumer_net.Infrastructure.Persistence;
using event_consumer_net.Application.Model;
using System.Collections.Generic;
using System.Linq;
using event_consumer_net.Application.Interface;

namespace event_consumer_net.Infrastructure.Services
{
    public class EventMessageProcessor : IMessageProcessor
    {

        private readonly IStaleEventCheckDbContext _staleEventCheckDbContext;
        private readonly IIdempotentEventCheckDbContext _idempotentEventCheckDbContext;
        private readonly ILogger<EventMessageProcessor> _logger;
        public static Payload[] LastConsumerPayload= new Payload[1];
        public EventMessageProcessor(ILogger<EventMessageProcessor> logger,IIdempotentEventCheckDbContext idempotentEventCheckDbContext,IStaleEventCheckDbContext staleEventCheckDbContext)
        {
            this._logger = logger;
            this._staleEventCheckDbContext=staleEventCheckDbContext;
            this._idempotentEventCheckDbContext=idempotentEventCheckDbContext;

        }
        public void Process(Payload payload, string eventType)
        {
            _logger.LogInformation("inside consumer's process method");
            SortieContent payloadContent = payload.ConvertContent<SortieContent>();
            _logger.LogInformation("{a},{b},{c}",payloadContent.MId, eventType, payloadContent.EventTimestamp);
            //Stale event check - to determine if timestamp of new event is older than the existing event in db
            StaleEventCheck check = _staleEventCheckDbContext.FindMIdAndEventType(payloadContent.MId, eventType);
            if (check == null)
            {
                _logger.LogInformation("Inside stale check");
                //Save StaleEventCheck to DB
                StaleEventCheck checkToSave = new StaleEventCheck("0", payloadContent.MId, eventType, payloadContent.EventTimestamp);
                _staleEventCheckDbContext.Save(checkToSave);
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
            List<IdempotentEventCheck> IdempotentEventCheckListFromDb = _idempotentEventCheckDbContext.FindMIdAndEventType(payloadContent.MId);
            //Set the SortieContent from the payload to the idempotentEventCheckList in memory
            List<IdempotentEventCheck> idempotentEventCheckList = GetIdempotentEventCheckList(payloadContent);
            if (IdempotentEventCheckListFromDb.Count == 0)
            {
                _logger.LogInformation("IdempotentEventCheckListFromDb");
                //Save all data to DB
                foreach (IdempotentEventCheck idempotentEventCheck in idempotentEventCheckList)
                {
                    _idempotentEventCheckDbContext.Save(idempotentEventCheck);
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
                    idempotentEventCheck.Id=crEvents.Id;
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