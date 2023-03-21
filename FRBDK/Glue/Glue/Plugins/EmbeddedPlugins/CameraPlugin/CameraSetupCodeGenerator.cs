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
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using Microsoft.Xna.Framework.Graphics;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal class CameraSetupCodeGenerator
    {
        #region Game1 Related code

        public static void GenerateCallInGame1(string gameFileName, bool whetherToCall)
        {
            var hasCameraCodeInGeneratedCode =
                 GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.StartupInGeneratedGame;

            if (hasCameraCodeInGeneratedCode)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
            }
            else
            {
                GenerateCameraSetupInGame1(gameFileName, whetherToCall);
            }
        }

        private static void GenerateCameraSetupInGame1(string gameFileName, bool whetherToCall)
        {
            string contents = null;
            if (!string.IsNullOrEmpty(gameFileName))
            {

                GlueCommands.Self.TryMultipleTimes(() =>
                    contents = FileManager.FromFileText(GlueState.Self.CurrentGlueProjectDirectory + gameFileName));
            }

            if (!string.IsNullOrEmpty(contents))
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

                    int index = CodeParser.GetIndexAfterBaseInitialize(contents);

                    if (index == -1)
                    {
                        GlueCommands.Self.PrintError("Could not find code in Game1.cs to add camera setup");
                    }
                    else
                    {
                        contents = contents.Insert(index, lineToReplaceWith + Environment.NewLine);
                    }

                }

                // load to see if it's changed, and only change it if so:
                var absoluteFile = new FilePath(FileManager.RelativeDirectory + gameFileName);

                var shouldSave = absoluteFile.Exists() == false;

                if (!shouldSave)
                {
                    var existingText = FileManager.FromFileText(absoluteFile.FullPath);
                    shouldSave = existingText != contents;
                }

                if (shouldSave)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        FileManager.SaveText(contents, absoluteFile.FullPath);
                    }, 5);
                }
            }
        }

        #endregion

        public static void UpdateOrAddCameraSetup()
        {
            TaskManager.Self.AddOrRunIfTasked(() =>
            {
                FilePath fileName;
                
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GeneratedCameraSetupFile)
                {
                    fileName = GlueState.Self.CurrentGlueProjectDirectory + @"Setup\CameraSetup.Generated.cs";
                }
                else
                {
                    fileName = GlueState.Self.CurrentGlueProjectDirectory + @"Setup\CameraSetup.cs";
                }

                string newContents = GetCameraSetupCsContents();

                GlueCommands.Self.TryMultipleTimes(() => FileManager.SaveText(newContents, fileName.FullPath), 5);

                // Now, verify that this thing is part of the project.
                bool wasAdded = GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, fileName, false, false);

                if (wasAdded)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }

            }, "Generating camera setup code", TaskExecutionPreference.AddOrMoveToEnd);
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

            GenerateCameraSetupDataClass(namespaceContents);

            GenerateResizeBehaviorEnum(namespaceContents);

            GenerateWidthOrHeightEnum(namespaceContents);

            var classContents = namespaceContents.Class("internal static", "CameraSetup");

            classContents.Line("public static Microsoft.Xna.Framework.GraphicsDeviceManager GraphicsDeviceManager { get; private set; }");

            GenerateStaticCameraSetupDefaults(classContents);

            GenerateResetMethodNew(displaySettings.GenerateDisplayCode, classContents);

            GenerateSetupCameraMethodNew(displaySettings.GenerateDisplayCode, classContents);

            GenerateResetWindow(displaySettings.GenerateDisplayCode, classContents);

            GenerateHandleResize(classContents);

            if(GetIfHasGumProject())
            {
                GenerateSetGumResolutionValues(classContents);
            }

            GenerateSetAspectRatio(classContents);

            GenerateKeepWindowOnTopCode(classContents);

            return fileCode.ToString();
        }

        private static void GenerateSetGumResolutionValues(ICodeBlock codeblock)
        {
            var functionBlock = codeblock.Function("public static void", "ResetGumResolutionValues", "");
            var gumIfIncreaseAreaBlock = functionBlock.If("Data.ResizeBehaviorGum == ResizeBehavior.IncreaseVisibleArea");

            //gumIfBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / (Data.Scale / 100.0f);");
            //gumIfBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasWidth = FlatRedBall.Camera.Main.DestinationRectangle.Width / (Data.Scale / 100.0f);");
            gumIfIncreaseAreaBlock.Line("global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = Data.Scale/100.0f;");

            // Don't use DefaultCanvasWidth and DefaultCanvasHeight, that wouldn't be responding to size:
            //gumIfIncreaseAreaBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasWidth = Gum.Managers.ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;");
            //gumIfIncreaseAreaBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasHeight = Gum.Managers.ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight; ");
            gumIfIncreaseAreaBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasWidth = FlatRedBall.Camera.Main.DestinationRectangle.Width;");
            gumIfIncreaseAreaBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height; ");


            var gumElseBlock = gumIfIncreaseAreaBlock.End().Else();

            gumElseBlock.Line("Gum.Wireframe.GraphicalUiElement.CanvasHeight = Data.ResolutionHeight / (Data.ScaleGum/100.0f);");
            var gumAspectRatio = gumElseBlock.If("Data.EffectiveAspectRatio != null")
                .Line(@"

                    if(Data.DominantInternalCoordinates == WidthOrHeight.Height)
                    {
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight = Data.ResolutionHeight / (Data.ScaleGum / 100.0f);
                        Gum.Wireframe.GraphicalUiElement.CanvasWidth = FlatRedBall.Math.MathFunctions.RoundToInt(Gum.Wireframe.GraphicalUiElement.CanvasHeight * (double)Data.EffectiveAspectRatio.Value);
                    }
                    else
                    {
                        Gum.Wireframe.GraphicalUiElement.CanvasWidth = Data.ResolutionWidth / (Data.ScaleGum/100.0f);
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight = FlatRedBall.Math.MathFunctions.RoundToInt(Gum.Wireframe.GraphicalUiElement.CanvasHeight / (double)Data.EffectiveAspectRatio.Value);
                    }                    

                    var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                    int destinationRectangleWidth;
                    int destinationRectangleHeight;
                    int x = 0;
                    int y = 0;
                    if (Data.EffectiveAspectRatio.Value > resolutionAspectRatio)
                    {
                        destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;
                        destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)Data.EffectiveAspectRatio.Value);
                    }
                    else
                    {
                        destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                        destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)Data.EffectiveAspectRatio.Value);
                    }

                    var canvasHeight = Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    var zoom = (float)destinationRectangleHeight / (float)Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    if(global::RenderingLibrary.SystemManagers.Default != null)
                    {
                        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;

                        foreach(var layer in global::RenderingLibrary.SystemManagers.Default.Renderer.Layers)
                        {
                            if(layer.LayerCameraSettings != null)
                            {
                                layer.LayerCameraSettings.Zoom = zoom;
                            }
                        }
                    }
                    
")
                .End().Else()
                .Line(@"

                    // since a fixed aspect ratio isn't specified, adjust the width according to the 
                    // current game aspect ratio and the canvas height
                    var currentAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (float)
                        FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;
                    Gum.Wireframe.GraphicalUiElement.CanvasWidth =
                        Gum.Wireframe.GraphicalUiElement.CanvasHeight * currentAspectRatio;

                    var graphicsHeight = Gum.Wireframe.GraphicalUiElement.CanvasHeight;
                    var windowHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height;
                    var zoom = windowHeight / (float)graphicsHeight;
                    if(global::RenderingLibrary.SystemManagers.Default != null)
                    {
                        global::RenderingLibrary.SystemManagers.Default.Renderer.Camera.Zoom = zoom;
                        foreach(var layer in global::RenderingLibrary.SystemManagers.Default.Renderer.Layers)
                        {
                            if(layer.LayerCameraSettings != null)
                            {
                                layer.LayerCameraSettings.Zoom = zoom;
                            }
                        }
                    }
                    ");


        }

        private static void GenerateKeepWindowOnTopCode(ICodeBlock codeBlock)
        {
            codeBlock.Line(@"
#if WINDOWS
        internal static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
        internal static readonly System.IntPtr HWND_NOTOPMOST = new System.IntPtr(-2);
        internal static readonly System.IntPtr HWND_TOP = new System.IntPtr(0);
        internal static readonly System.IntPtr HWND_BOTTOM = new System.IntPtr(1);
    
        [System.Flags]
        internal enum SetWindowPosFlags : uint
        {
            IgnoreMove = 0x0002,
            IgnoreResize = 0x0001,
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT

        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.DllImport(""user32.dll"")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetWindowRect(System.IntPtr hWnd, out RECT lpRect);

    
        [System.Runtime.InteropServices.DllImport(""user32.dll"", SetLastError = true)]
        internal static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        public static Microsoft.Xna.Framework.Rectangle GetWindowRectangle()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;

            GetWindowRect(hWnd, out RECT rectInner);

            return new Microsoft.Xna.Framework.Rectangle(
                rectInner.Left,
                rectInner.Top,
                rectInner.Right - rectInner.Left,
                rectInner.Bottom - rectInner.Top);

        }

        public static void SetWindowPosition(int x, int y)
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;
            SetWindowPos(
                hWnd,
                HWND_TOPMOST,
                x, y,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreResize
            );
        }

        public static void SetWindowAlwaysOnTop()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;
            SetWindowPos(
                hWnd,
                HWND_TOPMOST,
                0, 0,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize
            );
        }

        public static void UnsetWindowAlwaysOnTop()
        {
            var hWnd = FlatRedBall.FlatRedBallServices.Game.Window.Handle;

            SetWindowPos(
                hWnd,
                HWND_NOTOPMOST,
                0, 0,
                0, 0, //FlatRedBallServices.GraphicsOptions.ResolutionWidth, FlatRedBallServices.GraphicsOptions.ResolutionHeight,
                SetWindowPosFlags.IgnoreMove | SetWindowPosFlags.IgnoreResize
            );
        }
#else
        public static void SetWindowAlwaysOnTop()
            {
                // not supported on this platform, do nothing
            }

            public static void UnsetWindowAlwaysOnTop()
            {
                // not supported on this platform, do nothings
            }

#endif
");
        }

        static List<string> propertyChangesToIgnoreForCodeGeneration = new List<string>
        {
            nameof(DisplaySettingsViewModel.ShowAspectRatioMismatch),
            nameof(DisplaySettingsViewModel.KeepResolutionHeightConstantMessage),
            nameof(DisplaySettingsViewModel.KeepResolutionWidthConstantMessage),
            nameof(DisplaySettingsViewModel.OnResizeUiVisibility),
        };
        public static bool ShouldGenerateCodeWhenPropertyChanged(string propertyName)
        {
            if(propertyChangesToIgnoreForCodeGeneration.Contains(propertyName))
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

        private static void GenerateStaticCameraSetupDefaults(ICodeBlock classContents)
        {
            classContents.Line("public static CameraSetupData Data = new CameraSetupData");
            var block = classContents.Block();
            block.TabCount++;

            var displaySettings = GlueState.Self.CurrentGlueProject.DisplaySettings;

            block.Line($"Scale = {(displaySettings.Scale ).ToString(CultureInfo.InvariantCulture)}f,");
            block.Line($"IsGenerateCameraDisplayCodeEnabled = {(displaySettings.GenerateDisplayCode.ToString().ToLowerInvariant())},");

            block.Line($"ResolutionWidth = {displaySettings.ResolutionWidth},");
            block.Line($"ResolutionHeight = {displaySettings.ResolutionHeight},");
            block.Line($"Is2D = {displaySettings.Is2D.ToString().ToLowerInvariant()},");

            var showAspectRatio =
                displaySettings.AspectRatioBehavior == AspectRatioBehavior.FixedAspectRatio ||
                displaySettings.AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio;

            if(showAspectRatio)
            {
                decimal aspectRatioValue = 1;

                if(displaySettings.AspectRatioHeight != 0)
                {
                    aspectRatioValue = displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight;
                }
                block.Line($"AspectRatio = {aspectRatioValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}m,");
            }

            var showAspectRatio2 =
                displaySettings.AspectRatioBehavior == AspectRatioBehavior.RangedAspectRatio;

            if(showAspectRatio2)
            {

                decimal aspectRatioValue = 1;

                if (displaySettings.AspectRatioHeight2 != 0)
                {
                    aspectRatioValue = displaySettings.AspectRatioWidth2 / displaySettings.AspectRatioHeight2;
                }
                block.Line($"AspectRatio2 = {aspectRatioValue.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}m,");
            }

            block.Line($"IsFullScreen = {displaySettings.RunInFullScreen.ToString().ToLowerInvariant()},");
            block.Line($"AllowWindowResizing = {displaySettings.AllowWindowResizing.ToString().ToLowerInvariant()},");
            block.Line($"TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.{(TextureFilter)displaySettings.TextureFilter},");
            block.Line($"ResizeBehavior = ResizeBehavior.{displaySettings.ResizeBehavior},");

            if(GetIfHasGumProject())
            {
                var scalePercent = displaySettings.ScaleGum;
                if(scalePercent <=0)
                {
                    scalePercent = 100;
                }
                block.Line($"ScaleGum = {(scalePercent).ToString(CultureInfo.InvariantCulture)}f,");
                block.Line($"ResizeBehaviorGum = ResizeBehavior.{displaySettings.ResizeBehaviorGum},");
            }

            block.Line($"DominantInternalCoordinates = WidthOrHeight.{displaySettings.DominantInternalCoordinates},");
            classContents.Line(";");
        }

        private static void GenerateCameraSetupDataClass(CodeBlockNamespace namespaceContents)
        {
            var classBlock = namespaceContents.Class("public", "CameraSetupData");

            classBlock.AutoProperty("public bool", "IsGenerateCameraDisplayCodeEnabled");

            classBlock.AutoProperty("public float", "Scale");
            classBlock.AutoProperty("public float", "ScaleGum");
            classBlock.AutoProperty("public bool", "Is2D");
            classBlock.AutoProperty("public int", "ResolutionWidth");
            classBlock.AutoProperty("public int", "ResolutionHeight");
            classBlock.AutoProperty("public decimal?", "AspectRatio");
            classBlock.AutoProperty("public decimal?", "AspectRatio2");
            classBlock.AutoProperty("public bool", "AllowWindowResizing");
            classBlock.AutoProperty("public bool", "IsFullScreen");
            classBlock.AutoProperty("public ResizeBehavior", "ResizeBehavior");
            classBlock.AutoProperty("public ResizeBehavior", "ResizeBehaviorGum");
            classBlock.AutoProperty("public WidthOrHeight", "DominantInternalCoordinates");
            classBlock.AutoProperty("public Microsoft.Xna.Framework.Graphics.TextureFilter", "TextureFilter");

            classBlock.Line(@"
        public decimal? EffectiveAspectRatio
        {
            get
            {
                if(AspectRatio2 == null)
                {
                    return AspectRatio;
                }
                else if(AspectRatio == null)
                {
                    return AspectRatio2;
                }
                else if(FlatRedBall.FlatRedBallServices.ClientHeight == 0)
                {
                    // just in case:
                    return AspectRatio;
                }
                else
                {
                    // Neither AspectRatio nor 2 are null here

                    var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.ClientWidth / (decimal)FlatRedBall.FlatRedBallServices.ClientHeight;

                    var minAspect = System.Math.Min(AspectRatio.Value, AspectRatio2.Value);
                    var maxAspect = System.Math.Max(AspectRatio.Value, AspectRatio2.Value);

                    if(resolutionAspectRatio < minAspect)
                    {
                        return minAspect;
                    }
                    else if(resolutionAspectRatio > maxAspect)
                    {
                        return maxAspect;
                    }
                    else
                    {
                        // it's begween min and max, so return the resolution aspect ratio
                        return resolutionAspectRatio;
                    }
                }
            }
        }
");

            // set up here

        }

        private static void GenerateSetAspectRatio(ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "SetAspectRatioTo", 
                "decimal? aspectRatio, WidthOrHeight dominantInternalCoordinates, int desiredWidth, int desiredHeight");
            {
                functionBlock.Line("var resolutionAspectRatio = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth / (decimal)FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;");
                functionBlock.Line("int destinationRectangleWidth;");
                functionBlock.Line("int destinationRectangleHeight;");
                functionBlock.Line("int x = 0;");
                functionBlock.Line("int y = 0;");

                var ifBlock = functionBlock.If("aspectRatio == null");
                {
                    ifBlock.Line("destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;");
                    ifBlock.Line("destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;");
                }
                var elseIfBlock = ifBlock.End().ElseIf("aspectRatio > resolutionAspectRatio");
                {
                    elseIfBlock.Line("destinationRectangleWidth = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth;");
                    elseIfBlock.Line("destinationRectangleHeight = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleWidth / (float)aspectRatio);");

                    elseIfBlock.Line("y = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight - destinationRectangleHeight) / 2;");
                }
                var elseBlock = elseIfBlock.End().Else();
                {
                    elseBlock.Line("destinationRectangleHeight = FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionHeight;");
                    elseBlock.Line("destinationRectangleWidth = FlatRedBall.Math.MathFunctions.RoundToInt(destinationRectangleHeight * (float)aspectRatio);");

                    elseBlock.Line("x = (FlatRedBall.FlatRedBallServices.GraphicsOptions.ResolutionWidth - destinationRectangleWidth) / 2;");
                }

                var foreachBlock = functionBlock.ForEach("var camera in FlatRedBall.SpriteManager.Cameras");
                {

                    foreachBlock.Line("int currentX = x;");
                    foreachBlock.Line("int currentY = y;");
                    foreachBlock.Line("int currentWidth = destinationRectangleWidth;");
                    foreachBlock.Line("int currentHeight = destinationRectangleHeight;");

                    var switchBlock = foreachBlock.Switch("camera.CurrentSplitScreenViewport");
                    {
                        var caseBlock = switchBlock.Case("Camera.SplitScreenViewport.TopLeft")
                            .Line("currentWidth /= 2;")
                            .Line("currentHeight /= 2;");

                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.TopRight")
                            .Line("currentX = x + destinationRectangleWidth / 2;")
                            .Line("currentWidth /= 2;")
                            .Line("currentHeight /= 2;");


                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.BottomLeft")
                            .Line("currentY = y + destinationRectangleHeight / 2;")
                            .Line("currentWidth /= 2;")
                            .Line("currentHeight /= 2;");


                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.BottomRight")
                            .Line("currentX = x + destinationRectangleWidth / 2;")
                            .Line("currentY = y + destinationRectangleHeight / 2;")
                            .Line("currentWidth /= 2;")
                            .Line("currentHeight /= 2;");


                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.TopHalf")
                            .Line("currentHeight /= 2;");


                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.BottomHalf")
                            .Line("currentY = y + destinationRectangleHeight / 2;")
                            .Line("currentHeight /= 2;");


                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.LeftHalf")
                            .Line("currentWidth /= 2;");

                        caseBlock = switchBlock.Case("Camera.SplitScreenViewport.RightHalf")
                            .Line("currentX = x + destinationRectangleWidth / 2;")
                            .Line("currentWidth /= 2;");

                    }


                    foreachBlock.Line("camera.DestinationRectangle = new Microsoft.Xna.Framework.Rectangle(currentX, currentY, currentWidth, currentHeight);");

                    elseIfBlock = foreachBlock.If("dominantInternalCoordinates == WidthOrHeight.Height");
                    {
                        elseIfBlock.Line("camera.OrthogonalHeight = desiredHeight;");
                        elseIfBlock.Line("camera.FixAspectRatioYConstant();");
                    }
                    elseBlock = elseIfBlock.End().Else();
                    {
                        elseBlock.Line("camera.OrthogonalWidth = desiredWidth;");
                        elseBlock.Line("camera.FixAspectRatioXConstant();");
                    }

                }
            }
        }

        private static void GenerateHandleResize(ICodeBlock classContents)
        {
            var functionBlock = classContents.Function("private static void", "HandleResolutionChange", "object sender, System.EventArgs args");
            {
                functionBlock
                    .Line($"SetAspectRatioTo(Data.EffectiveAspectRatio, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);");

                functionBlock.If("Data.Is2D && Data.ResizeBehavior == ResizeBehavior.IncreaseVisibleArea")
                    .Line("FlatRedBall.Camera.Main.OrthogonalHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height / (Data.Scale/ 100.0f);")
                    .Line("FlatRedBall.Camera.Main.FixAspectRatioYConstant();");

                bool hasGumProject = GetIfHasGumProject();

                if (hasGumProject)
                {
                    functionBlock.Line("ResetGumResolutionValues();");
                }
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
                methodContents.Line("CameraSetup.GraphicsDeviceManager = graphicsDeviceManager;");
                methodContents.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.TextureFilter = Data.TextureFilter;");
                methodContents.Line("ResetWindow();");
                methodContents.Line("ResetCamera(cameraToSetUp);");

                if(GetIfHasGumProject())
                {
                    methodContents.Line("ResetGumResolutionValues();");
                }

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
                methodContents.Line($"FlatRedBall.FlatRedBallServices.Game.Window.AllowUserResizing = Data.AllowWindowResizing;");

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
                        ifBlock.Line("GraphicsDeviceManager.HardwareModeSwitch = false;");

                        // // If the window has been moved to the right, it will be partly off screen when fullscreen. To fix this, move it to the left first:
                        ifBlock.Line(
                            "FlatRedBall.FlatRedBallServices.Game.Window.Position = new Microsoft.Xna.Framework.Point(0,0);");


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

                    var innerIf = elseBlock.If("FlatRedBall.FlatRedBallServices.Game.Window.Position.Y < 25");
                    innerIf.Line("FlatRedBall.FlatRedBallServices.Game.Window.Position = new Microsoft.Xna.Framework.Point(FlatRedBall.FlatRedBallServices.Game.Window.Position.X, 25);");

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
            classContents.Line("/// Applies resolution and aspect ratio values to the FlatRedBall camera. If Gum is part of the project,");
            classContents.Line("/// then the Gum resolution will be applied. Note that this does not call Layout on the contained Gum objects,");
            classContents.Line("/// so this may need to be called explicitly if ResetCamera is called in custom code.");
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
                        .Line($"cameraToReset.FixAspectRatioYConstant();")
                    .End().Else()
                        .Line("cameraToReset.UsePixelCoordinates3D(0);")
                        .Line("var zoom = cameraToReset.DestinationRectangle.Height / (float)Data.ResolutionHeight;")
                        .Line("cameraToReset.Z /= zoom; ");

                    resetMethod.Line(
                            $"SetAspectRatioTo(Data.EffectiveAspectRatio, Data.DominantInternalCoordinates, Data.ResolutionWidth, Data.ResolutionHeight);");

                    if(GetIfHasGumProject())
                    {
                        resetMethod.Line("ResetGumResolutionValues();");
                    }
                }
            }
        }

        private static bool GetIfHasGumProject()
        {
            var gumProject = GlueState.Self.CurrentGlueProject
                .GetAllReferencedFiles()
                .FirstOrDefault(item => FlatRedBall.IO.FileManager.GetExtension(item.Name) == "gumx");
            var hasGumProject = gumProject != null;
            return hasGumProject;
        }

        #region Old Code
        private static string GetDisplaySetupOld(ICodeBlock classContents)
        {
            string fileContents = Resources.Resource1.CameraSetupTemplate;
            fileContents = CodeWriter.ReplaceNamespace(fileContents, ProjectManager.ProjectNamespace);


            GenerateSetupCameraMethodOld(classContents);
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

        private static void GenerateSetupCameraMethodOld(ICodeBlock classContents)
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
                    ifBlock.Line("GraphicsDeviceManager.SupportedOrientations = DisplayOrientation.Portrait;");
                }
                var elseBlock = ifBlock.End().Else();
                {
                    elseBlock.Line("GraphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;");
                }
                
                methodContents.Line("#endif");
            }
        }




#endregion
    }
}
