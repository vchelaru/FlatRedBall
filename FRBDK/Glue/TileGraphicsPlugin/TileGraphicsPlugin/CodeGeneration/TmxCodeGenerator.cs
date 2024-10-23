using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.Managers;
using TMXGlueLib;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace TileGraphicsPlugin.CodeGeneration
{
    class TmxCodeGenerator : ElementComponentCodeGenerator
    {
        #region Fields

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            foreach (var nos in element.NamedObjects)
            {
                if (ShouldGenerateFieldsForNamedObjects(nos))
                {
                    GenerateCreateFieldsForNamedObjects(nos, element as GlueElement, codeBlock);
                }
            }

            return codeBlock;
        }

        bool ShouldGenerateFieldsForNamedObjects(NamedObjectSave nos)
        {
            return
                nos.GetAssetTypeInfo()?.Extension == "tmx" &&
                nos.SourceType == SourceType.File &&
                !string.IsNullOrEmpty(nos.SourceFile) &&
                nos.GetCustomVariable("ShiftMapToMoveGameplayLayerToZ0")?.Value as bool? == true;
        }

        private void GenerateCreateFieldsForNamedObjects(NamedObjectSave nos, GlueElement glueElement, ICodeBlock codeBlock)
        {
            var file = glueElement.GetReferencedFileSave(nos.SourceFile);
            if(file == null)
            {
                return;
            }

            // open the TMX, look for object layers, generate fields for them all:
            var absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(file);
            if(absoluteFile.Exists())
            {
                var oldShouldLoadFromSource = Tileset.ShouldLoadValuesFromSource;
                //Tileset.ShouldLoadValuesFromSource = false;
                // we need this to be true so we get the tileset info:
                Tileset.ShouldLoadValuesFromSource = true;

                try
                {
                    var tiledMapSave = TiledMapSave.FromFile(absoluteFile.FullPath);

                    var orderedTilesets = tiledMapSave.Tilesets.OrderBy(item => item.Firstgid).ToArray();

                    foreach (var layer in tiledMapSave.MapLayers)
                    {
                        if (layer is mapObjectgroup objectLayer)
                        {

                            foreach(var item in objectLayer.@object)
                            {
                                if(!string.IsNullOrEmpty( item.Name))
                                {
                                    var tileset = tiledMapSave.Tilesets.First(possibleTileset => possibleTileset.Firstgid <= item.gid);
                                    var foundTile = tileset.Tiles.First(tile => tile.id + tileset.Firstgid == item.gid);

                                    var className = foundTile.Class;
                                    var entity = ObjectFinder.Self.GetEntitySaveUnqualified(className);

                                    if(entity != null)
                                    {

                                        codeBlock.Line($"{CodeWriter.GetGlueElementNamespace(entity)}.{entity.ClassName} {item.Name};");
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing - this will be handled elsewhere by the file tracking error system
                }
                Tileset.ShouldLoadValuesFromSource = oldShouldLoadFromSource;
            }
        }

        #endregion

        #region AddToManagers

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
                    TryGenerateShiftZ0Code(nos, codeBlock);

                    // not sure if we need this for files, but for now
                    // going to implement just on NOS's since that's the 
                    // preferred pattern.
                    GenerateCreateEntitiesCode(nos, element as GlueElement, codeBlock);
                }
            }



            return codeBlock;
        }

        private void GenerateAssignmentsForObjectLayerFields(NamedObjectSave nos, GlueElement glueElement, ICodeBlock codeBlock)
        {


            var file = glueElement.GetReferencedFileSave(nos.SourceFile);
            if (file == null)
            {
                return;
            }

            // open the TMX, look for object layers, generate fields for them all:
            var absoluteFile = GlueCommands.Self.GetAbsoluteFilePath(file);
            if (absoluteFile.Exists())
            {
                var oldShouldLoadFromSource = Tileset.ShouldLoadValuesFromSource;
                //Tileset.ShouldLoadValuesFromSource = false;
                // we need this to be true so we get the tileset info:
                Tileset.ShouldLoadValuesFromSource = true;

                try
                {
                    var tiledMapSave = TiledMapSave.FromFile(absoluteFile.FullPath);

                    var orderedTilesets = tiledMapSave.Tilesets.OrderBy(item => item.Firstgid).ToArray();

                    foreach (var layer in tiledMapSave.MapLayers)
                    {
                        if (layer is mapObjectgroup objectLayer)
                        {
                            foreach (var item in objectLayer.@object)
                            {
                                if (!string.IsNullOrEmpty(item.Name))
                                {
                                    var tileset = tiledMapSave.Tilesets.First(possibleTileset => possibleTileset.Firstgid <= item.gid);
                                    var foundTile = tileset.Tiles.First(tile => tile.id + tileset.Firstgid == item.gid);

                                    var className = foundTile.Class;
                                    var entity = ObjectFinder.Self.GetEntitySaveUnqualified(className);

                                    if (entity != null)
                                    {
                                        var defaultList = ObjectFinder.Self.GetDefaultListToContain(entity, glueElement);

                                        codeBlock.Line($"{item.Name} = {defaultList.FieldName}.FindByName(\"{item.Name}\");");
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing - this will be handled elsewhere by the file tracking error system
                }
                Tileset.ShouldLoadValuesFromSource = oldShouldLoadFromSource;

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

        #endregion

        #region AddToManagersBottomUp

        public override void GenerateAddToManagersBottomUp(ICodeBlock codeBlock, IElement element)
        {
            foreach (var nos in element.NamedObjects)
            {
                if (nos.GetAssetTypeInfo()?.Extension == "tmx")
                {
                    if (ShouldGenerateFieldsForNamedObjects(nos))
                    {
                        GenerateAssignmentsForObjectLayerFields(nos, element as GlueElement, codeBlock);
                    }
                }
            }
        }

        #endregion

        static bool IsAbstract(IElement element) => element.AllNamedObjects.Any(item => item.SetByDerived);

        private void TryGenerateShiftZ0Code(NamedObjectSave nos, ICodeBlock codeBlock)
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

    }
}
