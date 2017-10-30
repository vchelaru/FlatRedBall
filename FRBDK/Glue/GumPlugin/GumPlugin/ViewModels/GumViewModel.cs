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
    public enum FileAdditionBehavior
    {
        EmbedCodeFiles = 0,
        AddDll,
        IncludeNoFiles
    }

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

        // We don't use this to adjust the data (ReferencedFileSave, settings file), but it's here
        // for when we first adjust to the ReferencedFileSave so that we can check or uncheck the radio.

        FileAdditionBehavior behavior;
        public bool EmbedCodeFiles
        {
            get { return behavior == FileAdditionBehavior.EmbedCodeFiles; }
            set
            {
                if (value) behavior = FileAdditionBehavior.EmbedCodeFiles;
                UpdateBehaviorOnRfs();
                NotifyPropertyChanged(nameof(EmbedCodeFiles));
            }
        }

        public bool AddDll
        {
            get { return behavior == FileAdditionBehavior.AddDll; }
            set
            {
                if (value) behavior = FileAdditionBehavior.AddDll;
                UpdateBehaviorOnRfs();
                NotifyPropertyChanged(nameof(AddDll));

            }
        }

        public bool IncludeNoFiles
        {
            get { return behavior == FileAdditionBehavior.IncludeNoFiles; }
            set
            {
                if (value) behavior = FileAdditionBehavior.IncludeNoFiles;
                UpdateBehaviorOnRfs();
                NotifyPropertyChanged(nameof(IncludeNoFiles));

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
                FileAdditionBehavior behavior = (FileAdditionBehavior) backingRfs.Properties.GetValue<int>(nameof(FileAdditionBehavior));

                AddDll = behavior == FileAdditionBehavior.AddDll;
                EmbedCodeFiles = behavior == FileAdditionBehavior.EmbedCodeFiles;
                IncludeNoFiles = behavior == FileAdditionBehavior.IncludeNoFiles;

            }
            shouldRaiseEvents = true;
        }


        private void UpdateBehaviorOnRfs()
        {
            if(AddDll)
            {
                backingRfs.Properties.SetValue(nameof(FileAdditionBehavior), (int)FileAdditionBehavior.AddDll);
            }
            else if(EmbedCodeFiles)
            {
                backingRfs.Properties.SetValue(nameof(FileAdditionBehavior), (int)FileAdditionBehavior.EmbedCodeFiles);
            }
            else
            {
                backingRfs.Properties.SetValue(nameof(FileAdditionBehavior), (int)FileAdditionBehavior.IncludeNoFiles);
            }
        }

    }
}
