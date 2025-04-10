namespace Api
{
    public class RunnerFile
    {
        private string fileName;

        public RunnerFile(){
            this.fileName = "runners.txt";
        }

        public List<Runner> GetAllRunners(){
            List<Runner> runners = new List<Runner>();

            StreamReader inFile = new StreamReader(fileName);

            string line = inFile.ReadLine();
            while (line != null){
                string[] temp = line.Split('#');
                runners.Add(new Runner(){Name=temp[0], Email=temp[1], Phone=temp[2]});
                line = inFile.ReadLine();
            }

            inFile.Close();
            return runners;
        }
    }
}