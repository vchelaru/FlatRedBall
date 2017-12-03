using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace GumPlugin.Managers
{
    public class AppCommands : Singleton<AppCommands>
    {
        public void SaveGumx(bool saveAllElements = false)
        {
            string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

            GlueCommands.Self.FileCommands.IgnoreNextChangeOnFile(gumProjectFileName);
            AppState.Self.GumProjectSave.Save(gumProjectFileName, saveAllElements);
        }

        internal void AddScreen(ScreenSave gumScreen)
        {
            AppState.Self.GumProjectSave.Screens.Add(gumScreen);
            var elementReference = new ElementReference
            {
                ElementType = ElementType.Screen,
                Name = gumScreen.Name
            };
            AppState.Self.GumProjectSave.ScreenReferences.Add(elementReference);
            AppState.Self.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum screen " + gumScreen.Name);

        }

        internal void AddBehavior(BehaviorSave behavior)
        {
            AppState.Self.GumProjectSave.Behaviors.Add(behavior);
            var behaviorReference = new BehaviorReference
            {
                Name = behavior.Name
            };
            AppState.Self.GumProjectSave.BehaviorReferences.Add(behaviorReference);
            AppState.Self.GumProjectSave.BehaviorReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            FlatRedBall.Glue.Plugins.PluginManager.ReceiveOutput("Added Gum behavior " + behavior.Name);

        }

        internal void SaveScreen(ScreenSave gumScreen)
        {
            string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();

            var directory = FileManager.GetDirectory(gumProjectFileName) + ElementReference.ScreenSubfolder + "/";
            string screenFileName =
                directory + gumScreen.Name + "." + GumProjectSave.ScreenExtension;


            gumScreen.Save(screenFileName);

        }

        internal void SaveBehavior(BehaviorSave behavior)
        {
            string gumProjectFileName = GumProjectManager.Self.GetGumProjectFileName();
            var directory = FileManager.GetDirectory(gumProjectFileName) + BehaviorReference.Subfolder + "/";
            string behaviorFileName =
                directory + behavior.Name + "." + BehaviorReference.Extension;

            behavior.Save(behaviorFileName);

        }
    }
}
