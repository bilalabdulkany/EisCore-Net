using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EisCore.Domain.Entities;
using EisCore.Application.Interfaces;

namespace event_publisher_net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventPublisherController : ControllerBase
    {
       
        private IEventPublisherService _eventPublisher;
        private readonly ILogger<EventPublisherController> _logger;

        public EventPublisherController(ILogger<EventPublisherController> logger,IEventPublisherService eventPublisher)
        {

          this._eventPublisher=eventPublisher;              
          this._logger=logger;
        }

        [HttpPost("message")]       
        public IActionResult Publish(Payload message)
        {
            _logger.LogInformation("###-Controller Publishing-- {msg} -------###",message.Content);
            try{

            var messageProducerImpl= new MessageProducerImpl(message);
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._eventPublisher.publish(messageProducerImpl);
            watch.Stop();
             _logger.LogInformation("Time taken {m} ms from Controller", watch.ElapsedMilliseconds);
             
            }catch(Exception e){
                _logger.LogError("Error Occurred in Controller: {es}",e.StackTrace);
                return Content(e.StackTrace, "Error");
            }
            return Ok(message);
        }
    }
}
