using FlatRedBall.Arrow.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowIntentVm : IViewModel<ArrowIntentSave>, ITreeViewDisplayable, INotifyPropertyChanged
    {
        #region Fields

        ArrowIntentSave mModel;
        bool mIsSelected;

        #endregion

        #region Properties

        public ArrowIntentSave Model
        {
            get
            {
                return mModel;
            }
            set
            {
                mModel = value;
                Refresh();
            }
        }

        public string Name
        {
            get
            {
                return Model.Name;
            }
        }

        public string DisplayText
        {
            get
            {
                return Name;
            }
        }

        public bool IsSelected
        {
            get{ return mIsSelected;}
            set
            {
                mIsSelected = value;

                NotifyPropertyChanged("IsSelected");
            }
        }

        public ObservableCollection<ArrowIntentComponentVm> Components { get; private set; }


        bool mSuppressCollectionChangedEvents;
        #endregion

        #region Events
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        public ArrowIntentVm()
        {
            Components = new ObservableCollection<ArrowIntentComponentVm>();
            Components.CollectionChanged += HandleComponentsChanged;

        }

        private void HandleComponentsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!mSuppressCollectionChangedEvents)
            {
                // We need to modify the model depending on wwhat we did
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        object removedAsObject = e.OldItems[0];
                        ArrowIntentComponentVm asVm = removedAsObject as ArrowIntentComponentVm;

                        this.Model.Components.Remove(asVm.Model);

                        break;
                }

            }
        }

        private void Refresh()
        {
            mSuppressCollectionChangedEvents = true;
            Components.Match<ArrowIntentComponentVm, ArrowIntentComponentSave>(mModel.Components);
            mSuppressCollectionChangedEvents = false;
        }

        public void AddNewIntentComponent()
        {
            ArrowIntentComponentSave newIntentComponent = new ArrowIntentComponentSave();
            this.Model.Components.Add(newIntentComponent);
            Refresh();

        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public override string ToString()
        {
            return this.Name;
        }

    }
}
