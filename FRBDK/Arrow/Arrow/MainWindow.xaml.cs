using System;
using System.Windows;
using FlatRedBall.Arrow.Glue;
using FlatRedBall.Arrow.GlueView;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBallWpf;
using FlatRedBall.Arrow.Controls;
using FlatRedBall.Arrow.ViewModels;
using FlatRedBall.Arrow.DataTypes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Arrow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainGame _mainGame;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;

        }

        private void OnLoaded(Object sender, RoutedEventArgs routedEventArgs)
        {
            _mainGame = new MainGame(flatRedBallControl);
            _mainGame.MainWindow = this;
            ArrowCommands.Self.Initialize(this.AllElementsTreeView, this.SingleElementTreeView, this.DeleteMenuItem, this.CopyMenuItem);
            ArrowState.Self.Initialize(this, this.AllElementsTreeView, this.SingleElementTreeView);
            PropertyGridManager.Self.Initialize(DataGridUi);

            ArrowState.Self.CurrentArrowProject = new FlatRedBall.Arrow.DataTypes.ArrowProjectSave();

            GluxManager.ObjectFinder = new GlueObjectFinder();


            AvailableAssetTypes.Self.AddAssetTypes("Content/ContentTypes.csv");
            ExposedVariableManager.Initialize();

            this.AllElementsTreeView.DataContext = ArrowState.Self.CurrentArrowProjectVm;
            this.SingleElementTreeView.DataContext = ArrowState.Self;
            this.ContentsLabel.DataContext = ArrowState.Self;
        }

        private void AddElementClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.Element();
        }

        private void AddSpriteClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.Sprite();
        }

        private void AddCircleClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.Circle();
        }

        private void AddRectangleClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.Rectangle();
        }

        private void HandleLoadProjectClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.File.ShowLoadProject();
        }

        private void MainTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ArrowCommands.Self.UpdateToSelectedElement();
        }

        private void SingleElementTreeView_SelectedItemChanged_1(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ArrowCommands.Self.UpdateToSelectedInstance();
            
        }

        private void flatRedBallControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            EditingManager.Self.HandleKeyDown(sender, e);
        }

        private void CameraSettingsClick(object sender, RoutedEventArgs e)
        {
            CameraSettingsForm csf = new CameraSettingsForm();
            csf.CameraSave = ArrowState.Self.CurrentArrowProject.CameraSave;
            csf.CameraChanged += HandleCameraChanged;
            csf.Show();
        }

        private void HandleCameraChanged(object sender, EventArgs e)
        {
            BoundsManager.Self.UpdateTo(ArrowState.Self.CurrentArrowProject.CameraSave);
        }

        //private void AllElementsTreeView_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        //{
        //    ArrowCommands.Self.UpdateToSelectedElement();
        //}
        private void HandleAddElementInstanceClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.ElementInstance();
        }

        private void AddNewFileClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.NewFile();
        }

        private void IntentsMenuItemClick(object sender, RoutedEventArgs e)
        {
            IntentsEditorWindow eiw = new IntentsEditorWindow();
            eiw.DataContext = ArrowState.Self.CurrentArrowProjectVm;
            eiw.Show();
        }

        private void AllElementsTreeView_SelectedItemChanged_1(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ArrowCommands.Self.UpdateToSelectedElement();

        }


    }
}