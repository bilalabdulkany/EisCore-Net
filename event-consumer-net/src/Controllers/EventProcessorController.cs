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


        //private readonly ILogger<WeatherForecastController> _logger;

        public EventProcessorController()
        {

        }

        [HttpGet]
        public IActionResult Consume()
        {
            Console.WriteLine("##Consuming Message##");            
            return Ok();
        }
    }
}
