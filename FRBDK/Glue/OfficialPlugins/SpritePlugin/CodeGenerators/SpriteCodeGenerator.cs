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
            var fileVersion = GlueState.Self.CurrentGlueProject.FileVersion;

            var generatesSetCollisionFromAnimation =
                element.IsICollidableRecursive() && fileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSetCollisionFromAnimation;
            var generatesSyncShapesFromAnimation =
                fileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSyncShapesFromAnimation;

            if(generatesSetCollisionFromAnimation || generatesSyncShapesFromAnimation)
            {
                foreach(var nos in element.NamedObjects)
                {
                    var isSprite =
                        nos.SourceType == SourceType.FlatRedBallType && nos.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite;
                    if (isSprite)
                    {
                        if(generatesSetCollisionFromAnimation)
                        {
                            var setsCollision =
                                nos.GetCustomVariable(AssetTypeInfoManager.GetSetCollisionFromAnimationVariableDefinition().Name)?.Value as bool?;

                            if(setsCollision == true)
                            {
                                var createMissingShapes = nos.GetCustomVariable(AssetTypeInfoManager.GetCreateMissingShapesDefinition().Name)?.Value as bool? == true
                                    ? "true" : "false";

                                codeBlock.Line($"{nos.InstanceName}.SetCollisionFromAnimation(this, {createMissingShapes});");
                            }
                        }

                        if(generatesSyncShapesFromAnimation)
                        {
                            var syncsShapes =
                                nos.GetCustomVariable(AssetTypeInfoManager.GetSyncShapesFromAnimationVariableDefinition().Name)?.Value as bool?;

                            if(syncsShapes == true)
                            {
                                var createMissingShapes = nos.GetCustomVariable(AssetTypeInfoManager.GetCreateMissingSyncedShapesDefinition().Name)?.Value as bool? == true
                                    ? "true" : "false";

                                codeBlock.Line($"{nos.InstanceName}.SyncShapesFromAnimation(this, {createMissingShapes});");
                            }
                        }
                    }
                }
            }

            return codeBlock;
        }
    }
}
