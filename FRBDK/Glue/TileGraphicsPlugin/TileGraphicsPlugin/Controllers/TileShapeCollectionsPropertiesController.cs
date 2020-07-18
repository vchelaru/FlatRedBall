using FlatRedBall.Glue.Elements;
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
using TMXGlueLib;

namespace TileGraphicsPlugin.Controllers
{
    public class TileShapeCollectionsPropertiesController : 
        Singleton<TileShapeCollectionsPropertiesController>
    {
        Views.TileShapeCollectionProperties view;
        ViewModels.TileShapeCollectionPropertiesViewModel viewModel;

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
            switch(e.PropertyName)
            {
                case nameof(viewModel.SourceTmxName):
                    RefreshAvailableTypes();
                    break;
            }
        }

        public void RefreshAvailableTypes()
        {
            viewModel.AvailableTypes.Clear();

            var tmxName = viewModel.SourceTmxName;
            var element = GlueState.Self.CurrentElement;

            HashSet<string> types = new HashSet<string>();

            void AddTypesFromNos(NamedObjectSave nos)
            {
                if(nos.SourceType == SourceType.File && !string.IsNullOrWhiteSpace( nos.SourceFile))
                {
                    var element = nos.GetContainer();

                    var file = element?.GetReferencedFileSave(nos.SourceFile);

                    if(file != null)
                    {
                        var fullPath = GlueCommands.Self.FileCommands.GetFullFileName(file);

                        TiledMapSave tiledMapSave = TiledMapSave.FromFile(fullPath);

                        foreach(var tileset in tiledMapSave.Tilesets)
                        {
                            foreach(var kvp in tileset.TileDictionary)
                            {
                                var value = kvp.Value;

                                if(!string.IsNullOrWhiteSpace(value.Type))
                                {
                                    types.Add(value.Type);
                                }
                            }
                        }
                    }
                }
            }

            var baseNos = element.GetNamedObjectRecursively(tmxName);

            if(baseNos != null)
            {
                if(baseNos.SetByDerived)
                {
                    var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(element);
                    var noses = derivedElements.Select(item => item.GetNamedObjectRecursively(tmxName))
                        .ToArray();

                    foreach(var nos in noses)
                    {
                        AddTypesFromNos(nos);
                    }
                }
            }

            // todo - apply hashset to the view model
            foreach(var item in types)
            {
                viewModel.AvailableTypes.Add(item);
            }
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

            viewModel.GlueObject = namedObject;

            RefreshAvailableTiledObjects(element);

            viewModel.UpdateFromGlueObject();

            viewModel.IsEntireViewEnabled = namedObject.DefinedByBase == false;

            RefreshAvailableTypes();

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
