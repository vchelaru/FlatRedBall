using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{
    public class NamedObjectPairRelationshipViewModel : ViewModel
    {
        public string OtherObjectName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public string SelectedNamedObjectName
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public ObservableCollection<RelationshipListCellViewModel> Relationships
        {
            get { return Get<ObservableCollection<RelationshipListCellViewModel>>(); }
            set { Set(value); }
        }

        public event EventHandler AddObjectClicked;

        public NamedObjectPairRelationshipViewModel()
        {
            Relationships = new ObservableCollection<RelationshipListCellViewModel>();
        }

        public void AddNewRelationship()
        {
            AddObjectClicked(this, null);
        }
    }
}
