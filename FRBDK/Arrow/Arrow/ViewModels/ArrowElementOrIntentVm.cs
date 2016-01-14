using FlatRedBall.Arrow.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowElementOrIntentVm : ITreeViewDisplayable, INotifyPropertyChanged
    {
        #region Properties

        public ArrowElementVm ElementVm
        {
            get;
            set;
        }

        public ArrowIntentVm IntentVm
        {
            get;
            set;
        }

        ITreeViewDisplayable ContainedDisplay
        {
            get
            {
                if (ElementVm != null)
                {
                    return ElementVm;
                }
                else
                {
                    return IntentVm;
                }
            }
        }

        public string DisplayText
        {
            get
            {
                return ContainedDisplay.DisplayText;
            }
        }

        public Visibility AddInstanceVisibility
        {
            get
            {
                if (IsSelected || IntentVm != null)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }


        public bool IsSelected
        {
            get
            {
                return ContainedDisplay.IsSelected;
            }
            set
            {
                ContainedDisplay.IsSelected = value;

                NotifyPropertyChanged("IsSelected");
                NotifyPropertyChanged("AddInstanceVisibility");
            }
        }


        public ObservableCollection<ArrowElementVm> ContainedElements
        {
            get;
            private set;
        }
        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        public ArrowElementOrIntentVm() 
        {
            ContainedElements = new ObservableCollection<ArrowElementVm>();
        }

        public ArrowElementOrIntentVm(ArrowElementVm elementVm) 
            : this()
        {
            this.ElementVm = elementVm;
        }

        public ArrowElementOrIntentVm(ArrowIntentVm intentVm)
            : this()
        {
            this.IntentVm = intentVm;
        }

        #endregion


        #region Methods

        void NotifyPropertyChanged(string memberName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }

        }

        public void Refresh()
        {
            this.ContainedElements.Clear();

            if(this.IntentVm != null)
            {
                // get a local reference for performance:
                var intent = IntentVm;

                foreach (var element in ArrowState.Self.CurrentArrowProjectVm.Elements)
                {
                    if (element.Intent == intent.Name)
                    {
                        ContainedElements.Add(element);
                    }
                }
            }
        }

        #endregion

    }
}
