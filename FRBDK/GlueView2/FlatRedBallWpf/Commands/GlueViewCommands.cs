using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Screens;
using FlatRedBallWpf.ScriptLoading;
using GlueView2.AppState;
using GlueView2.IO;
using GlueView2.Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.Commands
{
    public class GlueViewCommands : Singleton<GlueViewCommands>
    {
        CurrentScreenManager currentScreenManager;
        public CurrentScreenManager CurrentScreenManager
        {
            get
            {
                if(currentScreenManager == null)
                {
                    currentScreenManager = new CurrentScreenManager();
                }

                return currentScreenManager;
            }
        }

        ScriptLoadingLogic scriptLoadingLogic;
        public ScriptLoadingLogic ScriptLoadingLogic
        {
            get
            {
                if(scriptLoadingLogic == null)
                {
                    scriptLoadingLogic = new ScriptLoadingLogic();
                }
                return scriptLoadingLogic;
            }
        }

        public GlueProjectSave GlueProject
        {
            get; private set;
        }

        public void LoadProject(FilePath filePath)
        {
            GlueViewState.Self.GlueProject = FileManager.XmlDeserialize<GlueProjectSave>(filePath.FullPath);


            var directory = filePath.GetDirectoryContainingThis();

            var loadedGameAssembly = ScriptLoadingLogic.LoadProjectCode(directory.FullPath);

            CurrentScreenManager.HandleLoadedAssembly(loadedGameAssembly, directory);
        }

        public void ShowScreen(string screenName)
        {

            CurrentScreenManager.ShowScreen(screenName);

        }
    }
}
