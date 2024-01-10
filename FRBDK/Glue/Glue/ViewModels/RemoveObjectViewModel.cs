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
        public ObservableCollection<string> ObjectsToRemove { get; private set; } = new ObservableCollection<string>();

        [DependsOn(nameof(ObjectsToRemove))]
        public Visibility ObjectsToRemoveVisibility => (ObjectsToRemove.Count > 0).ToVisibility();

        public string WouldYouLikeToDeleteMessage { get; private set; }

        public RemoveObjectViewModel()
        {
            ObjectsToRemove.CollectionChanged += (not, used) => NotifyPropertyChanged(nameof(ObjectsToRemove));
        }

        public void SetFrom(List<NamedObjectSave> namedObjects)
        {
            WouldYouLikeToDeleteMessage = "Would you like to delete:\n";

            foreach(var nos in namedObjects)
            {
                WouldYouLikeToDeleteMessage += nos.ToString() + "\n";
            }
        }
    }
}
