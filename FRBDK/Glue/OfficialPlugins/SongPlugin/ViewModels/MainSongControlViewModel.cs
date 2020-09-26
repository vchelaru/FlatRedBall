using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;
using GlueSaveClasses.Models.TypeConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.SongPlugin.ViewModels
{
    public class MainSongControlViewModel : PropertyListContainerViewModel
    {
        [SyncedProperty(OverridingPropertyName = "LoadedOnlyWhenReferenced", 
            ConverterType = typeof(InverterConverter))]
        public bool ShouldPlay
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [DependsOn("ShouldPlay")]
        public bool IsEnabled => ShouldPlay;

        [SyncedProperty]
        public bool ShouldLoopSong
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public int Volume
        {
            get => Get<int>();
            set => SetAndPersist(value);
        }

        [SyncedProperty(OverridingPropertyName = "DestroyOnUnload", 
            ConverterType = typeof(InverterConverter))]
        public bool ShouldKeepPlayingAfterLeavingScreen
        {
            get => Get<bool>();
            set => SetAndPersist(value);
        }

        [SyncedProperty]
        public bool IsSetVolumeChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsSetVolumeChecked))]
        public Visibility VolumeSliderVisibility => IsSetVolumeChecked
            ? Visibility.Visible
            : Visibility.Collapsed;

        public override void UpdateFromGlueObject()
        {
            base.UpdateFromGlueObject();

            if(GlueObject != null && ShouldPlay)
            {
                // See if the volume is null. If so, force it to 100
                var property = GlueObject.Properties.FirstOrDefault(item => item.Name == nameof(Volume));

                if(string.IsNullOrEmpty(property?.Name))
                {
                    // setting will force persist it
                    Volume = 100;
                }
            }
        }
    }
}
