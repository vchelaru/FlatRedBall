using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.SpritePlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.SpritePlugin.CodeGenerators
{
    internal class SpriteCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            WriteSyncShapesFromAnimationCode(codeBlock, element);

            return codeBlock;
        }

        // Runs the same code while the game is paused in Glue's live-edit mode, where normal Activity()
        // (and therefore CustomActivity/this line) does not execute - see ScreenManager.IsInEditMode.
        // Without this, animation-driven shapes (collision or plain children) never track the current
        // frame while editing, only once the game is un-paused. The Sprite itself keeps animating in edit
        // mode regardless (SpriteManager's own per-frame update, not Activity), so this has a visible
        // effect. Same pattern as EntityPerformancePlugin's VariableActivityCodeGenerator.
        public override void GenerateActivityEditMode(ICodeBlock codeBlock, GlueElement element)
        {
            WriteSyncShapesFromAnimationCode(codeBlock, element);
        }

        private void WriteSyncShapesFromAnimationCode(ICodeBlock codeBlock, IElement element)
        {
            var fileVersion = GlueState.Self.CurrentGlueProject.FileVersion;

            // Starting with SpriteHasSyncShapesFromAnimation, the same checkbox works on any entity: shapes
            // are synced as children via Sprite.SyncShapesFromAnimation, which only adds a newly-created
            // shape to Collision when the container is ICollidable. Below that version, the checkbox still
            // requires ICollidable and calls the older Sprite.SetCollisionFromAnimation, which writes
            // directly into Collision - unchanged for projects that haven't upgraded.
            var usesSyncShapesFromAnimation = fileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation;
            var usesSetCollisionFromAnimation =
                !usesSyncShapesFromAnimation &&
                element.IsICollidableRecursive() &&
                fileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSetCollisionFromAnimation;

            if(usesSyncShapesFromAnimation || usesSetCollisionFromAnimation)
            {
                foreach(var nos in element.NamedObjects)
                {
                    var isSprite =
                        nos.SourceType == SourceType.FlatRedBallType && nos.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite;
                    if (isSprite)
                    {
                        var setsCollision =
                            nos.GetCustomVariable(AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name)?.Value as bool?;

                        if(setsCollision == true)
                        {
                            var createMissingShapes = nos.GetCustomVariable(AssetTypeInfoManager.GetCreateMissingShapesDefinition().Name)?.Value as bool? == true
                                ? "true" : "false";

                            var methodName = usesSyncShapesFromAnimation ? "SyncShapesFromAnimation" : "SetCollisionFromAnimation";
                            codeBlock.Line($"{nos.InstanceName}.{methodName}(this, {createMissingShapes});");
                        }
                    }
                }
            }
        }
    }
}
