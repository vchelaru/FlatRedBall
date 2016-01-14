using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Reflection;

namespace UnitTests
{
    class OverallInitializer
    {
        public static void Initialize()
        {
            if (ObjectFinder.Self.GlueProject == null)
            {
                
                ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave();
                ProjectManager.Initialize();
#if TEST
                ProjectManager.ProjectBase = new TestProject();
#endif
                FlatRedBall.Instructions.InstructionManager.Initialize();

                AvailableAssetTypes.Self.Initialize("ContentTypes.csv", FlatRedBall.IO.FileManager.CurrentDirectory);
                ExposedVariableManager.Initialize();

                // Just to initialize:
                var throwaway = EditorData.FileAssociationSettings; 
            }

        }
    }
}
