using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue;
using FlatRedBall;
using FlatRedBall.Glue.Reflection;
using GlueViewOfficialPlugins.Scripting;

namespace GlueViewUnitTests
{
    class OverallInitializer
    {
        public static void Initialize()
        {
            if (ObjectFinder.Self.GlueProject == null)
            {
                ExposedVariableManager.Initialize();

                ObjectFinder.Self.GlueProject = new FlatRedBall.Glue.SaveClasses.GlueProjectSave();

                AvailableAssetTypes.Self.Initialize(FileManager.RelativeDirectory);


                // Perform a partial initialization of FRB:
                FlatRedBallServices.InitializeCommandLine();
                //SpriteManager.Initialize();

                SpriteManager.Camera.LeftDestination = 0;
                SpriteManager.Camera.RightDestination = 800;
                SpriteManager.Camera.TopDestination = 0;
                SpriteManager.Camera.BottomDestination = 640;


                // Do this after inintializing FlatRedBall
                GluxManager.ContentDirectory = FileManager.RelativeDirectory + "Content\\";
                GluxManager.ContentManagerName = "ContentManagerName";

                ScriptParsingPlugin.Self.StartUp();
            }

        }
    }
}
