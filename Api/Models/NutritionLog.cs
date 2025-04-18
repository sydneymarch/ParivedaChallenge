namespace api.Models
{
    public class NutritionLog
    {
        public string? Food { get; set; }
        public string? Drink { get; set; }
        public string? Notes { get; set; } // gear, injury, energy, etc.
        public DateTime ArrivalTime { get; set; }

        public NutritionLog() { }
    }
}
