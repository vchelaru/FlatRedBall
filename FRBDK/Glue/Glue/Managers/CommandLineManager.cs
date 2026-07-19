using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Managers
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        const string ExitWhenQuietArg = "exitwhenquiet";
        const int DefaultExitWhenQuietSeconds = 5;

        string mProjectToLoad = null;

        public string ProjectToLoad
        {
            get
            {
                return mProjectToLoad;
            }
        }

        /// <summary>
        /// If set (via the "exitwhenquiet" or "exitwhenquiet=N" command line argument), Glue
        /// will automatically close itself once TaskManager has been idle (AreAllAsyncTasksDone)
        /// for this many seconds. Intended for automation - open a project, let it fully
        /// generate code (and any other queued work), then exit without user interaction.
        /// Null (the default) means this behavior is disabled.
        /// </summary>
        public int? ExitWhenQuietSeconds { get; private set; } = null;


        public CommandLineManager()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();


            foreach(string var in commandLineArgs)
            {
                if (var.Contains(".glux"))
                {
                    mProjectToLoad = var;
                    mProjectToLoad = mProjectToLoad.Replace(".glux", ".csproj");
                }
                if (var.Contains(".gluj"))
                {
                    mProjectToLoad = var;
                    mProjectToLoad = mProjectToLoad.Replace(".gluj", ".csproj");
                }
                if (var.Contains(".csproj"))
                {
                    mProjectToLoad = var;
                }
                if (var.Equals(ExitWhenQuietArg, StringComparison.OrdinalIgnoreCase))
                {
                    ExitWhenQuietSeconds = DefaultExitWhenQuietSeconds;
                }
                else if (var.StartsWith(ExitWhenQuietArg + "=", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(var.Substring((ExitWhenQuietArg + "=").Length), out var exitWhenQuietSeconds))
                {
                    ExitWhenQuietSeconds = exitWhenQuietSeconds;
                }
            }


        }
    }
}
