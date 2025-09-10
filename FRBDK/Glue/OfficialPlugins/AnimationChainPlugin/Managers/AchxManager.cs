using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Services;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.Common.ViewModels;
using OfficialPlugins.ContentPreview.Views;
using SpineAtlasLibrary;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

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

    public AchxPreviewView GetView()
    {
        CreateViewIfNecessary();

        return View;
    }

    private void CreateViewIfNecessary()
    {
        if (View == null)
        {
            TextureCoordinateSelectionViewModel textureCoordinateSelectionViewModel =
                new TextureCoordinateSelectionViewModel();
            ViewModel = new AchxViewModel(this, textureCoordinateSelectionViewModel,
                Builder.Get<IDialogCommands>(),
                Builder.Get<IFileCommands>());
            ViewModel.PropertyChanged += HandleViewModelPropertyChanged;

            View = new AchxPreviewView(ViewModel, textureCoordinateSelectionViewModel,
                new SpritePlugin.Managers.CameraLogic(), new SpritePlugin.Managers.CameraLogic(),
                Builder.Get<IDialogCommands>()

                );
        }
    }

    private static void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.CurrentAnimationChain):
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
                else if(ViewModel.CurrentAnimationChain != null)
                {
                    View.PropertyGrid.Visibility = System.Windows.Visibility.Visible;
                    PropertyGridManager.ShowInPropertyGrid(ViewModel.CurrentAnimationChain, ViewModel);
                }
                break;
        }
    }

    public static void HideTab() => Tab?.Hide();

    public static void HandleStrongSelect()
    {
        Tab?.Focus();
    }

    internal void ShowTab(FilePath filePath)
    {
        var view = GetView();
        var changedFilePath = view.AchxFilePath != filePath;


        view.AchxFilePath = filePath;
        if (changedFilePath)
        {
            view.ResetCamera();
        }

        if (Tab == null)
        {
            Tab = Plugin.CreateTab(view, "Animation Editor", TabLocation.Center);
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

    public void SaveCurrentAchx()
    {
        ViewModel.SaveCurrentAchx();
    }

}
