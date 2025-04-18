namespace api.Models
{
    public class RunnerRace
    {
        public int ID { get; set; }
        public string RunnerName { get; set; } // ✅ this will match what's coming from frontend
        public string Email { get; set; }
        public string RaceName { get; set; }
        public double ProjectedPace { get; set; }
        public double TotalDistance { get; set; }
        public DateTime StartTime { get; set; }

        public List<AidStation> AidStations { get; set; } = new();
        public List<string> SharedWithEmails { get; set; } = new();
    }
}
