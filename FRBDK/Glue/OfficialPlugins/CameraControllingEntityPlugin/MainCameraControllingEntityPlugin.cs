using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace OfficialPlugins.CameraControllingEntityPlugin
{
    [Export(typeof(PluginBase))]
    public class MainCameraControllingEntityPlugin : PluginBase
    {
        public override string FriendlyName => "CameraControllingEntity Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AddAssetTypeInfo();
        }

        private void AddAssetTypeInfo()
        {
            var ati = new AssetTypeInfo();

            ati.FriendlyName = "Camera Controlling Entity";
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType
            {
                QualifiedType = "FlatRedBall.Entities.CameraControllingEntity"
            };

            ati.CanBeObject = true;
            ati.IsPositionedObject = true;
            ati.AddToManagersFunc += (element, nos, rfs, layerName) =>
            {
                var instanceName = GetInstanceName(nos);
                return $"FlatRedBall.SpriteManager.AddPositionedObject({instanceName}); {instanceName}.Activity();";
            };
            ati.ActivityMethod = "this.Activity()";
            ati.DestroyMethod = "FlatRedBall.SpriteManager.RemovePositionedObject(this);";

            var defaultInstance = new FlatRedBall.Entities.CameraControllingEntity();


            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.Targets),
                Type = "string",
                Category = "Targets",
                CustomGenerationFunc = GenerateTargetsCodeGen,
                CustomGetForcedOptionFunc = GetAvailableTargets

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.Map),
                Type = "string",
                Category = "Targets",
                CustomGenerationFunc = GenerateMapCodeGen,
                CustomGetForcedOptionFunc = GetAvailableMaps

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.ExtraMapPadding),
                Type = "float",
                Category = "Targets",

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.ScrollingWindowWidth),
                Type = "float",
                Category = "Targets",
            });


            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.ScrollingWindowHeight),
                Type = "float",
                Category = "Targets",
            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                // This is a warning but we cannot swap this because it would break existing projects which
                // use the name of this variable. If we want to change this, we need to do so in a new gluj version
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.LerpSmooth),
                Type = "bool",
                // This is a warning but we cannot swap this because it would break existing projects which
                // use the name of this variable. If we want to change this, we need to do so in a new gluj version
                DefaultValue = ValueToString(defaultInstance.LerpSmooth),
                Category = "Coefficients"

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                // This is a warning but we cannot swap this because it would break existing projects which
                // use the name of this variable. If we want to change this, we need to do so in a new gluj version
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.LerpCoefficient),
                Type = "float",
                DefaultValue = ValueToString(defaultInstance.TargetApproachCoefficient),
                Category = "Coefficients"
            });


            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.SnapToPixel),
                Type = "bool",
                DefaultValue = ValueToString(defaultInstance.SnapToPixel),
                Category = "Coefficients"
            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.SnapToPixelOffset),
                Type = "float",
                DefaultValue = ValueToString(defaultInstance.SnapToPixelOffset),
                Category = "Coefficients"
            });
            

            AvailableAssetTypes.Self.AddAssetType(ati);
        }

        string ValueToString(object value)
        {
            var valueAsString = CodeParser.ConvertValueToCodeString(value);
            if(value is float && valueAsString.EndsWith("f"))
            {
                valueAsString = valueAsString.Substring(0, valueAsString.Length - 1);
            }
            return valueAsString;
        }

        private List<string> GetAvailableTargets(IElement element, NamedObjectSave arg2, ReferencedFileSave arg3)
        {
            var targetObjects = element.AllNamedObjects.Where(item =>
                item.IsList ||
                (item.SourceType == SourceType.Entity && !string.IsNullOrEmpty(item.SourceClassType)));

            // why do we do field name? That would result in "mWhatever"
            //return targetObjects.Select(item => item.FieldName).ToList();
            return targetObjects.Select(item => item.InstanceName).ToList();
        }

        private List<string> GetAvailableMaps(IElement element, NamedObjectSave arg2, ReferencedFileSave arg3)
        {
            var mapObjects = element.AllNamedObjects.Where(item => item.GetAssetTypeInfo()?.Extension == "tmx");

            // see above for question about why we use field name
            //return mapObjects.Select(item => item.FieldName).ToList();
            return mapObjects.Select(item => item.InstanceName).ToList();
        }

        // nos is null when the element itself is the CameraControllingEntity (an entity whose base is that
        // type). The instance is then "this", and its variables live on the element.
        private static string GetInstanceName(NamedObjectSave nos) => nos?.InstanceName ?? "this";

        private static string GetVariableValue(IElement element, NamedObjectSave nos, string variableName) =>
            (nos != null ? nos.GetCustomVariable(variableName)?.Value : element.GetCustomVariable(variableName)?.DefaultValue) as string;

        private string GenerateMapCodeGen(IElement element, NamedObjectSave nos, ReferencedFileSave arg3, string memberName)
        {
            var value = GetVariableValue(element, nos, nameof(FlatRedBall.Entities.CameraControllingEntity.Map));

            if(!string.IsNullOrEmpty(value))
            {
                return $"{GetInstanceName(nos)}.Map = {value};";
            }
            else
            {
                return null;
            }
        }

        private string GenerateTargetsCodeGen(IElement nosContainer, NamedObjectSave nos, ReferencedFileSave arg3, string memberName)
        {
            var value = GetVariableValue(nosContainer, nos, nameof(FlatRedBall.Entities.CameraControllingEntity.Targets));

            if(!string.IsNullOrEmpty(value))
            {
                var referencedNos = nosContainer.GetNamedObjectRecursively(value);
                var instanceName = GetInstanceName(nos);

                if(referencedNos == null)
                {
                    return null;
                }
                else if(referencedNos.IsList)
                {

                    return $"{instanceName}.Targets = {referencedNos.InstanceName};";
                }
                else
                {
                    return $"{instanceName}.Targets.Clear(); {instanceName}.Targets.Add({referencedNos.InstanceName});";
                }
            }
            else
            {
                return null;
            }
        }
    }
}
