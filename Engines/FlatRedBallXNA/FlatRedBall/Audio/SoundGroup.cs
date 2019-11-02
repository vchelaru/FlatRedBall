using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace FlatRedBall.Audio
{
    public class SoundGroup
    {
        #region  SoundPlayingCoefficients
        class SoundPlayingCoefficients
        {
            /// <summary>
            /// 0 to 1
            /// </summary>
            public float? MinVolume;

            /// <summary>
            /// 0 to 1
            /// </summary>
            public float? MaxVolume;

            public float? MinPitchShiftInOctaves;

            public float? MaxPitchShiftInOctaves;

            public float? GetVolume()
            {
                if(MinVolume != null && MaxVolume != null)
                {
                    return FlatRedBallServices.Random.Between(MinVolume.Value, MaxVolume.Value);
                }
                else
                {
                    return null;
                }
            }

            public float? GetPitchShift()
            {
                if(MinPitchShiftInOctaves != null && MaxPitchShiftInOctaves != null)
                {
                    return FlatRedBallServices.Random.Between(MinPitchShiftInOctaves.Value, MaxPitchShiftInOctaves.Value);
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion

        #region Properties

        private List<SoundEffect> sounds = new List<SoundEffect>();
        private List<SoundPlayingCoefficients> coefficients =
        new List<SoundPlayingCoefficients>();
        private SoundPlayingCoefficients defaultCoefficient;

        public int NumberOfSoundsCurrentlyPlaying
        {
            get
            {
                int toReturn = 0;
                for(int i = 0; i < sounds.Count; i++)
                {
                    if(AudioManager.IsSoundEffectPlaying(sounds[i]))
                    {
                        toReturn++;
                    }
                }
                return toReturn;
            }
        }

        public int? MaximumCurrentSoundsAllowed
        {
            get; set;
        }

        #endregion

        public void AddSound(SoundEffect soundEffect)
        {
            sounds.Add(soundEffect);
            coefficients.Add(null);
        }

        public void AddSoundVolume(SoundEffect soundEffect, float? minVolume, float? maxVolume)
        {
            sounds.Add(soundEffect);

            var newCoefficients = new SoundPlayingCoefficients();
            newCoefficients.MinVolume = minVolume;
            newCoefficients.MaxVolume = maxVolume;

            newCoefficients.MinPitchShiftInOctaves = null;
            newCoefficients.MaxPitchShiftInOctaves = null;

            coefficients.Add(newCoefficients);

        }

        public void AddSoundPitch(SoundEffect soundEffect, float? minPitchShiftInOctaves, float? maxPitchShiftInOctaves)
        {
            sounds.Add(soundEffect);

            var newCoefficients = new SoundPlayingCoefficients();
            newCoefficients.MinVolume = null;
            newCoefficients.MaxVolume = null;

            newCoefficients.MinPitchShiftInOctaves = minPitchShiftInOctaves;
            newCoefficients.MaxPitchShiftInOctaves = maxPitchShiftInOctaves;

            coefficients.Add(newCoefficients);
        }

        public void AddSoundVolumeAndPitch(SoundEffect soundEffect, float? minVolume, float? maxVolume, 
            float? minPitchShiftInOctaves, float? maxPitchShiftInOctaves)
        {
            sounds.Add(soundEffect);

            var newCoefficients = new SoundPlayingCoefficients();
            newCoefficients.MinVolume = minVolume;
            newCoefficients.MaxVolume = maxVolume;

            newCoefficients.MinPitchShiftInOctaves = minPitchShiftInOctaves;
            newCoefficients.MaxPitchShiftInOctaves = maxPitchShiftInOctaves;

            coefficients.Add(newCoefficients);
        }


        /// <summary>
        /// Sets the default volume range for the entire sound effect group. This default will only apply
        /// if the specific sound effect being played has no specified range.
        /// </summary>
        /// <param name="minVolume"></param>
        /// <param name="maxVolume"></param>
        public void SetDefaultVolumeRange(float minVolume, float maxVolume)
        {
            if(defaultCoefficient == null)
            {
                defaultCoefficient = new SoundPlayingCoefficients();
            }
            defaultCoefficient.MinVolume = minVolume;
            defaultCoefficient.MaxVolume = maxVolume;
        }

        public void SetDefaultPitchShift(float minPitchShiftInOctaves, float maxPitchShiftInOctaves)
        {
            if (defaultCoefficient == null)
            {
                defaultCoefficient = new SoundPlayingCoefficients();
            }

            if (minPitchShiftInOctaves < -1)
            {
                throw new ArgumentException($"{nameof(minPitchShiftInOctaves)} must be greater than or equal to -1");
            }
            if (minPitchShiftInOctaves > 1)
            {
                throw new ArgumentException($"{nameof(minPitchShiftInOctaves)} must be less than or equal to 1");
            }
            if (maxPitchShiftInOctaves < -1)
            {
                throw new ArgumentException($"{nameof(maxPitchShiftInOctaves)} must be greater than or equal to -1");
            }
            if (maxPitchShiftInOctaves > 1)
            {
                throw new ArgumentException($"{nameof(maxPitchShiftInOctaves)} must be less than or equal to 1");
            }

            defaultCoefficient.MinPitchShiftInOctaves = minPitchShiftInOctaves;
            defaultCoefficient.MaxPitchShiftInOctaves = maxPitchShiftInOctaves;
        }

        public void Play()
        {
            if(MaximumCurrentSoundsAllowed == null || NumberOfSoundsCurrentlyPlaying < MaximumCurrentSoundsAllowed)
            {
                var gameRandom = FlatRedBallServices.Random;
                var soundToPlay = gameRandom.In(sounds);
                var coefficient = gameRandom.In(coefficients) ??
                    defaultCoefficient;

                float volume = coefficient?.GetVolume() ?? 1.0f;
                float pitchShift = coefficient?.GetPitchShift() ?? 0.0f;
                AudioManager.Play(soundToPlay, volume, pitchShift, 0);
            }
        }



    }
}
