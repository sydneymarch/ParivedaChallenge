using api.Models;
using System.Text.Json;
using ClosedXML.Excel;

namespace api.Utilities
{
    public class RaceUtility
    {
        //update ETA for aid stations AFTER specific index
        public static void AdjustFutureAidStations(RunnerRace race, int fromIndex, TimeSpan thisDelay, double thisPaceAdjustment)
        {
            if (race == null || fromIndex < 0 || fromIndex >= race.AidStations.Count - 1) //check valid inputs
            {
                return; 
            }

            DateTime baseTime = race.AidStations[fromIndex].EstimatedArrival; //start from eta of given station

            for (int i = fromIndex + 1; i < race.AidStations.Count; i++)
            { //go through all stations onward
                AidStation current = race.AidStations[i];

                //apply pace adjustment to calc new eta
                double pace = current.PredictedPace + thisPaceAdjustment;
                double legMinutes = current.MilesFromLast * pace;
                TimeSpan legTime = TimeSpan.FromMinutes(legMinutes);

                if (i == fromIndex + 1)
                {
                    baseTime = baseTime.Add(legTime).Add(thisDelay); //only next staiton eta gets delay added
                }
                else
                {
                    baseTime = baseTime.Add(legTime); //no delay only leg time
                }

                current.EstimatedArrival = baseTime; // update ETA for current station
            }

            UpdateRaceStats(race); //recalc total stats
        }

        //recalc total distance and projected pace
        public static void UpdateRaceStats(RunnerRace race)
        {
            double totalDistance = 0;
            double totalPace = 0;
            int count = race.AidStations.Count;

            if (count == 0)
            { //no aidstations, everything 0
                race.TotalDistance = 0;
                race.ProjectedPace = 0;
                return;
            }

            double runningMiles = 0;

            for (int i = 0; i < count; i++)
            { //loop through all stations
                runningMiles += race.AidStations[i].MilesFromLast; //add leg miles to total for miles in
                race.AidStations[i].MilesIn = runningMiles; //updates miles in

                totalDistance += race.AidStations[i].MilesFromLast; //just one total distance
                totalPace += race.AidStations[i].PredictedPace; //just one total pace before averaged
            }

            race.TotalDistance = totalDistance;
            race.ProjectedPace = totalPace / count;
        }

        //racalc eta for all future aid stations
        public static void ResetFutureAidStations(RunnerRace race, int startIndex)
        {
            //start from eta of provided station
            DateTime baseTime = DateTime.SpecifyKind(race.AidStations[startIndex].EstimatedArrival, DateTimeKind.Local);

            for (int i = startIndex + 1; i < race.AidStations.Count; i++)
            { //loop through all stations onward
                //get pace and distance from previous aid station
                double pace = race.AidStations[i].PredictedPace;
                double miles = race.AidStations[i].MilesFromLast;

                //convert to minutes
                double minutes = pace * miles;

                //add segment time to base eta to get new eta
                baseTime = baseTime.AddMinutes(minutes);
                race.AidStations[i].EstimatedArrival = baseTime;
            }
            UpdateRaceStats(race); //update overall stats
        }

        public static void SaveRacesToDisk(List<RunnerRace> races)
        {
            string folderPath = "races";
            Directory.CreateDirectory(folderPath); //create folder if doesnt exist yet

            for (int i = 0; i < races.Count; i++)
            { //save each race to seperate file using safe file names
                // make filename safe by removing symbols
                string safeEmail = races[i].Email.Replace("@", "_at_").Replace(".", "_");
                string safeName = races[i].RaceName.Replace(" ", "_").ToLower();
                //create file path races/race_name-name_at_gmail_com.json
                string fileName = Path.Combine(folderPath, safeName + "-" + safeEmail + ".json");

                //convert race object to nice json
                string json = JsonSerializer.Serialize(races[i], new JsonSerializerOptions { WriteIndented = true });
                //write to json overwrite if exists
                File.WriteAllText(fileName, json);
            }
        }

        //loads all saved race json files from disk and returns as a list
        public static List<RunnerRace> LoadRacesFromDisk()
        {
            string folderPath = "races";
            List<RunnerRace> loadedRaces = new List<RunnerRace>();

            if (!Directory.Exists(folderPath)) return loadedRaces; //return empty list if no folder exists

            string[] files = Directory.GetFiles(folderPath, "*.json"); //get all json files from folder

            for (int i = 0; i < files.Length; i++)
            { //loop throuhg all files found
                //read file contents to runnerrace object
                string json = File.ReadAllText(files[i]);
                RunnerRace race = JsonSerializer.Deserialize<RunnerRace>(json);

                //add race to list if not null
                if (race != null) loadedRaces.Add(race);
            }

            return loadedRaces;
        }

        //converts a race into an Excel file with two sheets and returns the bytes for download
        public static byte[] ExportRaceToExcelToBytes(RunnerRace race)
        {
            XLWorkbook workbook = new XLWorkbook();

            //race summary sheet!!
            IXLWorksheet summarySheet = workbook.Worksheets.Add("Race Summary");

            string title = race.RunnerName + "'s " + race.RaceName + " 🏃💨";
            summarySheet.Cell(1, 1).Value = title;
            summarySheet.Range("A1:B1").Merge();
            summarySheet.Cell(1, 1).Style.Font.Bold = true;
            summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
            summarySheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.LightSkyBlue;
            summarySheet.Range("A1:B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            //key info runner runner name start time distance pace
            summarySheet.Cell(2, 1).Value = "Runner Name";
            summarySheet.Cell(2, 2).Value = race.RunnerName;
            summarySheet.Cell(3, 1).Value = "Race Name";
            summarySheet.Cell(3, 2).Value = race.RaceName;
            summarySheet.Cell(4, 1).Value = "Start Time";
            summarySheet.Cell(4, 2).Value = race.StartTime.ToLocalTime().ToString("t");
            summarySheet.Cell(5, 1).Value = "Total Distance";
            summarySheet.Cell(5, 2).Value = race.TotalDistance;
            summarySheet.Cell(6, 1).Value = "Projected Pace";
            summarySheet.Cell(6, 2).Value = race.ProjectedPace;

            summarySheet.Range("A2:A6").Style.Font.Bold = true;
            summarySheet.Columns("A", "B").AdjustToContents();

            //aid station breakdown
            IXLWorksheet aidSheet = workbook.Worksheets.Add("Aid Station Breakdown");

            string[] headers = {
                "Name 🏁", "Miles In 📏", "Pace 🕒", "ETA ⌚", "Arrival 🚶", "Food 🍞", "Drink 💧", "Notes 📝"
            };

            //header row
            int col = 0;
            while (col < headers.Length)
            {
                aidSheet.Cell(1, col + 1).Value = headers[col];
                col = col + 1;
            }

            aidSheet.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.LightGreen;
            aidSheet.Range("A1:H1").Style.Font.Bold = true;
            aidSheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int i = 0;
            while (i < race.AidStations.Count)
            { //fills each aid station row
                AidStation s = race.AidStations[i];
                int row = i + 2;

                aidSheet.Cell(row, 1).Value = s.Name;
                aidSheet.Cell(row, 2).Value = s.MilesIn;
                aidSheet.Cell(row, 3).Value = s.PredictedPace;
                aidSheet.Cell(row, 4).Value = s.EstimatedArrival.ToLocalTime().ToString("t");

                if (s.Log.ArrivalTime == DateTime.MinValue)
                { //show arrival time if logged otherwise show -
                    aidSheet.Cell(row, 5).Value = "—";
                }
                else
                {
                    aidSheet.Cell(row, 5).Value = s.Log.ArrivalTime.ToLocalTime().ToString("t");
                }

                if (s.Log.Food != null)
                { //same for food
                    aidSheet.Cell(row, 6).Value = s.Log.Food;
                }
                else
                {
                    aidSheet.Cell(row, 6).Value = "—";
                }

                if (s.Log.Drink != null)
                { //and drink
                    aidSheet.Cell(row, 7).Value = s.Log.Drink;
                }
                else
                {
                    aidSheet.Cell(row, 7).Value = "—";
                }

                if (s.Log.Notes != null)
                { //also notes
                    aidSheet.Cell(row, 8).Value = s.Log.Notes;
                }
                else
                {
                    aidSheet.Cell(row, 8).Value = "—";
                }

                i++;//increment to next station
            }

            //pretty!
            aidSheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            aidSheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            aidSheet.Columns().AdjustToContents();

            for (int c = 1; c <= 8; c = c + 1)
            {
                aidSheet.Column(c).Width = aidSheet.Column(c).Width + 5; //with padding
            }

            //save to memory and return bytes
            using (MemoryStream stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
   }
}