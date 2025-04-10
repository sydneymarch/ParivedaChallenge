using Microsoft.AspNetCore.Mvc;
using Api;

namespace MyApp.Namespace //this is like a folder name in your code
{
    //this makes this class a controller, and the URL to reach it will be "/api/race"
    [Route("api/[controller]")]
    [ApiController]
    public class RaceController : ControllerBase //controllerBase gives us cool powers for APIs
    {
        //this is a pretend database for now — just a list of runner names saved in memory
        // private static List<string> runners = new List<string> { "Alex", "Jordan", "Casey" };

        //this is the method that gets run when someone visits /api/race in their browser
        [HttpGet] //this means “run this method when someone does a GET request”
        public IActionResult GetRunners()
        {
            RunnerFile fileHander = new RunnerFile();
            List<Runner> runners = fileHander.GetAllRunners();
            //Ok() sends back a 200 OK response with the list of runners
            return Ok(runners);
        }
    }
}

