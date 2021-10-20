using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlueFormsCore.Controls
{

    #region TabControlViewModel - migrate this to its own file?
    public class TabControlViewModel : ViewModel
    {
        public ObservableCollection<PluginTabPage> TopTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> BottomTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> LeftTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> RightTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();
        public ObservableCollection<PluginTabPage> CenterTabItems { get; private set; } = new ObservableCollection<PluginTabPage>();

        public PluginTabPage TopSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage BottomSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage LeftSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage RightSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public PluginTabPage CenterSelectedTab
        {
            get => Get<PluginTabPage>();
            set => Set(value);
        }

        public GridLength TopSplitterHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength TopPanelHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength LeftPanelWidth
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength LeftSplitterWidth
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength BottomSplitterHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public GridLength BottomPanelHeight
        {
            get => Get<GridLength>();
            set => Set(value);
        }

        public TabControlViewModel()
        {
            TopTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(TopTabItems));
            BottomTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(BottomTabItems));
            LeftTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(LeftTabItems));
            RightTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(RightTabItems));
            CenterTabItems.CollectionChanged += (_, __) => NotifyPropertyChanged(nameof(CenterTabItems));

            this.PropertyChanged += (sender, args) => HandlePropertyChanged(args.PropertyName);

            ExpandAndCollapseColumnAndRowWidths();
        }

        private void HandlePropertyChanged(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(TopTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if(TopTabItems.Count > 0 && TopSelectedTab == null)
                    {
                        TopSelectedTab = TopTabItems[0];
                    }
                    break;
                case nameof(BottomTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (BottomTabItems.Count > 0 && BottomSelectedTab == null)
                    {
                        BottomSelectedTab = BottomTabItems[0];
                    }
                    break;
                case nameof(LeftTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (LeftTabItems.Count > 0 && LeftSelectedTab == null)
                    {
                        LeftSelectedTab = LeftTabItems[0];
                    }
                    break;
                case nameof(RightTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (RightTabItems.Count > 0 && RightSelectedTab == null)
                    {
                        RightSelectedTab = RightTabItems[0];
                    }
                    break;
                case nameof(CenterTabItems):
                    ExpandAndCollapseColumnAndRowWidths();
                    if (CenterTabItems.Count > 0 && CenterSelectedTab == null)
                    {
                        CenterSelectedTab = CenterTabItems[0];
                    }
                    break;
            }
        }

        private void ExpandAndCollapseColumnAndRowWidths()
        {
            var shouldShrinkLeft = LeftTabItems.Count == 0 && LeftSplitterWidth.Value > 0;
            var shouldExpandLeft = LeftTabItems.Count > 0 && LeftSplitterWidth.Value == 0;

            var shouldShrinkTop = TopTabItems.Count == 0 && TopSplitterHeight.Value > 0;
            var shouldExpandTop = TopTabItems.Count > 0 && TopSplitterHeight.Value == 0;

            var shouldShrinkBottom = BottomTabItems.Count == 0 && BottomSplitterHeight.Value > 0;
            var shouldExpandBottom = BottomTabItems.Count > 0 && BottomSplitterHeight.Value == 0;

            if(shouldShrinkLeft)
            {
                LeftSplitterWidth = new GridLength(0);
                LeftPanelWidth = new GridLength(0, GridUnitType.Pixel);
            }
            else if(shouldExpandLeft)
            {
                LeftSplitterWidth = new GridLength(4);
                LeftPanelWidth = new GridLength(230, GridUnitType.Pixel);
                //LeftPanelWidth = new GridLength(1, GridUnitType.Star);
            }

            if(shouldShrinkTop)
            {
                TopSplitterHeight = new GridLength(0);
                TopPanelHeight = new GridLength(0);
            }
            else if(shouldExpandTop)
            {
                TopSplitterHeight = new GridLength(4);
                //TopPanelHeight = new GridLength(1, GridUnitType.Star);
                TopPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

            if(shouldShrinkBottom)
            {
                BottomSplitterHeight = new GridLength(0);
                BottomPanelHeight = new GridLength(0);
            }
            else if(shouldExpandBottom)
            {
                BottomSplitterHeight = new GridLength(4);
                //BottomPanelHeight = new GridLength(1, GridUnitType.Star);
                BottomPanelHeight = new GridLength(200, GridUnitType.Pixel);
            }

        }
    }

    #endregion

    /// <summary>
    /// Interaction logic for MainPanelControl.xaml
    /// </summary>
    public partial class MainPanelControl : UserControl
    {
        #region Fields/Properties

        public static string AppTheme = "Light";
        public static ResourceDictionary ResourceDictionary { get; private set; }
        public static bool IsExiting { get; private set; }

        public static TabControlViewModel ViewModel { get; private set; }

        System.Timers.Timer FileWatchTimer;

        #endregion

        public MainPanelControl()
        {
            InitializeComponent();

            InitializeThemes();

            ViewModel = new TabControlViewModel();
            this.DataContext = ViewModel;

            SetBinding(ViewModel);

            PluginManager.SetTabs(ViewModel);
            PluginManager.SetToolbarTray(ToolbarControl);

            CreateFileWatchTimer();
            //TopTabControl

        }

        public void ReactToCloseProject(bool shouldSave, bool isExiting, InitializationWindow initWindow = null)
        {
            MainPanelControl.IsExiting = isExiting;
            TaskManager.Self.RecordTaskHistory($"--Received Close Project Command --");

            // Let's set this to true so all tasks can end
            ProjectManager.WantsToClose = true;

            long msWaited = 0;
            // But give them a chance to end...
            while (TaskManager.Self.AreAllAsyncTasksDone == false)
            {
                // We want to wait until all tasks are done, but
                // if the task is to reload, we can continue or else
                // we'll have a deadlock
                var canContinue = TaskManager.Self.CurrentTask == UpdateReactor.ReloadingProjectDescription ||
                    (TaskManager.Self.CurrentTask.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTask.EndsWith(".glux")) ||
                    (TaskManager.Self.CurrentTask.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTask.EndsWith(".gluj")) ||
                    (TaskManager.Self.CurrentTask.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTask.EndsWith(".csproj")) ||
                    TaskManager.Self.CurrentTask.StartsWith("Reloading glux due to file change on disk");

                if (canContinue)
                {
                    break;
                }
                else
                {
                    const int sleepLength = 50;
                    System.Threading.Thread.Sleep(sleepLength);
                    msWaited += sleepLength;

                    // pump events
                    System.Windows.Forms.Application.DoEvents();

                    if (initWindow != null)
                    {
                        initWindow.Message = "Closing Project";
                        initWindow.SubMessage = $"Waiting for {TaskManager.Self.TaskCount} tasks to finish...\nCurrent Task: {TaskManager.Self.CurrentTask}";

                    }

                    const int maxMsToWait = 50 * 1000;

                    // don't wait forever. This is mainly so we wait for any simultaneous tasks.
                    // There shouldn't be any but just in case Vic messed up the code...
                    if (msWaited > maxMsToWait)
                    {
                        TaskManager.Self.RecordTaskHistory($"--Waited maximum time to finish tasks, but still have {TaskManager.Self.TaskCount} tasks left --");

                        break;
                    }
                }

            }


            if (shouldSave)
            {
                if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                {
                    GlueCommands.Self.ProjectCommands.SaveProjectsImmediately();
                    UpdateGlueSettings();
                }
            }


            ProjectManager.UnloadProject(isExiting);

            MainGlueWindow.Self.PropertyGrid.SelectedObject = null;

            GlueCommands.Self.DoOnUiThread(() => MainGlueWindow.Self.Text = "FlatRedBall");
            ProjectManager.WantsToClose = false;
            TaskManager.Self.RecordTaskHistory($"--Ending Close Project Command --");

        }

        private void CreateFileWatchTimer()
        {
            this.FileWatchTimer = //new System.Windows.Forms.Timer(this.components);
                new System.Timers.Timer();

            this.FileWatchTimer.Enabled = true;
            // the frequency of file change flushes. Reducing this time
            // makes Glue more responsive, but increases the chance of 
            // Glue performing a check mid update like on a git pull.
            // Note that the ChangeInformation also keeps a timer since the last
            // file was added, and will wait mMinimumTimeAfterChangeToReact until 
            // flushing.
            this.FileWatchTimer.Interval = 400;
            this.FileWatchTimer.Elapsed += this.FileWatchTimer_Tick;
            this.FileWatchTimer.Start();
        }

        private void SetBinding(TabControlViewModel viewModel)
        {
            TopTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.TopTabItems));
            TopTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.TopSelectedTab));

            BottomTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.BottomTabItems));
            BottomTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.BottomSelectedTab));

            LeftTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.LeftTabItems));
            LeftTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.LeftSelectedTab));

            RightTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.RightTabItems));
            RightTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.RightSelectedTab));

            CenterTabControl.SetBinding(TabControl.ItemsSourceProperty, nameof(viewModel.CenterTabItems));
            CenterTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.CenterSelectedTab));
        }

        private void InitializeThemes()
        {
            this.Resources.MergedDictionaries[0].Source =
                new Uri($"/Themes/{AppTheme}.xaml", UriKind.Relative);


            Style style = this.TryFindResource("UserControlStyle") as Style;
            if (style != null)
            {
                this.Style = style;
            }

            ResourceDictionary = Resources;
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (HotkeyManager.Self.TryHandleKeys(e))
            {
                e.Handled = true;
            }
        }

        private void FileWatchTimer_Tick(object sender, EventArgs e)
        {
            if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName))
                FileWatchManager.Flush();
        }

        private void UpdateGlueSettings()
        {
            var save = ProjectManager.GlueSettingsSave;

            string lastFileName = null;

            if (ProjectManager.ProjectBase != null)
            {
                lastFileName = ProjectManager.ProjectBase.FullFileName;
            }

            save.LastProjectFile = lastFileName;

            var glueExeFileName = ProjectLoader.GetGlueExeLocation();
            var foundItem = save.GlueLocationSpecificLastProjectFiles
                .FirstOrDefault(item => item.GlueFileName == glueExeFileName);

            var alreadyIsListed = foundItem != null;

            if (!alreadyIsListed)
            {
                foundItem = new ProjectFileGlueFilePair();
                save.GlueLocationSpecificLastProjectFiles.Add(foundItem);
            }
            foundItem.GlueFileName = glueExeFileName;
            foundItem.GameProjectFileName = lastFileName;

            // set up the positions of the window
            //save.WindowLeft = this.Left;
            //save.WindowTop = this.Top;
            //save.WindowHeight = this.Height;
            //save.WindowWidth = this.Width;
            save.StoredRecentFiles = MainGlueWindow.Self.NumberOfStoredRecentFiles;

            void SetTabs(List<string> tabNames, ObservableCollection<PluginTabPage> tabs)
            {
                tabNames.Clear();
                tabNames.AddRange(tabs.Select(item => item.Title));
            }

            SetTabs(save.TopTabs, PluginManager.TabControlViewModel.TopTabItems);
            SetTabs(save.LeftTabs, PluginManager.TabControlViewModel.LeftTabItems);
            SetTabs(save.CenterTabs, PluginManager.TabControlViewModel.CenterTabItems);
            SetTabs(save.RightTabs, PluginManager.TabControlViewModel.RightTabItems);
            SetTabs(save.BottomTabs, PluginManager.TabControlViewModel.BottomTabItems);


            GlueCommands.Self.GluxCommands.SaveSettings();
        }

    }
}
