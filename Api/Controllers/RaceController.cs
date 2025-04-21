using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using api.Models;
using api.Services;
using api.Utilities;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class RaceController : ControllerBase
    {
        private static List<RunnerRace> races = RaceUtility.LoadRacesFromDisk();
        private readonly UserService userService = new(); // handles user login and registration

        // GET: api/race
        [HttpGet]
        public IActionResult GetAllRaces()
        {
            return Ok(races); // returns every race in memory
        }

        // GET: api/race/{email}
        [HttpGet("{email}")]
        public IActionResult GetRacesByRunner(string email)
        {
            var result = races.Where(r => r.Email == email).ToList(); // get all races tied to email
            if (result.Count == 0) return NotFound(); // no match
            return Ok(result); // return all races for that runner
        }

        // POST: api/race
        [HttpPost]
        public IActionResult AddRace([FromBody] RunnerRace newRace)
        {
            if (newRace == null || string.IsNullOrWhiteSpace(newRace.Email)) return BadRequest(); // validate input
            var race = RaceService.AddRace(races, newRace); // add race through service
            return CreatedAtAction(nameof(GetRacesByRunner), new { email = race.Email }, race); // return new race
        }

        // POST: api/race/{email}/aidstations
        [HttpPost("{email}/aidstations")]
        public IActionResult AddAidStation(string email, [FromBody] AidStation station)
        {
            bool added = RaceService.AddAidStation(races, email, station);
            if (!added) {
                return NotFound(); // couldn't find race
            }
            return Ok(); // success
        }

        [HttpPut("{email}/aidstations/{index}/info")]
        public IActionResult UpdateAidStationInfo(string email, int index, [FromBody] AidStation updated)
        {
            bool updatedOk = RaceService.UpdateAidStationInfo(races, email, index, updated);
            if (!updatedOk)
            {
                return BadRequest();
            }

            return Ok();
        }

        // PUT: api/race/{email}/aidstations/{index}
        [HttpPut("{email}/aidstations/{index}")]
        public IActionResult UpdateAidStationLog(string email, int index, [FromBody] UpdateLogRequest request)
        {
            int delay = request.DelayShiftMinutes ?? 0;
            double pace = request.PaceAdjustment ?? 0;

            bool updated = RaceService.UpdateAidStationLog(races, email, index, request.Log, delay, pace);

            if (!updated)
            {
                return BadRequest(); // if the update failed
            }

            // find the updated race and return it
            RunnerRace updatedRace = null;
            for (int i = 0; i < races.Count; i++)
            {
                if (races[i].Email == email)
                {
                    updatedRace = races[i];
                    break;
                }
            }

            if (updatedRace == null)
            {
                return NotFound(); // just in case it was removed somehow
            }

            return Ok(updatedRace); // send back the whole updated race
        }



        // GET: api/race/{email}/next
        [HttpGet("{email}/next")]
        public IActionResult GetNextAidStationSummary(string email)
        {
            var result = RaceService.GetNextAidStationSummary(races, email);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // GET: api/race/{email}/{index}/export
        [HttpGet("{email}/{index}/export")]
        public IActionResult ExportRaceToExcel(string email, int index)
        {
            List<RunnerRace> userRaces = new List<RunnerRace>();

            for (int i = 0; i < races.Count; i++)
            {
                if (races[i].Email == email)
                {
                    userRaces.Add(races[i]);
                }
            }

            if (userRaces.Count == 0 || index < 0 || index >= userRaces.Count)
            {
                return BadRequest("Invalid race index or email.");
            }

            RunnerRace race = userRaces[index];
            byte[] fileBytes = RaceUtility.ExportRaceToExcelToBytes(race);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return StatusCode(500, "Could not create file.");
            }

            string safeRunner = race.RunnerName.Replace(" ", "_");
            string safeRace = race.RaceName.Replace(" ", "_");
            string fileName = safeRunner + "-" + safeRace + ".xlsx";

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }


        [HttpDelete("{email}")]
        public IActionResult DeleteAllRacesForRunner(string email)
        {
            bool deleted = RaceService.DeleteRacesByEmail(races, email);

            if (!deleted)
            {
                return NotFound(); // no races matched
            }

            return Ok(); // races deleted and saved
        }

        // DELETE: api/race/{email}/aidstations/{index}
        [HttpDelete("{email}/aidstations/{index}")]
        public IActionResult DeleteAidStation(string email, int index)
        {
            bool deleted = RaceService.DeleteAidStation(races, email, index);

            if (!deleted)
            {
                return NotFound(); // aid station not found or couldn't be removed
            }

            return Ok(); // success
        }
   

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var matchedUser = userService.LoginUser(request.Email, request.Password);
            if (matchedUser == null)
            {
                return Unauthorized(); // bad credentials
            }

            return Ok(matchedUser); // success
        }


        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            bool success = userService.RegisterUser(user);
            if (!success) return Conflict("Email already in use"); // prevent duplicate

            return Ok("Registration successful");
        }
    }
}
