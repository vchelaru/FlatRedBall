using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.VSHelpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OfficialPlugins.ProfilePlugin.Manager
{
    class CodeItemAdder : Singleton<CodeItemAdder>
    {
        CodeBuildItemAdder adder;

        public CodeItemAdder()
        {
            adder = new CodeBuildItemAdder();
            adder.OutputFolderInProject = "Profile";
            adder.AddFileBehavior = AddFileBehavior.AlwaysCopy;

            adder.Add("OfficialPlugins/ProfilePlugin/EmbeddedCodeFiles/ProfileManagerBase.cs");
        }

        public void UpdateCodePresenceInProject()
        {
            adder.PerformAddAndSaveTask(Assembly.GetExecutingAssembly());
        }
    }
}
