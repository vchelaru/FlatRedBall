using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RedGrinPlugin.ViewModels
{
    public class NetworkScreenViewModel : ViewModel
    {
        public bool IsNetworkScreen
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(IsNetworkScreen))]
        public Visibility Game1CodeVisibility
        {
            get
            {
                if (IsNetworkScreen)
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Collapsed;
                }
            }
        }

        internal void SetFrom(ScreenSave screen)
        {
            IsNetworkScreen = screen.Properties.GetValue<bool>(nameof(IsNetworkScreen));

        }

        internal void ApplyTo(ScreenSave screen)
        {
            screen.Properties.SetValue(nameof(IsNetworkScreen), IsNetworkScreen);
        }

        internal static bool IsNetworked(ScreenSave screen)
        {
            return screen.Properties.GetValue<bool>(nameof(NetworkScreenViewModel.IsNetworkScreen));
        }
    }
}
