using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Wizard.ViewModels
{
    class NamedObjectSaveViewModel : ViewModel
    {
        public bool IsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        public string TextDisplay
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public List<NamedObjectSaveViewModel> ContainedObjects
        {
            get; set;
        } = new List<NamedObjectSaveViewModel>();

        public NamedObjectSave BackingObject
        {
            get; set;
        }
    }
    class ElementViewModel : ViewModel
    {
        public List<NamedObjectSaveViewModel> NamedObjects { get; set; }
             = new List<NamedObjectSaveViewModel>();
        public string Name { get; set; }
    }

    class CreateObjectJsonViewModel : ViewModel
    {
        public List<ElementViewModel> Elements { get; set; } = new List<ElementViewModel>();
        //public List<ElementViewModel> Entities { get; set; } = new List<ElementViewModel>();

        public string GeneratedJson
        {
            get => Get<string>();
            set => Set(value);
        }
    }
}
