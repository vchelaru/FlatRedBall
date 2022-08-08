using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TileGraphicsPlugin.ViewModels
{
    class AdditionalEntitiesControlViewModel : ViewModel
    {
        public Visibility AllTileMapUiVisibility
        {
            get => Get<Visibility>();
            set => Set(value);
        }

        public Visibility ScreenListVisibility => Visibility.Visible;

        public bool IncludeListsInScreens
        {
            get => Get<bool>();
            set => Set(value); 
        }

        public AdditionalEntitiesControlViewModel()
        {
            // These will be set to true. The logic
            // behind setting these true is:
            // If they're set to true, the user may get
            // additional lists and factories that aren't
            // used (if they don't want it).
            // If they're set to false, a user may be confused
            // as to why entities aren't showing up. It's better
            // to avoid confusion and have unwanted functionality
            // generated
            IncludeListsInScreens = true;
        }
    }
}
