using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace AtlasPlugin.ViewModels
{
    class AtlasListViewModel : ViewModel
    {
        public bool SuppressChangedEvents
        {
            get;
            private set;
        }

        public ObservableCollection<AtlasViewModel> Atlases
        {
            get;
            private set;
        } = new ObservableCollection<AtlasViewModel>();

        public AtlasViewModel SelectedAtlas
        {
            get;
            set;
        }

        public ICommand AddCommand
        {
            get;
            private set;
        } 

        public string AddButtonText
        {
            get
            {
                return "Add new atlas";
            }
        }


        public AtlasListViewModel()
        {
            AddCommand = new CommandBase(AddAtlas, null);
        }

        public void SetFrom(TpsFileSave model)
        {
            SuppressChangedEvents = true;
            Atlases.Clear();


            foreach(var item in model.AtlasFilters)
            {
                var vm = new AtlasViewModel();
                vm.Folder = item;
                Atlases.Add(vm);
            }
            SuppressChangedEvents = false;

        }


        internal void HandleListBoxKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                bool canRemove = SelectedAtlas != null && 
                    // If "" is selected, then that means that the "entire content folder"
                    // atlas is selected, so don't remove
                    SelectedAtlas.Folder != "";

                if (canRemove)
                {

                    Atlases.Remove(SelectedAtlas);

                    if (Atlases.Count == 0)
                    {
                        // need to add the empty atlas:
                        var emptyFolderViewModel = new AtlasViewModel();
                        emptyFolderViewModel.Folder = "";
                        Atlases.Add(emptyFolderViewModel);
                    }
                }
            }
        }

        private void AddAtlas()
        {
            var dialog = new FolderBrowserDialog();
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            dialog.SelectedPath = GlueState.Self.CurrentMainContentProject.Directory.Replace("/", "\\");

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var viewModel = CreateViewModel(dialog.SelectedPath + "/");

                Atlases.Add(viewModel);

                // remove empty atlas if other Atlases exist:
                if(Atlases.Count > 1 )
                {
                    var emptyFolderAtlas = Atlases.FirstOrDefault(item => item.Folder == "");

                    if (emptyFolderAtlas != null)
                    {
                        Atlases.Remove(emptyFolderAtlas);
                    }
                }
            }
        }

        private AtlasViewModel CreateViewModel(string path)
        {
            var relative = FileManager.MakeRelative(path, GlueState.Self.CurrentMainContentProject.Directory);

            var viewModel = new AtlasViewModel();

            viewModel.Folder = relative;

            return viewModel;
        }
    }
}
