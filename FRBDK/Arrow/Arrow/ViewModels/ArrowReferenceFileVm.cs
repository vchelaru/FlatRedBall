using FlatRedBall.Arrow.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.ViewModels
{
    public class ArrowReferencedFileVm : IViewModel<ArrowReferencedFileSave>
    {
        public ArrowReferencedFileSave Model
        {
            get;
            set;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
