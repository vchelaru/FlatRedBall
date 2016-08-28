using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FlatRedBall.PlatformerPlugin.ViewModels
{
    public class PlatformerEntityViewModel : ViewModel
    {
        bool isPlatformer;

        public ObservableCollection<PlatformerValuesViewModel> PlatformerValues { get; private set; }

        public bool IsPlatformer
        {
            get
            {
                return isPlatformer;
            }
            set
            {
                base.ChangeAndNotify(ref isPlatformer, value);
                NotifyPropertyChanged(nameof(PlatformerUiVisibility));
            }
        }

        public Visibility PlatformerUiVisibility
        {
            get
            {
                if(IsPlatformer)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public PlatformerEntityViewModel()
        {
            PlatformerValues = new ObservableCollection<PlatformerValuesViewModel>();

            PlatformerValues.CollectionChanged += HandlePlatformerValuesChanged;
        }

        private void HandlePlatformerValuesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int m = 3;

            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    foreach (PlatformerValuesViewModel newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += HandlePlatformerValuePropertyChanged;
                    }

                    base.NotifyPropertyChanged(nameof(this.PlatformerValues));

                    break;
                case NotifyCollectionChangedAction.Remove:

                    base.NotifyPropertyChanged(nameof(this.PlatformerValues));

                    break;
            }
        }

        private void HandlePlatformerValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.NotifyPropertyChanged(nameof(this.PlatformerValues));
        }
    }
}
