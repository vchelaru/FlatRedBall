using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// Represents an object that can use AnimationChains for texture-flipping animation.
    /// </summary>
    public interface IAnimationChainAnimatable
    {
        #region Properties

        /// <summary>
        /// Whether animation is currently turned on.
        /// </summary>
        bool Animate
        {
            get;
        }

        /// <summary>
        /// Gets all animations stored in this.
        /// </summary>
        AnimationChainList AnimationChains
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets how fast AnimationChains will animate.  Default is 1.  A value
        /// of 2 will result in AnimationChains animating twice as fast.
        /// </summary>
        float AnimationSpeed
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the index of the current AnimationChain.
        /// </summary>
        int CurrentChainIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the current AnimationChain.
        /// </summary>
        AnimationChain CurrentChain
        {
            get;
        }

        /// <summary>
        /// Gets the name of the current AnimationChain or sets the current AnimationChain by name.
        /// </summary>
        string CurrentChainName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the current AnimationFrame index.
        /// </summary>
        int CurrentFrameIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets whether the current AnimationFrame just changed this frame due to animation.
        /// </summary>
        bool JustChangedFrame
        {
            get;
        }

        /// <summary>
        /// Gets whether the current AnimationChain just cycled (looped) this frame due to animation.
        /// </summary>
        bool JustCycled
        {
            get;
        }

        /// <summary>
        /// Whether the current AnimationFrame's relative position values (RelativeX and RelativeY) are applied
        /// when animating.
        /// </summary>
        bool UseAnimationRelativePosition { get; set;}


        #endregion
    }

    public static class IAnimationChainAnimatableExtensions
    {
        public static bool ContainsChainName(this IAnimationChainAnimatable animatable, string chainName)
        {
            if (animatable == null)
                throw new ArgumentNullException("animatable");

            foreach (var chain in animatable.AnimationChains)
                if (chain.Name == chainName)
                    return true;

            return false;
        }
    }
}
