using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Plugins.EmbeddedPlugins.CameraPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    /// <summary>
    /// Interaction logic for CameraSettingsControl.xaml
    /// </summary>
    public partial class CameraSettingsControl : UserControl
    {
        DisplaySettingsViewModel ViewModel => DataContext as DisplaySettingsViewModel;

        public CameraSettingsControl()
        {
            InitializeComponent();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }

        int[] ScaleValues = new int[] { 1600, 1200, 1000, 800, 600, 500, 400, 300, 250, 200, 150, 100, 75, 50, 33, 25, 10 };
        private void ScaleMinusClicked(object sender, RoutedEventArgs e)
        {
            var currentScale = ViewModel.Scale;

            for(int i = 0; i < ScaleValues.Length; i++)
            {
                if(ScaleValues[i] < currentScale)
                {
                    ViewModel.Scale = ScaleValues[i];
                    break;
                }
            }
        }

        private void ScalePlusClicked(object sender, RoutedEventArgs e)
        {
            var currentScale = ViewModel.Scale;

            for(int i = ScaleValues.Length-1; i > -1; i--)
            {
                if(ScaleValues[i] > currentScale)
                {
                    ViewModel.Scale = ScaleValues[i];
                    break;
                }
            }
        }

        //private void StretchRadioButtonGumChecked(object sender, RoutedEventArgs e)
        //{
        //    StretchAreaGumMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    StretchAreaGumMediaElement.Play();
        //}

        //private void IncreaseAreaRadioButtonGumChecked(object sender, RoutedEventArgs e)
        //{
        //    IncreaseAreaGumMediaElement.Position = TimeSpan.FromMilliseconds(1000);
        //    IncreaseAreaGumMediaElement.Play();
        //}


        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenBrowser(e.Uri.AbsoluteUri);
            e.Handled = true;

        }

        void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        private void HandleMediaEnded(object sender, RoutedEventArgs e)
        {
            (sender as MediaElement).Position = TimeSpan.FromMilliseconds(1);
            //(sender as MediaElement).Play();
        }

        private void SaveClicked(object sender, RoutedEventArgs args)
        {
            var tiw = new CustomizableTextInputWindow();

            tiw.Label.Text = "Enter a name for the display setting";

            var result = tiw.ShowDialog();

            // todo - no empty allowed...

            if(result == true)
            {
                var newEntry = ViewModel.ToDisplaySettings();
                newEntry.Name = tiw.Result;

                TaskManager.Self.Add( () =>
                {
                    var glueProject = GlueState.Self.CurrentGlueProject;

                    var existingWithMatchingName = glueProject.AllDisplaySettings
                        .FirstOrDefault(item => item.Name == newEntry.Name);

                    if(existingWithMatchingName != null)
                    {
                        glueProject.AllDisplaySettings.Remove(existingWithMatchingName);
                    }

                    glueProject.AllDisplaySettings.Add(newEntry);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }, "Saving project due to camera changes");

                var existingVmOption = ViewModel.AvailableOptions
                    .FirstOrDefault(item => item.Name == newEntry.Name);

                if(existingVmOption != null)
                {
                    ViewModel.AvailableOptions.Remove(existingVmOption);
                }

                ViewModel.AvailableOptions.Add(newEntry);
                ViewModel.SelectedOption = newEntry;

            }
        }

        private void DeleteClicked(object sender, RoutedEventArgs args)
        {
            ////////////Early Out///////////////
            if(ViewModel.SelectedOption == null)
            {
                return;
            }
            //////////End Early Out/////////////

            var name = ViewModel.SelectedOption.Name;

            ViewModel.AvailableOptions.Remove(ViewModel.SelectedOption);
            ViewModel.SelectedOption = ViewModel.AvailableOptions.FirstOrDefault();
            TaskManager.Self.Add(() =>
            {
                var existing = GlueState.Self.CurrentGlueProject.AllDisplaySettings
                    .FirstOrDefault(item => item.Name == name);
                if(existing != null)
                {
                    GlueState.Self.CurrentGlueProject.AllDisplaySettings.Remove(existing);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                }
            }, "Removing Display Option");
        }

        private void PresetResolutionDropdownClick(object sender, RoutedEventArgs e)
        {
            ResolutionDropdown.Items.Clear();

            void Add(int width, int height)
            {
                var vm = new ResolutionDropDownViewModel(width, height);
                var menuItem = new MenuItem();
                menuItem.Header = vm;
                menuItem.Click += (not, used) =>
                {
                    ViewModel.ResolutionWidth = width;
                    ViewModel.ResolutionHeight = height;
                };
                ResolutionDropdown.Items.Add(menuItem);
            }
            Add(256, 224);

            Add(360,240);
            Add(480,360);
            Add(640,480);
            Add(800,600);
            Add(1024,768);
            Add(1920,1080);

            ResolutionDropdown.IsOpen = true;


        }

    }
}
