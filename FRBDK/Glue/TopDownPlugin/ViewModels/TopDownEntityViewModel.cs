using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TopDownPlugin.ViewModels
{
    public class TopDownEntityViewModel : ViewModel
    {
        public ObservableCollection<TopDownValuesViewModel> TopDownValues { get; private set; }

        public bool IsTopDown
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsTopDown))]
        public Visibility TopDownUiVisibility
        {
            get
            {
                if(IsTopDown)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public TopDownEntityViewModel()
        {
            TopDownValues = new ObservableCollection<TopDownValuesViewModel>();

            TopDownValues.CollectionChanged += HandleTopDownValuesChanged;
        }

        private void HandleTopDownValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (TopDownValuesViewModel newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += HandleTopDownValuePropertyChanged;
                    }

                    base.NotifyPropertyChanged(nameof(this.TopDownValues));

                    break;
                case NotifyCollectionChangedAction.Remove:

                    base.NotifyPropertyChanged(nameof(this.TopDownValues));

                    break;
            }
        }

        private void HandleTopDownValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.NotifyPropertyChanged(nameof(this.TopDownValues));
        }
    }
}
