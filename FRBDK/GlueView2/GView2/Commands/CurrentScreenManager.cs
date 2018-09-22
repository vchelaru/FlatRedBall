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
            ClearScreen();

            var lastSlash = screenName.LastIndexOf("\\");
            var unqualifiedName = screenName.Substring(lastSlash + 1);

            // convert this into a type:
            var types = CurrentAssembly.GetTypes();
            foreach(var type in types)
            {
                if(type.FullName.EndsWith($".Screens.{unqualifiedName}"))
                {
                    CurrentScreen =  (Screen)CurrentAssembly.CreateObject(type.FullName);
                    break;
                }
            }

            if(CurrentScreen != null)
            {
                FlatRedBall.IO.FileManager.RelativeDirectory = projectRootFolder.FullPath;
                CurrentScreen.Initialize(true);
            }
        }

        internal void ClearScreen()
        {
            if (CurrentScreen != null)
            {
                CurrentScreen.Destroy();
                CurrentScreen = null;
            }
        }
    }
}
