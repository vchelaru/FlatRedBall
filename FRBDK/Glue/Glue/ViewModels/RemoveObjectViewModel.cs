using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace GlueFormsCore.ViewModels
{
    public class RemoveObjectViewModel : ViewModel
    {
        NamedObjectSave backingObject;

        public string ObjectName
        {
            get => Get<string>();
            set => Set(value);
        }

        public ObservableCollection<string> ObjectsToRemove { get; private set; } = new ObservableCollection<string>();

        [DependsOn(nameof(ObjectsToRemove))]
        public Visibility ObjectsToRemoveVisibility => (ObjectsToRemove.Count > 0).ToVisibility();

        [DependsOn(nameof(ObjectName))]
        public string WouldYouLikeToDeleteMessage => $"Would you like to delete {ObjectName}?";

        public RemoveObjectViewModel()
        {
            ObjectsToRemove.CollectionChanged += (not, used) => NotifyPropertyChanged(nameof(ObjectsToRemove));
        }

        public void SetFrom(NamedObjectSave nos)
        {
            backingObject = nos;

            ObjectName = nos.InstanceName;
        }
    }
}
