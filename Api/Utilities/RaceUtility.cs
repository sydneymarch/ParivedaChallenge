using api.Models;
using System.Text.Json;
using ClosedXML.Excel;

namespace api.Utilities
{
    public class RaceUtility
    {
        public static void AdjustFutureAidStations(RunnerRace race, int fromIndex, TimeSpan thisDelay, double thisPaceAdjustment)
        {
            if (race == null || fromIndex < 0 || fromIndex >= race.AidStations.Count - 1)
            {
                return;
            }

            DateTime baseTime = race.AidStations[fromIndex].EstimatedArrival;
            int totalDelayMinutes = thisDelay.Minutes;
            double totalPaceAdjustment = thisPaceAdjustment;

            for (int i = fromIndex + 1; i < race.AidStations.Count; i++)
            {
                AidStation current = race.AidStations[i];
                double pace = current.PredictedPace + totalPaceAdjustment;
                TimeSpan legTime = TimeSpan.FromMinutes(current.MilesFromLast * pace);

                baseTime = baseTime.Add(legTime);
                baseTime = baseTime.AddMinutes(totalDelayMinutes);

                current.EstimatedArrival = baseTime;

                totalPaceAdjustment += current.PaceAdjustment;
                totalDelayMinutes += current.DelayShiftMinutes;
            }
        }

        public static void UpdateRaceStats(RunnerRace race)
        {
            double totalDistance = 0;
            double totalPace = 0;
            int count = race.AidStations.Count;

            if (count == 0)
            {
                race.TotalDistance = 0;
                race.ProjectedPace = 0;
                return;
            }

            double runningMiles = 0;

            for (int i = 0; i < count; i++)
            {
                runningMiles += race.AidStations[i].MilesFromLast;
                race.AidStations[i].MilesIn = runningMiles;

                totalDistance += race.AidStations[i].MilesFromLast;
                totalPace += race.AidStations[i].PredictedPace;
            }

            race.TotalDistance = totalDistance;
            race.ProjectedPace = totalPace / count;
        }

        public static void ResetFutureAidStations(RunnerRace race, int startIndex)
        {
            DateTime baseTime = race.AidStations[startIndex].Log.ArrivalTime;

            for (int i = startIndex + 1; i < race.AidStations.Count; i++)
            {
                double pace = race.AidStations[i].PredictedPace;
                double miles = race.AidStations[i].MilesFromLast;
                double minutes = pace * miles;

                baseTime = baseTime.AddMinutes(minutes);
                race.AidStations[i].EstimatedArrival = baseTime;
            }
        }

        public static void SaveRacesToDisk(List<RunnerRace> races)
        {
            string folderPath = "races";
            Directory.CreateDirectory(folderPath);

            for (int i = 0; i < races.Count; i++)
            {
                string safeEmail = races[i].Email.Replace("@", "_at_").Replace(".", "_");
                string safeName = races[i].RaceName.Replace(" ", "_").ToLower();
                string fileName = Path.Combine(folderPath, safeName + "-" + safeEmail + ".json");

                string json = JsonSerializer.Serialize(races[i], new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fileName, json);
            }
        }

        public static List<RunnerRace> LoadRacesFromDisk()
        {
            string folderPath = "races";
            List<RunnerRace> loadedRaces = new List<RunnerRace>();

            if (!Directory.Exists(folderPath)) return loadedRaces;

            string[] files = Directory.GetFiles(folderPath, "*.json");

            for (int i = 0; i < files.Length; i++)
            {
                string json = File.ReadAllText(files[i]);
                RunnerRace race = JsonSerializer.Deserialize<RunnerRace>(json);
                if (race != null) loadedRaces.Add(race);
            }

            return loadedRaces;
        }

        public static void ExportRaceToExcel(RunnerRace race)
        {
            XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet summarySheet = workbook.Worksheets.Add("Race Summary");

            string title = race.RunnerName + "'s " + race.RaceName + "🏃💨";
            summarySheet.Cell(1, 1).Value = title;
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
            summarySheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightSkyBlue;

            summarySheet.Cell(2, 1).Value = "Runner Name";
            summarySheet.Cell(2, 2).Value = race.RunnerName;
            summarySheet.Cell(3, 1).Value = "Race Name";
            summarySheet.Cell(3, 2).Value = race.RaceName;
            summarySheet.Cell(4, 1).Value = "Start Time";
            summarySheet.Cell(4, 2).Value = race.StartTime.ToString("t");
            summarySheet.Cell(5, 1).Value = "Total Distance";
            summarySheet.Cell(5, 2).Value = race.TotalDistance;
            summarySheet.Cell(6, 1).Value = "Projected Pace";
            summarySheet.Cell(6, 2).Value = race.ProjectedPace;

            summarySheet.Range("A1:B1").Merge();
            summarySheet.Range("A1:B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            summarySheet.Range("A2:A6").Style.Font.Bold = true;
            summarySheet.Columns("A", "B").AdjustToContents();
            summarySheet.Column(1).Width = 35;

            IXLWorksheet aidSheet = workbook.Worksheets.Add("Aid Station Breakdown");
            string[] headers = { "Name 🏁", "Miles In 📏", "Pace 🕒", "ETA ⌚", "Arrival 🚶", "Food 🍞", "Drink 💧", "Notes 📝" };

            for (int i = 0; i < headers.Length; i++)
            {
                aidSheet.Cell(1, i + 1).Value = headers[i];
            }

            aidSheet.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.LightGreen;
            aidSheet.Range("A1:H1").Style.Font.Bold = true;
            aidSheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int i = 0; i < race.AidStations.Count; i++)
            {
                AidStation s = race.AidStations[i];
                int row = i + 2;

                aidSheet.Cell(row, 1).Value = s.Name;
                aidSheet.Cell(row, 2).Value = s.MilesIn;
                aidSheet.Cell(row, 3).Value = s.PredictedPace;
                aidSheet.Cell(row, 4).Value = s.EstimatedArrival.ToString("t");
                aidSheet.Cell(row, 5).Value = s.Log.ArrivalTime == DateTime.MinValue ? "—" : s.Log.ArrivalTime.ToString("t");
                aidSheet.Cell(row, 6).Value = s.Log.Food ?? "—";
                aidSheet.Cell(row, 7).Value = s.Log.Drink ?? "—";
                aidSheet.Cell(row, 8).Value = s.Log.Notes ?? "—";
            }

            aidSheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            aidSheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            aidSheet.Columns().AdjustToContents();

            for (int col = 1; col <= 8; col++)
            {
                aidSheet.Column(col).Width += 2;
            }

            string folderPath = "exports";
            Directory.CreateDirectory(folderPath);
            string safeRunnerName = race.RunnerName.Replace(" ", "_");
            string safeRaceName = race.RaceName.Replace(" ", "_");
            string fileName = $"{safeRunnerName}-{safeRaceName}.xlsx";
            string fullPath = Path.Combine(folderPath, fileName);

            workbook.SaveAs(fullPath);
        }
    }
}
