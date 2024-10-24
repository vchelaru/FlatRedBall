using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Animation;
using System.Xml.Serialization;
using FlatRedBall.Attributes;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.Math.Geometry;

namespace FlatRedBall.Content.AnimationChain
{
    [Serializable]
    public class AnimationFrameSave
    {
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        public bool FlipHorizontal;
        public bool ShouldSerializeFlipHorizontal()
        {
            return FlipHorizontal == true;
        }

        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        public bool FlipVertical;
        public bool ShouldSerializeFlipVertical()
        {
            return FlipVertical == true;
        }

        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        public string TextureName;

        /// <summary>
        /// The frame duration in seconds.
        /// </summary>
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

        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeX;
        public bool ShouldSerializeRelativeX()
        {
            return RelativeX != 0;
        }

        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeY;
        public bool ShouldSerializeRelativeY()
        {
            return RelativeY != 0;
        }

        public ShapeCollectionSave ShapeCollectionSave
        {
            get; set;
        }
        public bool ShouldSerializeShapeCollectionSave =>
            ShapeCollectionSave != null &&
            (
                ShapeCollectionSave.AxisAlignedRectangleSaves.Count > 0 ||
                ShapeCollectionSave.CircleSaves.Count > 0 ||
                ShapeCollectionSave.PolygonSaves.Count > 0 ||
                ShapeCollectionSave.AxisAlignedCubeSaves.Count > 0 ||
                ShapeCollectionSave.SphereSaves.Count > 0 
            );

        [XmlIgnore]
        [ExternalInstance]
        internal Texture2D mTextureInstance;


        public AnimationFrameSave() { }

        public AnimationFrameSave(AnimationFrame template)
        {
            FrameLength = template.FrameLength;
            TextureName = template.TextureName;
            FlipVertical = template.FlipVertical;
            FlipHorizontal = template.FlipHorizontal;

            LeftCoordinate = template.LeftCoordinate;
            RightCoordinate = template.RightCoordinate;
            TopCoordinate = template.TopCoordinate;
            BottomCoordinate = template.BottomCoordinate;

            ShapeCollectionSave = template.ShapeCollectionSave;

            RelativeX = template.RelativeX;
            RelativeY = template.RelativeY;

            TextureName = template.Texture.Name;
        }


        public AnimationFrame ToAnimationFrame(string contentManagerName)
        {
            return ToAnimationFrame(contentManagerName, true);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture)
        {

            return ToAnimationFrame(contentManagerName, loadTexture, TextureCoordinateType.UV);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture, TextureCoordinateType coordinateType)
        {
            AnimationFrame frame = new AnimationFrame();

            #region Set basic variables

            frame.TextureName = TextureName;
            frame.FrameLength = FrameLength;

            if (loadTexture)
            {
                if (mTextureInstance != null)
                {
                    frame.Texture = mTextureInstance;
                }
                // I think we should tolarte frames with a null Texture
                else if (!string.IsNullOrEmpty(TextureName))
                {
					frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
                    
                }
                //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
            }
            frame.FlipHorizontal = FlipHorizontal;
            frame.FlipVertical = FlipVertical;

            if (coordinateType == TextureCoordinateType.UV)
            {
                frame.LeftCoordinate = LeftCoordinate;
                frame.RightCoordinate = RightCoordinate;
                frame.TopCoordinate = TopCoordinate;
                frame.BottomCoordinate = BottomCoordinate;
            }
            else if (coordinateType == TextureCoordinateType.Pixel)
            {
                // April 16, 2015
                // Victor Chelaru
                // We used to throw this exception, but I don't know why we should, because
                // the Sprite won't show up, and the problem should be discoverable in tools
                // without a crash
                //if (frame.Texture == null)
                //{
                //    throw new Exception("The frame must have its texture loaded to use the Pixel coordinate type");
                //}

                if (frame.Texture != null)
                {
                    frame.LeftCoordinate = LeftCoordinate / frame.Texture.Width;
                    frame.RightCoordinate = RightCoordinate / frame.Texture.Width;

                    frame.TopCoordinate = TopCoordinate / frame.Texture.Height;
                    frame.BottomCoordinate = BottomCoordinate / frame.Texture.Height;
                }
            }
            
            
            frame.RelativeX = RelativeX;
            frame.RelativeY = RelativeY;

            frame.ShapeCollectionSave = ShapeCollectionSave;

            #endregion

            return frame;
        }


        internal static AnimationFrameSave FromXElement(System.Xml.Linq.XElement element)
        {
            AnimationFrameSave toReturn = new AnimationFrameSave();

                        
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "FlipHorizontal":
                        toReturn.FlipHorizontal = SceneSave.AsBool(subElement);
                        break;
                    case "FlipVertical":
                        toReturn.FlipVertical = SceneSave.AsBool(subElement);
                        break;
                    case "TextureName":
                        toReturn.TextureName = subElement.Value;
                        break;
                    case "FrameLength":
                        toReturn.FrameLength = SceneSave.AsFloat(subElement);
                        break;
                    case "LeftCoordinate":
                        toReturn.LeftCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "RightCoordinate":
                        toReturn.RightCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "TopCoordinate":
                        toReturn.TopCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "BottomCoordinate":
                        toReturn.BottomCoordinate = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeX":
                        toReturn.RelativeX = SceneSave.AsFloat(subElement);
                        break;
                    case "RelativeY":
                        toReturn.RelativeY = SceneSave.AsFloat(subElement);
                        break;
                }
            }

            return toReturn;
        }
    }
}
