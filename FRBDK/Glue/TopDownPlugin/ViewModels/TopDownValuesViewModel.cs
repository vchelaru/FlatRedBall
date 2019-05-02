using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Models;

namespace TopDownPlugin.ViewModels
{
    public class TopDownValuesViewModel : ViewModel
    {
        public string Name
        {
            get { return Get<string>(); }
            set { Set(value); }
        }

        public float MaxSpeed
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float AccelerationTime
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        public float DecelerationTime
        {
            get { return Get<float>(); }
            set { Set(value); }
        }

        internal void SetFrom(TopDownValues values)
        {
            this.Name = values.Name;
            this.MaxSpeed = values.MaxSpeed;
            this.AccelerationTime = values.AccelerationTime;
            this.DecelerationTime = values.DecelerationTime;
        }

        internal TopDownValues ToValues()
        {
            var toReturn = new TopDownValues();

            toReturn.Name = this.Name;
            toReturn.MaxSpeed = this.MaxSpeed;
            toReturn.AccelerationTime = this.AccelerationTime;
            toReturn.DecelerationTime = this.DecelerationTime;

            return toReturn;
        }
    }
}
