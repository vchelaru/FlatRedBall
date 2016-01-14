using FlatRedBall.Arrow.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using FlatRedBall.Arrow.ViewModels;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowProjectVm : IViewModel<ArrowProjectSave>
    {
        #region Fields
        ArrowProjectSave mModel;

        bool mSuppressCollectionChangedEvents;
        #endregion

        #region Properties

        public ArrowProjectSave Model
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

        public ObservableCollection<ArrowElementVm> Elements
        {
            get;
            private set;
        }
        public ObservableCollection<ArrowIntentVm> Intents
        {
            get;
            private set;
        }

        public ObservableCollection<ArrowElementOrIntentVm> TopLevelItems
        {
            get;
            private set;
        }

        #endregion

        #region Event

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        public ArrowProjectVm()
        {
            Elements = new ObservableCollection<ArrowElementVm>();
            Elements.CollectionChanged += HandleElementsChanged;


            Intents = new ObservableCollection<ArrowIntentVm>();
            Intents.CollectionChanged += HandleIntentsChanged;

            TopLevelItems = new ObservableCollection<ArrowElementOrIntentVm>();
        }

        private void HandleIntentsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!mSuppressCollectionChangedEvents)
            {
                // We need to modify the model depending on wwhat we did
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        object removedAsObject = e.OldItems[0];
                        ArrowIntentVm asVm = removedAsObject as ArrowIntentVm;

                        this.Model.Intents.Remove(asVm.Model);

                        break;
                }

                RebuildTopLevelItems();
            }
        }

        private void HandleElementsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RebuildTopLevelItems();

        }

        public void AddNewIntent(string name)
        {
            mModel.Intents.Add(new ArrowIntentSave() { Name = name });

            Refresh();
        }

        
        public void Refresh()
        {
            mSuppressCollectionChangedEvents = true;
            Elements.Match<ArrowElementVm, ArrowElementSave>(mModel.Elements);
            Intents.Match<ArrowIntentVm, ArrowIntentSave>(mModel.Intents);

            RebuildTopLevelItems();

            foreach (var intent in TopLevelItems)
            {
                intent.Refresh();
            }
            mSuppressCollectionChangedEvents = false;
        }

        private void RebuildTopLevelItems()
        {
            TopLevelItems.Clear();

            // We add all intents because they're always top-level
            foreach (var item in Intents)
            {
                TopLevelItems.Add(new ArrowElementOrIntentVm(item));
            }

            // We only add elements that have no intent
            foreach (var item in Elements)
            {
                if (string.IsNullOrEmpty(item.Intent))
                {
                    TopLevelItems.Add(new ArrowElementOrIntentVm(item));
                }
            }
        }

        #endregion

    }
}
