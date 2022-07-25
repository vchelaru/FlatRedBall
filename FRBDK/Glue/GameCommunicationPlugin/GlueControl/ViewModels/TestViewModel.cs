using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace GameCommunicationPlugin.GlueControl.ViewModels
{
    class TestViewModel : ViewModel
    {
        //public int Health
        //{
        //    get => Get<int>();
        //    set => Set(value);
        //}

        int mHealth;
        public int Health
        {
            get => mHealth;
            set
            {
                if(mHealth != value)
                {
                    mHealth = value;
                    NotifyPropertyChanged(nameof(Health));
                }
            }
        }

        public int MaxHealth
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(Health))]
        [DependsOn(nameof(MaxHealth))]
        public SolidColorBrush HealthColor =>
            Health/MaxHealth < .1 ? Brushes.Black
            : Brushes.Red;

        [DependsOn(nameof(Health))]
        public string HealthDisplay
        {
            get => $"Health: {Health}";
        }

        [DependsOn(nameof(Health))]
        public bool IsSubtractEnabled => Health > 0;

        public TestViewModel()
        {
            MaxHealth = 100;
        }
    }
}
