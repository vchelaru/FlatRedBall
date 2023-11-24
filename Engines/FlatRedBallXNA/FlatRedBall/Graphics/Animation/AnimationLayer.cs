using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.Animation
{
    #region Enums

    enum PlayingMode
    {
        NotPlaying,
        Once,
        Loop,
        Duration,
        Forever
    }
    #endregion

    public class AnimationLayer : INameable
    {
        string cachedChainName;
        string lastPlayCallAnimation = null;

        int loopsLeft;
        double playDuration;
        double timeAnimationStarted;

        PlayingMode playingMode;

        /// <summary>
        /// Function which returns which animation should be displayed by this layer. If 
        /// this layer should not be active, then this should return null.
        /// </summary>
        /// <remarks>
        /// Typically this will have if-statements checking for certain conditions
        /// such as whether the player is moving a certain speed, is on the ground, or
        /// is facing left/right.
        /// </remarks>
        public Func<string> EveryFrameAction;
        public Func<List<string>> EveryFrameActionSequence;

        public Action OnAnimationFinished;

        /// <summary>
        /// The name of the layer. This is not the name of the animation returned, but rather
        /// a way to identify the layer in code an debugging.
        /// </summary>
        public string Name { get; set; }

        internal AnimationController Container { get; set; }

        public bool HasPriority
        {
            get
            {
                return this.Container.HasPriority(this);
            }
        }

        public string CurrentChainName
        {
            get { return cachedChainName; }
        }

        public void PlayOnce(string animationName)
        {
            playingMode = PlayingMode.Once;
            lastPlayCallAnimation = animationName;

        }

        public void  PlayLoop(string animationName, int numberOfLoops)
        {
            playingMode = PlayingMode.Loop;
            this.loopsLeft = numberOfLoops;
            lastPlayCallAnimation = animationName;
        }

        public void PlayDuration(string animationName, double durationInSeconds)
        {
            playingMode = PlayingMode.Duration;
            playDuration = durationInSeconds;
            lastPlayCallAnimation = animationName;
            timeAnimationStarted = Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;
        }

        public void Play(string animationName)
        {
            playingMode = PlayingMode.Forever;
            lastPlayCallAnimation = animationName;
        }

        public void StopPlay()
        {
            playingMode = PlayingMode.NotPlaying;
            lastPlayCallAnimation = null;
        }

        internal void Activity()
        {
            cachedChainName = null;

            if (EveryFrameAction != null)
            {
                cachedChainName = EveryFrameAction();
            }
            else if(EveryFrameActionSequence != null)
            {
                var list = EveryFrameActionSequence();

                if(list?.Count > 0)
                {
                    // start the sequence, just in case there are no other animations...

                    cachedChainName = list[0];
                    var animatable = Container?.AnimatedObject;

                    if(animatable != null)
                    {

                        var indexToShow = 0;

                        int? matchingIndex = 0;
                        for(int i = 0; i < list.Count; i++)
                        {
                            if (animatable.IsPlayingAnimation(list[i]))
                            {
                                matchingIndex = i;
                                if(animatable.DidAnimationFinishOrLoop)
                                {
                                    var nextIndex = i < list.Count-1 
                                        ? i + 1
                                        : i; // don't loop the sequence. Maybe we'll add that option later.
                                    cachedChainName = list[nextIndex];
                                }
                                else
                                {
                                    cachedChainName = list[i];
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    cachedChainName = null;
                }
            }
            else
            {
                var playingModeThisFrame = playingMode;
                switch(playingMode)
                {
                    case PlayingMode.Duration:
                        cachedChainName = lastPlayCallAnimation;
                        if (Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(timeAnimationStarted) >= playDuration)
                        {
                            cachedChainName = null;
                            playingMode = PlayingMode.NotPlaying;
                        }
                        break;
                    case PlayingMode.Forever:
                        // never ends until they call stop
                        cachedChainName = lastPlayCallAnimation;
                        break;
                    case PlayingMode.Loop:
                        cachedChainName = lastPlayCallAnimation;
                        
                        if(Container.AnimatedObject.DidAnimationFinishOrLoop)
                        {
                            loopsLeft--;

                            if (loopsLeft <= 0)
                            {
                                cachedChainName = null;
                                playingMode = PlayingMode.NotPlaying;
                            }
                        }
                        break;
                    case PlayingMode.NotPlaying:
                        // do nothing
                        break;
                    case PlayingMode.Once:
                        cachedChainName = lastPlayCallAnimation;
                        if (Container.AnimatedObject.DidAnimationFinishOrLoop)
                        {
                            cachedChainName = null;
                            playingMode = PlayingMode.NotPlaying;
                        }

                        break;
                }

                if(playingModeThisFrame != PlayingMode.NotPlaying && OnAnimationFinished != null && string.IsNullOrEmpty(cachedChainName))
                {
                    OnAnimationFinished();
                }
            }
        }
    }
}
