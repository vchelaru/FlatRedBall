using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.SpritePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainSpritePlugin : PluginBase
    {
        public override string FriendlyName => "Sprite Plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            ModifySpriteAti();

            // This should be early so the variable can be added before codegen:
            this.ReactToLoadedGluxEarly += HandleGluxLoaded;
        }

        private void HandleGluxLoaded()
        {
            var shouldHaveUseAnimationTextureFlip = 
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SpriteHasUseAnimationTextureFlip;

            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var existingUseAnimationTextureVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.UseAnimationTextureFlip));
            var existingIgnoreAnimationTextureFlipVariableDefinition = ati.VariableDefinitions
                .FirstOrDefault(item => item.Name == nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip));

            var doesAtiAlreadyHaveUseAnimationTextureFlip = existingUseAnimationTextureVariableDefinition != null;

            // Update the presence of the UseAnimationTextureFlip variable definition
            if(shouldHaveUseAnimationTextureFlip && !doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                var useAnimationTextureFlipVariableDefinition = new VariableDefinition();
                useAnimationTextureFlipVariableDefinition.Type = "bool";
                useAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.UseAnimationTextureFlip);
                useAnimationTextureFlipVariableDefinition.Category = "Animation";
                useAnimationTextureFlipVariableDefinition.DefaultValue = "true";

                var useAnimationRelativePositionVariableDefinition = ati.VariableDefinitions.FirstOrDefault(item =>
                    item.Name == nameof(FlatRedBall.Sprite.UseAnimationRelativePosition));
                if(useAnimationRelativePositionVariableDefinition != null)
                {
                    var indexOf = ati.VariableDefinitions.IndexOf(useAnimationRelativePositionVariableDefinition);

                    ati.VariableDefinitions.Insert(indexOf + 1, useAnimationTextureFlipVariableDefinition);

                }
                else
                {
                    ati.VariableDefinitions.Add(useAnimationTextureFlipVariableDefinition);
                }
            }
            else if(!shouldHaveUseAnimationTextureFlip && doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                ati.VariableDefinitions.Remove(existingUseAnimationTextureVariableDefinition);
            }

            if(shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition != null)
            {
                ati.VariableDefinitions.Remove(existingIgnoreAnimationTextureFlipVariableDefinition);
            }
            if(!shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition == null)
            {
                var ignoreAnimationTextureFlipVariableDefinition = new VariableDefinition();
                ignoreAnimationTextureFlipVariableDefinition.Type = "bool";
                ignoreAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip);
                ignoreAnimationTextureFlipVariableDefinition.Category = "Animation";
                ignoreAnimationTextureFlipVariableDefinition.DefaultValue = "false";

                ati.VariableDefinitions.Add(ignoreAnimationTextureFlipVariableDefinition);
            }
        }

        private static void ModifySpriteAti()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var textureVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Texture");
            if (textureVariable != null)
            {
                textureVariable.PreferredDisplayer = typeof(EditableComboBoxDisplay);
            }


        }
    }
}
