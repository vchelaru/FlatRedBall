using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    class GumLayerAssociationCodeGenerator : ElementComponentCodeGenerator
    {
        bool ShouldGenerate
        {
            get
            {
                return AppState.Self.GumProjectSave != null;
            }
        }

        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                // This needs to be before so that all layers are created and associated before
                // any components try to access them
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;
            }
        }

        IEnumerable<NamedObjectSave> GetObjectsForGumLayers(IElement element)
        {
            return element.AllNamedObjects.Where(item => item.IsLayer &&
                NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full);
        }


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            if (ShouldGenerate)
            {


                // Creates Gum layers for every FRB layer, so that objects can be moved between layers at runtime, and so code gen
                // can use these for objects that are placed on layers in Glue.
                foreach (var layer in GetObjectsForGumLayers(element))
                {
                    var rfs = GetScreenRfsIn(element);

                    var idbName = rfs?.GetInstanceName();

                    if (string.IsNullOrEmpty(idbName) && element is FlatRedBall.Glue.SaveClasses.ScreenSave)
                    {
                        idbName = "gumIdb";
                    }

                    if(idbName != null)
                    {

                        codeBlock.Line(layer.InstanceName + "Gum = RenderingLibrary.SystemManagers.Default.Renderer.AddLayer();");
                        codeBlock.Line(layer.InstanceName + "Gum.Name = \"" + layer.InstanceName + "Gum\";");

                        codeBlock.Line(idbName + ".AddGumLayerToFrbLayer(" + layer.InstanceName + "Gum, " + layer.InstanceName + ");");
                    }
                }

            }
            return base.GenerateAddToManagers(codeBlock, element);
        }

        private ReferencedFileSave GetScreenRfsIn(IElement element)
        {
            return element.ReferencedFiles.FirstOrDefault(item =>
                FileManager.GetExtension(item.Name) == GumProjectSave.ScreenExtension);
        }
    }
}
