using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.EmbeddedPlugins.ProjectTreeView.ViewModels
{
    public class GlueProjectViewModel
    {
        public ObservableCollection<ScreenViewModel> Screens { get; private set; } = new ObservableCollection<ScreenViewModel>();
        public ObservableCollection<EntityViewModel> Entities{ get; private set; } = new ObservableCollection<EntityViewModel>();
        public ObservableCollection<ReferencedFileViewModel> GlobalContent { get; private set; }
         = new ObservableCollection<ReferencedFileViewModel>();
    }
}
