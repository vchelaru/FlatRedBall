using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.SpritePlugin.Managers;
using PropertyTools.Wpf;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OfficialPlugins.ContentPreview.Views
{
    /// <summary>
    /// Interaction logic for AchxPreviewView.xaml
    /// </summary>
    public partial class AchxPreviewView : UserControl
    {
        #region Fields/Properties

        AchxViewModel ViewModel => DataContext as AchxViewModel;


        //FilePath textureFilePath;
        //FilePath TextureFilePath
        //{
        //    get => textureFilePath;
        //    set
        //    {
        //        if (value != textureFilePath)
        //        {
        //            ForceRefreshMainSpriteTexture(value);
        //            ForceRefreshMainAnimationSpriteTexture(value);
        //        }
        //    }
        //}

        FilePath achxFilePath;
        public FilePath AchxFilePath
        {
            get => achxFilePath;
            set
            {
                if(value != achxFilePath)
                {
                    achxFilePath = value;
                    ForceRefreshAchx(value);
                }
            }
        }

        public SKBitmap Texture => TopWindowManager.Texture;

        CameraLogic TopWindowCameraLogic;
        CameraLogic BottomWindowCameraLogic;

        TopWindowManager TopWindowManager;
        BottomWindowManager BottomWindowManager;

        #endregion

        public AchxPreviewView()
        {
            InitializeComponent();

            this.Loaded += HandleLoaded;

            this.DataContextChanged += HandleDataContextChanged;
            //MemberCategoryManager.SetMemberCategories(PropertyGrid);

            PropertyGridManager.Initialize(PropertyGrid);

        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.SelectedAnimationChain):

                    TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
                    TopWindowManager.RefreshTexture(achxFilePath, ViewModel.SelectedAnimationChain);

                    if(ViewModel.SelectedAnimationChain != null)
                    {
                        BottomWindowManager.ForceRefreshMainAnimationSpriteTexture(TopWindowManager.TextureFilePath);
                    }

                    BottomWindowManager.RefreshAnimationPreview(ViewModel);

                    TopGumCanvas.InvalidateVisual();
                    BottomGumCanvas.InvalidateVisual();

                    break;
                case nameof(ViewModel.IsShowGuidesChecked):
                    BottomWindowManager.RefreshBottomGuideVisibility(ViewModel);
                    break;
            }
        }

        public void ForceRefreshAchx(FilePath achxFilePath = null, bool preserveSelection = false)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                var previouslySelected = ViewModel.SelectedAnimationChain;

                achxFilePath = achxFilePath ?? this.AchxFilePath;

                AnimationChainListSave animationChain = null;
                if (achxFilePath?.Exists() == true)
                {
                    animationChain = AnimationChainListSave.FromFile(achxFilePath.FullPath);
                }
                ViewModel.BackgingData = animationChain;

                TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
                
                TopWindowManager.RefreshTexture(achxFilePath, ViewModel.SelectedAnimationChain);

                if (ViewModel.SelectedAnimationChain != null)
                {
                    BottomWindowManager.ForceRefreshMainAnimationSpriteTexture(TopWindowManager.TextureFilePath);
                }

                RefreshTreeView(animationChain);

                if(preserveSelection && previouslySelected != null)
                {
                    ViewModel.SelectedAnimationChain = ViewModel.VisibleRoot
                        .FirstOrDefault(item => item.Name == previouslySelected.Name);
                }

            });
        }

        private void RefreshTreeView(AnimationChainListSave animationChain)
        {
            ViewModel.VisibleRoot.Clear();

            if (animationChain == null) return;

            foreach(var animation in animationChain.AnimationChains)
            {
                var animationViewModel = new AnimationChainViewModel();
                animationViewModel.SetFrom(animation, ViewModel.ResolutionWidth, ViewModel.ResolutionHeight);
                ViewModel.VisibleRoot.Add(animationViewModel);

                animationViewModel.FrameUpdatedByUi += HandleFrameUpdated;
            }
        }

        private void HandleFrameUpdated()
        {
            TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
            TopGumCanvas.InvalidateVisual();
            BottomWindowManager.RefreshAnimationPreview(ViewModel);
            BottomGumCanvas.InvalidateVisual();

            // now save it:
            var animationChain = ViewModel.BackgingData;
            var filePath = this.achxFilePath;

            GlueCommands.Self.FileCommands.IgnoreChangeOnFileUntil(
                filePath, DateTimeOffset.Now.AddSeconds(2));
            try
            {
                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    animationChain.Save(filePath.FullPath);
                });
            }
            catch (Exception ex)
            {
                GlueCommands.Self.PrintError(ex.ToString());
            }
        }



        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            TopWindowManager.FillSpriteToView(ViewModel);
        }

        public void Initialize(CameraLogic topWindowCameraLogic, CameraLogic bottomWindowCameraLogic)
        {
            this.TopWindowCameraLogic = topWindowCameraLogic;
            this.BottomWindowCameraLogic = bottomWindowCameraLogic;

            TopWindowManager = new TopWindowManager(TopGumCanvas, this, topWindowCameraLogic, (this.DataContext as AchxViewModel)?.TopWindowZoom);

            BottomWindowManager = new BottomWindowManager(BottomGumCanvas, this, BottomWindowCameraLogic, (this.DataContext as AchxViewModel)?.BottomWindowZoom);
        }

        private void GumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TopWindowCameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            TopGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(TopGumCanvas);
        }

        private void GumCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            TopWindowCameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TopWindowCameraLogic.HandleMouseWheel(e);
        }

        private void GumAnimationCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BottomWindowCameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            BottomGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(BottomGumCanvas);
        }

        private void GumAnimationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            BottomWindowCameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void GumAnimationCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            BottomWindowCameraLogic.HandleMouseWheel(e);
        }

        public void ResetCamera() => TopWindowManager.ResetCamera(ViewModel);

        private void TreeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject originalSource = e.OriginalSource as DependencyObject;
            var treeViewItem = GetTreeViewItemFromOriginalSource(originalSource);

            if (treeViewItem != null)
            {
                if(treeViewItem.DataContext is AnimationChainViewModel animationChainVm)
                {
                    BottomWindowManager.FocusSingleToSprite();
                    FocusWholeToAnimation(animationChainVm.BackingModel);
                }
                else if(treeViewItem.DataContext is AnimationFrameViewModel animationFrameVm)
                {
                    BottomWindowManager.FocusSingleToSprite();
                    TopWindowManager.FocusWholeToFrame(animationFrameVm.BackingModel, ViewModel);
                }
            }
        }

        private void FocusWholeToAnimation(AnimationChainSave backingModel)
        {
            if (backingModel.Frames.Count > 0)
            {
                var firstFrame = backingModel.Frames[0];
                TopWindowManager.FocusWholeToFrame(firstFrame, ViewModel);
            }
        }

        private TreeListBoxItem GetTreeViewItemFromOriginalSource(DependencyObject originalSource)
        {
            while (originalSource != null && !(originalSource is TreeListBoxItem) && !(originalSource is TreeView))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            return originalSource as TreeListBoxItem;
        }
    }
}
