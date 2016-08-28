using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.Glue.MVVM
{
    public class ViewModel : INotifyPropertyChanged
    {
        protected void ChangeAndNotify<T>(ref T property, T value, [CallerMemberName] string propertyName = null) 
        {
            if (EqualityComparer<T>.Default.Equals(property, value) == false)
			{
                property = value;
                NotifyPropertyChanged(propertyName);
			}
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
