using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal class CameraSetupCodeGenerator
    {
        public static void CallSetupCamera(string gameFileName, bool whetherToCall)
        {
            if (!string.IsNullOrEmpty(gameFileName))
            {
                string contents = FileManager.FromFileText(GlueState.Self.CurrentGlueProjectDirectory + gameFileName);

                string whatToLookFor = "CameraSetup.SetupCamera(SpriteManager.Camera, graphics";

                string lineToReplaceWith = "CameraSetup.SetupCamera(SpriteManager.Camera, graphics);";

                if (whetherToCall)
                {
                    lineToReplaceWith = "\t\t\t" + lineToReplaceWith;
                }
                else
                {
                    lineToReplaceWith = "\t\t\t//" + lineToReplaceWith;
                }

                if (contents.Contains(whatToLookFor))
                {
                    // Only replace this if it's commented out:
                    int startOfLine;
                    int endOfLine;
                    StringFunctions.GetStartAndEndOfLineContaining(contents, "CameraSetup.SetupCamera", out startOfLine, out endOfLine);

                    string line = contents.Substring(startOfLine, endOfLine - startOfLine);

                    bool shouldReplace = line.Trim().StartsWith("//");
                    if (shouldReplace)
                    {
                        StringFunctions.ReplaceLine(ref contents, "CameraSetup.SetupCamera", lineToReplaceWith);
                    }
                }
                else
                {
                    // We gotta find where to put the start call.  This should be after 
                    // FlatRedBallServices.InitializeFlatRedBall

                    int index = CodeParser.GetIndexAfterFlatRedBallInitialize(contents);
                    contents = contents.Insert(index, lineToReplaceWith + Environment.NewLine);
                }

                FileManager.SaveText(contents, FileManager.RelativeDirectory + gameFileName);
            }
        }

        public static void UpdateOrAddCameraSetup()
        {
            
            
           string fileName = GlueState.Self.CurrentGlueProjectDirectory + @"Setup\CameraSetup.cs";

            string newContents = GetCameraSetupCsContents();


            FileManager.SaveText(newContents, fileName);

            // Now, verify that this thing is part of the project.
            bool wasAdded = ProjectManager.UpdateFileMembershipInProject(ProjectManager.ProjectBase, fileName, false, false);

            if (wasAdded)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        private static string GetCameraSetupCsContents()
        {
            string newContents = Resources.Resource1.CameraSetupTemplate;

            newContents = CodeWriter.ReplaceNamespace(newContents, ProjectManager.ProjectNamespace);

            ICodeBlock classContents = new CodeBlockBase(null);
            classContents.TabCount = 2;

            classContents.Line("// This is a generated file created by Glue. To change this file, edit the camera settings in Glue.");
            classContents.Line("// To access the camera settings, push the camera icon.");

            GenerateSetupCameraMethod(classContents);
            GenerateResetCameraMethod(classContents);

            StringFunctions.ReplaceLine(ref newContents, "// Generated Code:", classContents.ToString());
            return newContents;
        }

        private static void GenerateResetCameraMethod(ICodeBlock classContents)
        {
            ICodeBlock methodContents = classContents.Function(
                "internal static void", "ResetCamera", "Camera cameraToReset");

            methodContents.Line("cameraToReset.X = 0;");
            methodContents.Line("cameraToReset.Y = 0;");

            methodContents.Line("cameraToReset.XVelocity = 0;");
            methodContents.Line("cameraToReset.YVelocity = 0;");

            // We can't detach because by this point the Camera may already
            // be attached to an Entity:
            //methodContents.Line("cameraToReset.Detach();");
            // November 6, 2015
            // I wondered why we
            // didn't do this until
            // I saw this comment. I'm
            // probably not going to be 
            // the only one, so let's put
            // some info here:
            methodContents.Line("// Glue does not generate a detach call because the camera may be attached by this point");
            // I wondered "but why doesn't
            // the ResetCamera method get called
            // earlier, before the Camera has a chance
            // to attach itself to an entity. The reason
            // is because we can't do things to the camera
            // prior to AddToManagers because the camera may
            // be used in the previous screen if this screen is
            // loading async. 


        }

        private static void GenerateSetupCameraMethod(ICodeBlock classContents)
        {
            //internal static void SetupCamera(Camera cameraToSetUp, GraphicsDeviceManager graphicsDeviceManager)

            // for 360, which doesn't support optional parameters.
            ICodeBlock methodContents = classContents.Function(
                "internal static void", 
                "SetupCamera", 
                string.Format("Camera cameraToSetUp, GraphicsDeviceManager graphicsDeviceManager", 
                    ProjectManager.GlueProjectSave.ResolutionWidth,
                    ProjectManager.GlueProjectSave.ResolutionHeight));
            methodContents.Line(string.Format("SetupCamera(cameraToSetUp, graphicsDeviceManager, {0}, {1});", ProjectManager.GlueProjectSave.ResolutionWidth,
                    ProjectManager.GlueProjectSave.ResolutionHeight));



            methodContents = classContents.Function(
                "internal static void",
                "SetupCamera",
                "Camera cameraToSetUp, GraphicsDeviceManager graphicsDeviceManager, int width, int height");


            methodContents.TabCount = 3;

            AddSetResolutionCode(methodContents);

            AddUsePixelCoordinatesCode(methodContents);
        }

        private static void AddUsePixelCoordinatesCode(ICodeBlock methodContents)
        {
            if (ProjectManager.GlueProjectSave.In2D)
            {
                if (ProjectManager.GlueProjectSave.SetOrthogonalResolution)
                {
                    methodContents.Line(string.Format("cameraToSetUp.UsePixelCoordinates(false, {0}, {1});",
                                                      ProjectManager.GlueProjectSave.OrthogonalWidth,
                                                      ProjectManager.GlueProjectSave.OrthogonalHeight));
                }
                else
                {
                    methodContents.Line("cameraToSetUp.UsePixelCoordinates();");
                }
            }
        }

        private static void AddSetResolutionCode(ICodeBlock methodContents)
        {
            if (ProjectManager.GlueProjectSave.SetResolution)
            {
                bool pcOnlySetResolution = ProjectManager.GlueProjectSave.ApplyToFixedResolutionPlatforms == false;

                methodContents.Line("#if WINDOWS");

                if (ProjectManager.GlueProjectSave.RunFullscreen)
                {
                    methodContents.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(width, height);");

                }
                else
                {

                    methodContents.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(width, height);");
                }

                methodContents.Line("#elif IOS || ANDROID");

                // We used to not set the resolution on iOS/Android, but this makes the camera not use the full screen.
                // We want it to do that, so we'll do this:
                // Update November 12, 2015
                // Setting everything to fullscreen makes things render correctly, and also
                // hides the status UI on iOS, but we also need to set the resolution to the
                // native resolution for the cursor to work correctly:
                //methodContents.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(width, height);");
                methodContents.Line(
                    //"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(width, height);"
                    "FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight);"
                    );


                // closes the #if platform section
                methodContents.Line("#endif");

                methodContents.Line("#if WINDOWS_PHONE || WINDOWS_8 || IOS || ANDROID");


                var ifBlock = methodContents.If("height > width");
                {
                    ifBlock.Line("graphicsDeviceManager.SupportedOrientations = DisplayOrientation.Portrait;");
                }
                var elseBlock = ifBlock.End().Else();
                {
                    elseBlock.Line("graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;");
                }
                
                methodContents.Line("#endif");
            }
        }
    }
}
