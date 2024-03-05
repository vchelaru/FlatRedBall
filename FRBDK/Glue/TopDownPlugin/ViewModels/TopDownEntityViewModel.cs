using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TopDownPlugin.Models;

namespace TopDownPlugin.ViewModels
{
    
    public class TopDownEntityViewModel : ViewModel
    {
        #region IsTopDown-related

        public ObservableCollection<TopDownValuesViewModel> TopDownValues { get; private set; }

        public EntitySave BackingData { get; set; }

        public bool IsTopDown
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool InheritsFromTopDown
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsTopDown))]
        [DependsOn(nameof(InheritsFromTopDown))]
        bool IsEffectivelyTopDown => IsTopDown || InheritsFromTopDown;

        [DependsOn(nameof(IsEffectivelyTopDown))]
        public Visibility TopDownUiVisibility => IsEffectivelyTopDown.ToVisibility();

        [DependsOn(nameof(InheritsFromTopDown))]
        public Visibility TopDownCheckBoxVisibility => (InheritsFromTopDown == false).ToVisibility();

        [DependsOn(nameof(InheritsFromTopDown))]
        public Visibility InheritanceLabelVisibility => InheritsFromTopDown.ToVisibility();

        #endregion

        public List<string> LeftSideItems { get; private set; } = new List<string>
        {
            "Movement Values",
            "Animation"
        };

        public int SelectedLeftSideIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(SelectedLeftSideIndex))]
        public Visibility MovementValueVisibility => 
            SelectedLeftSideIndex == 0 ? 
                Visibility.Visible : 
                Visibility.Collapsed;

        [DependsOn(nameof(SelectedLeftSideIndex))]
        public Visibility AnimationVisibility =>
            SelectedLeftSideIndex == 1 ?
                Visibility.Visible :
                Visibility.Collapsed;


        public ObservableCollection<AnimationRowViewModel> AnimationRows { get; private set; } =
            new ObservableCollection<AnimationRowViewModel>();



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

        public void AssignAnimationRowEvents(AnimationRowViewModel viewModel)
        {
            viewModel.MoveUp += () =>
            {
                var index = this.AnimationRows.IndexOf(viewModel);
                if (index > 0)
                {
                    this.AnimationRows.Move(index, index - 1);
                }
            };

            viewModel.MoveDown += () =>
            {
                var index = this.AnimationRows.IndexOf(viewModel);
                if (index < this.AnimationRows.Count - 1)
                {
                    this.AnimationRows.Move(index, index + 1);
                }
            };

            viewModel.Remove += () =>
            {
                this.AnimationRows.Remove(viewModel);
            };

            viewModel.Duplicate += () =>
            {
                var values = new IndividualTopDownAnimationValues();
                viewModel.ApplyTo(values);

                var newVm = new AnimationRowViewModel();
                newVm.SetFrom(values);
                AssignAnimationRowEvents(newVm);
                var newIndex = this.AnimationRows.IndexOf(viewModel) + 1;
                this.AnimationRows.Insert(newIndex, newVm);
            };
        }

    }
}
