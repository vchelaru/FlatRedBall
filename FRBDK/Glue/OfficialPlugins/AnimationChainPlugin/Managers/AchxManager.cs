using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.ContentPreview.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal class AchxManager
    {
        static AchxPreviewView View;

        static PluginBase Plugin;
        static PluginTab Tab;

        public static FilePath AchxFilePath => View?.AchxFilePath;

        public static AchxViewModel ViewModel { get; private set; }

        public static void Initialize(PluginBase plugin)
        {
            Plugin = plugin;
        }

        public static AchxPreviewView GetView()
        {
            CreateViewIfNecessary();

            return View;
        }

        private static void CreateViewIfNecessary()
        {
            if (View == null)
            {
                View = new AchxPreviewView();
                ViewModel = new AchxViewModel();
                ViewModel.IsShowGuidesChecked = true;
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
                View.DataContext = ViewModel;
                View.Initialize(new SpritePlugin.Managers.CameraLogic(), new SpritePlugin.Managers.CameraLogic());
            }
        }

        private static void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.SelectedAnimationChain):
                case nameof(ViewModel.SelectedAnimationFrame):
                case nameof(ViewModel.SelectedShape):
                    if(ViewModel.SelectedShape != null)
                    {
                        if(ViewModel.SelectedShape is CircleViewModel circle)
                        {
                            View.PropertyGrid.Visibility = System.Windows.Visibility.Visible;
                            PropertyGridManager.ShowInPropertyGrid(circle);
                        }
                        else
                        {
                            View.PropertyGrid.Visibility = System.Windows.Visibility.Hidden;

                        }
                    }
                    else if(ViewModel.SelectedAnimationFrame != null)
                    {
                        View.PropertyGrid.Visibility = System.Windows.Visibility.Visible;
                        PropertyGridManager.ShowInPropertyGrid(ViewModel.SelectedAnimationFrame);
                    }
                    else if(ViewModel.SelectedAnimationChain != null)
                    {
                        View.PropertyGrid.Visibility = System.Windows.Visibility.Visible;
                        PropertyGridManager.ShowInPropertyGrid(ViewModel.SelectedAnimationChain);
                    }
                    break;
            }
        }

        public static void HideTab() => Tab?.Hide();

        public static void HandleStrongSelect()
        {
            Tab?.Focus();
        }

        internal static void ShowTab(FilePath filePath)
        {
            var view = GetView();
            var changedFilePath = view.AchxFilePath != filePath;


            view.AchxFilePath = filePath;
            var vm = ViewModel;
            vm.ResolutionWidth = view.Texture?.Width ?? 0;
            vm.ResolutionHeight = view.Texture?.Height ?? 0;
            if (changedFilePath)
            {
                view.ResetCamera();
            }

            if (Tab == null)
            {
                Tab = Plugin.CreateTab(view, "ACHX Preview", TabLocation.Center);
            }

            Tab.Show();
            view.TopGumCanvas.InvalidateVisual();
        }


        internal static void ForceRefreshAchx(FilePath filePath) =>
            View.ForceRefreshAchx(filePath, preserveSelection:true);

        internal static bool GetIfIsHandlingHotkeys()
        {
            if( Tab == null || View == null)
            {
                return false;
            }
            else
            {
                return View.GetIfIsHandlingHotkeys();
            }
        }
    }
}
