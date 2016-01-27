using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BuildServerUploaderConsole.Processes
{
    public class EmailResults
    {
        private readonly StringBuilder _bldr = new StringBuilder();

        public void WriteMessage(string message)
        {
            _bldr.AppendLine(message);
            System.Console.WriteLine(message);
        }

    }
}
