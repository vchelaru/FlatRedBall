using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace FlatRedBallProfiler.ViewModels
{
    public class ContentManagerViewModel : ViewModel
    {
        FlatRedBall.Content.ContentManager backingData;

        public ContentViewModel SelectedItem { get; set; }

        public ObservableCollection<ContentViewModel> ContainedContent
        {
            get;
            private set;
        }

        public string Name
        {
            get
            {
                return backingData.Name;
            }
        }



        public ContentManagerViewModel()
        {
            ContainedContent = new ObservableCollection<ContentViewModel>();
        }

        public void UpdateTo(FlatRedBall.Content.ContentManager contentManager)
        {
            backingData = contentManager;
            base.RaisePropertyChanged("Name");
            ContainedContent.Clear();
            // force refresh?

            List<ContentViewModel> contentList = new List<ContentViewModel>();

            foreach (var item in backingData.DisposableObjects)
            {
                var contentViewModel = new ContentViewModel(item);
                contentViewModel.Parent = this;

                contentList.Add(contentViewModel);
            }

            foreach(var item in contentList.OrderBy(item=>item.Name))
            {
                ContainedContent.Add(item);
            }


        }

    }
}
