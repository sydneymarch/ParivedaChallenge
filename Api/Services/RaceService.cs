using api.Models;
using api.Utilities;

namespace api.Services
{
    public class RaceService
    {
        public static RunnerRace AddRace(List<RunnerRace> races, RunnerRace newRace)
        {
            for (int i = 0; i < races.Count; i++)
            {
                if (races[i].Email == newRace.Email && races[i].RaceName == newRace.RaceName)
                {
                    races[i] = newRace;
                    RaceUtility.SaveRacesToDisk(races);
                    return newRace;
                }
            }

            if (newRace.AidStations == null) newRace.AidStations = new List<AidStation>();
            if (newRace.SharedWithEmails == null) newRace.SharedWithEmails = new List<string>();
            newRace.StartTime = DateTime.SpecifyKind(newRace.StartTime, DateTimeKind.Local);
            RecalculateTotals(newRace);
            races.Add(newRace);
            RaceUtility.SaveRacesToDisk(races);
            return newRace;
        }

        public static bool AddAidStation(List<RunnerRace> races, string email, AidStation station)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null) return false;

            race.AidStations.Add(station);
            int index = race.AidStations.Count - 1;

            if (index == 0)
            {
                DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                race.AidStations[0].EstimatedArrival = localStart.AddMinutes(
                    station.MilesFromLast * station.PredictedPace
                );
            }
            else
            {
                DateTime previousETA = race.AidStations[index - 1].EstimatedArrival;
                double pace = station.PredictedPace;
                double minutes = station.MilesFromLast * pace;

                race.AidStations[index].EstimatedArrival = previousETA.AddMinutes(minutes);
            }
            RaceUtility.AdjustFutureAidStations(race, index, TimeSpan.Zero, 0);
            RecalculateTotals(race);
            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        public static bool UpdateAidStationLog(List<RunnerRace> races, string email, int index, NutritionLog log, int delayShiftMinutes, double paceAdjustment)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false;

            race.AidStations[index].Log = log;
            race.AidStations[index].DelayShiftMinutes = delayShiftMinutes;
            race.AidStations[index].PaceAdjustment = paceAdjustment;

            TimeSpan delay = TimeSpan.FromMinutes(delayShiftMinutes);
            RaceUtility.AdjustFutureAidStations(race, index, delay, paceAdjustment);

            RaceUtility.UpdateRaceStats(race);
            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        public static bool UpdateAidStationInfo(List<RunnerRace> races, string email, int index, AidStation updated)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false;

            AidStation existing = race.AidStations[index];
            existing.Name = updated.Name;
            existing.MilesFromLast = updated.MilesFromLast;
            existing.PredictedPace = updated.PredictedPace;

            RecalculateTotals(race);

            if (index > 0 && race.AidStations[index - 1].EstimatedArrival != DateTime.MinValue)
            {
                RaceUtility.ResetFutureAidStations(race, index - 1);
            }
            else
            {
                DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                race.AidStations[0].EstimatedArrival = localStart.AddMinutes(
                    race.AidStations[0].MilesFromLast * race.AidStations[0].PredictedPace
                );

                if (race.AidStations.Count > 1)
                {
                    RaceUtility.ResetFutureAidStations(race, 0);
                }
            }

            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        public static RunnerRace? FindRaceByEmail(List<RunnerRace> races, string email)
        {
            for (int i = 0; i < races.Count; i++)
            {
                if (races[i].Email == email) return races[i];
            }
            return null;
        }

        public static object GetNextAidStationSummary(List<RunnerRace> races, string email)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null) return null;

            DateTime referenceTime = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);

            for (int i = race.AidStations.Count - 1; i >= 0; i--)
            {
                if (race.AidStations[i].Log?.Food != null)
                {
                    referenceTime = race.AidStations[i].Log.ArrivalTime;
                    break;
                }
            }

            for (int i = 0; i < race.AidStations.Count; i++)
            {
                if (race.AidStations[i].Log?.Food == null)
                {
                    double estimatedMinutes = race.AidStations[i].MilesFromLast * race.AidStations[i].PredictedPace;
                    DateTime eta = referenceTime.AddMinutes(estimatedMinutes);
                    return new
                    {
                        Name = race.AidStations[i].Name,
                        Eta = eta,
                        Checklist = new List<string> { "food", "drink", "gear", "notes" }
                    };
                }
            }

            return "All aid stations complete!";
        }

        public static bool DeleteRacesByEmail(List<RunnerRace> races, string email)
        {
            bool found = false;
            for (int i = races.Count - 1; i >= 0; i--)
            {
                if (races[i].Email == email)
                {
                    races.RemoveAt(i);
                    found = true;
                }
            }

            if (found) RaceUtility.SaveRacesToDisk(races);
            return found;
        }

        public static bool DeleteAidStation(List<RunnerRace> races, string email, int index)
        {
            RunnerRace race = FindRaceByEmail(races, email);
            if (race == null || index < 0 || index >= race.AidStations.Count) return false;

            race.AidStations.RemoveAt(index);
            RecalculateTotals(race);

            // if we removed any station except the last, reset future ETAs
            if (index == 0)
            {
                if (race.AidStations.Count > 0)
                {
                    DateTime localStart = DateTime.SpecifyKind(race.StartTime, DateTimeKind.Local);
                    race.AidStations[0].EstimatedArrival = localStart.AddMinutes(
                        race.AidStations[0].MilesFromLast * race.AidStations[0].PredictedPace
                    );

                    if (race.AidStations.Count > 1)
                    {
                        RaceUtility.ResetFutureAidStations(race, 0);
                    }
                }
            }
            else if (index < race.AidStations.Count)
            {
                RaceUtility.ResetFutureAidStations(race, index - 1);
            }

            RaceUtility.SaveRacesToDisk(races);
            return true;
        }

        public static void RecalculateTotals(RunnerRace race)
        {
            double totalMiles = 0;
            double totalPace = 0;
            int count = race.AidStations.Count;

            for (int i = 0; i < count; i++)
            {
                totalMiles += race.AidStations[i].MilesFromLast;
                totalPace += race.AidStations[i].PredictedPace;
            }

            race.TotalDistance = totalMiles;
            race.ProjectedPace = count > 0 ? totalPace / count : 0;
        }
    }
}
