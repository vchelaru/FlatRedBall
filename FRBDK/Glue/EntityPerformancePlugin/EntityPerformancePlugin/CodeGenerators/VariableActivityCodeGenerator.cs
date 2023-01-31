using EntityPerformancePlugin.Models;
using FlatRedBall;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityPerformancePlugin.CodeGenerators
{
    class VariableActivityCodeGenerator : ElementComponentCodeGenerator
    {
        #region Fields/Properties

        public ProjectManagementValues Values { get; set; }

        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        #endregion

        #region Activity

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            WriteLiveUpdateCode(codeBlock, element as GlueElement);

            return codeBlock;
        }

        public override void GenerateActivityEditMode(ICodeBlock codeBlock, GlueElement element)
        {
            WriteLiveUpdateCode(codeBlock, element);

            // If this contains any entity lists, and those entities are not actively managed, then we need to call update on them:
            foreach(var listNos in element.NamedObjects.Where(item => item.IsList))
            {
                var entity = ObjectFinder.Self.GetEntitySave(listNos.SourceClassGenericType);

                if(entity != null && entity.IsManuallyUpdated)
                {
                    var foreachInner = codeBlock.ForEach($"var item in {listNos.FieldName}");
                    foreachInner.Line("item.ActivityEditMode();");
                }
            }
        }

        private void WriteLiveUpdateCode(ICodeBlock codeBlock, GlueElement element)
        {
            EntityManagementValues managementValues = Values?.EntityManagementValueList?.FirstOrDefault(item => item.Name == element.Name);

            if (managementValues != null)
            {
                var entitySave = element as EntitySave;
                if (entitySave != null && managementValues.PropertyManagementMode == Enums.PropertyManagementMode.SelectManagedProperties)
                {
                    GenerateVariableActivityFor(codeBlock, entitySave, managementValues.SelectedProperties);
                }

                foreach (var namedObject in element.AllNamedObjects)
                {

                    var isManuallyUpdated = namedObject.IsManuallyUpdated;

                    InstanceManagementValues instanceManagementValues = null;

                    if (isManuallyUpdated)
                    {
                        instanceManagementValues = managementValues.InstanceManagementValuesList.FirstOrDefault(item => item.Name == namedObject.InstanceName);
                    }

                    var shouldGenerateForThisObject =
                        // Don't generate if it's a container, use the main entity's values
                        namedObject.IsContainer == false &&
                        instanceManagementValues?.SelectedProperties?.Count > 0;

                    if (shouldGenerateForThisObject)
                    {
                        GenerateVariableActivityFor(codeBlock, namedObject, instanceManagementValues);
                    }
                }
            }
        }

        private void GenerateVariableActivityFor(ICodeBlock codeBlock, EntitySave element, List<string> selectedProperties)
        {
            var name = "this";

            var assetTypeInfo = element.GetAssetTypeInfo();

            GenerateActivityFor(codeBlock, selectedProperties, name, assetTypeInfo);

            var needsVisualUpdateCallsInActivity = element.IsManuallyUpdated &&
                selectedProperties.Contains("Attachment") == false &&
                selectedProperties.Any();

            if(needsVisualUpdateCallsInActivity)
            {
                var ati = element.GetAssetTypeInfo();

                if (ati == AvailableAssetTypes.CommonAtis.Text)
                {
                    GenerateTextVariableUpdateDependenciesFor(codeBlock, name, selectedProperties);
                }
                else if (ati == AvailableAssetTypes.CommonAtis.Sprite)
                {
                    GenerateSpriteVariableUpdateDependenciesFor(codeBlock, name, selectedProperties);
                }
            }
        }

        private void GenerateVariableActivityFor(ICodeBlock codeBlock, NamedObjectSave namedObject, InstanceManagementValues instanceManagementValues)
        {
            var name = namedObject.FieldName;

            var assetTypeInfo = namedObject.GetAssetTypeInfo();

            GenerateActivityFor(codeBlock, instanceManagementValues.SelectedProperties, name, assetTypeInfo);
        }

        private void GenerateActivityFor(ICodeBlock codeBlock, List<string> properties, string name, AssetTypeInfo assetTypeInfo)
        {
            if (IsPositionedObject(assetTypeInfo))
            {
                GeneratePositionedObjectVariableActivityFor(codeBlock, name, properties);
            }

            if (IsIColorable(assetTypeInfo))
            {
                GenerateIColorableVariableActivityFor(codeBlock, name, properties);
            }

            if (IsIInstructable(assetTypeInfo))
            {
                GenerateIInstructableVariableActivityFor(codeBlock, name, properties);
            }

            if (IsText(assetTypeInfo))
            {
                GenerateTextVariableActivityFor(codeBlock, name, properties);
            }

            if (IsAnimationChainAnimatable(assetTypeInfo))
            {
                GenerateIAnimationChainAnimatableActivityFor(codeBlock, name, properties);
            }

            if (IsIScalable(assetTypeInfo))
            {
                GenerateIScalableActivityFor(codeBlock, name, properties);
            }
        }
        #region PositionedObject Activity

        private void GeneratePositionedObjectVariableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.X, false);
            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.Y, false);
            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.Z, false);

            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.X, true);
            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.Y, true);
            GenerateVelocityAccelerationDragCodeForAxis(codeBlock, name, properties, Axis.Z, true);


            GenerateRotationVelocityCode(codeBlock, name, properties, false);
            GenerateRotationVelocityCode(codeBlock, name, properties, true);
        }

        private static void GenerateVelocityAccelerationDragCodeForAxis(ICodeBlock codeBlock, string name, List<string> properties, Axis axis, bool isRelative)
        {
            string prefix = null;
            if(isRelative)
            {
                prefix = "Relative";
            }
            if (properties.Contains($"{prefix}{axis}Velocity"))
            {
                if (properties.Contains($"{prefix}{axis}Acceleration"))
                {
                    codeBlock.Line(
                        $"{name}.{prefix}Position.{axis} += {name}.{prefix}Velocity.{axis} * FlatRedBall.TimeManager.SecondDifference + {name}.{prefix}Acceleration.{axis} * FlatRedBall.TimeManager.SecondDifferenceSquaredDividedByTwo;");

                    codeBlock.Line($"{name}.{prefix}Velocity.{axis} += {name}.{prefix}Acceleration.{axis} * FlatRedBall.TimeManager.SecondDifference;");
                }
                else
                {
                    codeBlock.Line($"{name}.{prefix}Position.{axis} += {name}.{prefix}Velocity.{axis} * FlatRedBall.TimeManager.SecondDifference;");
                }

                // Drag does not apply to relative values
                if(properties.Contains($"Drag") && isRelative == false)
                {
                    codeBlock.Line($"{name}.Velocity.{axis} -= {name}.Velocity.{axis} * {name}.Drag * FlatRedBall.TimeManager.SecondDifference;");
                }
            }
        }

        private void GenerateRotationVelocityCode(ICodeBlock codeBlock, string name, List<string> properties, bool isRelative)
        {
            string prefix = null;
            if (isRelative)
            {
                prefix = "Relative";
            }

            var hasRotationXVelocity = properties.Contains($"{prefix}{nameof(PositionedObject.RotationXVelocity)}");
            var hasRotationYVelocity = properties.Contains($"{prefix}{nameof(PositionedObject.RotationYVelocity)}");
            var hasRotationZVelocity = properties.Contains($"{prefix}{nameof(PositionedObject.RotationZVelocity)}");


            if (hasRotationXVelocity)
            {
                codeBlock.Line(
                    $"{name}.{prefix}RotationX += {name}.{prefix}RotationXVelocity * FlatRedBall.TimeManager.SecondDifference;");
            }
            if (hasRotationYVelocity)
            {
                codeBlock.Line(
                    $"{name}.{prefix}RotationY += {name}.{prefix}RotationYVelocity * FlatRedBall.TimeManager.SecondDifference;");
            }
            if (hasRotationZVelocity)
            {
                codeBlock.Line(
                    $"{name}.{prefix}RotationZ += {name}.{prefix}RotationZVelocity * FlatRedBall.TimeManager.SecondDifference;");
            }
        }

        #endregion

        #region IColorableActivity

        private void GenerateIColorableVariableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            if(properties.Contains(nameof(IColorable.RedRate)))
            {
                var line = $"{name}.Red += {name}.RedRate * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
            if (properties.Contains(nameof(IColorable.GreenRate)))
            {
                var line = $"{name}.Green += {name}.GreenRate * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
            if (properties.Contains(nameof(IColorable.BlueRate)))
            {
                var line = $"{name}.Blue += {name}.BlueRate * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
            if (properties.Contains(nameof(IColorable.AlphaRate)))
            {
                var line = $"{name}.Alpha += {name}.AlphaRate * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
        }

        #endregion

        #region IInstructable Activity

        private void GenerateIInstructableVariableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            if(properties.Contains("Instruction Execution"))
            {
                codeBlock.Line(
                    $"FlatRedBall.Instructions.InstructionManager.ExecuteInstructionsOnConsideringTime({name});");
            }
        }

        #endregion

        #region Text Activity

        private void GenerateTextVariableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            if (properties.Contains(nameof(Text.ScaleVelocity)))
            {
                var line = $"{name}.Scale += {name}.ScaleVelocity;";
                codeBlock.Line(line);
            }

            if(properties.Contains(nameof(Text.SpacingVelocity)))
            { 
                var line = $"{name}.Spacing += {name}.SpacingVelocity;";
                codeBlock.Line(line);
            }
        }

        #endregion

        #region IAnimationChainAnimatableActivity

        private void GenerateIAnimationChainAnimatableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            if (properties.Contains(nameof(IAnimationChainAnimatable.Animate)))
            {
                // for now assume sprite:
                var line = $"{name}.AnimateSelf(FlatRedBall.TimeManager.CurrentTime);";
                codeBlock.Line(line);
            }
        }

        #endregion

        #region IScalableActivity

        private void GenerateIScalableActivityFor(ICodeBlock codeBlock, string name, List<string> properties)
        {
            if(properties.Contains(nameof(IScalable.ScaleXVelocity)))
            {
                var line = $"{name}.ScaleX += {name}.ScaleXVelocity * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
            if (properties.Contains(nameof(IScalable.ScaleYVelocity)))
            {
                var line = $"{name}.ScaleY += {name}.ScaleYVelocity * FlatRedBall.TimeManager.SecondDifference;";
                codeBlock.Line(line);
            }
        }

        #endregion

        #endregion

        #region UpdateDependencies

        public override void GenerateUpdateDependencies(ICodeBlock codeBlock, IElement element)
        {
            if (element is EntitySave)
            {
                GenerateEntitySaveUpdateDependencies(codeBlock, element);
            }
            else if(element is ScreenSave)
            {
                GenerateScreenSaveUpdateDependencies(codeBlock, element);
            }
        }

        private void GenerateScreenSaveUpdateDependencies(ICodeBlock codeBlock, IElement element)
        {

            // Only do top level and lists, not contents of lists, or else they might get update dependencies called 2 times
            //foreach (var namedObject in element.AllNamedObjects)
            foreach (var namedObject in element.NamedObjects)
            {
                var isEntity = namedObject.SourceType == SourceType.Entity;

                EntitySave entity = null;

                bool isList = false;

                if (isEntity)
                {
                    entity = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassType);
                }
                else if(namedObject.SourceType == SourceType.FlatRedBallType && namedObject.IsList)
                {
                    var classType = namedObject.SourceClassGenericType;

                    entity = ObjectFinder.Self.GetEntitySave(classType);

                    if(entity != null)
                    {
                        isList = true;
                    }
                }
                if(entity != null)
                {
                    EntityManagementValues managementValues = 
                        Values?.EntityManagementValueList?.FirstOrDefault(item => item.Name == entity.Name);

                    bool shouldCallUpdateDependencies = managementValues != null &&
                        managementValues.SelectedProperties.Contains("Attachment");

                    if(shouldCallUpdateDependencies)
                    {
                        if(isList)
                        {
                            var forBlock = codeBlock.For($"int i = 0; i < {namedObject.FieldName}.Count; i++");
                            forBlock.Line($"{namedObject.FieldName}[i].UpdateDependencies(currentTime);");
                        }
                        else
                        {
                            codeBlock.Line($"{namedObject.FieldName}.UpdateDependencies(currentTime);");
                        }
                    }


                }
            }
        }

        private void GenerateEntitySaveUpdateDependencies(ICodeBlock codeBlock, IElement element)
        {
            EntityManagementValues managementValues = Values?.EntityManagementValueList?.FirstOrDefault(item => item.Name == element.Name);

            if (managementValues != null)
            {
                var innerCodeBlock = new CodeBlockBase(null);

                GenerateEntityUpdateDependencies(innerCodeBlock, element, managementValues);

                GenerateNamedObjectUpdateDependencies(innerCodeBlock, element, managementValues);

                var doAnyNamedObjectsHaveUpdateDependencies = innerCodeBlock.BodyCodeLines.Any();



                if (doAnyNamedObjectsHaveUpdateDependencies)
                {
                    var ifBlock = codeBlock.If("this.LastDependencyUpdate != currentTime");
                    // todo - need to call base here if we actually want to update dependencies
                    var shouldCallBase =
                        managementValues.PropertyManagementMode == Enums.PropertyManagementMode.FullyManaged ||
                        managementValues.SelectedProperties.Contains("Attachment");

                    if (shouldCallBase)
                    {
                        ifBlock.Line("base.UpdateDependencies(currentTime);");
                    }

                    ifBlock.Line("this.mLastDependencyUpdate = currentTime;");

                    ifBlock.InsertBlock(innerCodeBlock);
                }
            }
        }

        private void GenerateEntityUpdateDependencies(CodeBlockBase codeBlock, IElement element, EntityManagementValues managementValues)
        {
            var ati = element.GetAssetTypeInfo();

            string name = "this";

            if (ati?.RuntimeTypeName == "Text")
            {
                GenerateTextVariableUpdateDependenciesFor(codeBlock, name, managementValues.SelectedProperties);
            }
            else if (ati?.RuntimeTypeName == "Sprite")
            {
                GenerateSpriteVariableUpdateDependenciesFor(codeBlock, name, managementValues.SelectedProperties);
            }
        }

        private void GenerateNamedObjectUpdateDependencies(ICodeBlock codeBlock, IElement element, EntityManagementValues managementValues)
        {
            foreach (var namedObject in element.AllNamedObjects)
            {
                var isManuallyUpdated = namedObject.IsManuallyUpdated;

                InstanceManagementValues instanceManagementValues = null;

                if (isManuallyUpdated)
                {
                    instanceManagementValues = managementValues.InstanceManagementValuesList.FirstOrDefault(item => item.Name == namedObject.InstanceName);
                }

                var shouldGenerateUpdateDependenciesForObject =
                    namedObject.IsContainer == false &&
                    instanceManagementValues?.SelectedProperties?.Count > 0;

                if (shouldGenerateUpdateDependenciesForObject)
                {
                    GenerateVariableUpdateDependenciesFor(codeBlock, namedObject, instanceManagementValues);
                }
            }
        }

        private void GenerateVariableUpdateDependenciesFor(ICodeBlock codeBlock, NamedObjectSave namedObject, InstanceManagementValues instanceManagementValues)
        {
            var name = namedObject.FieldName;

            var ati = namedObject.GetAssetTypeInfo();

            GeneratePositionedObjectVariableUpdateDependenciesFor(codeBlock, name, instanceManagementValues);

            if(ati == AvailableAssetTypes.CommonAtis.Text)
            {
                GenerateTextVariableUpdateDependenciesFor(codeBlock, name, instanceManagementValues.SelectedProperties);
            }
            else if(ati == AvailableAssetTypes.CommonAtis.Sprite)
            {
                GenerateSpriteVariableUpdateDependenciesFor(codeBlock, name, instanceManagementValues.SelectedProperties);
            }

        }

        private void GenerateTextVariableUpdateDependenciesFor(ICodeBlock codeBlock, string name, List<string> managementValues)
        {
            // If we got here then we know the text has at least *something* that it 
            // wants updated. If it's already doing attachments, then we don't have to 
            // do anything here. If it's not doing attachments but has some properties that
            // it cares about, then we'll call the more-efficient UpdateInternalRenderingVariables
            if (managementValues.Contains("Attachment") == false)
            {
                codeBlock.Line($"{name}.UpdateInternalRenderingVariables();");
            }

        }

        private void GenerateSpriteVariableUpdateDependenciesFor(ICodeBlock codeBlock, string name, List<string> managementValues)
        {
            // Even if the sprite is performing its attachments it needs to be manually updated
            codeBlock.Line($"FlatRedBall.SpriteManager.ManualUpdate({name});");
        }

        private void GeneratePositionedObjectVariableUpdateDependenciesFor(ICodeBlock codeBlock, string name, InstanceManagementValues instanceManagementValues)
        {
            if(instanceManagementValues.Has("Attachment"))
            {
                codeBlock.Line($"{name}.UpdateDependencies(FlatRedBall.TimeManager.CurrentTime);");
            }
        }

        #endregion

        #region "Is" methods

        private bool IsPositionedObject(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo?.IsPositionedObject == true;
        }

        private bool IsIColorable(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo == AvailableAssetTypes.CommonAtis.Sprite || assetTypeInfo == AvailableAssetTypes.CommonAtis.Text;
        }

        private bool IsIInstructable(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo == AvailableAssetTypes.CommonAtis.Sprite || 
                assetTypeInfo == AvailableAssetTypes.CommonAtis.Text ||
                assetTypeInfo?.RuntimeTypeName == "PositionedObject" ||
                assetTypeInfo?.RuntimeTypeName == "FlatRedBall.PositionedObject" ||
                assetTypeInfo == AvailableAssetTypes.CommonAtis.Circle ||
                assetTypeInfo == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle ||
                assetTypeInfo == AvailableAssetTypes.CommonAtis.Polygon  ||
                assetTypeInfo == AvailableAssetTypes.CommonAtis.CapsulePolygon
                ;

        }

        private bool IsText(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo == AvailableAssetTypes.CommonAtis.Text;

        }

        private bool IsAnimationChainAnimatable(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo?.RuntimeTypeName == "Sprite";
        }

        private bool IsIScalable(AssetTypeInfo assetTypeInfo)
        {
            return assetTypeInfo?.RuntimeTypeName == "Sprite";
        }

        #endregion
    }


    static class InstanceManagementValuesExtensions
    {
        public static bool Has(this InstanceManagementValues values, string propertyName)
        {
            return values.SelectedProperties.Contains(propertyName);
        }
    }


}
