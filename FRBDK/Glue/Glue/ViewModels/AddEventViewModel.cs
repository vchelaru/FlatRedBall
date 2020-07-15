using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace GlueFormsCore.ViewModels
{
    public class AddEventViewModel : ViewModel
    {
        public CustomEventType DesiredEventType
        {
            get => Get<CustomEventType>();
            set => Set(value);
        }

        public string TunnelingObject
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TunnelingEvent
        {
            get => Get<string>();
            set => Set(value);
        }

    }
}
