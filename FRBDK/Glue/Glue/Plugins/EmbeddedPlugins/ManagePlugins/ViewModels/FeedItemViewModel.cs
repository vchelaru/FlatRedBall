using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using FlatRedBall.Glue.Plugins.Rss;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels
{
    class FeedItemViewModel : ViewModel
    {
        string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
                NotifyPropertyChanged(nameof(Title));
            }
        }
        public ICommand ClickCommand { get; set; }
        public RssItem RssItem { get; set; }

        public FeedItemViewModel()
        {
            ClickCommand = new CommandBase(PerformDownload);
        }

        void PerformDownload()
        {
            EditorObjects.IoC.Container.Get<PluginUpdater>().StartDownload(RssItem.DirectLink, HandleDownloadComplete);
        }

        private void HandleDownloadComplete() { }
    }
}
