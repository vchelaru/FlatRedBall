using AsepriteDotNet;
using FlatRedBall.Attributes;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Controls.DataUi;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.AnimationChainPlugin.ViewModels;
using OfficialPlugins.Common.Controls;
using OfficialPlugins.SpritePlugin.Managers;
using PropertyTools.Wpf;
using RenderingLibrary;
using SkiaGum.GueDeriving;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        #region Initialization

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += HandleViewModelPropertyChanged;
                InitializeSettingsPropertyGrid();

                ViewModel.Settings.PropertyChanged += HandleSettingsPropertyChanged;
                ViewModel.AnimationChainCollectionChanged += HandleAnimationChainCollectionChanged;
                ViewModel.FrameUpdatedByUi += HandleFrameUpdated;
            }
        }

        private void InitializeSettingsPropertyGrid()
        {
            this.SettingsPropertyGrid.Instance = ViewModel.Settings;

            GetMember(nameof(ViewModel.Settings.BackgroundColor)).PreferredDisplayer = typeof(ColorDisplay);

            WpfDataUi.DataTypes.InstanceMember GetMember(string memberName)
            {
                foreach (var category in SettingsPropertyGrid.Categories)
                {
                    var found = category.Members.FirstOrDefault(item => item.Name == memberName);
                    if (found != null)
                    {
                        return found;
                    }
                }
                return null;
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

            TopWindowManager = new TopWindowManager(TopGumCanvas, this, topWindowCameraLogic,
                ViewModel?.TopWindowZoom, ViewModel.Settings);

            BottomWindowManager = new BottomWindowManager(BottomGumCanvas, this, BottomWindowCameraLogic,
                ViewModel?.BottomWindowZoom, ViewModel.Settings);
        }

        #endregion




        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.CurrentAnimationChain):

                    TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
                    TopWindowManager.RefreshTexture(achxFilePath, ViewModel.CurrentAnimationChain);

                    if (ViewModel.CurrentAnimationChain != null)
                    {
                        BottomWindowManager.ForceRefreshMainAnimationSpriteTexture(TopWindowManager.TextureFilePath);
                    }

                    BottomWindowManager.RefreshAnimationPreview(ViewModel);

                    TopGumCanvas.InvalidateVisual();
                    BottomGumCanvas.InvalidateVisual();

                    // ForceGumLayout is needed to calculate the bars:
                    TopGumCanvas.ForceGumLayout();
                    RefreshTopWindowScrollBars();

                    break;
            }
        }

        private void HandleSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(ViewModel.Settings.ShowGuides):
                    BottomWindowManager.RefreshBottomGuideVisibility(ViewModel);
                    break;
            }

        }

        public void ForceRefreshAchx(FilePath achxFilePath = null, bool preserveSelection = false)
        {
            GlueCommands.Self.DoOnUiThread(() =>
            {
                var previouslySelected = ViewModel.CurrentAnimationChain;

                achxFilePath = achxFilePath ?? this.AchxFilePath;

                AnimationChainListSave animationChainListSave = null;
                if (achxFilePath?.Exists() == true)
                {
                    animationChainListSave = AnimationChainListSave.FromFile(achxFilePath.FullPath);
                }
                ViewModel.BackgingData = animationChainListSave;
                ViewModel.AchxFilePath = achxFilePath;

                TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
                
                TopWindowManager.RefreshTexture(achxFilePath, ViewModel.CurrentAnimationChain);

                if (ViewModel.CurrentAnimationChain != null)
                {
                    BottomWindowManager.ForceRefreshMainAnimationSpriteTexture(TopWindowManager.TextureFilePath);
                }

                ViewModel.SetFrom(animationChainListSave, achxFilePath, 
                    TopWindowManager.MainSprite.Texture?.Width??0,
                    TopWindowManager.MainSprite.Texture?.Height??0);

                if(preserveSelection && previouslySelected != null)
                {
                    ViewModel.CurrentAnimationChain = ViewModel.VisibleRoot
                        .FirstOrDefault(item => item.Name == previouslySelected.Name);
                }

            });
        }

        private void HandleAnimationChainCollectionChanged(AnimationChainViewModel model, NotifyCollectionChangedEventArgs args)
        {
            SaveCurrentAchx();
        }

        HashSet<string> FramePropertiesWhichShouldNotTriggerSave = new()
        {
            nameof(AnimationFrameViewModel.Text)
        };

        private void HandleFrameUpdated(AnimationFrameViewModel frame, string propertyName)
        {

            //////////////////////Early Out////////////////////////////////
            if(FramePropertiesWhichShouldNotTriggerSave.Contains(propertyName))
            {
                return;
            }
            ////////////////////End Early Out//////////////////////////////

            if (propertyName == nameof(AnimationFrameViewModel.RelativeTextureName))
            {
                // refresh the top view:
                TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
                TopWindowManager.RefreshTexture(achxFilePath, ViewModel.CurrentAnimationChain);

                if (ViewModel.CurrentAnimationChain != null)
                {
                    BottomWindowManager.ForceRefreshMainAnimationSpriteTexture(TopWindowManager.TextureFilePath);
                }
            }
            TopWindowManager.RefreshTopCanvasOutlines(ViewModel);
            TopGumCanvas.InvalidateVisual();
            BottomWindowManager.RefreshAnimationPreview(ViewModel);
            BottomGumCanvas.InvalidateVisual();

            SaveCurrentAchx();
        }

        private void SaveCurrentAchx()
        {
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

        #region Top Canvas Events

        private void TopGumCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TopWindowCameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            TopGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(TopGumCanvas);
        }

        private void TopGumCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var moved = TopWindowCameraLogic.HandleMouseMove(e);
            if(moved)
            {
                RefreshTopWindowScrollBars();
            }
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void TopGumCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var changed = TopWindowCameraLogic.HandleMouseWheel(e);
            if(changed)
            {
                RefreshTopWindowScrollBars();
            }
        }

        private void TopGumCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TopGumCanvas.ForceGumLayout();
            RefreshTopWindowScrollBars();
        }

        #endregion

        #region Bottom Canvas

        private void BottomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            BottomWindowCameraLogic.HandleMousePush(e);
            //MouseEditingLogic.HandleMousePush(e);

            // This allows the canvas to receive focus:
            // Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/ed6caee6-2cae-4db8-a2df-eafad44dbe37/mouse-focus-versus-keyboard-focus?forum=wpf#:~:text=In%20WPF%2C%20some%20elements%20will%20get%20keyboard%20focus,trick%3A%20userControl.MouseLeftButtonDown%20%2B%3D%20delegate%20%7B%20userControl.Focusable%20%3D%20true%3B
            BottomGumCanvas.Focusable = true;
            IInputElement element = Keyboard.Focus(BottomGumCanvas);
        }

        private void BottomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            BottomWindowCameraLogic.HandleMouseMove(e);
            //MouseEditingLogic.HandleMouseMove(e);
        }

        private void BottomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            BottomWindowCameraLogic.HandleMouseWheel(e);
        }

        #endregion

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

        #region Scroll Bar Logic

        private void RefreshTopWindowScrollBars()
        {
            var zoom = TopGumCanvas.SystemManagers.Renderer.Camera.Zoom;
            {
                var canvasDimension = TopGumCanvas.ActualHeight;
                var cameraAbsoluteStart = TopGumCanvas.SystemManagers.Renderer.Camera.AbsoluteTop;
                var mainSpriteDimension = TopWindowManager.MainSprite.GetAbsoluteHeight();
                var scrollBar = TopWindowVerticalSrollBar;

                RefreshScrollBar(zoom, canvasDimension, cameraAbsoluteStart, mainSpriteDimension, scrollBar);
            }

            {
                var canvasDimension = TopGumCanvas.ActualWidth;
                var cameraAbsoluteStart = TopGumCanvas.SystemManagers.Renderer.Camera.AbsoluteLeft;
                var mainSpriteDimension = TopWindowManager.MainSprite.GetAbsoluteWidth();
                var scrollBar = TopWindowHorizontalScrollBar;

                RefreshScrollBar(zoom, canvasDimension, cameraAbsoluteStart, mainSpriteDimension, scrollBar);
            }


        }

        private static void RefreshScrollBar(float zoom, double canvasDimension, float cameraAbsoluteStart, float mainSpriteDimension, ScrollBar scrollBar)
        {
            var min = (0 - canvasDimension / 2) / zoom;
            var max = mainSpriteDimension - canvasDimension / 2;
            var viewportSize = canvasDimension / zoom;

            scrollBar.Minimum = min;
            scrollBar.Maximum = max;
            scrollBar.ViewportSize = viewportSize / zoom;
            scrollBar.Value = cameraAbsoluteStart;
        }

        private void TopWindowVerticalSrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TopGumCanvas.SystemManagers.Renderer.Camera.Y = (float)e.NewValue;
            TopWindowManager.MoveBackgroundToCamera();
            TopGumCanvas.InvalidateVisual();
        }

        private void TopWindowHorizontalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TopGumCanvas.SystemManagers.Renderer.Camera.X = (float)e.NewValue;
            TopWindowManager.MoveBackgroundToCamera();
            TopGumCanvas.InvalidateVisual();
        }


        #endregion

        private void TreeListBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);


            // Check if CTRL+C was pressed
            if (ctrlDown && key == Key.C)
            {
                AnimationChainCopyPasteManager.HandleCopy(ViewModel);
                e.Handled = true;
            }
            // Check if CTRL+V was pressed
            else if (ctrlDown && key == Key.V)
            {
                AnimationChainCopyPasteManager.HandlePaste(ViewModel);
                e.Handled = true;
            }
        }

        internal bool GetIfIsHandlingHotkeys()
        {

            var focusedElement = Keyboard.FocusedElement as DependencyObject;

            while(focusedElement != null)
            {
                if(focusedElement is ListBox)
                {
                    return true;
                }
                else
                {
                    focusedElement = VisualTreeHelper.GetParent(focusedElement);
                }
            }
            return false;
        }

        private void OpenInAnimationEditorButton_Click(object sender, RoutedEventArgs e)
        {
            var rfs = GlueState.Self.CurrentReferencedFileSave;
            if(rfs != null)
            {
                GlueCommands.Self.FileCommands.OpenReferencedFileInDefaultProgram(rfs);
            }
        }
    }
}
