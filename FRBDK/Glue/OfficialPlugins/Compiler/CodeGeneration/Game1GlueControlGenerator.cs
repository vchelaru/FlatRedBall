using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
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
            if (IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line($"glueControlManager = new GlueControl.GlueControlManager({PortNumber});");
                codeBlock.Line("glueControlManager.Start();");
                codeBlock.Line("this.Exiting += (not, used) => glueControlManager.Kill();");


                // Vic says - We run all Glue commands before running custom initialize. The reason is - custom initialize
                // may make modifications to objects that are created by glue commands (such as assigning acceleration to objects
                // in a list), but it is unlikely that scripts will make modifications to objects created in CustomInitialize because
                // objects created in CustomInitialize cannot be modified by level editor.
                codeBlock.Line("FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += (newScreen) => ");
                var innerBlock = codeBlock.Block();
                innerBlock.Line("glueControlManager.ReRunAllGlueToGameCommands();");
                var isFirst = true;
                foreach(var entity in GlueState.Self.CurrentGlueProject.Entities)
                {
                    if(entity.CreatedByOtherEntities)
                    {
                        if(isFirst)
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
            }
        }
    }
}
