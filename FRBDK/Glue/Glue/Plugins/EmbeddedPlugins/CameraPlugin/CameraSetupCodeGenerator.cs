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
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal class CameraSetupCodeGenerator
    {
        public static void AddCameraSetupCall(string gameFileName, bool whetherToCall)
        {
            string contents = null;
            if (!string.IsNullOrEmpty(gameFileName))
            {

                GlueCommands.Self.TryMultipleTimes(() =>
                    contents = FileManager.FromFileText(GlueState.Self.CurrentGlueProjectDirectory + gameFileName));
            }

            if(!string.IsNullOrEmpty(contents))
            { 

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

                    if(index == -1)
                    {
                        GlueCommands.Self.PrintError("Could not find code in Game1.cs to add camera setup");
                    }
                    else
                    {
                        contents = contents.Insert(index, lineToReplaceWith + Environment.NewLine);
                    }

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

            GenerateCameraSetupData(namespaceContents);

            GenerateResizeBehaviorEnum(namespaceContents);

            GenerateWidthOrHeightEnum(namespaceContents);

            var classContents = namespaceContents.Class("internal static", "CameraSetup");

            classContents.Line("static Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager;");

            GenerateStaticCameraSetupData(classContents);

            GenerateResetMethodNew(displaySettings.GenerateDisplayCode, classContents);

            GenerateSetupCameraMethodNew(displaySettings.GenerateDisplayCode, classContents);

            GenerateResetWindow(displaySettings.GenerateDisplayCode, classContents);

            GenerateHandleResize(classContents);

            GenerateSetAspectRatio(classContents);

            return fileCode.ToString();
        }

        static List<string> excludedProperties = new List<string>
        {
            nameof(DisplaySettingsViewModel.ShowAspectRatioMismatch),
            nameof(DisplaySettingsViewModel.KeepResolutionHeightConstantMessage),
            nameof(DisplaySettingsViewModel.KeepResolutionWidthConstantMessage),
            nameof(DisplaySettingsViewModel.OnResizeUiVisibility),
            nameof(DisplaySettingsViewModel.AspectRatioValuesVisibility),
        };
        public static bool ShouldGenerateCodeWhenPropertyChanged(string propertyName)
        {
            if(excludedProperties.Contains(propertyName))
            {
                return false;
            }
            return true;
        }

        private static void GenerateResizeBehaviorEnum(CodeBlockNamespace namespaceContents)
        {
            var enumBlock = namespaceContents.Enum("public", "ResizeBehavior");
            enumBlock.Line("StretchVisibleArea,");
            enumBlock.Line("IncreaseVisibleArea");
        }

        public static void GenerateWidthOrHeightEnum(CodeBlockNamespace namespaceContents)
        {
            var enumBlock = namespaceContents.Enum("public", "WidthOrHeight");
            enumBlock.Line("Width,");
            enumBlock.Line("Height");
        }

        private static void GenerateStaticCameraSetupData(ICodeBlock classContents)
        {
            classContents.Line("public static CameraSetupData Data = new CameraSetupData");
            var block = classContents.Block();

            var displaySettings = GlueState.Self.CurrentGlueProject.DisplaySettings;

            block.Line($"Scale = {(displaySettings.Scale ).ToString(CultureInfo.InvariantCulture)}f,");
            block.Line($"ResolutionWidth = {displaySettings.ResolutionWidth},");
            block.Line($"ResolutionHeight = {displaySettings.ResolutionHeight},");
            block.Line($"Is2D = {displaySettings.Is2D.ToString().ToLowerInvariant()},");

            if(displaySettings.FixedAspectRatio)
            {
                decimal aspectRatioValue = 1;

                if(displaySettings.AspectRatioHeight != 0)
                {
                    aspectRatioValue = displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight;
                }
                block.Line($"AspectRatio = {aspectRatioValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}m,");
            }

            block.Line($"IsFullScreen = {displaySettings.RunInFullScreen.ToString().ToLowerInvariant()},");
            block.Line($"AllowWidowResizing = {displaySettings.AllowWindowResizing.ToString().ToLowerInvariant()},");
            block.Line($"ResizeBehavior = ResizeBehavior.{displaySettings.ResizeBehavior},");
            block.Line($"DominantInternalCoordinates = WidthOrHeight.{displaySettings.DominantInternalCoordinates},");
            classContents.Line(";");
        }

        private static void GenerateCameraSetupData(CodeBlockNamespace namespaceContents)
        {
            var classBlock = namespaceContents.Class("public", "CameraSetupData");

            classBlock.AutoProperty("public float", "Scale");
            classBlock.AutoProperty("public bool", "Is2D");
            classBlock.AutoProperty("public int", "ResolutionWidth");
            classBlock.AutoProperty("public int", "ResolutionHeight");
            classBlock.AutoProperty("public decimal?", "AspectRatio");
            classBlock.AutoProperty("public bool", "AllowWidowResizing");
            classBlock.AutoProperty("public bool", "IsFullScreen");
            classBlock.AutoProperty("public ResizeBehavior", "ResizeBehavior");
            classBlock.AutoProperty("public WidthOrHeight", "DominantInternalCoordinates");



            // set up here

        }

        private static void GenerateSetAspectRatio(ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "SetAspectRatioTo", 
                "decimal aspectRatio, WidthOrHeight dominantInternalCoordinates, int desiredWidth, int desiredHeight");
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

                ifBlock = functionBlock.If("dominantInternalCoordinates == WidthOrHeight.Height");
                {
                    ifBlock.Line("FlatRedBall.Camera.Main.OrthogonalHeight = desiredHeight;");
                    ifBlock.Line("FlatRedBall.Camera.Main.FixAspectRatioYConstant();");
                }
                elseBlock = ifBlock.End().Else();
                {
                    elseBlock.Line("FlatRedBall.Camera.Main.OrthogonalWidth = desiredWidth;");
                    elseBlock.Line("FlatRedBall.Camera.Main.FixAspectRatioXConstant();");
                }
            }
        }

        private static void GenerateHandleResize(ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "HandleResolutionChange", "object sender, System.EventArgs args");
            {
                functionBlock
                    .If("Data.AspectRatio != null")
                    .Line($"SetAspectRatioTo(Data.AspectRatio.Value, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);");

                functionBlock.If("Data.Is2D && Data.ResizeBehavior == ResizeBehavior.IncreaseVisibleArea")
                    .Line("FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / (Data.Scale/ 100.0f);")
                    .Line("FlatRedBall.Camera.Main.FixAspectRatioYConstant();");
            }
        }

        private static void GenerateSetupCameraMethodNew(bool generateDisplayCode, ICodeBlock classContents)
        {
            var methodContents = classContents.Function(
                "internal static void",
                "SetupCamera",
                $"Camera cameraToSetUp, Microsoft.Xna.Framework.GraphicsDeviceManager graphicsDeviceManager");

            if (generateDisplayCode)
            {
                methodContents.Line("CameraSetup.graphicsDeviceManager = graphicsDeviceManager;");
                methodContents.Line("ResetWindow();");
                methodContents.Line("ResetCamera(cameraToSetUp);");


                methodContents.Line(
                    $"FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += HandleResolutionChange;");
            }
        }

        private static void GenerateResetWindow(bool generateDisplayCode, ICodeBlock classContents)
        {
            var methodContents = classContents.Function(
                "internal static void",
                "ResetWindow",
                "");
            if(generateDisplayCode)
            {

                methodContents.Line("#if WINDOWS || DESKTOP_GL");

                // This needs to come before the fullscreen assignment, because if not it changes the border style
                methodContents.Line($"FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = Data.AllowWidowResizing;");

                string widthVariable = "Data.ResolutionWidth";
                string heightVariable = "Data.ResolutionHeight";
                var ifBlock = methodContents.If("Data.IsFullScreen");
                {

                    ifBlock.Line("#if DESKTOP_GL");

                    // We used to do this on WINDOWS too but that isn't stable so we use borderless
                    // Actually no, we should just use borderless everywhere so it works the same on all platforms:

                    bool useActualFullscreenOnDesktopGl = false;
                    if (useActualFullscreenOnDesktopGl)
                    {
                        ifBlock.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(Data.ResolutionWidth, Data.ResolutionHeight);");
                    }
                    else
                    {
                        // from here:
                        // http://community.monogame.net/t/how-to-implement-borderless-fullscreen-on-desktopgl-project/8359
                        ifBlock.Line("graphicsDeviceManager.HardwareModeSwitch = false;");

                        // If in fullscreen we want the widow to take up just the resolution of the screen:

                        ifBlock.Line(
                            "FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(" +
                            "Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, " +
                            "Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height, " +
                            "FlatRedBall.Graphics.WindowedFullscreenMode.FullscreenBorderless);");

                    }


                    ifBlock.Line("#elif WINDOWS");
                    ifBlock.Line("System.IntPtr hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;");
                    ifBlock.Line("var control = System.Windows.Forms.Control.FromHandle(hWnd);");
                    ifBlock.Line("var form = control.FindForm();");


                    ifBlock.Line("form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;");
                    ifBlock.Line("form.WindowState = System.Windows.Forms.FormWindowState.Maximized;");
                    ifBlock.Line("#endif");
                }
                var elseBlock = ifBlock.End().Else();
                {
                    //widthVariable = $"(int)({widthVariable} * Data.Scale/ 100.0f)";
                    //heightVariable = $"(int)({heightVariable} * Data.Scale/ 100.0f)";
                    //elseBlock.Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution({widthVariable}, {heightVariable});");

                    //var width = (int)(Data.ResolutionWidth * Data.Scale / 100.0f); 
                    //var height = (int)(Data.ResolutionHeight * Data.Scale / 100.0f);
                    elseBlock.Line($"var width = (int)({widthVariable} * Data.Scale / 100.0f);");
                    elseBlock.Line($"var height = (int)({heightVariable} * Data.Scale / 100.0f);");

                    elseBlock.Line("// subtract to leave room for windows borders");
                    elseBlock.Line("var maxWidth = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 6;");
                    elseBlock.Line("var maxHeight = Microsoft.Xna.Framework.Graphics.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 28;");

                    elseBlock.Line("width = System.Math.Min(width, maxWidth);");
                    elseBlock.Line("height = System.Math.Min(height, maxHeight);");

                    elseBlock.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(width, height);");

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

                methodContents
                    .If("Data.IsFullScreen")
                    .Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetFullScreen(Data.ResolutionWidth, Data.ResolutionHeight);")
                    .End()
                    .Else()
                    .Line($"FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution({widthVariable}, {heightVariable});")
                    .Line($"var newWindowSize = new Windows.Foundation.Size({widthVariable}, {heightVariable});")
                    .Line($"Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryResizeView(newWindowSize); ")
                    ;

                // closes the #if platform section
                methodContents.Line("#endif");
            }
        }

        private static void GenerateResetMethodNew(bool generateDisplayCode, ICodeBlock classContents)
        {
            var resetMethod = classContents.Function(
                "internal static void", "ResetCamera", "Camera cameraToReset = null");
            {
                if (generateDisplayCode)
                {
                    resetMethod.If("cameraToReset == null")
                        .Line("cameraToReset = FlatRedBall.Camera.Main;");

                    resetMethod.Line($"cameraToReset.Orthogonal = Data.Is2D;");
                    var ifStatement = resetMethod.If("Data.Is2D")
                        .Line($"cameraToReset.OrthogonalHeight = Data.ResolutionHeight;")
                        .Line($"cameraToReset.OrthogonalWidth = Data.ResolutionWidth;")
                        .Line($"cameraToReset.FixAspectRatioYConstant();");

                    ifStatement = resetMethod.If("Data.AspectRatio != null")
                        .Line(
                            $"SetAspectRatioTo(Data.AspectRatio.Value, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);");
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
