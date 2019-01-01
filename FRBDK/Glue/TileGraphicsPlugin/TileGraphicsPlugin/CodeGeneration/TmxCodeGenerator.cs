using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.Managers;

namespace TileGraphicsPlugin.CodeGeneration
{
    class TmxCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {

            foreach(var file in element.ReferencedFiles)
            {
                var ati = file.GetAssetTypeInfo();

                if(ati == AssetTypeInfoAdder.Self.TmxAssetTypeInfo)
                {
                    GenerateAddToManagers(codeBlock, file);
                }
            }

            return codeBlock;
        }

        private void GenerateAddToManagers(ICodeBlock codeBlock, ReferencedFileSave file)
        {
            if(file.GetProperty<bool>(EntityCreationManager.CreateEntitiesInGeneratedCodePropertyName))
            {
                var instanceName = file.GetInstanceName();
                codeBlock.Line(
                    $"FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom({instanceName});");

            }
        }
    }
}
