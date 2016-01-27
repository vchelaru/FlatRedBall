using System;

namespace BuildServerUploaderConsole.Processes
{
    public class TraceResults : IResults
    {
        public void WriteMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void Send()
        {
        }
    }
}
