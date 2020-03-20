using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Scripting
{
    public class WaitAction : IScriptAction
    {
        public double TimeToWaitInSeconds { get; set; }

        double? timeStarted;

        public bool Execute()
        {
            timeStarted = TimeManager.CurrentScreenTime;
            return true;
        }

        public bool IsComplete()
        {
            return timeStarted != null &&
                TimeManager.CurrentScreenSecondsSince(timeStarted.Value) >
                TimeToWaitInSeconds;
        }
    }
}
