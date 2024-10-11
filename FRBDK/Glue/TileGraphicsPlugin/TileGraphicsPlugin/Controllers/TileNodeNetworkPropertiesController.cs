using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TileGraphicsPlugin;
using TMXGlueLib;

namespace TiledPlugin.Controllers
{
    public class TileNodeNetworkPropertiesController : 
        Singleton<TileNodeNetworkPropertiesController>
    {
        Views.TileNodeNetworkProperties view;
        ViewModels.TileNodeNetworkPropertiesViewModel viewModel;

        public Views.TileNodeNetworkProperties GetView()
        {
            if(view == null)
            {
                view = new Views.TileNodeNetworkProperties();
                viewModel = new ViewModels.TileNodeNetworkPropertiesViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                view.DataContext = viewModel;
            }
            return view;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.SourceTmxName):
                    var element = GlueState.Self.CurrentElement;
                    RefreshAvailableTypes(element);
                    RefreshAvailableLayers(element);
                    break;
            }
        }

        public void RefreshAvailableTypes(GlueElement element)
        {
            viewModel.AvailableTypes.Clear();
            var tmxName = viewModel.SourceTmxName;

            var types = TileGraphicsPlugin.Controllers.TileShapeCollectionsPropertiesController
                .GetAvailableTypes(tmxName, element)
                .OrderBy(item => item)
                ;

            // todo - apply hashset to the view model
            foreach (var item in types)
            {
                viewModel.AvailableTypes.Add(item);
            }
        }

        public void RefreshAvailableLayers(GlueElement element)
        {
            viewModel.AvailableLayerNames.Clear();
            var tmxName = viewModel.SourceTmxName;

            var layers = TileGraphicsPlugin.Controllers.TileShapeCollectionsPropertiesController.GetAvailableLayers(tmxName, element);

            foreach(var item in layers)
            {
                viewModel.AvailableLayerNames.Add(item);
            }
        }


        public bool IsTileNodeNetwork(NamedObjectSave namedObject)
        {
            var isTileNodeNetwork = false;

            if (namedObject != null)
            {
                var ati = namedObject.GetAssetTypeInfo();
                isTileNodeNetwork =
                            ati == AssetTypeInfoAdder.Self.TileNodeNetworkAssetTypeInfo;
            }

            return isTileNodeNetwork;
        }

        public void RefreshViewModelTo(NamedObjectSave namedObject, GlueElement element)
        {
            // Disconnect the view from the view model so that 
            // changes that happen here dont' have side effects
            // through the view. For example, if we don't disconnect
            // the two, then clearing the available TMX objects will set
            // the selection to null, and even persist it to the Glue object
            view.DataContext = null;

            viewModel.GlueObject = namedObject;

            RefreshAvailableTiledObjects(element);

            viewModel.UpdateFromGlueObject();

            viewModel.IsEntireViewEnabled = namedObject.DefinedByBase == false;

            RefreshAvailableTypes(element);
            RefreshAvailableLayers(element);

            view.DataContext = viewModel;
        }

        private void RefreshAvailableTiledObjects(IElement element)
        {
            // refresh availble TMXs
            IEnumerable<ReferencedFileSave> referencedFileSaves = TileGraphicsPlugin.Controllers.TileShapeCollectionsPropertiesController.GetTmxFilesIn(element);

            IEnumerable<NamedObjectSave> namedObjects = TileGraphicsPlugin.Controllers.TileShapeCollectionsPropertiesController.GetTmxNamedObjectsIn(element);

            if (viewModel.TmxObjectNames == null)
            {
                viewModel.TmxObjectNames = new System.Collections.ObjectModel.ObservableCollection<string>();
            }
            viewModel.TmxObjectNames.Clear();

            var tempList = new List<string>();

            foreach (var rfs in referencedFileSaves)
            {
                tempList.Add(rfs.GetInstanceName());
            }
            foreach (var nos in namedObjects)
            {
                tempList.Add(nos.InstanceName);
            }

            foreach(var item in tempList.OrderBy(item => item))
            {
                viewModel.TmxObjectNames.Add(item);
            }
        }

    }
}
