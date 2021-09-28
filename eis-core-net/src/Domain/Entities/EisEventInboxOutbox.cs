using System;
using EisCore.Domain.Entities;

namespace EisCore.Domain.Entities
{
    public class EisEventInboxOutbox
    {
        public string Id{get;set;}
        public string EventId{get;set;}
        public string TopicQueueName{get;set;}
        public EisEvent eisEvent{get;set;}
        private DateTime EventTimestamp{get;set;}
        private string IsEventProcessed{get;set;}
        private string InOut{get;set;}
    }
}