using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.RenderTargetPlugin
{
    [Export(typeof(PluginBase))]
    class MainPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            AddRenderTargetAti();
        }

        private void AddRenderTargetAti()
        {
            AssetTypeInfo renderTargetAti = new AssetTypeInfo();

            // todo: need to make an explicit InstantiateOnAddToManagers bool for ATI, set to true for layers and this, instead of relying on "this = " in the AddToManagers call

            renderTargetAti.CanBeObject = true;
            renderTargetAti.QualifiedRuntimeTypeName = new PlatformSpecificType
            {
                QualifiedType = "Microsoft.Xna.Framework.Graphics.RenderTarget2D"
            };
            renderTargetAti.FriendlyName = "RenderTarget";
            renderTargetAti.AddToManagersFunc = GetAddToManagersCodeForRenderTarget;
            renderTargetAti.ConstructorFunc = GetConstructorFunc;

            renderTargetAti.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "Width",
                Type = "int",
                UsesCustomCodeGeneration = true
            });


            renderTargetAti.VariableDefinitions.Add(new VariableDefinition
            {
                Name = "Height",
                Type = "int",
                UsesCustomCodeGeneration = true

            });

            // Seems like currently Glue doesn't differentiate between remove from managers and destroy...may need to investigate this if it becomes a problem.
            renderTargetAti.DestroyMethod = "this.Dispose()";
            renderTargetAti.BaseAssetTypeInfo = 
                AvailableAssetTypes.Self.AllAssetTypes
                    .FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Graphics.Texture2D");

            AvailableAssetTypes.Self.AddAssetType(renderTargetAti);
        }

        private string GetAddToManagersCodeForRenderTarget(IElement arg1, NamedObjectSave arg2, ReferencedFileSave arg3, string arg4)
        {
            return "";
        }

        private string GetConstructorFunc(IElement element, NamedObjectSave namedObject, ReferencedFileSave file)
        {
            string objectName = namedObject.FieldName;



            string width;
            string height;

            var widthVariable = namedObject.GetCustomVariable("Width");
            var heightVariable = namedObject.GetCustomVariable("Height");

            if(widthVariable?.Value == null)
            {
                width = "FlatRedBall.Camera.Main.DestinationRectangle.Width";
            }
            else
            {
                width = widthVariable.Value.ToString();
            }

            if(heightVariable?.Value == null)
            {
                height = "FlatRedBall.Camera.Main.DestinationRectangle.Height";
            }
            else
            {
                height = heightVariable.Value.ToString();
            }

            var toReturn = $"{objectName} = new Microsoft.Xna.Framework.Graphics.RenderTarget2D(FlatRedBall.FlatRedBallServices.GraphicsDevice, {width}, {height});";
            return toReturn;
        }
    }
}
