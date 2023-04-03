using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Math.Geometry;
using GlueFormsCore.FormHelpers;
using GlueFormsCore.ViewModels;
using OfficialPlugins.DoorEntityPlugin.CodeGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        bool HasDoorEntity => GlueState.Self.CurrentGlueProject?.Entities.Any(item => item.Name == doorEntityName) == true;

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;

            this.RegisterCodeGenerator(new DoorEntityCodeGenerator());

            this.RegisterCodeGenerator(new DoorEntityPlayerPositioninCodeGenerator());

            this.ReactToLoadedGlux += HandleGluxLoaded;

        }

        #region Glux loaded

        private void HandleGluxLoaded()
        {
            if(HasDoorEntity)
            {
                CreateAssetTypes();
            }
        }

        #endregion

        #region Tree view right-click

        private void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> listToAddTo)
        {
            var canAdd =
                !HasDoorEntity &&
                rightClickedTreeNode.IsRootEntityNode();


            if (canAdd)
            {
                listToAddTo.Add("Add DoorEntity", (not, used) => AddDoorEntity());
            }
        }

        #endregion

        #region DoorEntity AssetTypeInfo (ATI)

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
                Name = "AutoNavigate",
                Type = "bool",
                Category = "Destination",
                DefaultValue = "true"
            });

            ati.VariableDefinitions.Add(new VariableDefinition()
            {
                Name = "DestinationScreen",
                Type = "string",
                Category = "Destination",
                CustomGetForcedOptionFunc = (element, nos, rfs) =>
                {
                    var names = GlueState.Self.CurrentGlueProject.Screens
                        .Where(item => item.IsAbstract == false)
                        .Select(item => item.ClassName).ToList();
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

        #endregion

        #region Add Door Entity

        private async void AddDoorEntity()
        {
            CreateAssetTypes();

            var newEntity = await CreateEntitySave();

            // now this is automatic:
            //await CreateEntityListInGameScreen(newEntity);

            await CreateWidthHeightVariables(newEntity);

            await AddVariablesFromAssetTypeInfo(newEntity);

            await CreateCollisionRelationshipBetweenPlayerAndDoors();

            await CreateCollisionRelationshipEvent();
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
            vm.IncludeListsInScreens = true;

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

        private async Task AddVariablesFromAssetTypeInfo(EntitySave doorEntity)
        {

            var ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(doorEntityName, this);

            foreach(var variableDefinition in ati.VariableDefinitions)
            {
                var customVariable = doorEntity.CustomVariables.FirstOrDefault(item => item.Name == variableDefinition.Name);
                var isNew = false;
                if(customVariable == null)
                {
                    customVariable = new CustomVariable();
                    isNew = true;
                }
                customVariable.Name = variableDefinition.Name;
                customVariable.Type = variableDefinition.Type;
                customVariable.Category = variableDefinition.Category;

                if(!string.IsNullOrEmpty(variableDefinition.DefaultValue))
                {
                    customVariable.DefaultValue = PropertyValuePair.ConvertStringToType(
                        variableDefinition.DefaultValue, variableDefinition.Type);
                }

                if(isNew)
                {
                    await GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElementAsync(customVariable, doorEntity, save: false);
                }
            }
        }


        private async Task CreateCollisionRelationshipEvent()
        {
            var viewModel = new AddEventViewModel();
            viewModel.BeforeOrAfter = BeforeOrAfter.Before;
            viewModel.DesiredEventType = FlatRedBall.Glue.Controls.CustomEventType.Exposed;
            viewModel.EventName = "PlayerListVsDoorEntityListCollisionOccurred";
            viewModel.TunnelingEvent = "CollisionOccurred";
            viewModel.TunnelingObject = "PlayerListVsDoorEntityList";

            await GlueCommands.Self.GluxCommands.ElementCommands.AddEventToElement(viewModel, GameScreen);
        }

        #endregion
    }
}
