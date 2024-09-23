using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GumPlugin.ViewModels
{
    #region Enums

    public enum FileAdditionBehavior
    {
        EmbedCodeFiles = 0,
        // DLLs are now going to ship as part of the templates, so no need for the plugin to handle it
        //AddDll,
        IncludeNoFiles
    }

    #endregion

    // This got converted to a PropertyListContainerViewModel i nMarch 2021. Properties here could get updated 
    public class GumViewModel : PropertyListContainerViewModel
    {
        GumProjectSave backingGumProject;
        ReferencedFileSave backingRfs;
            
        [SyncedProperty]
        public bool UseAtlases
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool IsMatchGameResolutionInGumChecked
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool AutoCreateGumScreens
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }


        [SyncedProperty]
        public bool ShowMouse
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool MakeGumInstancesPublic
        {
            get => Get<bool>();
            set => SetAndPersist(value);
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
                    UpdateFileAdditionBehaviorOnRfs();
                    NotifyPropertyChanged(nameof(EmbedCodeFiles));
                }
            }
        }

        public Visibility GumCoreFileUiVisibility =>
            IsEmbedCodeFilesEnabled.ToVisibility();

        bool IsEmbedCodeFilesEnabled => GlueState.Self.CurrentGlueProject != null &&
            GlueState.Self.CurrentGlueProject.FileVersion < 
            // There is no version explicitly for when we embedded .dlls, but it should have been set by this point
            (int)GlueProjectSave.GluxVersions.GumGueHasGetAnimation;


        [SyncedProperty]
        public bool IncludeFormsInComponents
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool IncludeComponentToFormsAssociation
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        public bool IncludeNoFiles
        {
            get { return behavior == FileAdditionBehavior.IncludeNoFiles; }
            set
            {
                if (value && behavior != FileAdditionBehavior.IncludeNoFiles)
                {
                    behavior = FileAdditionBehavior.IncludeNoFiles;
                    UpdateFileAdditionBehaviorOnRfs();
                    NotifyPropertyChanged(nameof(IncludeNoFiles));
                }
            }
        }

        [SyncedProperty]
        public bool ShowDottedOutlines
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        public void SetFrom(GumProjectSave gumProjectSave, ReferencedFileSave referencedFileSave)
        {
            backingGumProject = gumProjectSave;
            backingRfs = referencedFileSave;

            GlueObject = referencedFileSave;


            UseAtlases = backingRfs.Properties.GetValue<bool>(nameof(UseAtlases));
            AutoCreateGumScreens = backingRfs.Properties.GetValue<bool>(nameof(AutoCreateGumScreens));
            ShowDottedOutlines = backingRfs.Properties.GetValue<bool>(nameof(ShowDottedOutlines));
            FileAdditionBehavior behavior = (FileAdditionBehavior) backingRfs.Properties.GetValue<int>(nameof(FileAdditionBehavior));

            EmbedCodeFiles = behavior == FileAdditionBehavior.EmbedCodeFiles;
            IncludeNoFiles = behavior == FileAdditionBehavior.IncludeNoFiles;
            IncludeFormsInComponents = backingRfs.Properties.GetValue<bool>(nameof(IncludeFormsInComponents));
            IncludeComponentToFormsAssociation = backingRfs.Properties.GetValue<bool>(nameof(IncludeComponentToFormsAssociation));

            ShowMouse = backingRfs.Properties.GetValue<bool>(nameof(ShowMouse));
            UpdateFromGlueObject();
        }


        private void UpdateFileAdditionBehaviorOnRfs()
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
