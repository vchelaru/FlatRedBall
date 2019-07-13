using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPlugins.CollisionPlugin.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OfficialPlugins.CollisionPlugin
{
    [Export(typeof(PluginBase))]
    public class MainCollisionPlugin : PluginBase
    {
        #region Fields/Properties

        CollisionRelationshipViewModel viewModel;
        CollisionRelationshipView control;
        PluginTab pluginTab;

        public override string FriendlyName => "Collision Plugin";

        // 1.0
        //  - Initial release
        // 1.1
        //  - CollisionRelationships now have their Name set
        public override Version Version => new Version(1, 1);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            UnregisterAllCodeGenerators();

            AvailableAssetTypes.Self.RemoveAssetType(
                AssetTypeInfoManager.Self.CollisionRelationshipAti);
            return true;
        }

        public override void StartUp()
        {
            CreateViewModel();

            var collisionCodeGenerator = new CollisionCodeGenerator();

            AvailableAssetTypes.Self.AddAssetType(
                AssetTypeInfoManager.Self.CollisionRelationshipAti);

            RegisterCodeGenerator(collisionCodeGenerator);

            AssignEvents();
        }

        private void CreateViewModel()
        {
            viewModel = new CollisionRelationshipViewModel();
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(viewModel.FirstCollisionName):
                case nameof(viewModel.SecondCollisionName):
                    var nos = viewModel.GlueObject as NamedObjectSave;
                    TryFixSourceClassType(nos);
                    break;
            }
        }

        private void AssignEvents()
        {
            this.ReactToItemSelectHandler += HandleTreeViewItemSelected;

            this.AddEventsForObject += HandleAddEventsForObject;
            this.GetEventSignatureArgs += GetEventSignatureAndArgs;
        }

        private void GetEventSignatureAndArgs(NamedObjectSave namedObjectSave, EventResponseSave eventResponseSave, out string type, out string signatureArgs)
        {
            if (namedObjectSave.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti &&
                eventResponseSave.SourceObjectEvent == "CollisionOccurred")
            {
                bool firstThrowaway;
                bool secondThrowaway;

                var firstType = AssetTypeInfoManager.GetFirstGenericType(namedObjectSave, out firstThrowaway);
                var secondType = AssetTypeInfoManager.GetSecondGenericType(namedObjectSave, out secondThrowaway);

                type = $"System.Action<{firstType}, {secondType}>";
                signatureArgs = $"{firstType} first, {secondType} second";
            }
            else
            {
                type = null;
                signatureArgs = null;
            }
        }

        private void HandleAddEventsForObject(NamedObjectSave namedObject, List<ExposableEvent> listToAddTo)
        {
            if(namedObject.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti)
            {
                var newEvent = new ExposableEvent("CollisionOccurred");
                listToAddTo.Add(newEvent);
            }
        }

        private void HandleTreeViewItemSelected(TreeNode selectedTreeNode)
        {
            var selectedNos = GlueState.Self.CurrentNamedObjectSave;


            var shouldShowControl = false;

            if(selectedNos != null)
            {
                TryFixSourceClassType(selectedNos);
            }

            if(selectedNos?.GetAssetTypeInfo() == AssetTypeInfoManager.Self.CollisionRelationshipAti)
            {
                RefreshViewModelTo(selectedNos);

                shouldShowControl = true;
            }

            if (shouldShowControl)
            {
                if(control == null)
                {
                    control = new CollisionRelationshipView();
                    pluginTab = this.CreateTab(control, "Collision");
                    this.ShowTab(pluginTab, TabLocation.Center);
                    control.DataContext = viewModel;
                }
                else
                {
                    this.ShowTab(pluginTab);
                }
            }
            else
            {
                this.RemoveTab(pluginTab);
            }

        }

        private void TryFixSourceClassType(NamedObjectSave selectedNos)
        {
            
            if(selectedNos.SourceClassType == "CollisionRelationship" ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollisionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsListRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsListRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("CollisionRelationship<") == true)
            {
                selectedNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti
                    .QualifiedRuntimeTypeName.PlatformFunc(selectedNos);
            }
        }

        private void RefreshViewModelTo(NamedObjectSave selectedNos)
        {
            // show UId

            if (control != null)
            {
                control.DataContext = null;
            }

            viewModel.GlueObject = selectedNos;

            RefreshAvailableCollisionObjects(GlueState.Self.CurrentElement);

            viewModel.UpdateFromGlueObject();

            if (control     != null)
            {
                control.DataContext = viewModel;
            }
        }

        private void RefreshAvailableCollisionObjects(IElement element)
        {

            viewModel.FirstCollisionItemSource.Clear();
            viewModel.SecondCollisionItemSource.Clear();

            var namedObjects = element.AllNamedObjects.ToArray();

            List<string> names = new List<string>();

            // consider:
            // 1. Individual ICollidables
            // 2. Lists of ICollidables
            // 3. TileShapeCollections
            // 4. ShapeCollections

            foreach(var nos in namedObjects)
            {
                var nosElement = nos.GetReferencedElement();
                var nosAti = nos.GetAssetTypeInfo();

                var entity = nosElement as EntitySave;

                var shouldConsider = false;

                if(entity?.ImplementsICollidable == true)
                {
                    shouldConsider = true;
                }

                if(!shouldConsider)
                {
                    // See if it's a list of ICollidables
                    shouldConsider = nos != null && 
                        nos.SourceType == SourceType.FlatRedBallType &&
                        nos.SourceClassType == "PositionedObjectList<T>" &&
                        !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                        ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType)?.ImplementsICollidable == true;
                }

                if(!shouldConsider)
                {
                    shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                        "FlatRedBall.TileCollisions.TileShapeCollection";
                }

                if(!shouldConsider)
                {
                    shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                        "FlatRedBall.Math.Geometry.ShapeCollection";
                }

                if(shouldConsider)
                {
                    //names.Add(nos.InstanceName);
                    viewModel.FirstCollisionItemSource.Add(nos.InstanceName);
                    viewModel.SecondCollisionItemSource.Add(nos.InstanceName);

                }
            }

        }
    }
}
