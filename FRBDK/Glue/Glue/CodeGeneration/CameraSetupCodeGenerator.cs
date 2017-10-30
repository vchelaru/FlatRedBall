using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System.Globalization;

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

                GlueCommands.Self.TryMultipleTimes(() =>
                    FileManager.SaveText(contents, FileManager.RelativeDirectory + gameFileName), 5);
            }
        }

        public static void UpdateOrAddCameraSetup()
        {
           string fileName = GlueState.Self.CurrentGlueProjectDirectory + @"Setup\CameraSetup.cs";

            string newContents = GetCameraSetupCsContents();

            GlueCommands.Self.TryMultipleTimes(()=>FileManager.SaveText(newContents, fileName), 5);

            // Now, verify that this thing is part of the project.
            bool wasAdded = GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, fileName, false, false);

            if (wasAdded)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        private static string GetCameraSetupCsContents()
        {
            bool shouldGenerateNew = GlueState.Self.CurrentGlueProject.DisplaySettings != null;

            string fileContents = null;

            ICodeBlock classContents = new CodeBlockBase(null);

            classContents.Line("// This is a generated file created by Glue. To change this file, edit the camera settings in Glue.");
            classContents.Line("// To access the camera settings, push the camera icon.");
            if(shouldGenerateNew == false)
            {
                classContents.TabCount = 2;
                fileContents = GetDisplaySetupOld(classContents);
            }
            else
            {
                fileContents = GetDisplaySetupNew(classContents);
            }
            return fileContents;
        }

        private static string GetDisplaySetupNew(ICodeBlock fileCode)
        {
            var displaySettings = GlueState.Self.CurrentGlueProject.DisplaySettings;


            fileCode.Line("using Camera = FlatRedBall.Camera;");

            var namespaceContents = fileCode.Namespace(ProjectManager.ProjectNamespace);
            var classContents = namespaceContents.Class("internal static", "CameraSetup");

            classContents.Line($"const float Scale = {(displaySettings.Scale / 100.0m).ToString(CultureInfo.InvariantCulture)}f;");

            GenerateResetMethodNew(displaySettings, classContents);

            GenerateSetupCameraMethodNew(displaySettings, classContents);

            GenerateHandleResize(displaySettings, classContents);

            GenerateSetAspectRatio(displaySettings, classContents);

            return fileCode.ToString();
        }

        private static void GenerateSetAspectRatio(DisplaySettings displaySettings, ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "SetAspectRatioTo", "decimal aspectRatio");
            {
                functionBlock.Line("var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;");
                functionBlock.Line("int destinationRectangleWidth;");
                functionBlock.Line("int destinationRectangleHeight;");
                functionBlock.Line("int x = 0;");
                functionBlock.Line("int y = 0;");

                var ifBlock = functionBlock.If("aspectRatio > resolutionAspectRatio");
                {
                    ifBlock.Line("destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;");
                    ifBlock.Line("destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)aspectRatio);");

                    ifBlock.Line("y = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight - destinationRectangleHeight) / 2;");
                }
                var elseBlock = ifBlock.End().Else();
                {
                    elseBlock.Line("destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;");
                    elseBlock.Line("destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)aspectRatio);");

                    elseBlock.Line("x = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth - destinationRectangleWidth) / 2;");
                }

                functionBlock.Line("FlatRedBall.Camera.Main.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(x, y, destinationRectangleWidth, destinationRectangleHeight);");
                functionBlock.Line("FlatRedBall.Camera.Main.FixAspectRatioYConstant();");
            }
        }

        private static void GenerateHandleResize(DisplaySettings displaySettings, ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "HandleResolutionChange", "object sender, System.EventArgs args");
            {
                functionBlock.Line($"SetAspectRatioTo({displaySettings.AspectRatioWidth.ToString(CultureInfo.InvariantCulture)} / {displaySettings.AspectRatioHeight.ToString(CultureInfo.InvariantCulture)}m);");

                if(displaySettings.Is2D && displaySettings.ResizeBehavior == ResizeBehavior.IncreaseVisibleArea)
                {
                    functionBlock.Line("FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / Scale;");
                    functionBlock.Line("FlatRedBall.Camera.Main.FixAspectRatioYConstant();");


                }
            }
        }

        private static void GenerateSetupCameraMethodNew(DisplaySettings displaySettings, ICodeBlock classContents)
        {
            var methodContents = classContents.Function(
                "internal static void",
                "SetupCamera",
                $"Camera cameraToSetUp, Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager, int width = {displaySettings.ResolutionWidth}, int height = {displaySettings.ResolutionHeight}");

            if (displaySettings.GenerateDisplayCode)
            {


                methodContents.Line("#if WINDOWS || DESKTOP_GL");

                // This needs to come before the fullscreen assignment, because if not it changes the border style
                methodContents.Line($"FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = {displaySettings.AllowWindowResizing.ToString().ToLowerInvariant()};");

                string widthVariable = "width";
                string heightVariable = "height";
                if (displaySettings.RunInFullScreen)
                {
                    methodContents.Line("#if DESKTOP_GL");

                    // We used to do this on WINDOWS too but that isn't stable so we use borderless
                    methodContents.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(width, height);");
                    methodContents.Line("#elif WINDOWS");
                    methodContents.Line("System.IntPtr hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;");
                    methodContents.Line("var control = System.Windows.Forms.Control.FromHandle(hWnd);");
                    methodContents.Line("var form = control.FindForm();");


                    methodContents.Line("form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;");
                    methodContents.Line("form.WindowState = System.Windows.Forms.FormWindowState.Maximized;");
                    methodContents.Line("#endif");
                }
                else
                {
                    if (displaySettings.Scale != 100)
                    {
                        widthVariable = $"(int)({widthVariable} * Scale)";
                        heightVariable = $"(int)({heightVariable} * Scale)";
                    }
                    methodContents.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution({widthVariable}, {heightVariable});");
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
                methodContents.Line("#elif UWP");
                if (displaySettings.RunInFullScreen)
                {
                    methodContents.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(width, height);");
                }
                else
                {
                    methodContents.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution({widthVariable}, {heightVariable});");
                }
                // closes the #if platform section
                methodContents.Line("#endif");


                methodContents.Line("ResetCamera(cameraToSetUp);");

                if (displaySettings.FixedAspectRatio)
                {
                    methodContents.Line(
                        $"FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleResolutionChange;");
                }
            }
        }

        private static void GenerateResetMethodNew(SaveClasses.DisplaySettings displaySettings, ICodeBlock classContents)
        {
            var resetMethod = classContents.Function(
                "internal static void", "ResetCamera", "Camera cameraToReset");
            {
                if (displaySettings.GenerateDisplayCode)
                {
                    if (displaySettings.Is2D)
                    {
                        resetMethod.Line($"FlatRedBall.Camera.Main.Orthogonal = true;");
                        resetMethod.Line($"FlatRedBall.Camera.Main.OrthogonalHeight = {displaySettings.ResolutionHeight};");
                        resetMethod.Line($"FlatRedBall.Camera.Main.OrthogonalWidth = {displaySettings.ResolutionWidth};");

                        // Even though we reset the camera, we want to make sure the aspect ratio matches the destination rect...
                        // Because the user may not have forced an aspect ratio setting in the settings:
                        resetMethod.Line($"FlatRedBall.Camera.Main.FixAspectRatioYConstant();");
                    }

                    if(displaySettings.FixedAspectRatio)
                    {
                        resetMethod.Line(
                            $"SetAspectRatioTo({displaySettings.AspectRatioWidth.ToString(CultureInfo.InvariantCulture)} / {displaySettings.AspectRatioHeight.ToString(CultureInfo.InvariantCulture)}m);");
                    }
                }
            }
        }

#region Old Code
        private static string GetDisplaySetupOld(ICodeBlock classContents)
        {
            string fileContents = Resources.Resource1.CameraSetupTemplate;
            fileContents = CodeWriter.ReplaceNamespace(fileContents, ProjectManager.ProjectNamespace);


            GenerateSetupCameraMethod(classContents);
            GenerateResetCameraMethod(classContents);

            StringFunctions.ReplaceLine(ref fileContents, "// Generated Code:", classContents.ToString());
            return fileContents;
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




#endregion
    }
}
