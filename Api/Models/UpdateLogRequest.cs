namespace api.Models
{
    public class UpdateLogRequest
    {
        public NutritionLog Log { get; set; }
        public int? DelayShiftMinutes { get; set; }
        public double? PaceAdjustment { get; set; }
    }
}
