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
            if(element.IsICollidableRecursive() && GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasSetCollisionFromAnimation)
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

                            codeBlock.Line($"{nos.InstanceName}.SetCollisionFromAnimation(this, {createMissingShapes});");
                        }
                    }
                }
            }

            return codeBlock;
        }
    }
}
