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
        // August 8, 2022
        // History on this:
        // The ability to add
        // lists to GameScreen
        // originally was created
        // to simplify the creation
        // of entities through Tiled.
        // Over time it became standard
        // to add lists of entities to the
        // GameScreen regardless of whether
        // there is a Tiled map. Since the option
        // to add Lists to GameScreen is no longer
        // tied to Tiled, this could (should?) get moved
        // out into a separate plugin. But...it doesn't matter
        // that much because the Tiled plugin is always active.
        // If more work is being done here, eventually we'll want
        // to move this to its own plugin.
        public static void HandleModifyAddEntityWindow(AddEntityWindow window)
        {
            var allScreens = GlueState.Self.CurrentGlueProject.Screens;

            var doesProjectHaveGameScreen = allScreens.Any(item => item.ClassName == "GameScreen" ||
                item.ClassName.EndsWith(".GameScreen"));

            if(doesProjectHaveGameScreen)
            {
                var viewModel = new AdditionalEntitiesControlViewModel();

                var commonViewModel = window.DataContext as AddEntityViewModel;
                commonViewModel.PropertyChanged += (sender, args) =>
                {
                    switch(args.PropertyName)
                    {
                        case nameof(AddEntityViewModel.HasInheritance):
                            viewModel.AllTileMapUiVisibility = (commonViewModel.HasInheritance == false).ToVisibility();
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
