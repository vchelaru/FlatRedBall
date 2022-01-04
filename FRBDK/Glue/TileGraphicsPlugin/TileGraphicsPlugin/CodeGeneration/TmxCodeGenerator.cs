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

            foreach(var nos in element.NamedObjects)
            {
                if(nos.GetAssetTypeInfo().Extension == "tmx")
                {
                    // not sure if we need this for files, but for now
                    // going to implement just on NOS's since that's the 
                    // preferred pattern.
                    GenerateCreateEntitiesCode(nos, codeBlock);

                    GenerateShiftZ0Code(nos, codeBlock);
                }
            }



            return codeBlock;
        }

        private void GenerateCreateEntitiesCode(NamedObjectSave nos, ICodeBlock codeBlock)
        {
            var shouldGenerate = nos.DefinedByBase == false &&
                nos.GetCustomVariable("CreateEntitiesFromTiles")?.Value as bool?  == true;

            if(shouldGenerate)
            {
                codeBlock.Line(
                    $"FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom({nos.InstanceName});");

            }
        }

        private void GenerateShiftZ0Code(NamedObjectSave nos, ICodeBlock codeBlock)
        {
            var shouldGenerate = nos.DefinedByBase == false &&
                nos.GetCustomVariable("ShiftMapToMoveGameplayLayerToZ0")?.Value as bool? == true;

            if(shouldGenerate)
            {
                var gameplayLayerVarName = $"{nos.InstanceName}_gameplayLayer";
                codeBlock.Line($"var {gameplayLayerVarName} = {nos.InstanceName}.MapLayers.FindByName(\"GameplayLayer\");");

                codeBlock = codeBlock.If($"{gameplayLayerVarName} != null");
                codeBlock = codeBlock.Line($"{gameplayLayerVarName}.ForceUpdateDependencies();");
                codeBlock = codeBlock.Line($"{nos.InstanceName}.Z = -{gameplayLayerVarName}.Z;");
            }                   
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
