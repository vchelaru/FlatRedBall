using FlatRedBall.Glue;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using GlueFormsCore.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for MainPanelControl.xaml
    /// </summary>
    public partial class MainPanelControl : UserControl
    {
        #region Fields/Properties

        //public static string AppTheme = "Dark";
        //public static string AppTheme = "Light";
        public static ResourceDictionary ResourceDictionary { get; private set; }
        public static bool IsExiting { get; private set; }

        public static TabControlViewModel ViewModel { get; private set; }

        System.Timers.Timer FileWatchTimer;

        public static MainPanelControl Self { get; private set; }

        #endregion

        public MainPanelControl()
        {
            Self = this;

            InitializeComponent();

            InitializeThemes();

            ViewModel = new TabControlViewModel();
            this.DataContext = ViewModel;

            SetBinding(ViewModel);

            PluginManager.SetTabs(ViewModel);
            PluginManager.SetToolbarTray(ToolbarControl);

            CreateFileWatchTimer();

            CreateWindowTimer();

            this.PreviewMouseLeftButtonUp += MainPanelControl_PreviewMouseLeftButtonUp;
            this.PreviewMouseMove += MainPanelControl_PreviewMouseMove;
            //TopTabControl

        }


        System.Timers.Timer timer;
        private void CreateWindowTimer()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += (not, used) =>
            {
                PluginManager.ReactToGlobalTimer();
            };
            timer.Interval = 250;
            timer.Start();
        }

        private void MainPanelControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Released && GlueState.Self.DraggedTreeNode != null)
            {
                GlueState.Self.DraggedTreeNode = null;

            }
        }

        private void MainPanelControl_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(GlueState.Self.DraggedTreeNode != null)
            {
                GlueState.Self.DraggedTreeNode = null;
            }
        }

        public async void ReactToCloseProject(bool shouldSave, bool isExiting, InitializationWindowWpf initWindow = null)
        {
            var didCreateOwnInitWindow = false;
            if (initWindow == null)
            {
                didCreateOwnInitWindow = true;
                initWindow = new InitializationWindowWpf();

                // EVentually we want to convert this to WPF...
                //GlueGui.ShowWindow(initWindow, MainGlueWindow.Self);
                initWindow.Show();
            }

            MainPanelControl.IsExiting = isExiting;
            TaskManager.Self.RecordTaskHistory($"--Received Close Project Command --");

            // Let's set this to true so all tasks can end
            ProjectManager.WantsToCloseProject = true;

            // It's possible that all tasks finish, but that an async function isn't finished running.
            // This would result in more tasks getting added on. Therefore, we want to loop and make sure that:
            // 1. All tasks are finished
            // 2. We waited some time (1 second?)
            // 3. No tasks were awaited
            bool didAwaitTasks = false;
            do
            {
                didAwaitTasks = WaitForAllTaksToFinish(initWindow);

                if (didAwaitTasks)
                {
                    System.Threading.Thread.Sleep(1_000);
                }
            } while (didAwaitTasks);

            if (shouldSave)
            {
                if (ProjectManager.ProjectBase != null && !string.IsNullOrEmpty(ProjectManager.ProjectBase.FullFileName?.FullPath))
                {
                    GlueCommands.Self.ProjectCommands.SaveProjectsImmediately();
                    GlueCommands.Self.UpdateGlueSettingsFromCurrentGlueStateImmediately();
                }
            }


            ProjectManager.UnloadProject(isExiting);

            GlueCommands.Self.DoOnUiThread(() =>
            {
                if (GlueCommands.Self.DialogCommands.PropertyGrid != null)
                {
                    GlueCommands.Self.DialogCommands.PropertyGrid.SelectedObject = null;
                }
            });

            GlueCommands.Self.DoOnUiThread(() => GlueCommands.Self.DialogCommands.SetTitle(Localization.Texts.FrbEditor));
            ProjectManager.WantsToCloseProject = false;
            TaskManager.Self.RecordTaskHistory($"--Ending Close Project Command --");

            if (didCreateOwnInitWindow)
            {
                initWindow.Close();
            }
        }

        private static bool WaitForAllTaksToFinish(InitializationWindowWpf initWindow)
        {
            bool didWait = false;
            long msWaited = 0;
            const int maxMsToWait = 60 * 1000;

            // But give them a chance to end...
            while (TaskManager.Self.AreAllAsyncTasksDone == false)
            {
                // We want to wait until all tasks are done, but
                // if the task is to reload, we can continue or else
                // we'll have a deadlock
                var canContinue = TaskManager.Self.CurrentTaskDescription == UpdateReactor.ReloadingProjectDescription ||
                    (TaskManager.Self.CurrentTaskDescription.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTaskDescription.EndsWith(".glux")) ||
                    (TaskManager.Self.CurrentTaskDescription.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTaskDescription.EndsWith(".gluj")) ||
                    (TaskManager.Self.CurrentTaskDescription.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTaskDescription.EndsWith(".csproj")) ||
                    (TaskManager.Self.CurrentTaskDescription.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTaskDescription.EndsWith("." + GlueProjectSave.ScreenExtension)) ||
                    (TaskManager.Self.CurrentTaskDescription.StartsWith("Reacting to changed file") && TaskManager.Self.CurrentTaskDescription.EndsWith("." + GlueProjectSave.EntityExtension)) ||
                    TaskManager.Self.CurrentTaskDescription.StartsWith("Reloading glux due to file change on disk") ||
                    TaskManager.Self.CurrentTaskDescription.StartsWith("Reloading Project due to changed file") ||
                    (TaskManager.Self.IsInTask() && TaskManager.Self.TaskCount == 1);

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
                        initWindow.SubMessage = $"Waiting for {TaskManager.Self.TaskCount} tasks to finish...\nCurrent Task: {TaskManager.Self.CurrentTaskDescription}";

                    }


                    // don't wait forever. This is mainly so we wait for any simultaneous tasks.
                    // There shouldn't be any but just in case Vic messed up the code...
                    if (msWaited > maxMsToWait)
                    {
                        // If the first task barfed, don't consider that awaited. Just move on...
                        TaskManager.Self.RecordTaskHistory($"--Waited maximum time to finish tasks, but still have {TaskManager.Self.TaskCount} tasks left --");
                        break;
                    }
                    else
                    {
                        didWait = true;
                    }
                }

            }

            return didWait;
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
            TopTabControl.SetBinding(TabControl.ItemsSourceProperty, 
                nameof(viewModel.TopTabItems) + "." + nameof(TabContainerViewModel.Tabs));

            TopTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.TopSelectedTab));

            BottomTabControl.SetBinding(TabControl.ItemsSourceProperty, 
                nameof(viewModel.BottomTabItems) + "." + nameof(TabContainerViewModel.Tabs));
            BottomTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.BottomSelectedTab));

            LeftTabControl.SetBinding(TabControl.ItemsSourceProperty, 
                nameof(viewModel.LeftTabItems) + "." + nameof(TabContainerViewModel.Tabs));
            LeftTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.LeftSelectedTab));

            RightTabControl.SetBinding(TabControl.ItemsSourceProperty, 
                nameof(viewModel.RightTabItems) + "." + nameof(TabContainerViewModel.Tabs));
            RightTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.RightSelectedTab));

            CenterTabControl.SetBinding(TabControl.ItemsSourceProperty, 
                nameof(viewModel.CenterTabItems) + "." + nameof(TabContainerViewModel.Tabs));
            CenterTabControl.SetBinding(TabControl.SelectedItemProperty, nameof(viewModel.CenterSelectedTab));
        }

        private void InitializeThemes()
        {
            //this.Resources.MergedDictionaries[0].Source =
            //    new Uri($"/Themes/{AppTheme}.xaml", UriKind.Relative);


            //Style style = this.TryFindResource("UserControlStyle") as Style;
            //if (style != null)
            //{
            //    this.Style = style;
            //}

            //ResourceDictionary = Resources;
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var handledByPlugin = PluginManager.IsHandlingHotkeys();
            if (!handledByPlugin)
            {
                // If this is coming from a text box, don't try to apply hotkeys
                // Maybe in the future we want to be selective, like only apply certain
                // hotkeys (ctrl+f) but not others (delete)?
                var isTextBox = e.OriginalSource is TextBoxBase;

                var isHandled = HotkeyManager.Self.TryHandleKeys(e, isTextBox).Result;

                if (isHandled)
                {
                    e.Handled = true;
                }
            }
        }

        private void FileWatchTimer_Tick(object sender, EventArgs e)
        {
            if (ProjectManager.ProjectBase != null && GlueState.Self.CurrentGlueProject != null)
            {
                var throwaway = FileWatchManager.Flush();
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.XButton1 == MouseButtonState.Pressed)
            {
                // navigate back
                TreeNodeStackManager.Self.GoBack();
            }
            if(e.XButton2 == MouseButtonState.Pressed)
            {
                // navigate forward
                TreeNodeStackManager.Self.GoForward();
            }
        }

        internal void ApplyGlueSettings(GlueSettingsSave glueSettingsSave)
        {
            if(glueSettingsSave.LeftTabWidthPixels > 1)
            {
                ViewModel.LeftPanelWidth = new GridLength(glueSettingsSave.LeftTabWidthPixels.Value);
                // To prevent expansion from resetting:
                ViewModel.LeftSplitterWidth = new GridLength(4);

            }

        }

        public void Invoke(Action action)
        {
            var wasInTask = TaskManager.Self.IsInTask();

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (wasInTask)
                    {
                        RunOnUiThreadTasked(action);
                    }
                    else
                    {
                        action();
                    }
                }
                catch (Exception)
                {
                    if (!GlueCommands.Self.DialogCommands.IsMainWindowDisposed() && !ProjectManager.WantsToCloseProject)
                    {
                        throw;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });
        }

        public T Invoke<T>(Func<T> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();

            base.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (wasInTask)
                    {
                        RunOnUiThreadTasked(func);
                    }
                    else
                    {
                        func();
                    }
                }
                catch (Exception)
                {
                    if (!GlueCommands.Self.DialogCommands.IsMainWindowDisposed())
                    {
                        throw;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return default;
        }

        public Task Invoke(Func<Task> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();
            Task toReturn = Task.CompletedTask;

            var asyncResult = base.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    toReturn = wasInTask ? RunOnUiThreadTasked(func) : func();
                }
                catch (Exception)
                {
                    if (!GlueCommands.Self.DialogCommands.IsMainWindowDisposed())
                    {
                        throw;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return asyncResult.Task;
        }

        public Task<T> Invoke<T>(Func<Task<T>> func)
        {
            var wasInTask = TaskManager.Self.IsInTask();
            Task<T> toReturn = Task.FromResult(default(T));

            base.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (wasInTask)
                    {
                        toReturn = RunOnUiThreadTasked(func);
                    }
                    else
                    {
                        toReturn = func();
                    }
                }
                catch (Exception)
                {
                    if (!GlueCommands.Self.DialogCommands.IsMainWindowDisposed())
                    {
                        throw;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return toReturn;
        }

        public void BeginInvoke(Action action) => Dispatcher.BeginInvoke(action);

        private void RunOnUiThreadTasked(Action action) => action();
        private T RunOnUiThreadTasked<T>(Func<T> action) => action();
        private Task<T> RunOnUiThreadTasked<T>(Func<Task<T>> action) => action();
    }
}
