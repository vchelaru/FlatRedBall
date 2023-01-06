using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.Managers;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
                if(nos.GetAssetTypeInfo()?.Extension == "tmx")
                {
                    GenerateShiftZ0Code(nos, codeBlock);

                    // not sure if we need this for files, but for now
                    // going to implement just on NOS's since that's the 
                    // preferred pattern.
                    GenerateCreateEntitiesCode(nos, element as GlueElement, codeBlock);
                }
            }



            return codeBlock;
        }

        static bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);

        private void GenerateCreateEntitiesCode(NamedObjectSave nos, GlueElement nosOwner, ICodeBlock codeBlock)
        {
            var shouldGenerate = nos.DefinedByBase == false &&
                nos.GetCustomVariable("CreateEntitiesFromTiles")?.Value as bool?  == true;

            if(shouldGenerate)
            {
                var shouldReplaceFactoryListsTemporarily = nosOwner is EntitySave;

                List<NamedObjectSave> listsToAddTemporarily = new List<NamedObjectSave>();
                HashSet<string> factoryTypesToClear = new HashSet<string>();


                // For information on this code, see:
                // https://github.com/vchelaru/FlatRedBall/issues/582
                if(shouldReplaceFactoryListsTemporarily)
                {
                    listsToAddTemporarily.AddRange(nosOwner.NamedObjects.Where(item => item.IsList && item.AssociateWithFactory));

                    foreach (var list in listsToAddTemporarily)
                    {
                        EntitySave listEntityType = ObjectFinder.Self.GetEntitySave(list.SourceClassGenericType);

                        if(listEntityType != null && listEntityType.CreatedByOtherEntities && !IsAbstract(listEntityType))
                        {
                            string entityClassName = FlatRedBall.IO.FileManager.RemovePath(listEntityType.Name);
                            string factoryName = $"Factories.{entityClassName}Factory";
                            factoryTypesToClear.Add(factoryName);
                        }
                    }

                    if (listsToAddTemporarily.Count > 0)
                    {
                        codeBlock.Line("//Temporarily replacing factory lists so that any entities created in this TMX are added solely to this entity's lists.");
                    }
                    // cache off in local varaible and clear
                    foreach(var factoryType in factoryTypesToClear)
                    {
                        // Example: ((Performance.IEntityFactory)Factories.EnemyFactory.Self).ListsToAddTo.ToArray()
                        codeBlock.Line($"var {factoryType.Replace(".", "_")}_tempList = ((Performance.IEntityFactory){factoryType}.Self).ListsToAddTo.ToArray();");
                        codeBlock.Line($"{factoryType}.ClearListsToAddTo();");
                    }

                    foreach (var list in listsToAddTemporarily)
                    {
                        EntitySave listEntityType = ObjectFinder.Self.GetEntitySave(list.SourceClassGenericType);
                        string entityClassName = FlatRedBall.IO.FileManager.RemovePath(listEntityType.Name);
                        string factoryName = $"Factories.{entityClassName}Factory";

                        codeBlock.Line($"((Performance.IEntityFactory){factoryName}.Self).ListsToAddTo.Add({list.InstanceName} as System.Collections.IList);");
                    }
                }

                codeBlock.Line(
                    $"FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom({nos.InstanceName});");

                if(shouldReplaceFactoryListsTemporarily)
                {
                    // cache off in local varaible and clear
                    foreach (var factoryType in factoryTypesToClear)
                    {
                        codeBlock.Line($"{factoryType}.ClearListsToAddTo();");
                        codeBlock.Line($"((Performance.IEntityFactory){factoryType}.Self).ListsToAddTo.AddRange({factoryType.Replace(".", "_")}_tempList);");
                    }
                }

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
                codeBlock = codeBlock.Line($"// What if the map's Z isn't 0? Add its Z to make up for that");
                codeBlock = codeBlock.Line($"{nos.InstanceName}.Z = {nos.InstanceName}.Z - {gameplayLayerVarName}.Z;");
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
