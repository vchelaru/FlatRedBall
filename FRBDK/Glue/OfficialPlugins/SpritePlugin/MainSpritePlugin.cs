using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using OfficialPlugins.Common.Controls;
using OfficialPlugins.SpritePlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Text;
using WpfDataUi.Controls;
using WpfDataUiCore.Controls;

namespace OfficialPlugins.SpritePlugin
{
    [Export(typeof(PluginBase))]
    internal class MainSpritePlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Sprite Plugin";

        public override Version Version => new Version(1, 0);

        #endregion

        public override void StartUp()
        {
            AddSpriteColorAtiVariables();

            AddTextureCoordinateVariables();

            // This should be early so the variable can be added before codegen:
            this.ReactToLoadedGluxEarly += HandleGluxLoaded;
        }

        private void HandleGluxLoaded()
        {
            AdjustIgnoreAnimationVariables();

            //this.CreateAndAddTab(new TextureCoordinateSelectionView(), "Test");
        }

        private static void AdjustIgnoreAnimationVariables()
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
            if (shouldHaveUseAnimationTextureFlip && !doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                var useAnimationTextureFlipVariableDefinition = new VariableDefinition();
                useAnimationTextureFlipVariableDefinition.Type = "bool";
                useAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.UseAnimationTextureFlip);
                useAnimationTextureFlipVariableDefinition.Category = "Animation";
                useAnimationTextureFlipVariableDefinition.DefaultValue = "true";

                var useAnimationRelativePositionVariableDefinition = ati.VariableDefinitions.FirstOrDefault(item =>
                    item.Name == nameof(FlatRedBall.Sprite.UseAnimationRelativePosition));
                if (useAnimationRelativePositionVariableDefinition != null)
                {
                    var indexOf = ati.VariableDefinitions.IndexOf(useAnimationRelativePositionVariableDefinition);

                    ati.VariableDefinitions.Insert(indexOf + 1, useAnimationTextureFlipVariableDefinition);

                }
                else
                {
                    ati.VariableDefinitions.Add(useAnimationTextureFlipVariableDefinition);
                }
            }
            else if (!shouldHaveUseAnimationTextureFlip && doesAtiAlreadyHaveUseAnimationTextureFlip)
            {
                ati.VariableDefinitions.Remove(existingUseAnimationTextureVariableDefinition);
            }

            if (shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition != null)
            {
                ati.VariableDefinitions.Remove(existingIgnoreAnimationTextureFlipVariableDefinition);
            }
            if (!shouldHaveUseAnimationTextureFlip && existingIgnoreAnimationTextureFlipVariableDefinition == null)
            {
                var ignoreAnimationTextureFlipVariableDefinition = new VariableDefinition();
                ignoreAnimationTextureFlipVariableDefinition.Type = "bool";
                ignoreAnimationTextureFlipVariableDefinition.Name = nameof(FlatRedBall.Sprite.IgnoreAnimationChainTextureFlip);
                ignoreAnimationTextureFlipVariableDefinition.Category = "Animation";
                ignoreAnimationTextureFlipVariableDefinition.DefaultValue = "false";

                ati.VariableDefinitions.Add(ignoreAnimationTextureFlipVariableDefinition);
            }
        }

        private static void AddSpriteColorAtiVariables()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var textureVariable = ati.VariableDefinitions.FirstOrDefault(item => item.Name == "Texture");
            if (textureVariable != null)
            {
                textureVariable.PreferredDisplayer = typeof(EditableComboBoxDisplay);
            }

            var redVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Red");
            redVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            redVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;


            var greenVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Green");
            greenVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            greenVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueVariableDefinition = ati.VariableDefinitions.Find(item => item.Name == "Blue");
            blueVariableDefinition.PreferredDisplayer = typeof(SliderDisplay);
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DisplayedValueMultiplier)] = 255.0;
            blueVariableDefinition.PropertiesToSetOnDisplayer[nameof(SliderDisplay.DecimalPointsFromSlider)] = 0;

            var blueIndex = ati.VariableDefinitions.IndexOf(blueVariableDefinition);

            var colorHexValueDefinition = new VariableDefinition();
            colorHexValueDefinition.Name = "ColorHex";
            colorHexValueDefinition.Category = "Appearance";
            colorHexValueDefinition.DefaultValue = null;
            colorHexValueDefinition.Type = "string";
            colorHexValueDefinition.UsesCustomCodeGeneration = true;
            colorHexValueDefinition.PreferredDisplayer = typeof(ColorHexTextBox);
            colorHexValueDefinition.CustomVariableGet = ColorHexVariableGet;
            colorHexValueDefinition.CustomVariableSet = ColorHexVariableSet;
            ati.VariableDefinitions.Insert(blueIndex + 1, colorHexValueDefinition);

        }

        private void AddTextureCoordinateVariables()
        {
            var ati = AvailableAssetTypes.CommonAtis.Sprite;

            var mapSpriteTextureVariable = new VariableDefinition();
            mapSpriteTextureVariable.PreferredDisplayer = typeof(MapTextureButtonContainer);
            mapSpriteTextureVariable.UsesCustomCodeGeneration = true;
            mapSpriteTextureVariable.Type = "string"; // not used
            mapSpriteTextureVariable.Name = "MapSpriteTexturePlaceholder";
            mapSpriteTextureVariable.Category = "Texture";
            var variableToAddAfter = ati.VariableDefinitions.FirstOrDefault(item => item.Name == nameof(Sprite.Texture));
            var index = ati.VariableDefinitions.IndexOf(variableToAddAfter);
            ati.VariableDefinitions.Insert(index+1, mapSpriteTextureVariable);
        }

        private static void ColorHexVariableSet(GlueElement element, NamedObjectSave nos, string variableName, object newValue)
        {
            var colorConverter = new ColorConverter();
            var newValueAsString = newValue as string;
            if (!string.IsNullOrEmpty(newValueAsString))
            {
                if (!newValueAsString.StartsWith("#"))
                {
                    newValueAsString = "#" + newValueAsString;
                }
                try
                {
                    string redVariableName, greenVariableName, blueVariableName;
                    GetRedGreenBlueVariableNames(nos, variableName, out redVariableName, out greenVariableName, out blueVariableName);

                    if (!string.IsNullOrEmpty(redVariableName) && !string.IsNullOrEmpty(greenVariableName) &&
                        !string.IsNullOrEmpty(blueVariableName))
                    {
                        var color = (Color)colorConverter.ConvertFromString(newValueAsString);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, redVariableName, color.R / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, greenVariableName, color.G / 255.0f, performSaveAndGenerateCode: false, updateUi: false);
                        GlueCommands.Self.GluxCommands.SetVariableOn(nos, blueVariableName, color.B / 255.0f, performSaveAndGenerateCode: true, updateUi: true);
                    }
                }
                catch
                {
                    // do we want to do anything?
                }

            }
        }

        private static object ColorHexVariableGet(GlueElement element, NamedObjectSave nos, string variableName)
        {
            string redVariableName, greenVariableName, blueVariableName;
            GetRedGreenBlueVariableNames(nos, variableName, out redVariableName, out greenVariableName, out blueVariableName);

            if (!string.IsNullOrEmpty(redVariableName) && !string.IsNullOrEmpty(greenVariableName) &&
                !string.IsNullOrEmpty(blueVariableName))
            {
                var red = ((ObjectFinder.Self.GetValueRecursively(nos, element, redVariableName) as float?) ?? 0) * 255;
                var green = ((ObjectFinder.Self.GetValueRecursively(nos, element, greenVariableName) as float?) ?? 0) * 255;
                var blue = ((ObjectFinder.Self.GetValueRecursively(nos, element, blueVariableName) as float?) ?? 0) * 255;

                var redInt = MathFunctions.RoundToInt(red);
                var greenInt = MathFunctions.RoundToInt(green);
                var blueInt = MathFunctions.RoundToInt(blue);

                // source: https://stackoverflow.com/questions/39137486/converting-colour-name-to-hex-in-c-sharp
                var hexValue = $"{redInt:X2}{greenInt:X2}{blueInt:X2}";
                return hexValue;
            }


            return "";
        }

        private static void GetRedGreenBlueVariableNames(NamedObjectSave nos, string variableName, out string redVariableName, out string greenVariableName, out string blueVariableName)
        {
            var nosAti = nos.GetAssetTypeInfo();
            redVariableName = null;
            greenVariableName = null;
            blueVariableName = null;
            if (nosAti == AvailableAssetTypes.CommonAtis.Sprite)
            {
                redVariableName = "Red";
                greenVariableName = "Green";
                blueVariableName = "Blue";
            }
            else if (nos.SourceType == SourceType.Entity && variableName != null)
            {
                var entityType = ObjectFinder.Self.GetElement(nos);
                if (entityType != null)
                {
                    var foundVariable = entityType.CustomVariables.Find(item => item.Name == variableName);

                    if (foundVariable != null)
                    {
                        var objectInEntity = entityType.GetNamedObject(foundVariable.SourceObject);

                        if (objectInEntity?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite)
                        {
                            redVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Red")?.Name;
                            greenVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Green")?.Name;
                            blueVariableName = entityType.CustomVariables.FirstOrDefault(item => item.SourceObject == objectInEntity.InstanceName && item.SourceObjectProperty == "Blue")?.Name;

                        }
                    }

                }
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

}

