using FlatRedBall.Audio;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlatRedBall.Forms.Controls.Games
{
    public class SettingsView : FrameworkElement
    {
        Slider MusicVolumeSlider;

        Slider SoundVolumeSlider;

        CheckBox FullscreenCheckBox;

        public SoundEffect SoundEffectToPlayOnRelease { get; set; }

        public double MusicVolumePercentage 
        {
            get => MusicVolumeSlider.Value;
            set
            {
                if(value != MusicVolumeSlider.Value)
                {
                    MusicVolumeSlider.Value = value;
                    PushValueToViewModel();
                }
            }
        }

        public double SoundVolumePercentage 
        { 
            get => SoundVolumeSlider.Value;
            set
            {
                if( value != SoundVolumeSlider.Value)
                {
                    SoundVolumeSlider.Value = value;
                    PushValueToViewModel();
                }
            }
        }

        // Treat fullscreen as one word: https://english.stackexchange.com/questions/162421/fullscreen-or-full-screen
        public bool IsFullscreen
        {
            get => FullscreenCheckBox.IsChecked == true;
            set
            {
                if(FullscreenCheckBox.IsChecked != value )
                {
                    FullscreenCheckBox.IsChecked = value;
                    PushValueToViewModel();
                }
            }
        }

        // The Window's rectangle position when it was changed from windowed to fullscreen
        static Rectangle? windowedRectanglePosition;


        public event Action<bool> FullscreenSet;

        /// <summary>
        /// Whether changes on the UI are automatically applied to the underlying engine. Set this to false
        /// if changes are manually applied (such as through a ViewModel).
        /// </summary>
        public bool IsAutoApplyingChangesToEngine
        {
            get; set;
        } = true;


        public SettingsView() : base() 
        {
            Initialize();
        }

        public SettingsView(GraphicalUiElement visual) : base(visual)
        {
            Initialize();
        }

        void Initialize()
        {
            Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, EventArgs e)
        {
            if(Visual != null)
            { 
                var musicSliderVisual = Visual.GetGraphicalUiElementByName("MusicSliderInstance");
                MusicVolumeSlider = musicSliderVisual.FormsControlAsObject as Slider;
                MusicVolumeSlider.ValueChanged += HandleMusicValueChanged;
                MusicVolumeSlider.Maximum = 100;
                MusicVolumeSlider.Minimum = 0;

                var soundSliderVisual = Visual.GetGraphicalUiElementByName("SoundSliderInstance");
                SoundVolumeSlider = soundSliderVisual.FormsControlAsObject as Slider;
                SoundVolumeSlider.ValueChanged += HandleSoundValueChanged;
                SoundVolumeSlider.ValueChangeCompleted += HandleSoundValueChangedCompleted;
                SoundVolumeSlider.Maximum = 100;
                SoundVolumeSlider.Minimum = 0;

                var fullscreenCheckboxInstance = Visual.GetGraphicalUiElementByName("FullscreenCheckboxInstance");
                FullscreenCheckBox = fullscreenCheckboxInstance.FormsControlAsObject as CheckBox;
                FullscreenCheckBox.Checked += HandleFullscreenCheckboxChanged;
                FullscreenCheckBox.Unchecked += HandleFullscreenCheckboxChanged;

                FullscreenCheckBox.IsChecked = FlatRedBallServices.GraphicsOptions.IsFullScreen;
                SoundVolumePercentage = 100 * AudioManager.MasterSoundVolume;

#if !__IOS__
                MusicVolumePercentage = 100 * MediaPlayer.Volume;
#endif
            }
        }

        #region Event Handlers

        private void HandleMusicValueChanged(object sender, EventArgs e)
        {
            if(IsAutoApplyingChangesToEngine)
            {
#if !__IOS__
                // MediaPlayer.Volume is handled by AudioManager.MasterSongVolume
                //MediaPlayer.Volume = (float)MusicVolumePercentage / 100;
                AudioManager.MasterSongVolume = (float)MusicVolumePercentage / 100;
#endif
            }

            PushValueToViewModel(nameof(MusicVolumePercentage));
        }

        private void HandleSoundValueChanged(object sender, EventArgs e)
        {
            if (IsAutoApplyingChangesToEngine)
            {
                AudioManager.MasterSoundVolume = (float)SoundVolumePercentage / 100;
            }

            PushValueToViewModel(nameof(SoundVolumePercentage));
        }

        private void HandleSoundValueChangedCompleted(object sender, EventArgs e)
        {
            if(SoundEffectToPlayOnRelease != null)
            {
                AudioManager.Play(SoundEffectToPlayOnRelease);
            }
        }

        private void HandleFullscreenCheckboxChanged(object sender, EventArgs e)
        {
            if (IsAutoApplyingChangesToEngine)
            {
                if(IsFullscreen)
                {
                    windowedRectanglePosition = FlatRedBallServices.Game.Window.ClientBounds;
                }
                // toggling between fullscreen/windowed is done through the CameraSetup

                // CameraSetup should be moved into the engine, or at least CameraData and ResetWindow, but
                // unfortunately that would be difficult to do with codegen. Currently codegen writes everything 
                // in CameraSetupData, so would we make a new .gluj version? We can't do that because some games link
                // to the engine (they get all the latest engine changes) yet they still are on an old .gluj version. Therefore
                // we need to have a setting which can be changed on the setup object in Glue. But that's a lot of work and I don't
                // want to do that yet, so let's just call the event for now...
                FullscreenSet?.Invoke(IsFullscreen);

                if (!IsFullscreen && windowedRectanglePosition != null)
                {
#if !UWP && !__IOS__ && !ANDROID && !XNA4_OLD && !FNA
                    FlatRedBallServices.Game.Window.Position = new Point(windowedRectanglePosition.Value.X, windowedRectanglePosition.Value.Y);
#endif
                }
            }

            PushValueToViewModel(nameof(IsFullscreen));
        }



#endregion
    }
}
