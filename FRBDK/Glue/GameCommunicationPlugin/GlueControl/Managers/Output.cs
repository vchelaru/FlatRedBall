using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    public static class Output
    {
        static Action<string> PrintOutputFunc;
        public static void Initialize(Action<string> printOutputFunc)
        {
            PrintOutputFunc = printOutputFunc;
        }

        public static void Print(string whatToPrint) => PrintOutputFunc(whatToPrint);
    }
}
