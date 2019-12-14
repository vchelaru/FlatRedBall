using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
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

            var doesProjectContainAnyTmxFiles = allScreens.Any(item => item.ReferencedFiles.Any(IsRfsTiledMap));

            if(doesProjectContainAnyTmxFiles)
            {
                var viewModel = new AdditionalEntitiesControlViewModel();

                var control = new AdditionalEntitiesControls();
                control.DataContext = viewModel;
                window.AddToStackPanel(control);
            }
        }
    }
}
