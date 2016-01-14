using AtlasPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using FlatRedBall.Glue;

namespace AtlasPlugin.Managers
{
    class AtlasFileManager
    {

        TpsFileSave loadedFile;
        public TpsFileSave LoadedFile
        {
            get { return loadedFile; }
        }

        ReferencedFileSave referencedFileSave;

        AtlasListViewModel viewModel;
        public AtlasListViewModel ViewModel
        {
            get
            {
                return viewModel;
            }
            set
            {
                viewModel = value;

                ViewModel.PropertyChanged += HandlePropertyChanged;
                ViewModel.Atlases.CollectionChanged += HandleAtlasCollectionChanged;
            }
        }

        private void HandleAtlasCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ////////////////////////////// Early out:////////////////////////////
            if (ViewModel.SuppressChangedEvents)
            {
                return;
            }
            ////////////////////////////// End early out////////////////////////////

            var shouldSave = false;
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                var whatWasAdded = e.NewItems[0] as AtlasViewModel;

                string folderToAdd = whatWasAdded.Folder;

                loadedFile.AddAtlas(folderToAdd);

                shouldSave = true;
            }
            else if(e.Action == NotifyCollectionChangedAction.Remove)
            {
                // Someone: Check this when deletion is supported:
                var whatWasRemoved = e.OldItems[0] as AtlasViewModel;

                string folderToRemove = whatWasRemoved.Folder;

                loadedFile.RemoveAtlas(folderToRemove);

                shouldSave = true;
            }
            else if(e.Action == NotifyCollectionChangedAction.Reset)
            {
                loadedFile.ClearAtlases();
                shouldSave = true;
            }
            else
            {
                throw new NotImplementedException();
            }

            if(shouldSave)
            {
                
                var fileName = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);

                loadedFile.Save(fileName);

            }
        }

        public static string AtlasFolder
        {
            get
            {
                return GlueState.Self.ContentDirectory + "Atlases/";
            }
        }

        private bool RemoveEmptyAtlasIfOtherAtlasesExist()
        {
            bool didRemove = false;
            var atlases = loadedFile.AtlasFilters.ToList();

            if(atlases.Count > 1 && atlases.Any(item=>item == ""))
            {
                // we need to remove the empty atlas:
                loadedFile.RemoveAtlas("");
                didRemove = true;
            }

            return didRemove;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We don't need this yet because we currently aren't setting any properties
            // on the atlas besides the folders, and those are handled by the HandleAtlasCollectionChanged.
            // We may be setting properties in the future, so I'm going to leave this function here.
        }

        public void SetRfs(ReferencedFileSave referencedFile)
        {
            referencedFileSave = referencedFile;


            if (referencedFileSave != null)
            {
                LoadFileFromRfs(referencedFileSave);
                viewModel.SetFrom(loadedFile);
            }
            else
            {
                loadedFile = null;
            }
        }

        private void LoadFileFromRfs(ReferencedFileSave referencedFile)
        {
            string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(referencedFile);

            if (System.IO.File.Exists(absoluteFileName))
            {

                TpsLoadResult result;
                loadedFile = TpsFileSave.Load(absoluteFileName, out result);
            }
            else
            {
                loadedFile = null;
            }
        }

        public void CreateNewProject()
        {
            var newTps = new TpsFileSave();
            newTps.SetDefaultValues();

            string fileName = AtlasFolder + "TexturePackerProject.tps";

            newTps.Save(fileName);

            bool userCancelled = false;

            // Select the Global Content tree node to add the file there:
            GlueState.Self.CurrentTreeNode = FlatRedBall.Glue.FormHelpers.ElementViewWindow.GlobalContentFileNode;


            var rfs = FlatRedBall.Glue.FormHelpers.RightClickHelper.AddSingleFile(
                    fileName, ref userCancelled);

            GlueState.Self.CurrentReferencedFileSave = rfs;


        }
    }
}
