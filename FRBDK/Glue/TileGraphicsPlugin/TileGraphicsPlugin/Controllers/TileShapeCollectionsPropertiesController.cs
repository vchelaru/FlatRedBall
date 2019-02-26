using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;

namespace TileGraphicsPlugin.Controllers
{
    public class TileShapeCollectionsPropertiesController : 
        Singleton<TileShapeCollectionsPropertiesController>
    {
        Views.TileShapeCollectionProperties view;
        ViewModels.TileShapeCollectionPropertiesViewModel viewModel;

        bool shouldApplyViewModelChanges = true;

        public Views.TileShapeCollectionProperties GetView()
        {
            if(view == null)
            {
                view = new Views.TileShapeCollectionProperties();
                viewModel = new ViewModels.TileShapeCollectionPropertiesViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                view.DataContext = viewModel;
            }
            return view;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(shouldApplyViewModelChanges)
            {
                ApplyViewModelValuesToNamedObject();

                // save
                GlueCommands.Self.GluxCommands.SaveGluxTask();

                // regenerate
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
            }
        }

        private void ApplyViewModelValuesToNamedObject()
        {
            var nos = GlueState.Self.CurrentNamedObjectSave;

            nos.Properties.SetValue(nameof(viewModel.IsCollisionVisible), viewModel.IsCollisionVisible);
            nos.Properties.SetValue(nameof(viewModel.CollisionInclusion), viewModel.CollisionInclusion.ToString());
            nos.Properties.SetValue(nameof(viewModel.CollisionTileType), viewModel.CollisionTileType);
        }

        public bool IsTileShapeCollection(NamedObjectSave namedObject)
        {
            var isTileShapeCollection = false;

            if (namedObject != null)
            {
                var ati = namedObject.GetAssetTypeInfo();
                isTileShapeCollection =
                            namedObject.SourceType == SourceType.File &&
                            !string.IsNullOrEmpty(namedObject.SourceName) &&
                            ati == AssetTypeInfoAdder.Self.TileShapeCollectionAssetTypeInfo;
            }

            return isTileShapeCollection;
        }

        public void RefreshViewModelTo(NamedObjectSave namedObject)
        {
            shouldApplyViewModelChanges = false;

            viewModel.IsCollisionVisible = namedObject.Properties.GetValue<bool>(nameof(viewModel.IsCollisionVisible));
            string collisionInclusionAsString = namedObject.Properties.GetValue<string>(nameof(viewModel.CollisionInclusion));

            if(string.IsNullOrEmpty(collisionInclusionAsString))
            {
                viewModel.CollisionInclusion = CollisionInclusion.EntireLayer;
            }
            else
            {
                viewModel.CollisionInclusion = 
                    (CollisionInclusion)Enum.Parse(typeof(CollisionInclusion), collisionInclusionAsString);
            }

            viewModel.CollisionTileType = namedObject.Properties.GetValue<string>(nameof(viewModel.CollisionTileType));

            shouldApplyViewModelChanges = true;
        }
    }
}
