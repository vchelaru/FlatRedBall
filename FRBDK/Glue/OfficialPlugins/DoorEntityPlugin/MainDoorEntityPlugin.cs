using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Math.Geometry;
using GlueFormsCore.ViewModels;
using OfficialPlugins.DoorEntityPlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.DoorEntityPlugin
{
    [Export(typeof(PluginBase))]
    public class MainDoorEntityPlugin : PluginBase
    {
        #region Fields/Properties
        public override string FriendlyName => "DoorEntity Plugin";

        public override Version Version => new Version(1, 0);

        ScreenSave GameScreen => ObjectFinder.Self.GetScreenSave("Screens\\GameScreen");

        string doorEntityName = "Entities\\DoorEntity";

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.AddMenuItemTo("Test Add DoorEntity", AddDoorEntity, "Plugins");

            var codeGenerator = new DoorEntityCodeGenerator();
            this.RegisterCodeGenerator(codeGenerator);

            this.ReactToLoadedGlux += HandleGluxLoaded;

        }

        private void HandleGluxLoaded()
        {
            var hasDoorEntity = GlueState.Self.CurrentGlueProject.Entities.Any(item => item.Name == doorEntityName);

            if(hasDoorEntity)
            {
                CreateAssetTypes();
            }
        }

        #region Add Door Entity

        private async void AddDoorEntity()
        {
            CreateAssetTypes();

            var newEntity = await CreateEntitySave();

            await CreateEntityListInGameScreen(newEntity);

            await CreateWidthHeightVariables(newEntity);

            await CreateDestinationVariables(newEntity);

            await CreateCollisionRelationshipBetweenPlayerAndDoors();
        }

        private void CreateAssetTypes()
        {
            var ati = new AssetTypeInfo();

            ati.CanBeObject = true;
            ati.FriendlyName = "DoorEntity";
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType() { QualifiedType = doorEntityName };

            // we probably need x/y/z?

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "X",
                Type = "float",
                Category = "Variables"
            });


            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "Y",
                Type = "float",
                Category = "Variables"
            });


            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "Z",
                Type = "float",
                Category = "Variables"
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "DestinationScreen",
                Type = "string",
                Category = "Destination",
                CustomGetForcedOptionFunc = (element, nos, rfs) =>
                {
                    var names = GlueState.Self.CurrentGlueProject.Screens.Select(item => item.ClassName).ToList();
                    return names;
                }
            });


            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "DestinationObject",
                Type = "string",
                Category = "Destination",
                CustomGetForcedOptionFunc = (element, nos, rfs) =>
                {
                    var destinationScreenName =
                        ObjectFinder.Self.GetValueRecursively(nos, element as GlueElement, "DestinationScreen") as string;
                    //element.GetVariableValueRecursively("DestinationScreen") as string;
                    ScreenSave destinationScreen = null;
                    if (!string.IsNullOrEmpty(destinationScreenName))
                    {
                        destinationScreen = GlueState.Self.CurrentGlueProject.Screens.FirstOrDefault(item => item.ClassName == destinationScreenName);
                    }

                    List<NamedObjectSave> namedObjects = null;

                    if (destinationScreen != null)
                    {
                        namedObjects = destinationScreen.AllNamedObjects.Where(item =>
                            item.GetAssetTypeInfo()?.IsPositionedObject == true ||
                            item.SourceType == SourceType.Entity)
                        .ToList();
                    }

                    if (namedObjects != null)
                    {
                        return namedObjects.Select(item => item.InstanceName).ToList();
                    }
                    else
                    {
                        return new List<string>();
                    }

                }
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "Width",
                Type = "float",
                Category = "Size"
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "Height",
                Type = "float",
                Category = "Size"
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "DestinationX",
                Type = "float?",
                Category = "Destination"
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "DestinationY",
                Type = "float?",
                Category = "Destination"
            });


            this.AddAssetTypeInfo(ati);
        }

        private async Task CreateEntityListInGameScreen(EntitySave newEntity)
        {
            var gameScreen = GameScreen;

            if (gameScreen != null)
            {
                var addObjectVm = new AddObjectViewModel();
                addObjectVm.SourceType = FlatRedBall.Glue.SaveClasses.SourceType.FlatRedBallType;
                addObjectVm.SourceClassType = AvailableAssetTypes.CommonAtis.PositionedObjectList.RuntimeTypeName;
                addObjectVm.SourceClassGenericType = newEntity.Name;
                addObjectVm.ObjectName = "DoorEntityList";

                await GlueCommands.Self.GluxCommands.AddNewNamedObjectToAsync(addObjectVm, gameScreen, selectNewNos:false);
            }
        }

        private static async Task<EntitySave> CreateEntitySave()
        {
            var vm = new AddEntityViewModel();

            vm.Name = "DoorEntity";
            vm.IsAxisAlignedRectangleChecked = true;
            vm.IsICollidableChecked = true;
            vm.IsCreateFactoryChecked = true;

            var newEntity = await GlueCommands.Self.GluxCommands.EntityCommands.AddEntityAsync(vm);
            return newEntity;
        }

        private async Task CreateWidthHeightVariables(EntitySave doorEntity)
        {
            var rectangleNos = doorEntity
                .AllNamedObjects
                .First(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.AxisAlignedRectangle);

            var widthVariable = new CustomVariable();
            widthVariable.Name = "Width";
            widthVariable.Type = "float";

            widthVariable.SourceObject = rectangleNos.InstanceName;
            widthVariable.SourceObjectProperty = nameof(AxisAlignedRectangle.Width);
            widthVariable.Category = "Size"; // is this right?

            await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(widthVariable, doorEntity, save:false);

            var heightVariable = new CustomVariable();
            heightVariable.Name = "Height";
            heightVariable.Type = "float";

            heightVariable.SourceObject = rectangleNos.InstanceName;
            heightVariable.SourceObjectProperty = nameof(AxisAlignedRectangle.Height);
            heightVariable.Category = "Size";

            await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(heightVariable, doorEntity, save:true);

        }

        private async Task CreateCollisionRelationshipBetweenPlayerAndDoors()
        {
            var gameScreen = GameScreen;

            var playerList = gameScreen.NamedObjects.Find(item => item.InstanceName == "PlayerList");
            var doorEntityList = gameScreen.NamedObjects.Find(item => item.InstanceName == "DoorEntityList");

            if(playerList != null && doorEntityList != null)
            {
                await PluginManager.ReactToCreateCollisionRelationshipsBetween(playerList, doorEntityList);

            }
        }

        private async Task CreateDestinationVariables(EntitySave doorEntity)
        {

            var ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(doorEntityName, this);

            foreach(var variableDefinition in ati.VariableDefinitions)
            {

                var customVariable = new CustomVariable();
                customVariable.Name = variableDefinition.Name;
                customVariable.Type = variableDefinition.Type;
                customVariable.Category = variableDefinition.Category;
                await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(customVariable, doorEntity, save: false);
            }
        }

        #endregion
    }
}
