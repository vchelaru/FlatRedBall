using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Content.AnimationChain
{

    /// <summary>
    /// The base class for AnimationFrameSave and AnimationFrameSaveContent.
    /// </summary>
#if !UWP && !WINDOWS_8
    [Serializable]
#endif
    public class AnimationFrameSaveBase
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        #endregion
        public bool FlipHorizontal;
		public bool ShouldSerializeFlipHorizontal()
		{
			return FlipHorizontal == true;
		}

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        #endregion
        public bool FlipVertical;
		public bool ShouldSerializeFlipVertical()
		{
			return FlipVertical == true;
		}

        #region XML Docs
        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        #endregion
        public string TextureName;

        #region XML Docs
        /// <summary>
        /// The amount of time in seconds the AnimationFrame should be shown for.
        /// </summary>
        #endregion
        public float FrameLength;

        /// <summary>
        /// The left coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float LeftCoordinate;

        /// <summary>
        /// The right coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float RightCoordinate = 1;

        /// <summary>
        /// The top coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float TopCoordinate;

        /// <summary>
        /// The bottom coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float BottomCoordinate = 1;

        #region XML Docs
        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeX;
		public bool ShouldSerializeRelativeX()
		{
			return RelativeX != 0;
		}

        #region XML Docs
        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
		public float RelativeY;
		public bool ShouldSerializeRelativeY()
		{
			return RelativeY != 0;
		}

        #endregion
    }
}
