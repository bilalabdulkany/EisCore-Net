using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EisCore.Model;
using EisCore;
using System.Diagnostics;

namespace event_publisher_net.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventPublisherController : ControllerBase
    {
       
        private EventPublisher _eventPublisher;
        private EventProcessor _eventProcessor;

        

        private readonly ILogger<EventPublisherController> _logger;

        public EventPublisherController(ILogger<EventPublisherController> logger,EventPublisher eventPublisher,EventProcessor eventProcessor)
        {

          this._eventPublisher=eventPublisher;
          this._eventProcessor=eventProcessor;            
          this._logger=logger;
        }

        [HttpPost("message")]       
        public IActionResult Publish(Payload message)
        {
            Console.WriteLine($"###-Controller Publishing-- {message} -------###");
            try{

            var messageProducerImpl= new MessageProducerImpl(message);
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            this._eventPublisher.publish(messageProducerImpl);
            watch.Stop();
             _logger.LogInformation("Time taken {m} ms from Controller", watch.ElapsedMilliseconds);
             
            }catch(Exception e){
                Console.WriteLine(e.StackTrace);
                return Content(e.StackTrace, "Error");
            }
            return Ok(message);
        }

        [HttpGet]
        public IActionResult Consume(){
            Console.WriteLine("##Consuming Message##");
            _eventProcessor.RunConsumerEventListener();
            return Ok();
        }

    }
}
