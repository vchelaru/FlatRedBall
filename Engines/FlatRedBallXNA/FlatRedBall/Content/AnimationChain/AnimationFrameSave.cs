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
    /// <summary>
    /// Per-frame color operation as stored in the .achx file format. This intentionally mirrors the
    /// values produced by the FlatRedBall animation editor (Multiply, Add) rather than the engine's
    /// richer FlatRedBall.Graphics.ColorOperation enum. It is mapped to a runtime ColorOperation
    /// (Multiply -> Modulate, Add -> Add) when converted to an AnimationFrame.
    /// </summary>
    public enum AnimationFrameColorOperation
    {
        Multiply,
        Add
    }

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
        /// Whether the texture should be flipped diagonally (mirrored about the line running from
        /// the top-left to the bottom-right). Combines with FlipHorizontal/FlipVertical to produce
        /// all 8 orientations of a square.
        /// </summary>
        public bool FlipDiagonal;
        public bool ShouldSerializeFlipDiagonal()
        {
            return FlipDiagonal == true;
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

        #region Color (optional, per-frame tint - added for .achx color support)

        /// <summary>
        /// Optional per-frame red tint channel, 0 - 255. Null means "unset" and is omitted from the .achx file.
        /// Converted to the 0 - 1 range when applied to a Sprite.
        /// </summary>
        public int? Red;
        public bool ShouldSerializeRed() => Red.HasValue;

        /// <summary>
        /// Optional per-frame green tint channel, 0 - 255. Null means "unset" and is omitted from the .achx file.
        /// </summary>
        public int? Green;
        public bool ShouldSerializeGreen() => Green.HasValue;

        /// <summary>
        /// Optional per-frame blue tint channel, 0 - 255. Null means "unset" and is omitted from the .achx file.
        /// </summary>
        public int? Blue;
        public bool ShouldSerializeBlue() => Blue.HasValue;

        /// <summary>
        /// Optional per-frame alpha (opacity) channel, 0 - 255. Null means "unset" and is omitted from the .achx file.
        /// Alpha is independent of ColorOperation.
        /// </summary>
        public int? Alpha;
        public bool ShouldSerializeAlpha() => Alpha.HasValue;

        /// <summary>
        /// Optional per-frame color operation. Null means "unset" (no tint operation) and is omitted from the .achx file.
        /// </summary>
        public AnimationFrameColorOperation? ColorOperation;
        public bool ShouldSerializeColorOperation() => ColorOperation.HasValue;

        #endregion

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
            FlipDiagonal = template.FlipDiagonal;

            LeftCoordinate = template.LeftCoordinate;
            RightCoordinate = template.RightCoordinate;
            TopCoordinate = template.TopCoordinate;
            BottomCoordinate = template.BottomCoordinate;

            ShapeCollectionSave = template.ShapeCollectionSave;

            RelativeX = template.RelativeX;
            RelativeY = template.RelativeY;

            Red = ToColorByte(template.Red);
            Green = ToColorByte(template.Green);
            Blue = ToColorByte(template.Blue);
            Alpha = ToColorByte(template.Alpha);
            ColorOperation = ToSaveColorOperation(template.ColorOperation);

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
            frame.FlipDiagonal = FlipDiagonal;

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

            frame.Red = ToColorFloat(Red);
            frame.Green = ToColorFloat(Green);
            frame.Blue = ToColorFloat(Blue);
            frame.Alpha = ToColorFloat(Alpha);
            frame.ColorOperation = ToRuntimeColorOperation(ColorOperation);

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
                    case "FlipDiagonal":
                        toReturn.FlipDiagonal = SceneSave.AsBool(subElement);
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
                    case "Red":
                        toReturn.Red = SceneSave.AsInt(subElement);
                        break;
                    case "Green":
                        toReturn.Green = SceneSave.AsInt(subElement);
                        break;
                    case "Blue":
                        toReturn.Blue = SceneSave.AsInt(subElement);
                        break;
                    case "Alpha":
                        toReturn.Alpha = SceneSave.AsInt(subElement);
                        break;
                    case "ColorOperation":
                        if (Enum.TryParse<AnimationFrameColorOperation>(subElement.Value, out var parsedColorOperation))
                        {
                            toReturn.ColorOperation = parsedColorOperation;
                        }
                        break;
                }
            }

            return toReturn;
        }

        private static float? ToColorFloat(int? colorByte) =>
            colorByte.HasValue ? colorByte.Value / 255f : (float?)null;

        private static int? ToColorByte(float? colorFloat)
        {
            if (!colorFloat.HasValue)
            {
                return null;
            }

            int asByte = (int)System.Math.Round(colorFloat.Value * 255f);
            return System.Math.Max(0, System.Math.Min(255, asByte));
        }

        private static FlatRedBall.Graphics.ColorOperation? ToRuntimeColorOperation(AnimationFrameColorOperation? colorOperation)
        {
            switch (colorOperation)
            {
                case AnimationFrameColorOperation.Multiply:
                    return FlatRedBall.Graphics.ColorOperation.Modulate;
                case AnimationFrameColorOperation.Add:
                    return FlatRedBall.Graphics.ColorOperation.Add;
                default:
                    return null;
            }
        }

        private static AnimationFrameColorOperation? ToSaveColorOperation(FlatRedBall.Graphics.ColorOperation? colorOperation)
        {
            switch (colorOperation)
            {
                case FlatRedBall.Graphics.ColorOperation.Modulate:
                    return AnimationFrameColorOperation.Multiply;
                case FlatRedBall.Graphics.ColorOperation.Add:
                    return AnimationFrameColorOperation.Add;
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            return $"{TextureName} {LeftCoordinate} {TopCoordinate} {RightCoordinate} {BottomCoordinate}";
        }
    }
}
