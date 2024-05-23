using System.Text.RegularExpressions;

namespace FrbEditorOutputEvaluator
{
    class LineWithTime
    {
        public string Message { get; set; }
        public double ExecutionTime { get; set; }

        public override string ToString()
        {
            return $"{ExecutionTime} - {Message}";
        }
    }

    internal class Program
    {


        static void Main(string[] args)
        {
            var filepath = "C:\\Users\\Owner\\Documents\\GitHub\\DeadvivorsOGL\\Output.txt";

            var lines = System.IO.File.ReadAllLines(filepath);

            List<LineWithTime> lineTimes = new ();

            foreach(var line in lines)
            {
                var lineWithTime = ParseString(line);
                
                if(lineWithTime != null)
                {
                    lineTimes.Add(lineWithTime);
                }
            }

            lineTimes.Sort((a,b) => b.ExecutionTime.CompareTo(a.ExecutionTime));

            int m = 3;
        }



        static LineWithTime ParseString(string input)
        {
            LineWithTime toReturn = null;
            //string pattern = @"(?<=\d{2}:\d{2}:\d{2}\.)(\d+\.\d+)(.*)\s*(?<number>\d+\.\d+)?";
            string pattern = @".* - .* (\d+\.\d+)";

            // Create a Match object
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                // Extracting the time, string, and number
                var message = match.Groups[0].Value; // Message starting after the dash
                double? executionTime = double.Parse(match.Groups[1].Value); // Optional number


                if(executionTime != null)
                {

                    toReturn = new LineWithTime();
                    toReturn.ExecutionTime = executionTime.Value;
                    toReturn.Message = message;

                }
            }

            return toReturn;
        }
    }
}
