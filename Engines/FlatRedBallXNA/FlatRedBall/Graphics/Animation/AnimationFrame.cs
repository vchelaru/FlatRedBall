using System;
using System.Collections.Generic;
using System.Text;

#if FRB_MDX
using FlatRedBall;
#else // FRB_XNA  || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif
using System.Xml;
using System.Xml.Serialization;
using FlatRedBall.Instructions;

namespace FlatRedBall.Graphics.Animation
{
    #region XML Docs
    /// <summary>
    /// Stores information about one frame in a texture-flipping animation.
    /// </summary>
    /// <remarks>
    /// Includes
    /// information about which Texture2D to show, whether the Texture2D should be flipped,
    /// the length of time to show the Texture2D for, texture coordinates (for sprite sheets), and
    /// relative positioning.
    /// </remarks>
    #endregion
    public class AnimationFrame :  IEquatable<AnimationFrame>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// Empty AnimationFrame.
        /// </summary>
        #endregion
        public static AnimationFrame Empty;
        #region XML Docs
        /// <summary>
        /// The texture that the AnimationFrame will show.
        /// </summary>
        #endregion
        [XmlIgnore]
        public Texture2D Texture;

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        #endregion
        public bool FlipHorizontal;

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        #endregion
        public bool FlipVertical;

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

        #region XML Docs
        /// <summary>
        /// The left coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// </summary>
        #endregion
        public float LeftCoordinate;

        #region XML Docs
        /// <summary>
        /// The right coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// </summary>
        #endregion
        public float RightCoordinate = 1;

        #region XML Docs
        /// <summary>
        /// The top coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// </summary>
        #endregion
        public float TopCoordinate;

        #region XML Docs
        /// <summary>
        /// The bottom coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// </summary>
        #endregion
        public float BottomCoordinate = 1;

        #region XML Docs
        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeX;

        #region XML Docs
        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeY;

        #endregion

        #region Properties

        public List<Instruction> Instructions
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame.
        /// </summary>
        #endregion
        public AnimationFrame() 
        {
            Instructions = new List<Instruction>();
        }

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame.
        /// </summary>
        /// <param name="texture">The Texture2D to use for this AnimationFrame.</param>
        /// <param name="frameLength">The amount of time in seconds that this AnimationFrame will display for when 
        /// it is used in an AnimationChain.</param>
        #endregion
        public AnimationFrame(Texture2D texture, float frameLength)
        {
            Texture = texture;
            FrameLength = frameLength;
            FlipHorizontal = false;
            FlipVertical = false;
            
            Instructions = new List<Instruction>();

            if (texture != null)
            {
                TextureName = texture.Name;
            }
        }

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame.
        /// </summary>
        /// <param name="textureName">The string name of the Texture2D to use for this AnimationFrame.
        /// This will be loaded through the content pipeline using the arugment contentManagerName.</param>
        /// <param name="frameLength">The amount of time in seconds that this AnimationFrame will display for when
        /// it is used in an AnimationChain.</param>
        /// <param name="contentManagerName">The content manager name to use when loading the Texture2D .</param>
        #endregion
        public AnimationFrame(string textureName, float frameLength, string contentManagerName)
        {
            Texture = FlatRedBallServices.Load<Texture2D>(textureName, contentManagerName);
            FrameLength = frameLength;
            FlipHorizontal = false;
            FlipVertical = false;

            Instructions = new List<Instruction>();

            TextureName = textureName;
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame with identical properties.  The new AnimationFrame
        /// will not belong to the AnimationChain that this AnimationFrameBelongs to unless manually
        /// added.
        /// </summary>
        /// <returns>The new AnimationFrame instance.</returns>
        #endregion
        public AnimationFrame Clone()
        {
            AnimationFrame animationFrame = this.MemberwiseClone() as AnimationFrame;
            return animationFrame;
        }

        #region XML Docs
        /// <summary>
        /// Returns a string representation of this.
        /// </summary>
        /// <returns>String representation of this.</returns>
        #endregion
        public override string ToString()
        {
            if (Texture != null)
                return Texture.Name.ToString();
            else
                return "<EMPTY>";
        }

        #endregion

        #endregion

        #region IEquatable<AnimationFrame> Members

        bool IEquatable<AnimationFrame>.Equals(AnimationFrame other)
        {
            return this == other;
        }

        #endregion
    }
}
