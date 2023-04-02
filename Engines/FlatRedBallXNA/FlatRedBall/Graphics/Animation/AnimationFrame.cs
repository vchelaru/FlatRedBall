using System;
using System.Collections.Generic;
using System.Text;


using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using System.Xml;
using System.Xml.Serialization;
using FlatRedBall.Instructions;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.IO;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// Stores information about one frame in a texture-flipping animation.
    /// </summary>
    /// <remarks>
    /// Includes
    /// information about which Texture2D to show, whether the Texture2D should be flipped,
    /// the length of time to show the Texture2D for, texture coordinates (for sprite sheets), and
    /// relative positioning.
    /// </remarks>
    public class AnimationFrame :  IEquatable<AnimationFrame>
    {
        #region Fields

        /// <summary>
        /// Empty AnimationFrame.
        /// </summary>
        public static AnimationFrame Empty;

        /// <summary>
        /// The texture that the AnimationFrame will show.
        /// </summary>
        [XmlIgnore]
        public Texture2D Texture;

        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        public bool FlipHorizontal;

        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        public bool FlipVertical;

        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        public string TextureName;

        /// <summary>
        /// The amount of time in seconds the AnimationFrame should be shown for.
        /// </summary>
        public float FrameLength;

        /// <summary>
        /// The left coordinate in texture coordinates of the AnimationFrame.  Default is 0. 
        /// This value is in texture coordinates, not pixels. A value of 1 represents the right-side
        /// of the texture.
        /// </summary>
        public float LeftCoordinate;

        /// <summary>
        /// The right coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the right-side
        /// of the texture.
        /// </summary>
        public float RightCoordinate = 1;

        /// <summary>
        /// The top coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom
        /// of the texture;
        /// </summary>
        public float TopCoordinate;

        /// <summary>
        /// The bottom coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom
        /// of the texture;
        /// </summary>
        public float BottomCoordinate = 1;

        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeX;

        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeY;


        /// <summary>
        /// Shapes associated with this animation. This may be null, or it may contain any number of shapes which can be used
        /// for collision.
        /// </summary>
        public ShapeCollectionSave ShapeCollectionSave;

        #endregion

        #region Properties

        public List<Instruction> Instructions
        {
            get;
            private set;
        }

        public Vector3 RelativePosition => new Vector3(RelativeX, RelativeY, 0);

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

        /// <summary>
        /// Creates a new AnimationFrame with identical properties.  The new AnimationFrame
        /// will not belong to the AnimationChain that this AnimationFrameBelongs to unless manually
        /// added.
        /// </summary>
        /// <returns>The new AnimationFrame instance.</returns>
        public AnimationFrame Clone()
        {
            var newAnimationFrame = this.MemberwiseClone() as AnimationFrame;

            if(ShapeCollectionSave != null)
            {
                newAnimationFrame.ShapeCollectionSave = FileManager.CloneObject(this.ShapeCollectionSave);
            }

            return newAnimationFrame;
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
