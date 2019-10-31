using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleLogging
{
    public static class Logger
    {       
        public static void Log(string message, params object[] args)
        {
            File.AppendAllText("C:/flatredballprojects/tmxglue_log.txt",
                               string.Format("{0}: {1}\r\n", DateTime.Now.ToShortTimeString(), string.Format(message, args)));
        }
    }
}
