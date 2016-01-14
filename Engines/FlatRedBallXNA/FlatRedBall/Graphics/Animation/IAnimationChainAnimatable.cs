using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Graphics.Animation
{
    #region XML Docs
    /// <summary>
    /// Represents an object that can use AnimationChains for texture-flipping animation.
    /// </summary>
    #endregion
    public interface IAnimationChainAnimatable
    {
        #region Properties

        #region XML Docs
        /// <summary>
        /// Whether animation is currently turned on.
        /// </summary>
        #endregion
        bool Animate
        {
            get;
        }

        #region XML Docs
        /// <summary>
        /// Gets all animations stored in this.
        /// </summary>
        #endregion
        AnimationChainList AnimationChains
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets how fast AnimationChains will animate.  Default is 1.  A value
        /// of 2 will result in AnimationChains animating twice as fast.
        /// </summary>
        #endregion
        float AnimationSpeed
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the index of the current AnimationChain.
        /// </summary>
        #endregion
        int CurrentChainIndex
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Gets the current AnimationChain.
        /// </summary>
        #endregion
        AnimationChain CurrentChain
        {
            get;
        }

        #region XML Docs
        /// <summary>
        /// Gets the name of the current AnimationChain or sets the current AnimationChain by name.
        /// </summary>
        #endregion
        string CurrentChainName
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the current AnimationFrame index.
        /// </summary>
        #endregion
        int CurrentFrameIndex
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Gets whether the current AnimationFrame just changed this frame due to animation.
        /// </summary>
        #endregion
        bool JustChangedFrame
        {
            get;
        }

        #region XML Docs
        /// <summary>
        /// Gets whether the current AnimationChain just cycled (looped) this frame due to animation.
        /// </summary>
        #endregion
        bool JustCycled
        {
            get;
        }

        #region XML Docs
        /// <summary>
        /// Whether the current AnimationFrame's relative position values (RelativeX and RelativeY) are applied
        /// when animating.
        /// </summary>
        #endregion
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
