using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPluginsCore.CameraControllingEntityPlugin
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
            ati.AddToManagersFunc += (element, nos, rfs, layerName) => $"FlatRedBall.SpriteManager.AddPositionedObject({nos.InstanceName});";
            ati.ActivityMethod = "this.Activity()";
            ati.DestroyMethod = "FlatRedBall.SpriteManager.RemovePositionedObject(this);";

            var defaultInstance = new FlatRedBall.Entities.CameraControllingEntity();


            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.Targets),
                Type = "string",
                Category = "Targets",
                CustomGenerationFunc = GenerateTargetsCodeGen

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.Map),
                Type = "string",
                Category = "Targets",
                CustomGenerationFunc = GenerateMapCodeGen

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.ExtraMapPadding),
                Type = "float",
                Category = "Targets",

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.LerpSmooth),
                Type = "bool",
                DefaultValue = CodeParser.ConvertValueToCodeString(defaultInstance.LerpSmooth),
                Category = "Coefficients"

            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.LerpCoefficient),
                Type = "float",
                DefaultValue = CodeParser.ConvertValueToCodeString(defaultInstance.LerpCoefficient),
                Category = "Coefficients"
            });


            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.SnapToPixel),
                Type = "bool",
                DefaultValue = CodeParser.ConvertValueToCodeString(defaultInstance.SnapToPixel),
                Category = "Coefficients"
            });

            ati.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Entities.CameraControllingEntity.SnapToPixelOffset),
                Type = "float",
                DefaultValue = CodeParser.ConvertValueToCodeString(defaultInstance.SnapToPixelOffset),
                Category = "Coefficients"
            });
            

            AvailableAssetTypes.Self.AddAssetType(ati);
        }

        private string GenerateMapCodeGen(IElement arg1, NamedObjectSave nos, ReferencedFileSave arg3)
        {
            var value = 
                nos.GetCustomVariable(nameof(FlatRedBall.Entities.CameraControllingEntity.Map))?.Value as string;

            if(!string.IsNullOrEmpty(value))
            {
                return $"{nos.InstanceName}.Map = {value};";
            }
            else
            {
                return null;
            }
        }

        private string GenerateTargetsCodeGen(IElement nosContainer, NamedObjectSave nos, ReferencedFileSave arg3)
        {
            var value =
                nos.GetCustomVariable(nameof(FlatRedBall.Entities.CameraControllingEntity.Targets))?.Value as string;

            if(!string.IsNullOrEmpty(value))
            {
                var referencedNos = nosContainer.GetNamedObjectRecursively(value);

                if(referencedNos == null)
                {
                    return null;
                }
                else if(referencedNos.IsList)
                {

                    return $"{nos.InstanceName}.Targets = {referencedNos.InstanceName};";
                }
                else
                {
                    return $"{nos.InstanceName}.Targets.Clear(); {nos.InstanceName}.Targets.Add({referencedNos.InstanceName});";
                }
            }
            else
            {
                return null;
            }
        }
    }
}
