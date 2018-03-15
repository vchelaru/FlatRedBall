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
        // DLLs are now going to ship as part of the templates, so no need for the plugin to handle it
        //AddDll,
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

                if (value && behavior != FileAdditionBehavior.EmbedCodeFiles)
                {
                    behavior = FileAdditionBehavior.EmbedCodeFiles;
                    UpdateBehaviorOnRfs();
                    NotifyPropertyChanged(nameof(EmbedCodeFiles));
                }
            }
        }
        
        public bool IncludeFormsInComponents
        {
            get { return Get<bool>(); }
            set
            {
                backingRfs.Properties.SetValue(
                    nameof(IncludeFormsInComponents), value);

                Set(value);
            }
        }

        public bool IncludeComponentToFormsAssociation
        {
            get { return Get<bool>(); }
            set
            {
                backingRfs.Properties.SetValue(
                    nameof(IncludeComponentToFormsAssociation), value);
                Set(value);
            }
        }

        public bool IncludeNoFiles
        {
            get { return behavior == FileAdditionBehavior.IncludeNoFiles; }
            set
            {
                if (value && behavior != FileAdditionBehavior.IncludeNoFiles)
                {
                    behavior = FileAdditionBehavior.IncludeNoFiles;
                    UpdateBehaviorOnRfs();
                    NotifyPropertyChanged(nameof(IncludeNoFiles));
                }
            }
        }

        public bool ShowDottedOutlines
        {
            get
            {
                return Get<bool>();
            }
            set
            {
                backingRfs.Properties.SetValue(
                    nameof(ShowDottedOutlines), value);

                Set(value);
            }
        }


        public void SetFrom(GumProjectSave gumProjectSave, ReferencedFileSave referencedFileSave)
        {
                backingGumProject = gumProjectSave;
                backingRfs = referencedFileSave;

                UseAtlases = backingRfs.Properties.GetValue<bool>(nameof(UseAtlases));
                AutoCreateGumScreens = backingRfs.Properties.GetValue<bool>(nameof(AutoCreateGumScreens));
                ShowDottedOutlines = backingRfs.Properties.GetValue<bool>(nameof(ShowDottedOutlines));
                FileAdditionBehavior behavior = (FileAdditionBehavior) backingRfs.Properties.GetValue<int>(nameof(FileAdditionBehavior));

                EmbedCodeFiles = behavior == FileAdditionBehavior.EmbedCodeFiles;
                IncludeNoFiles = behavior == FileAdditionBehavior.IncludeNoFiles;
                IncludeFormsInComponents = backingRfs.Properties.GetValue<bool>(nameof(IncludeFormsInComponents));
                IncludeComponentToFormsAssociation = backingRfs.Properties.GetValue<bool>(nameof(IncludeComponentToFormsAssociation));
        }


        private void UpdateBehaviorOnRfs()
        {
            if(EmbedCodeFiles)
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
