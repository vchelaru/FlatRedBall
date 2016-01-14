using FlatRedBall.Instructions.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    [Serializable]
    public class ArrowInstanceGeneralVm : IViewModel<object>
    {
        #region Fields

        object mGeneralInstance;
        bool mIsSelected;

        #endregion

        public bool IsSelected
        {
            get
            {
                return mIsSelected;
            }
            set
            {
                mIsSelected = value;

                RaisePropertyChanged("IsSelected");
            }
        }


        public object Model
        {
            get
            {
                return mGeneralInstance;
            }
            set
            {
                mGeneralInstance = value;
            }
        }

        public string Name
        {
            get
            {
                if (mGeneralInstance == null)
                {
                    return "No object";
                }
                else
                {
                    string name = 
                        (string)LateBinder.GetInstance(mGeneralInstance.GetType()).GetValue(mGeneralInstance, "Name");

                    return name;
                }
            }
            set
            {
                if (mGeneralInstance != null)
                {

                    LateBinder.GetInstance(mGeneralInstance.GetType()).SetValue(mGeneralInstance, "Name", value);
                    RaisePropertyChanged("Name");
                }
            }
        }

        void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }
    }
}
