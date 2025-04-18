using System;
using System.Collections.Generic;
using System.IO;
using api.Models;

namespace Api
{
    public class RunnerFile
    {
        private string fileName;

        public RunnerFile()
        {
            this.fileName = "runners.txt"; // default file path
        }

        public List<Runner> GetAllRunners()
        {
            List<Runner> runners = new List<Runner>();

            try
            {
                if (!File.Exists(fileName)) return runners; // return empty list if file doesn't exist

                StreamReader inFile = new StreamReader(fileName);

                string line = inFile.ReadLine();
                while (line != null)
                {
                    string[] temp = line.Split('#'); // split line into fields
                    if (temp.Length == 3) // make sure all fields are there
                    {
                        runners.Add(new Runner()
                        {
                            Name = temp[0],
                            Email = temp[1],
                            Phone = temp[2]
                        });
                    }

                    line = inFile.ReadLine(); // go to next line
                }

                inFile.Close(); // close the file
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading runner file: {e.Message}"); // print error
            }

            return runners; // return the list
        }

        public void SaveRunner(Runner r)
        {
            try
            {
                StreamWriter outFile = File.AppendText(fileName); // open file for appending
                outFile.WriteLine($"{r.Name}#{r.Email}#{r.Phone}"); // write runner line
                outFile.Close(); // close file
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving runner: {e.Message}"); // show error if fail
            }
        }
    }
}
