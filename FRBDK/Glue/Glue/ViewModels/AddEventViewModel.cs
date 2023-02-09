using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
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

        public string EventName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SourceVariable
        {
            get => Get<string>();
            set => Set(value);
        }

        public BeforeOrAfter BeforeOrAfter
        {
            get => Get<BeforeOrAfter>();
            set => Set(value);
        }

        public string DelegateType
        {
            get => Get<string>();
            set => Set(value);
        }

        public List<ExposableEvent> ExposableEvents 
        {
            get => Get<List<ExposableEvent>>();
            set => Set(value);
        }

        public AddEventViewModel()
        {
            ExposableEvents = new List<ExposableEvent>();
        }

    }
}
