using FlatRedBall.Arrow.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowElementVm : IViewModel<ArrowElementSave>,  ITreeViewDisplayable
    {
        #region Fields
        bool mIsSelected;
        ObservableCollection<ArrowInstanceGeneralVm> mAllInstances = new ObservableCollection<ArrowInstanceGeneralVm>();
        ObservableCollection<ArrowReferencedFileVm> mFiles = new ObservableCollection<ArrowReferencedFileVm>();

        ArrowElementSave mModel;

        bool mSuppressCollectionChangedEvents;

        #endregion

        #region Properties

        public string DisplayText
        {
            get
            {
                return ToString();
            }
        }

        public bool IsSelected
        {
            get
            {
                //return true;
                return mIsSelected;
            }
            set
            {
                mIsSelected = value;
                NotifyPropertyChanged("IsSelected");
                NotifyPropertyChanged("AddInstanceVisibility");
            }
        }

        public ObservableCollection<ArrowInstanceGeneralVm> AllInstances
        {
            get
            {
                return mAllInstances;
            }
        }

        public ObservableCollection<ArrowReferencedFileVm> Files
        {
            get
            {
                return mFiles;
            }
        }

        public ArrowElementSave Model
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

        public string ContentsString
        {
            get
            {
                return this.Model.Name + " Contents";
            }
        }

        public string Intent
        {
            get
            {
                return Model.Intent;
            }
            set
            {
                Model.Intent = value;

                NotifyPropertyChanged("Intent");
            }
        }
               

        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods

        public override string ToString()
        {
            if (Model != null)
            {
                return Model.Name;
            }
            else
            {
                return "No element";
            }
        }


        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


        public void Refresh()
        {
            mSuppressCollectionChangedEvents = true;
            AllInstances.Match<ArrowInstanceGeneralVm, object>(Model.AllInstances);
            mFiles.Match<ArrowReferencedFileVm, ArrowReferencedFileSave>(Model.Files);
            mSuppressCollectionChangedEvents = false;
        }



    }
}
