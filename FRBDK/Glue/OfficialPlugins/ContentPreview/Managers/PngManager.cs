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
    internal static class PngManager
    {
        static PngPreviewView View;

        public static PngViewModel ViewModel { get; private set; }

        public static PngPreviewView GetView(FilePath filePath)
        {
            CreateViewIfNecessary();

            return View;
        }

        private static void CreateViewIfNecessary()
        {
            if(View == null)
            {
                View = new PngPreviewView();
                ViewModel = new PngViewModel();
                View.DataContext = ViewModel;
                View.Initialize(new SpritePlugin.Managers.CameraLogic());
            }
        }
    }
}
