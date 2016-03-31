using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.Rss;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins.ViewModels
{
    class BrowseGlueVaultViewModel : ViewModel
    {
        public ObservableCollection<FeedItemViewModel> AvailablePlugins { get; set; } = new ObservableCollection<FeedItemViewModel>();

        public void UpdateFrom(AllFeed allFeed)
        {
            AvailablePlugins.Clear();

            if (allFeed != null)
            {
                foreach (var item in allFeed.Items)
                {
                    var viewModel = new FeedItemViewModel
                    {
                        Title = item.Title,
                        RssItem = item
                    };
                    AvailablePlugins.Add(viewModel);
                }
            }
        }
    }
}
