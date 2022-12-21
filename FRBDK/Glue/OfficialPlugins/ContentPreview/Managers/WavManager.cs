using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
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

        public static void Initialize(PluginBase plugin)
        {
            Plugin = plugin;
        }

        public static WavPreviewView GetView(FilePath filePath)
        {
            CreateViewIfNecessary();

            return View;
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

        internal static void ShowTab(FilePath filePath)
        {
            var view = GetView(filePath);
            var vm = ViewModel;
            view.WavFilePath = filePath;
            
            if (Tab == null)
            {
                Tab = Plugin.CreateTab(view, "WAV Preview", TabLocation.Center);
            }

            Tab.Show();
        }
    }
}
