using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.Animation
{
    enum PlayingMode
    {
        NotPlaying,
        Once,
        Loop,
        Duration,
        Forever
    }

    public class AnimationLayer
    {
        string cachedChainName;
        string lastPlayCallAnimation = null;

        int loopsLeft;
        double playDuration;
        double timeAnimationStarted;

        PlayingMode playingMode;

        public Func<string> EveryFrameAction;

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
            else
            {

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
                        if(Container.AnimatedObject.JustCycled)
                        {
                            loopsLeft--;
                            cachedChainName = lastPlayCallAnimation;

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
                        if (Container.AnimatedObject.JustCycled)
                        {
                            cachedChainName = null;
                            playingMode = PlayingMode.NotPlaying;
                        }

                        break;
                }
            }
        }
    }
}
