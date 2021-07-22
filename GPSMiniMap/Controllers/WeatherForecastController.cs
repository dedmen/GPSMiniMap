using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GPSMiniMap.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ContentResult Get()
        {
            return Content(@"
<!DOCTYPE html>
<html>
  <head>
    <title>Geolocation</title>
    <script src=""https://polyfill.io/v3/polyfill.min.js?features=default""></script>
    <!--jsFiddle will insert css and js-->
  </head>
  <body style=""height: 100vh; width: 100vw;"">
     <div id=""map""style=""position: static;""></div>

    <!--Async script executes immediately and must be after any DOM elements used in callback. -->
    <script
      src=""https://maps.googleapis.com/maps/api/js?key=AIzaSyCUpK6J2jftObofvVyw9Y9IP19lYx1bB4E&callback=initMap&libraries=&v=weekly&channel=2""
      async
    ></script>
    <script src=""~/js/signalr/dist/browser/signalr.js""></script>
    < script
      src=""WeatherForecast/stuff.js""
      async
    ></script>
  </body>
</html> 

", "text/html", System.Text.Encoding.UTF8);
        }

        [HttpGet("stuff.js")]
        public ContentResult GetScript()
        {
            return Content(System.IO.File.ReadAllText("stuff.js"), "text/javascript; charset=UTF-8", System.Text.Encoding.UTF8);
        }
        // AIzaSyCUpK6J2jftObofvVyw9Y9IP19lYx1bB4E API key
    }
}
