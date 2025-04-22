using api.Models;
using api.Utilities;

namespace api.Services
{
    public class RaceService
    {
        //add new race or replace old one
        public static RunnerRace AddRace(List<RunnerRace> races, RunnerRace newRace)
        {
            for (int i = 0; i < races.Count; i++) //check if race already exists with email
            {
                if (races[i].Email == newRace.Email && races[i].RaceName == newRace.RaceName)
                {
                    //replace exisint gwith new one
                    races[i] = newRace;
                    RaceUtility.SaveRacesToDisk(races);
                    return newRace;
                }
            }

            //ensure initialized lists
            if (newRace.AidStations == null) newRace.AidStations = new List<AidStation>();
            if (newRace.SharedWithEmails == null) newRace.SharedWithEmails = new List<string>();
            //set start time to local time
            newRace.StartTime = DateTime.SpecifyKind(newRace.StartTime, DateTimeKind.Local);
            RecalculateTotals(newRace); //total distance and pace
            races.Add(newRace);
            RaceUtility.SaveRacesToDisk(races);
            return newRace;
        }

        //add new aid station to a race and updates ETA
        public static bool AddAidStation(List<RunnerRace> races, string email, AidStation station)
        {
            //find race by email
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null) return false;

            //add to the end of list
            race.AidStations.Add(station);
            int index = race.AidStations.Count - 1;

            //if this is the first station, set ETA using start time
            if (index == 0)
            {
                DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                race.AidStations[0].EstimatedArrival = localStart.AddMinutes(
                    station.MilesFromLast * station.PredictedPace
                );
            }
            else //otherwise use previous station ETA
            {
                DateTime previousETA = race.AidStations[index - 1].EstimatedArrival;
                double pace = station.PredictedPace;
                double minutes = station.MilesFromLast * pace;

                race.AidStations[index].EstimatedArrival = previousETA.AddMinutes(minutes);
            }
            //recalc everything downstream from this aid station
            RaceUtility.AdjustFutureAidStations(race, index, TimeSpan.Zero, 0);
            RecalculateTotals(race);
            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        //updates the nutrition log and adjust timing for future aid stations
        public static bool UpdateAidStationLog(List<RunnerRace> races, string email, int index, NutritionLog log, int delayShiftMinutes, double paceAdjustment)
        {
            //find race that matches the runners email
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false;

            //save the entered log and update delay/pace
            race.AidStations[index].Log = log;
            race.AidStations[index].DelayShiftMinutes = delayShiftMinutes;
            race.AidStations[index].PaceAdjustment = paceAdjustment;

            //convert delay to a timespan to be able to add to ETA
            TimeSpan delay = TimeSpan.FromMinutes(delayShiftMinutes);
            RaceUtility.AdjustFutureAidStations(race, index, delay, paceAdjustment); //recalc ETAs starting from this station
            RaceUtility.UpdateRaceStats(race); //update total distance and pace for race
            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        //update name distance and predicted pace of specific aid station
        public static bool UpdateAidStationInfo(List<RunnerRace> races, string email, int index, AidStation updated)
        {
            //find runner's race
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false;

            //overwrite specific station info with new values
            AidStation existing = race.AidStations[index];
            existing.Name = updated.Name;
            existing.MilesFromLast = updated.MilesFromLast;
            existing.PredictedPace = updated.PredictedPace;

            //recalculate total distance and average pace
            RecalculateTotals(race);

            if (index > 0 && race.AidStations[index - 1].EstimatedArrival != DateTime.MinValue)
            { //if station has valid previous ETA AND not the first station
                RaceUtility.ResetFutureAidStations(race, index - 1);
            }
            else
            { //otherwise reset from the start time
                DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                race.AidStations[0].EstimatedArrival = localStart.AddMinutes
                (race.AidStations[0].MilesFromLast * race.AidStations[0].PredictedPace);

                if (race.AidStations.Count > 1) //if more stations exist reset those too
                {
                    RaceUtility.ResetFutureAidStations(race, 0); //downstream stations
                }
            }

            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        //find first race that matches given email
        public static RunnerRace? FindRaceByEmail(List<RunnerRace> races, string email)
        {
            for (int i = 0; i < races.Count; i++) 
            {
                if (races[i].Email == email) return races[i]; //check if race belongs to given email
            }
            return null; //no match found
        }

        //finds the next aid station that hasn't been logged yet
        public static object GetNextAidStationSummary(List<RunnerRace> races, string email)
        {
            RunnerRace race = FindRaceByEmail(races, email); //get race by email
            if (race == null) return null;

            DateTime referenceTime = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local); //initilizing to start time so it has something

            for (int i = race.AidStations.Count - 1; i >= 0; i--)
            { //loop backward to find latest aid station that has actual arrival time/marker has been logged
                if (race.AidStations[i].Log.ArrivalTime != DateTime.MinValue)
                {
                    referenceTime = race.AidStations[i].Log.ArrivalTime;
                    break; //can calculate ETA from this point
                }

            for (int i = 0; i < race.AidStations.Count; i++)
            { //loop forward to find next aid station that hasn't been logged yet
                if (race.AidStations[i].Log.ArrivalTime == DateTime.MinValue)
                {
                    double estimatedMinutes = race.AidStations[i].MilesFromLast * race.AidStations[i].PredictedPace; //calc ETA
                    DateTime eta = referenceTime.AddMinutes(estimatedMinutes);
                    return new
                    { //checklist of items for headers in frontend to show with ETAs
                        Name = race.AidStations[i].Name,
                        Eta = eta,
                        Checklist = new List<string> { "food", "drink", "gear", "notes" }
                    };
                }
            }

            return "All aid stations complete!"; //will return this is all stations have been logged
        }

        //delete all races by email
        public static bool DeleteRacesByEmail(List<RunnerRace> races, string email)
        {
            bool found = false;
            for (int i = races.Count - 1; i >= 0; i--)
            { //backwards loop to avoid index issues and shifting
                if (races[i].Email == email)
                {
                    races.RemoveAt(i);
                    found = true;
                }
            }

            if (found) RaceUtility.SaveRacesToDisk(races); //only need to save if at least one was found
            return found;
        }

        //deletes one aid station by index and updates future ETAs
        public static bool DeleteAidStation(List<RunnerRace> races, string email, int index)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false; //check valid inputs

            race.AidStations.RemoveAt(index); //remove station form list
            RecalculateTotals(race); //update total distance and pace

            //recalc ETAs based on where deletion happened
            if (index == 0)
            { //if first station was deleted
                if (race.AidStations.Count > 0)
                { //reset ETAs from race start
                    DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                    race.AidStations[0].EstimatedArrival = localStart.AddMinutes(
                        race.AidStations[0].MilesFromLast * race.AidStations[0].PredictedPace
                    );

                    if (race.AidStations.Count > 1)
                    {
                        RaceUtility.ResetFutureAidStations(race, 0); //update ETA for all stations after first
                    }
                }
            }
            else if (index < race.AidStations.Count)
            { //deleted a middle station
                RaceUtility.ResetFutureAidStations(race, index - 1); //reset all stations ETAs onward
            }

            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        //updates race total distance and average pace
        public static void RecalculateTotals(RunnerRace race)
        {
            double totalMiles = 0;
            double totalPace = 0;
            int count = race.AidStations.Count;

            for (int i = 0; i < count; i++)
            { //loop through all aid stations
                totalMiles += race.AidStations[i].MilesFromLast;
                totalPace += race.AidStations[i].PredictedPace;
            }

            race.TotalDistance = totalMiles;
            if (count > 0)
            {
                race.ProjectedPace = totalPace / count;
            } else
            { //protect from divide by zero!!
                race.ProjectedPace = 0;
            }
        }
    }
}
