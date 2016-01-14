using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FlatRedBallProfiler.ViewModels
{
    public class AllContentManagersViewModel
    {
        public ObservableCollection<ContentManagerViewModel> ContentManagers
        {
            get;
            private set;
        }

        public ContentManagerViewModel SelectedItem
        {
            get;
            set;
        }

        public ContentManagerViewModel CurrentContentManager
        {
            get;
            set;
        }

        public AllContentManagersViewModel()
        {
            ContentManagers = new ObservableCollection<ContentManagerViewModel>();
        }

        public void UpdateToEngine()
        {
            ContentManagers.Clear();

            foreach(var manager in FlatRedBall.FlatRedBallServices.ContentManagers)
            {
                ContentManagerViewModel managerViewModel = new ContentManagerViewModel();
                managerViewModel.UpdateTo(manager);

                ContentManagers.Add(managerViewModel);
            }
        }
    }
}
