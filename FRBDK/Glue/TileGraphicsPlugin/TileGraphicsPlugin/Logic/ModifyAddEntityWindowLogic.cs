using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TileGraphicsPlugin.ViewModels;
using TileGraphicsPlugin.Views;

namespace TileGraphicsPlugin.Logic
{
    class ModifyAddEntityWindowLogic
    {
        public static void HandleModifyAddEntityWindow(AddEntityWindow window)
        {
            // See if any screens have tile maps
            var allScreens = GlueState.Self.CurrentGlueProject.Screens;

            bool IsRfsTiledMap(ReferencedFileSave rfs)
            {
                return rfs.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo;
            }

            var doesProjectContainAnyTmxFiles = allScreens.Any(item =>
            {
                var hasRfs = item.ReferencedFiles.Any(IsRfsTiledMap);

                if(!hasRfs)
                {
                    hasRfs = item.NamedObjects.Any(nos => nos.GetAssetTypeInfo() == AssetTypeInfoAdder.Self.TmxAssetTypeInfo);
                }

                return hasRfs;
            });

            if(doesProjectContainAnyTmxFiles)
            {
                var viewModel = new AdditionalEntitiesControlViewModel();

                var commonViewModel = window.DataContext as AddEntityViewModel;
                commonViewModel.PropertyChanged += (sender, args) =>
                {
                    switch(args.PropertyName)
                    {
                        case nameof(AddEntityViewModel.SelectedBaseEntity):
                            var isNone = commonViewModel.SelectedBaseEntity == "<NONE>";
                            viewModel.AllTileMapUiVisibility = isNone.ToVisibility();
                            break;
                    }
                };

                var control = new AdditionalEntitiesControls();
                control.DataContext = viewModel;
                window.AddControl(control);
            }
        }
    }
}
