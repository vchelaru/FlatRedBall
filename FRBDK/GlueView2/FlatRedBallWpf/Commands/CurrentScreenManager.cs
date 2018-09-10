using FlatRedBall.Screens;
using GlueView2.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.Commands
{
    public class CurrentScreenManager
    {
        public Assembly CurrentAssembly
        {
            get;
            private set;
        }

        FilePath projectRootFolder;

        public Screen CurrentScreen
        {
            get;
            private set;
        }



        public void HandleLoadedAssembly (Assembly newAssembly, FilePath root)
        {
            // todo - unload assembly?

            projectRootFolder = root;
            CurrentAssembly = newAssembly;
        }

        public void ShowScreen(string screenName)
        {
            if(CurrentScreen != null)
            {
                CurrentScreen.Destroy();
            }

            CurrentScreen =  (Screen)CurrentAssembly.CreateObject(screenName);

            FlatRedBall.IO.FileManager.RelativeDirectory = projectRootFolder.FullPath;
            CurrentScreen.Initialize(true);
        }
    }
}
