using FlatRedBall.Glue;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Properties;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Themes;
using Glue;
using GlueFormsCore.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for MainPanelControl.xaml
    /// </summary>
    public partial class MainPanelControl : UserControl
    {
        #region Fields/Properties

        //public static string AppTheme = "Dark";
        public static string AppTheme = "Light";
        public static ResourceDictionary ResourceDictionary { get; private set; } = new();
        public static bool IsExiting { get; private set; }

        public static TabControlViewModel ViewModel { get; private set; }

        System.Timers.Timer FileWatchTimer;

        public static MainPanelControl Self { get; private set; }

        #endregion

        public MainPanelControl()
        {
            Self = this;
            InitializeComponent();

            ViewModel = new TabControlViewModel();
            this.DataContext = ViewModel;

            PluginManager.SetTabs(ViewModel);
            PluginManager.SetToolbarTray(ToolbarControl);

            CreateFileWatchTimer();

            this.PreviewMouseLeftButtonUp += MainPanelControl_PreviewMouseLeftButtonUp;
            this.PreviewMouseMove += MainPanelControl_PreviewMouseMove;
            //TopTabControl

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
                if (MainGlueWindow.Self.PropertyGrid != null)
                {
                    MainGlueWindow.Self.PropertyGrid.SelectedObject = null;
                }
            });

            GlueCommands.Self.DoOnUiThread(() => MainGlueWindow.Self.Text = Localization.Texts.FrbEditor);
            ProjectManager.WantsToCloseProject = false;
            TaskManager.Self.RecordTaskHistory($"--Ending Close Project Command --");

            if (didCreateOwnInitWindow)
            {
                initWindow.Close();
            }
        }

        public void SwitchThemes(ThemeConfig config)
        {
            Switch(Resources);
            
            void Switch(ResourceDictionary resource)
            {
                if (resource.Source != null)
                {
                    string source = resource.Source.OriginalString;

                    if (config.Mode is not null && Regex.IsMatch(source, @"Frb\.Brushes\.(Dark|Light)\.xaml$"))
                    {
                        resource.Source = config.Mode switch
                        {
                            ThemeMode.Light => new Uri(source.Replace("Dark", "Light"), UriKind.RelativeOrAbsolute),
                            ThemeMode.Dark => new Uri(source.Replace("Light", "Dark"), UriKind.RelativeOrAbsolute),
                            _ => throw new NotImplementedException()
                        };
                    }

                    if (config.Accent is { } accent && source.Contains("Frb.Accents.xaml"))
                    {
                        resource.Remove("Frb.Colors.Primary");
                        resource.Remove("Frb.Colors.Primary.Dark");
                        resource.Remove("Frb.Colors.Primary.Light");
                        resource.Remove("Frb.Colors.Primary.Contrast");

                        resource.Add("Frb.Colors.Primary", accent);
                        resource.Add("Frb.Colors.Primary.Dark", MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent));
                        resource.Add("Frb.Colors.Primary.Light", MaterialDesignColors.ColorManipulation.ColorAssist.Lighten(accent));
                        resource.Add("Frb.Colors.Primary.Contrast", MaterialDesignColors.ColorManipulation.ColorAssist.ContrastingForegroundColor(MaterialDesignColors.ColorManipulation.ColorAssist.Darken(accent)));
                    }
                }

                foreach (var mergedResource in resource.MergedDictionaries)
                {
                    Switch(mergedResource);
                }

                MainGlueWindow.Self.SyncMenuStripWithTheme(Self);
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
            if(glueSettingsSave.LeftTabWidthPixels is > 0)
            {
                ViewModel.LeftPanelWidth = new(glueSettingsSave.LeftTabWidthPixels.Value);
            }

            if (glueSettingsSave.RightTabWidthPixels is > 0)
            {
                ViewModel.RightPanelWidth = new(glueSettingsSave.RightTabWidthPixels.Value);
            }

            if (glueSettingsSave.BottomTabHeightPixels is > 0)
            {
                ViewModel.BottomPanelHeight = new(glueSettingsSave.BottomTabHeightPixels.Value);
            }

        }

        private void TabItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is TabItem { DataContext: PluginTab tab })
            {
                tab.OnMouseEvent(e);
            }
        }
    }
}
