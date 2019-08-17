using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.CollisionPlugin.ViewModels
{
    public class RelationshipListCellViewModel : ViewModel
    {
        public NamedObjectSave CollisionRelationshipNamedObject
        {
            get { return Get<NamedObjectSave>(); }
            set { Set(value); }
        }

        public NamedObjectSave OwnerNamedObject
        {
            get { return Get<NamedObjectSave>(); }
            set { Set(value); }
        }

        public NamedObjectSave OtherNamedObject
        {
            get { return Get<NamedObjectSave>(); }
            set { Set(value); }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public Visibility AddRelationshipVisibility
        {
            get
            {
                if(CollisionRelationshipNamedObject == null)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public Visibility RelationshipDetailsVisibility
        {
            get
            {
                if(CollisionRelationshipNamedObject == null)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        [DependsOn(nameof(CollisionRelationshipNamedObject))]
        public string RelationshipDetails
        {
            get
            {
                if(CollisionRelationshipNamedObject != null)
                {
                    return CollisionRelationshipNamedObject.InstanceName;
                }
                else
                {
                    return null;
                }
            }
        }



    }
}
