using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{
    public class CollidableNamedObjectRelationshipViewModel : ViewModel
    {
        public string CollisionRelationshipsTitle
        {
            get { return Get<string>(); }
            set { Set(value); }
        }


        public ObservableCollection<NamedObjectPairRelationshipViewModel> NamedObjectPairs
        {
            get { return Get<ObservableCollection<NamedObjectPairRelationshipViewModel>>(); }
            set { Set(value); }
        }

        public CollidableNamedObjectRelationshipViewModel()
        {
            NamedObjectPairs = new ObservableCollection<NamedObjectPairRelationshipViewModel>();
        }
    }
}
