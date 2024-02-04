using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Instructions;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.IO;
using GlueFormsCore.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Glue.Managers
{
    public static class StartupManager
    {
        public static async void StartUpGlue(Action<PluginCategories> ShareUiReferences)
        {
            // We need to load the glue settings before loading the plugins so that we can shut off plugins according to settings
            GlueCommands.Self.LoadGlueSettings();
            var mainCulture = GlueState.Self.GlueSettingsSave.CurrentCulture;
            if (mainCulture != null)
            {
                Localization.Texts.Culture = mainCulture;
                Thread.CurrentThread.CurrentCulture = mainCulture;
                Thread.CurrentThread.CurrentUICulture = mainCulture;
            }

            // Some stuff can be parallelized.  We're going to run stuff
            // that can be parallelized in parallel, and then block to wait for
            // all tasks to finish when we need to

            AddObjectsToIocContainer();

            AddErrorReporters();

            var initializationWindow = new InitializationWindowWpf();


            initializationWindow.Show();

            SetScreenMessage(Localization.Texts.InitializingGlueSystems);

            // Add Glue.Common
            PropertyValuePair.AdditionalAssemblies.Add(typeof(PlatformSpecificType).Assembly);

            // Monogame:
            PropertyValuePair.AdditionalAssemblies.Add(typeof(Microsoft.Xna.Framework.Audio.SoundEffectInstance).Assembly);

            // Event manager
            SetScreenSubMessage(Localization.Texts.InitializingEventManager);
            FlatRedBall.Glue.Managers.TaskManager.Self.Add(FlatRedBall.Glue.Events.EventManager.Initialize, Localization.Texts.InitializingEventManager);
            SetScreenSubMessage(Localization.Texts.InitializingExposedVariableManager);

            try
            {
                ExposedVariableManager.Initialize();
            }
            catch (Exception ex)
            {
                GlueGui.ShowException(Localization.Texts.ErrorCannotLoadGlue, Localization.Texts.Error, ex);
                Environment.Exit(2);
                return;
            }

            SetScreenSubMessage(Localization.Texts.InitializingRightClickMenus);
            RightClickHelper.Initialize();

            SetScreenSubMessage(Localization.Texts.InitializingPropertyGrids);
            PropertyGridRightClickHelper.Initialize();

            SetScreenSubMessage(Localization.Texts.InitializingInstructionManager);
            InstructionManager.Initialize();

            SetScreenSubMessage(Localization.Texts.InitializingTypeConverter);
            TypeConverterHelper.InitializeClasses();

            SetScreenMessage(Localization.Texts.LoadingSettings);

            // Initialize before loading GlueSettings;
            // Also initialize before loading plugins so that plugins
            // can access the standard ATIs
            var startupPath = FileManager.GetDirectory(System.Reflection.Assembly.GetExecutingAssembly().Location);

            AvailableAssetTypes.Self.Initialize(startupPath);

            SetScreenMessage(Localization.Texts.LoadingPlugins);

            var pluginsToIgnore = (GlueState.Self.CurrentPluginSettings != null)
                ? GlueState.Self.CurrentPluginSettings.PluginsToIgnore
                : new List<string>();

            PluginManager.Initialize(true, pluginsToIgnore);
            ShareUiReferences(PluginCategories.All);

            try
            {
                FileManager.PreserveCase = true;

                SetScreenMessage(Localization.Texts.InitializingFileWatch);
                FileWatchManager.Initialize();

                SetScreenMessage(Localization.Texts.LoadingCustomTypeInfo);
                FlatRedBall.Glue.ProjectManager.Initialize();

                // LoadSettings before loading projects
                FlatRedBall.Glue.EditorData.LoadPreferenceSettings();

                while (FlatRedBall.Glue.Managers.TaskManager.Self.AreAllAsyncTasksDone == false)
                {
                    System.Threading.Thread.Sleep(100);
                }
                await LoadProjectConsideringSettingsAndArgs(initializationWindow);

                // This needs to happen after loading the project:
                ShareUiReferences(PluginCategories.ProjectSpecific);

                EditorData.FileAssociationSettings.LoadSettings();
                EditorData.LoadGlueLayoutSettings();



            }
            catch (Exception exc)
            {
                if (GlueGui.ShowGui)
                {
                    MessageBox.Show(exc.ToString());

                    FileManager.SaveText(exc.ToString(),
                        FileManager.UserApplicationDataForThisApplication + "InitError.txt");
                    PluginManager.ReceiveError(exc.ToString());

                    GlueState.Self.HasErrorOccurred = true;
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (GlueGui.ShowGui)
                {
                    initializationWindow.Close();
                }
            }

            // this gives the search bar focus, so hotkeys work
            // If we don't wait a little bit, it won't work, so give 
            // a small delay:
            await Task.Delay(100);
            PluginManager.ReactToCtrlF();
            return;

            void SetScreenSubMessage(string message)
            {
                initializationWindow.SubMessage = message;
                System.Windows.Forms.Application.DoEvents();
            }

            void SetScreenMessage(string message)
            {
                initializationWindow.Message = message;
                System.Windows.Forms.Application.DoEvents();
            }
        }
        
        private static void AddObjectsToIocContainer()
        {
            EditorObjects.IoC.Container.Set(new SetPropertyManager());
            EditorObjects.IoC.Container.Set(new NamedObjectSetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveCategorySetVariableLogic());
            EditorObjects.IoC.Container.Set(new StateSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new EventResponseSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new ReferencedFileSaveSetPropertyManager());
            EditorObjects.IoC.Container.Set(new CustomVariableSaveSetPropertyLogic());
            EditorObjects.IoC.Container.Set(new EntitySaveSetPropertyLogic());
            EditorObjects.IoC.Container.Set(new ScreenSaveSetVariableLogic());
            EditorObjects.IoC.Container.Set(new GlobalContentSetVariableLogic());
            EditorObjects.IoC.Container.Set(new PluginUpdater());

            EditorObjects.IoC.Container.Set<IGlueState>(GlueState.Self);
            EditorObjects.IoC.Container.Set<IGlueCommands>(GlueCommands.Self);

            EditorObjects.IoC.Container.Set(new GlueErrorManager());
        }

        private static void AddErrorReporters()
        {
            EditorObjects.IoC.Container.Get<GlueErrorManager>()
                .Add(new CsvErrorReporter());

        }

        private static async Task LoadProjectConsideringSettingsAndArgs(InitializationWindowWpf initializationWindow)
        {
            // This must be called after setting the GlueSettingsSave
            ProjectLoader.Self.GetCsprojToLoad(out var csprojToLoad);

            if (!string.IsNullOrEmpty(csprojToLoad))
            {
                if (initializationWindow != null)
                {
                    initializationWindow.Message = String.Format(Localization.Texts.LoadingX, csprojToLoad);
                }

                await ProjectLoader.Self.LoadProject(csprojToLoad, initializationWindow);
            }
        }
    }
}
