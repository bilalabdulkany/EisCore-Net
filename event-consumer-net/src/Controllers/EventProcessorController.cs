using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EisCore;
using event_consumer_net.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace EventProcessorController.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventProcessorController : ControllerBase
    {


        //private readonly ILogger<WeatherForecastController> _logger;

        public EventProcessorController()
        {

        }

        [HttpGet]
        public IActionResult DisplayMessage()
        {
            string eventConsumed = "Last Consumed Event:: " + EventMessageProcessor.LastConsumerPayload[0];
            Console.WriteLine(eventConsumed);
            return Ok();
        }
    }
}
