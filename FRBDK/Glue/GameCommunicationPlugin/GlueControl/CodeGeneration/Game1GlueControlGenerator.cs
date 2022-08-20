using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration
{
    public class Game1GlueControlGenerator : Game1CodeGenerator
    {
        public bool IsGlueControlManagerGenerationEnabled { get; set; }
        public int PortNumber { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line("GlueControl.GlueControlManager glueControlManager;");
            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            GenerateCameraSetup(codeBlock);

            GenerateGlueControlManagerInitialize(codeBlock);

            GenerateStartScreen(codeBlock);
        }

        private void GenerateCameraSetup(ICodeBlock codeBlock)
        {
            if(HasStartupInGeneratedGame)
            {
                codeBlock.Line("var args = System.Environment.GetCommandLineArgs();");
                codeBlock.Line("bool? changeResize = null;");
                codeBlock.Line("var resizeArgs = args.FirstOrDefault(item => item.StartsWith(\"AllowWindowResizing=\"));");
                codeBlock.Line("if (!string.IsNullOrEmpty(resizeArgs))");
                codeBlock.Line("{");
                codeBlock.Line("    var afterEqual = resizeArgs.Split('=')[1];");
                codeBlock.Line("    changeResize = bool.Parse(afterEqual);");
                codeBlock.Line("}");
                codeBlock.Line("if (changeResize != null)");
                codeBlock.Line("{");
                codeBlock.Line("    CameraSetup.Data.AllowWindowResizing = changeResize.Value;");
                codeBlock.Line("}");

                codeBlock.Line("CameraSetup.SetupCamera(FlatRedBall.Camera.Main, graphics);");
            }
        }

        string GetStartupScreenUnqualified()
        {
            ScreenSave requiredScreen = null;

            var project = GlueState.Self.CurrentGlueProject;
            for (int i = 0; i < project.Screens.Count; i++)
            {
                ScreenSave screenSave = project.Screens[i];

                if (screenSave.IsRequiredAtStartup)
                {
                    requiredScreen = screenSave;
                    break;
                }
            }

            var screenName = requiredScreen?.Name ?? project.StartUpScreen;

            return screenName;
        }

        bool HasStartupInGeneratedGame =>
            GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.StartupInGeneratedGame;

        private void GenerateStartScreen(ICodeBlock codeBlock)
        {
            if(HasStartupInGeneratedGame)
            {
                var project = GlueState.Self.CurrentGlueProject;
                var startUpScreen = GetStartupScreenUnqualified();
                if(!string.IsNullOrEmpty(startUpScreen))
                {
                    startUpScreen = GlueState.Self.ProjectNamespace + "." + startUpScreen.Replace("\\", ".");
                    codeBlock.Line($"System.Type startScreenType = typeof({startUpScreen});");

                }
                else
                {
                    codeBlock.Line("System.Type startScreenType = null;");
                }

                codeBlock.Line("var commandLineArgs = System.Environment.GetCommandLineArgs();");
                var ifBlock = codeBlock.If("commandLineArgs.Length > 0");
                {
                    ifBlock.Line("var thisAssembly = this.GetType().Assembly;");
                    ifBlock.Line("// see if any of these are screens:");
                    var foreachBlock = ifBlock.ForEach("var item in commandLineArgs");
                    {
                        foreachBlock.Line("var type = thisAssembly.GetType(item);");

                        var innerIf = foreachBlock.If("type != null");
                        {
                            innerIf.Line("startScreenType = type;");
                            innerIf.Line("break;");
                        }
                    }
                }

                var startScreenIf = codeBlock.If("startScreenType != null");
                {
                    startScreenIf.Line("FlatRedBall.Screens.ScreenManager.Start(startScreenType);");
                }
            }
        }

        private void GenerateGlueControlManagerInitialize(ICodeBlock codeBlock)
        {
            if (IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line($"glueControlManager = new GlueControl.GlueControlManager({PortNumber});");
                //codeBlock.Line("glueControlManager.Start();");
                //codeBlock.Line("this.Exiting += (not, used) => glueControlManager.Kill();");
                codeBlock.Line("FlatRedBall.FlatRedBallServices.GraphicsOptions.SizeOrOrientationChanged += (not, used) =>");
                var sizeChangedInnerBlock = codeBlock.Block();
                var sizeChangedInnerBlockIf = sizeChangedInnerBlock.If("FlatRedBall.Screens.ScreenManager.IsInEditMode");
                sizeChangedInnerBlockIf.Line("GlueControl.Editing.CameraLogic.UpdateCameraToZoomLevel(zoomAroundCursorPosition: false);");
                sizeChangedInnerBlock.Line("GlueControl.Editing.CameraLogic.PushZoomLevelToEditor();");
                codeBlock.Line(";");

                // Vic says - We run all Glue commands before running custom initialize. The reason is - custom initialize
                // may make modifications to objects that are created by glue commands (such as assigning acceleration to objects
                // in a list), but it is unlikely that scripts will make modifications to objects created in CustomInitialize because
                // objects created in CustomInitialize cannot be modified by level editor.
                codeBlock.Line("FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += (newScreen) => ");
                var innerBlock = codeBlock.Block();
                innerBlock.Line("glueControlManager.ReRunAllGlueToGameCommands();");
                var isFirst = true;
                foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
                {
                    if (entity.CreatedByOtherEntities && !entity.IsAbstract)
                    {
                        if (isFirst)
                        {
                            innerBlock.Line("// These get nulled out when screens are destroyed so we have to re-assign them");
                        }
                        // this has a factory, so we should += the 
                        // turns out we don't qualify factories in namespaces.....well maybe we should but not going to bother with that now.
                        // If this ever changes, we'll have to tie it to a new gluj version.
                        var entityClassName = entity.ClassName;
                        innerBlock.Line($"Factories.{entityClassName.Replace("\\", ".").Replace("/", ".")}Factory.EntitySpawned += (newEntity) =>  GlueControl.InstanceLogic.Self.ApplyEditorCommandsToNewEntity(newEntity);");
                        isFirst = false;
                    }
                }
                codeBlock.Line(";");

                codeBlock.Line("FlatRedBall.Screens.ScreenManager.AfterScreenDestroyed += (screen) =>");
                var innerDestroyBlock = codeBlock.Block();
                innerDestroyBlock.Line("GlueControl.Editing.EditorVisuals.DestroyContainedObjects();");

                codeBlock.Line(";");
            }
        }
    }
}
