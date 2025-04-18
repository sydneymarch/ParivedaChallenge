namespace api.Models
{
    public class AidStation
    {
        public string Name { get; set; }
        public double MilesIn { get; set; }
        public double MilesFromLast { get; set; }
        public double PredictedPace { get; set; }
        public DateTime EstimatedArrival { get; set; }
        public NutritionLog Log { get; set; } = new(); // detailed log of what happened at the station
        public int DelayShiftMinutes { get; set; } = 0;
        public double PaceAdjustment { get; set; } = 0;
    }
}
