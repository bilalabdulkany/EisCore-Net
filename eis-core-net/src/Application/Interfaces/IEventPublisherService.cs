using System;
using EisCore.Domain.Entities;

namespace EisCore.Application.Interfaces
{
    public interface IEventPublisherService
    {
        void publish(IMessageEISProducer messageObject);
    }
}