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
                view.DataContext = viewModel;
            }
            return view;
        }

        public bool IsTileShapeCollection(NamedObjectSave namedObject)
        {
            var isTileShapeCollection = false;

            if (namedObject != null)
            {
                var ati = namedObject.GetAssetTypeInfo();
                isTileShapeCollection =
                            ati == AssetTypeInfoAdder.Self.TileShapeCollectionAssetTypeInfo;
            }

            return isTileShapeCollection;
        }

        public void RefreshViewModelTo(NamedObjectSave namedObject, IElement element)
        {
            // Disconnect the view from the view model so that 
            // changes that happen here dont' have side effects
            // through the view. For example, if we don't disconnect
            // the two, then clearing the available TMX objects will set
            // the selection to null, and even persist it to the Glue object
            view.DataContext = null;

            shouldApplyViewModelChanges = false;

            viewModel.GlueObject = namedObject;

            RefreshAvailableTiledObjects(element);

            viewModel.UpdateFromGlueObject();

            viewModel.IsEntireViewEnabled = namedObject.DefinedByBase == false;

            shouldApplyViewModelChanges = true;
            view.DataContext = viewModel;

        }

        private void RefreshAvailableTiledObjects(IElement element)
        {
            // refresh availble TMXs
            var referencedFileSaves = element.ReferencedFiles
                .Where(item =>
                    item.LoadedAtRuntime &&
                    item.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo);

            var namedObjects = element.AllNamedObjects
                .Where(item =>
                    item.IsDisabled == false &&
                    item.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo);

            if (viewModel.TmxObjectNames == null)
            {
                viewModel.TmxObjectNames = new System.Collections.ObjectModel.ObservableCollection<string>();
            }
            viewModel.TmxObjectNames.Clear();
            foreach (var rfs in referencedFileSaves)
            {
                viewModel.TmxObjectNames.Add(rfs.GetInstanceName());
            }
            foreach (var nos in namedObjects)
            {
                viewModel.TmxObjectNames.Add(nos.InstanceName);
            }
        }
    }
}
