using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.ComponentModel.Composition;
using CompilerPlugin.Managers;
using CompilerPlugin.Views;
using CompilerPlugin.Models;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Newtonsoft.Json;
using CompilerPlugin.ViewModels;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using CompilerLibrary.ViewModels;

namespace CompilerPlugin
{
    [Export(typeof(PluginBase))]
    public class CompilerPlugin : PluginBase
    {
        public override string FriendlyName => "Compiler Plugin";

        public override Version Version => new Version(1, 0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            _compilerViewModel = CompilerViewModel.Self;
            _compiler = new Compiler(_compilerViewModel);
            _runner = new Runner(ReactToPluginEvent, _compilerViewModel);
            ReactToLoadedGlux += HandleGluxLoaded;
            ReactToUnloadedGlux += HandleGluxUnLoaded;

            _runner.AfterSuccessfulRun += () =>
            {
                ReactToPluginEvent("Runner_GameStarted", "");
            };
            _runner.OutputReceived += (output) =>
            {
                ReactToPluginEvent("Compiler_Output_Standard", output);
            };

            CreateBuildControl();
        }

        private void HandleGluxLoaded()
        {
            LoadOrCreateBuildSettings();
            _compiler.BuildSettingsUser = BuildSettingsUser;
            _compilerViewModel.HasLoadedGlux = true;
        }

        private void HandleGluxUnLoaded()
        {
            _compilerViewModel.HasLoadedGlux = false;
        }

        #region Fields/Properties
        public BuildTabView MainControl { get; private set; }
        PluginTab buildTab;
        BuildSettingsUser BuildSettingsUser;
        private Compiler _compiler;
        private Runner _runner;
        private CompilerViewModel _compilerViewModel;
        private bool ignoreViewModelChanges;

        FilePath BuildSettingsUserFilePath => GlueState.Self.ProjectSpecificSettingsFolder + "BuildSettings.user.json";

        public async void HandleCompilerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out////////////////
            var propertyName = e.PropertyName;

            switch (propertyName)
            {

                case nameof(CompilerViewModel.IsRunning):
                    //CommandSender.CancelConnect();
                    break;
            }
        }
        #endregion

        #region Private Methods
        private void LoadOrCreateBuildSettings()
        {
            BuildSettingsUser = null;
            if (BuildSettingsUserFilePath.Exists())
            {
                try
                {
                    var text = System.IO.File.ReadAllText(BuildSettingsUserFilePath.FullPath);
                    BuildSettingsUser = JsonConvert.DeserializeObject<BuildSettingsUser>(text);
                }
                catch
                {
                    // do nothing
                }
            }

            if (BuildSettingsUser == null)
            {
                BuildSettingsUser = new BuildSettingsUser();
            }
        }

        private void CreateBuildControl()
        {
            MainControl = new BuildTabView();
            MainControl.DataContext = _compilerViewModel;
            _compilerViewModel.Configuration = "Debug";
            _compilerViewModel.PropertyChanged += HandleCompilerViewModelPropertyChanged;

            buildTab = CreateTab(MainControl, "Build", TabLocation.Bottom);
            buildTab.Show();

            AssignControlEvents();
        }

        private void AssignControlEvents()
        {
            MainControl.BuildClicked += async (not, used) =>
            {
                var succeeded = await _compiler.Compile((value) => ReactToPluginEvent("Compiler_Output_Standard", value), (value) => ReactToPluginEvent("Compiler_Output_Error", value), _compilerViewModel.Configuration, _compilerViewModel.IsPrintMsBuildCommandChecked);
                if (!succeeded)
                {
                    GlueCommands.Self.DialogCommands.FocusTab("Build");
                }
            };

            MainControl.RunClicked += async (not, used) =>
            {
                var succeeded = await _compiler.Compile((value) => ReactToPluginEvent("Compiler_Output_Standard", value), (value) => ReactToPluginEvent("Compiler_Output_Error", value), _compilerViewModel.Configuration, _compilerViewModel.IsPrintMsBuildCommandChecked);
                if (succeeded)
                {
                    if (succeeded)
                    {
                        _runner.IsRunning = false;
                        await _runner.Run(preventFocus: false);
                    }
                    else
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";
                        var innerResult = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage);
                        if(innerResult == System.Windows.MessageBoxResult.Yes)
                        {
                            await _runner.Run(preventFocus: false);
                        }
                    }
                }
            };

            MainControl.MSBuildSettingsClicked += () =>
            {
                var viewModel = new BuildSettingsWindowViewModel();
                var view = new BuildSettingsWindow();
                view.DataContext = viewModel;
                viewModel.SetFrom(BuildSettingsUser);

                var results = view.ShowDialog();

                if (results == true)
                {
                    // apply VM:
                    viewModel.ApplyTo(BuildSettingsUser);

                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        var textToSave = JsonConvert.SerializeObject(BuildSettingsUser);
                        System.IO.File.WriteAllText(BuildSettingsUserFilePath.FullPath, textToSave);
                    });
                }
            };
        }
        #endregion

        #region Overrided Method

        public override void HandleEvent(string eventName, string payload)
        {
            base.HandleEvent(eventName, payload);

            switch (eventName)
            {
                case "Runner_MoveWindow":
                    {
                        var settings = JObject.Parse(payload);
                        _runner.MoveWindow(
                            settings.ContainsKey("X") ? settings.Value<int>("X") : 0,
                            settings.ContainsKey("Y") ? settings.Value<int>("Y") : 0,
                            settings.ContainsKey("Width") ? settings.Value<int>("Width") : 1,
                            settings.ContainsKey("Height") ? settings.Value<int>("Height") : 1,
                            settings.ContainsKey("Repaint") ? settings.Value<bool>("Repaint") : true
                        );
                    }
                    break;
                case "Compiler_Output_Standard":
                    {
                        MainControl.PrintOutput(payload);
                        
                    }
                    break;
                case "Compiler_Output_Error":
                    {
                        MainControl.PrintOutput(payload);

                    }
                    break;
            }
        }

        protected override async Task<string> HandleEventWithReturnImplementation(string eventName, string payload)
        {
            switch (eventName)
            {
                case "Compiler_DoCompile":
                    {
                        var settings = JObject.Parse(payload);
                        try
                        {
                            var result = await _compiler.Compile(
                                (value) => 
                                    ReactToPluginEvent("Compiler_Output_Standard", value), 
                                (value) => 
                                    ReactToPluginEvent("Compiler_Output_Error", value), 
                                settings.ContainsKey("Configuration") ? settings.Value<string>("Configuration") : _compilerViewModel.Configuration, 
                                settings.ContainsKey("PrintMsBuildCommand") ? settings.Value<bool>("PrintMsBuildCommand") : _compilerViewModel.IsPrintMsBuildCommandChecked
                            );

                            return JsonConvert.SerializeObject(new
                            {
                                Succeeded = result
                            });
                        }
                        catch (Exception ex)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                Succeeded = false,
                                Error = ex.ToString()
                            });
                        }
                    }

                case "Runner_DoRun":
                    {
                        var settings = JObject.Parse(payload);
                        return JsonConvert.SerializeObject(await _runner.Run(settings.ContainsKey("PreventFocus") ? settings.Value<bool>("PreventFocus") : false, settings.ContainsKey("RunArguments") ? settings.Value<string>("RunArguments") : ""));
                    }
                case "Runner_Kill":
                    return await _runner.KillGameProcess();
            }

            return null;
        }
        #endregion

        #region Classes
        private class CompilerOptions
        {
            public Guid? RequestId { get; set; }
            public string Configuration { get; internal set; }
            public bool PrintMsBuildCommand { get; internal set; }
        }

        private class CompilerResult
        {
            public Guid? Id { get; internal set; }
            public bool Succeeded { get; internal set; }
        }
        #endregion
    }
}
