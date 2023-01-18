using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using NAudio.Wave;
using OfficialPlugins.ContentPreview.ViewModels;
using OfficialPlugins.ContentPreview.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ContentPreview.Managers
{
    internal static class WavManager
    {
        static WavPreviewView View;

        static PluginBase Plugin;
        static PluginTab Tab;

        public static WavViewModel ViewModel { get; private set; }

        public static FilePath WavFilePath => View?.WavFilePath;

        public static void Initialize(PluginBase plugin)
        {
            Plugin = plugin;
        }

        private static void CreateViewIfNecessary()
        {
            if (View == null)
            {
                View = new WavPreviewView();
                ViewModel = new WavViewModel();
                View.DataContext = ViewModel;
            }
        }

        public static void HideTab() => Tab?.Hide();

        public static void HandleStrongSelect()
        {
            Tab?.Focus();
            View.PlaySound();
        }

        internal static void ShowTab(FilePath filePath)
        {
            CreateViewIfNecessary();
            View.WavFilePath = filePath;

            if(filePath?.Exists() == true)
            {
                var wf = new WaveFileReader(filePath.FullPath);
                ViewModel.Duration = wf.TotalTime;
            }
            else
            {
                ViewModel.Duration = new TimeSpan();
            }
            if (Tab == null)
            {
                Tab = Plugin.CreateTab(View, "WAV Preview", TabLocation.Center);
            }

            Tab.Show();
        }

        internal static void ForceRefreshWav(FilePath filePath) => View?.ForceRefreshToFilePath(filePath);
    }
}
