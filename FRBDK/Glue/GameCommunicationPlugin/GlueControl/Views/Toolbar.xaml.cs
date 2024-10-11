using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameCommunicationPlugin.GlueControl
{
    /// <summary>
    /// Interaction logic for RunnerToolbar.xaml
    /// </summary>
    public partial class RunnerToolbar : UserControl
    {
        public event EventHandler RunClicked;

        RunnerToolbarViewModel ViewModel => DataContext as RunnerToolbarViewModel;

        public bool IsOpen
        {
            get => SplitButton.IsOpen;
            set => SplitButton.IsOpen = value;
        }

        public RunnerToolbar()
        {
            InitializeComponent();

            InitializeItemsControlTemplate();
        }

        private void InitializeItemsControlTemplate()
        {
        }

        private void HandleButtonClick(object sender, RoutedEventArgs args)
        {
            RunClicked?.Invoke(this, null);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var screen = (sender as MenuItem).Header as ScreenReferenceViewModel;

            ViewModel.StartupScreenName = screen.ScreenName;

            IsOpen = false;
        }


        private void SearchBar_ClearSearchButtonClicked()
        {
            ViewModel.SearchBoxText = null;
            IsOpen = true;
        }

        private void SplitButton_Closed(object sender, RoutedEventArgs e)
        {
            foreach(var item in ViewModel.AllScreens)
            {
                item.IsSelected = false;
            }
            ViewModel.SearchBoxText = null;
        }

        private void SearchBar_ArrowKeyPushed(Key key)
        {
            if (key == Key.Up)
            {
                var highlighted = ViewModel.AvailableScreens.FirstOrDefault(item => item.IsSelected);
                if (highlighted != null)
                {
                    var index = ViewModel.AvailableScreens.IndexOf(highlighted);

                    if (index > 0)
                    {
                        highlighted.IsSelected = false;

                        ViewModel.AvailableScreens[index - 1].IsSelected = true;
                    }
                }
                else
                {
                    var toSelect = ViewModel.AvailableScreens.FirstOrDefault();
                    if (toSelect != null)
                    {
                        toSelect.IsSelected = true;
                    }
                }
            }
            else if (key == Key.Down)
            {
                var highlighted = ViewModel.AvailableScreens.FirstOrDefault(item => item.IsSelected);
                if (highlighted != null)
                {
                    var index = ViewModel.AvailableScreens.IndexOf(highlighted);

                    if (index < ViewModel.AvailableScreens.Count - 1)
                    {
                        highlighted.IsSelected = false;

                        ViewModel.AvailableScreens[index + 1].IsSelected = true;
                    }
                }
                else
                {
                    var toSelect = ViewModel.AvailableScreens.FirstOrDefault();
                    if (toSelect != null)
                    {
                        toSelect.IsSelected = true;
                    }
                }
            }
        }

        private void SearchBar_EnterPressed()
        {
            var highlighted = ViewModel.AvailableScreens.FirstOrDefault(item => item.IsSelected);
            if (highlighted != null)
            {
                ViewModel.StartupScreenName = highlighted.ScreenName;
            }
        }

        internal void HighlightFirstItem()
        {
            if (ViewModel.AvailableScreens.Count > 0)
            {
                ViewModel.AvailableScreens[0].IsSelected = true;
            }
        }

        private void SplitButton_Opened(object sender, RoutedEventArgs e)
        {
            HighlightFirstItem();
        }
    }
}
