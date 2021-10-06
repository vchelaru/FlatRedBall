using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Managers
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        string mProjectToLoad = null;

        public string ProjectToLoad
        {
            get
            {
                return mProjectToLoad;
            }
        }


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
            }


        }
    }
}
