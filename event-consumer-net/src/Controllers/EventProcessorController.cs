using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EisCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace EventProcessorController.Controllers
{    
    [ApiController]
    [Route("[controller]")]
    public class EventProcessorController : ControllerBase
    {
        private readonly EventProcessor _eventProcessor;

        //private readonly ILogger<WeatherForecastController> _logger;

        public EventProcessorController(EventProcessor eventProcessor)
        {
            _eventProcessor = eventProcessor;
        }

        [HttpGet]
        public IActionResult Consume(){
            Console.WriteLine("##Consuming Message##");
            _eventProcessor.RunConsumerEventListener();
            return Ok();
        }
    }
}
