using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.ViewModels
{
    class GumViewModel : ViewModel
    {
        GumProjectSave backingGumProject;
        ReferencedFileSave backingRfs;

        bool shouldRaiseEvents = true;

        bool useAtlases;
        public bool UseAtlases
        {
            get
            {
                return useAtlases;
            }
            set
            {
                if (useAtlases != value)
                {
                    useAtlases = value;

                    backingRfs.Properties.SetValue(
                        nameof(UseAtlases), value);

                    base.NotifyPropertyChanged(nameof(UseAtlases));
                }
            }
        }

        bool autoCreateGumScreens;
        public bool AutoCreateGumScreens
        {
            get
            {
                return autoCreateGumScreens;
            }
            set
            {
                if(autoCreateGumScreens != value)
                {
                    autoCreateGumScreens = value;

                    backingRfs.Properties.SetValue(
                        nameof(AutoCreateGumScreens), value);

                    base.NotifyPropertyChanged(nameof(AutoCreateGumScreens));
                }
            }
        }

        bool showDottedOutlines;
        public bool ShowDottedOutlines
        {
            get
            {
                return showDottedOutlines;
            }
            set
            {
                if(showDottedOutlines != value)
                {
                    showDottedOutlines = value;

                    backingRfs.Properties.SetValue(
                        nameof(ShowDottedOutlines), value);

                    base.NotifyPropertyChanged(nameof(ShowDottedOutlines));
                }
            }
        }

        protected override void NotifyPropertyChanged(string propertyName)
        {
            if(shouldRaiseEvents)
            {
                base.NotifyPropertyChanged(propertyName);
            }
        }

        public void SetFrom(GumProjectSave gumProjectSave, ReferencedFileSave referencedFileSave)
        {
            shouldRaiseEvents = false;
            {
                backingGumProject = gumProjectSave;
                backingRfs = referencedFileSave;

                UseAtlases = backingRfs.Properties.GetValue<bool>(nameof(UseAtlases));
                AutoCreateGumScreens = backingRfs.Properties.GetValue<bool>(nameof(AutoCreateGumScreens));
                ShowDottedOutlines = backingRfs.Properties.GetValue<bool>(nameof(ShowDottedOutlines));
            }
            shouldRaiseEvents = true;
        }

    }
}
