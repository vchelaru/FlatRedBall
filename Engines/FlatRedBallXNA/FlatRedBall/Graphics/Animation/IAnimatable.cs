using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.Animation
{
    public interface IAnimatable
    {
        //bool IsAnimating { get; set; }

        void PlayAnimation(string animationName);

        bool HasAnimation(string animationName);

        bool IsPlayingAnimation(string animationName);

        bool DidAnimationFinishOrLoop { get; }
    }
}
