using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TiledPluginCore.ViewModels;
using TiledPluginCore.Views;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using System.Collections.Specialized;

namespace TiledPluginCore.Controllers
{
    class LevelScreenController : Singleton<LevelScreenController>
    {
        #region Fields/Properties

        LevelScreenView view;
        LevelScreenViewModel viewModel;

        const string IsTmxLevel = nameof(IsTmxLevel);

        bool isIgnoringViewModelChanges = false;

        #endregion

        #region View-related

        public bool GetIfShouldShow()
        {
            var screen = GlueState.Self.CurrentScreenSave;

            return screen?.Name == "Screens\\GameScreen";
        }

        internal LevelScreenView GetView()
        {
            if (view == null)
            {
                view = new LevelScreenView();
                view.RenameScreen += (not, used) => HandleRenameScreenClicked();

                viewModel = new ViewModels.LevelScreenViewModel();
                viewModel.PropertyChanged += HandleViewModelPropertyChanged;
                viewModel.TmxFiles.CollectionChanged += HandleTmxFileCollectionChanged;
                view.DataContext = viewModel;
            }
            return view;
        }

        internal void HandleTabShown()
        {
            if(viewModel.AutoCreateTmxScreens)
            {
                GenerateScreensForAllTmxFiles();
            }
        }

        #endregion

        #region View Model

        internal void RefreshViewModelTo(FlatRedBall.Glue.SaveClasses.ScreenSave currentScreenSave)
        {
            isIgnoringViewModelChanges = true;

            viewModel.GlueObject = currentScreenSave;

            RefreshViewModelTmxFileList();

            RefreshOrphanedScreens();

            viewModel.UpdateFromGlueObject();
            
            isIgnoringViewModelChanges = false;
        }

        private void RefreshOrphanedScreens()
        {
            var allScreens = GlueState.Self.CurrentGlueProject?.Screens ?? new List<ScreenSave>();

            List<ScreenSave> orphanedScreens = new List<ScreenSave>();

            var levelScreens = GetLevelScreens();

            foreach(var screen in levelScreens)
            {
                foreach(var tmxFile in screen.ReferencedFiles.Where(item => item.Name.ToLowerInvariant().EndsWith(".tmx")))
                {
                    var filePath = GlueCommands.Self.GetAbsoluteFilePath(tmxFile);

                    if(!filePath.Exists())
                    {
                        orphanedScreens.Add(screen);
                        break;
                    }
                }
            }

            viewModel.OrphanedScreens.Clear();
            foreach(var screen in orphanedScreens)
            {
                viewModel.OrphanedScreens.Add(screen.Name);
            }
        }

        private void RefreshViewModelTmxFileList()
        {
            viewModel.TmxFiles.Clear();

            var allTmxFiles = GetAllLevelTmxFiles();

            var contentDirectory = GlueState.Self.ContentDirectory;
            foreach (var tmxFile in allTmxFiles)
            {
                viewModel.TmxFiles.Add(FileManager.MakeRelative(tmxFile.FullPath, contentDirectory));
            }
        }

        private void HandleTmxFileCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ////////////Early Out/////////////
            if (isIgnoringViewModelChanges)
            {
                return;
            }
            //////////End Early Out///////////

            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newItems = e.NewItems;

                    if(newItems.Count > 0)
                    {
                        GenerateScreensForAllTmxFiles();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:

                    foreach(var item in e.OldItems)
                    {
                        var screenName = GetLevelScreenNameFor(GlueState.Self.ContentDirectory +  item);

                        if(screenName != null)
                        {
                            var screen = ObjectFinder.Self.GetScreenSave(screenName);
                            GlueCommands.Self.GluxCommands.RemoveScreen(screen);
                        }
                    }
                    break;
            }

        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ////////////Early Out/////////////
            if(isIgnoringViewModelChanges)
            {
                return;
            }
            //////////End Early Out///////////

            switch(e.PropertyName)
            {
                case nameof(viewModel.AutoCreateTmxScreens):
                    if(viewModel.AutoCreateTmxScreens)
                    {
                        GenerateScreensForAllTmxFiles();
                    }
                    else
                    {
                        RemoveScreensForAllTmxFiles();
                    }
                    break;
                case nameof(viewModel.ShowLevelScreensInTreeView):
                    var isHidden = !viewModel.ShowLevelScreensInTreeView;
                    var tmxLevelScreens =
                        GlueState.Self.CurrentGlueProject.Screens.Where(item => item.Properties.GetValue<bool>(IsTmxLevel)).ToArray();
                    foreach (var screen in tmxLevelScreens)
                    {
                        screen.IsHiddenInTreeView = isHidden;
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screen);
                    }
                    break;
            }
        }

        #endregion

        #region Utilities

        private static List<FilePath> GetAllLevelTmxFiles()
        {
            // This returns all TMX files that are in the content folder
            // and not referenced by a non-level element.
            // They are considered level files if they are files unreferenced by
            // Glue, or if they are referenced only by a level screen.

            var contentDirectory = GlueState.Self.ContentDirectory;
            var files = FileManager.GetAllFilesInDirectory(contentDirectory, "tmx").Select(item => new FilePath(item)).ToList();

            void RemoveRfsFromTmx(FlatRedBall.Glue.SaveClasses.ReferencedFileSave tmxRfs)
            {
                var filePath = GlueCommands.Self.FileCommands.GetFilePath(tmxRfs);

                if (files.Contains(filePath))
                {
                    files.Remove(filePath);
                }
            }

            foreach(var screen in GlueState.Self.CurrentGlueProject.Screens)
            {
                var isLevel = screen.Properties.GetValue<bool>(IsTmxLevel);
                if(!isLevel)
                {
                    foreach(var rfs in screen.ReferencedFiles)
                    {
                        RemoveRfsFromTmx(rfs);
                    }
                }
            }
            foreach(var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                foreach (var rfs in entity.ReferencedFiles)
                {
                    RemoveRfsFromTmx(rfs);
                }
            }

            foreach (var rfs in GlueState.Self.CurrentGlueProject.GlobalFiles)
            {
                RemoveRfsFromTmx(rfs);
            }

            return files;

        }

        private string GetLevelScreenNameFor(FilePath tmxFile)
        {
            var stripped = tmxFile.NoPathNoExtension
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace("(", " ")
                .Replace(")", " ");

            stripped = char.ToUpper(stripped[0]) + stripped.Substring(1) + "Level";



            return "Screens\\" + stripped;
        }

        private List<ScreenSave> GetLevelScreens()
        {
            List<ScreenSave> screens = new List<ScreenSave>();

            if(GlueState.Self.CurrentGlueProject != null)
            {
                screens = GlueState.Self.CurrentGlueProject.Screens
                    .Where(item => item.Properties.GetValue<bool>(IsTmxLevel))
                    .ToList();
            }

            return screens;
        }


        #endregion

        #region Glue Project

        private async void GenerateScreensForAllTmxFiles()
        {
            var tmxFiles = GetAllLevelTmxFiles();

            var shouldSave = false;

            foreach(var tmxFile in tmxFiles)
            {
                var expectedScreenName = GetLevelScreenNameFor(tmxFile);

                var existingScreen = ObjectFinder.Self.GetScreenSave(expectedScreenName);

                if(existingScreen == null)
                {
                    var newScreen = new ScreenSave();
                    newScreen.Name = expectedScreenName;
                    newScreen.Properties.SetValue(IsTmxLevel, true);
                    newScreen.IsHiddenInTreeView = viewModel.ShowLevelScreensInTreeView == false;
                    newScreen.BaseScreen = "Screens\\GameScreen";

                    await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(newScreen, suppressAlreadyExistingFileMessage: true);
                    GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(newScreen);


                    // add the TMX file:
                    var rfs = GlueCommands.Self.GluxCommands.AddSingleFileTo(tmxFile.FullPath, tmxFile.NoPathNoExtension, null, null, false, null, newScreen, null, false);
                    var mapRfs = newScreen.GetNamedObject("Map");
                    mapRfs.SourceType = SourceType.File;
                    mapRfs.SourceFile = rfs.Name;
                    mapRfs.SourceName = "Entire File (LayeredTileMap)";

                    shouldSave = true;
                    //GlueCommands.Self.GluxCommands.ScreenCommands.

                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(newScreen);

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newScreen);

                }
            }

            if(shouldSave)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        private void RemoveScreensForAllTmxFiles()
        {
            foreach(var screen in GetLevelScreens())
            {
                TaskManager.Self.AddOrRunIfTasked(() =>
                {
                    // don't delete the files, in case the user wants to reference
                    // them or re-add.
                    GlueCommands.Self.GluxCommands.RemoveScreen(screen);
                }, $"Removing {screen}");
            }
        }

        internal async void HandleRenameScreenClicked()
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new TMX name";

            tiw.Result = viewModel.SelectedTmxFile;

            var result = tiw.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // first we rename the screen...
                var currentFilePath = viewModel.SelectedTmxFilePath;
                var currentScreenName = GetLevelScreenNameFor(currentFilePath);
                var currentScreen = ObjectFinder.Self.GetScreenSave(currentScreenName);

                var desiredFilePath = new FilePath(GlueState.Self.ContentDirectory + tiw.Result);
                var desiredScreenName = GetLevelScreenNameFor(desiredFilePath);

                var desiredScreenNameWithoutScreenPrefix = desiredScreenName.Substring("Screens\\".Length);

                var isScreenNameValid = NameVerifier.IsScreenNameValid(desiredScreenNameWithoutScreenPrefix, 
                    currentScreen, out string whyScreenNameIsntValid);

                var rfs = currentScreen.GetReferencedFileSave(viewModel.SelectedTmxFile);
                var isRfsNameValid = NameVerifier.IsReferencedFileNameValid(tiw.Result, rfs.GetAssetTypeInfo(), 
                    rfs, currentScreen, out string whyRfsIsntValid);

                if (currentFilePath.GetDirectoryContainingThis() != desiredFilePath.GetDirectoryContainingThis())
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox("The old file was located in \n" +
                        currentFilePath.GetDirectoryContainingThis() + "\n" +
                        "The new file is located in \n" +
                        desiredFilePath.GetDirectoryContainingThis() + "\n" +
                        "Currently Glue does not support changing directories.");
                }
                else if(!isRfsNameValid)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(
                        "Could not rename the TMX file because it would produce an invalid Glue file name:\n" +
                        whyRfsIsntValid);
                }
                else if (!isScreenNameValid)
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(
                        "Could not rename the TMX file because it would produce an invalid screen name:\n" + 
                        whyScreenNameIsntValid);
                }
                else

                {
                    await GlueCommands.Self.GluxCommands.ElementCommands.RenameElement(currentScreen, desiredScreenNameWithoutScreenPrefix);


                    GlueCommands.Self.FileCommands.RenameReferencedFileSave(rfs, tiw.Result);

                    isIgnoringViewModelChanges = true;

                    RefreshViewModelTmxFileList();

                    isIgnoringViewModelChanges = false;

                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(currentScreen);
                }
                // then we rename the file
            }

        }

        #endregion
    }
}
