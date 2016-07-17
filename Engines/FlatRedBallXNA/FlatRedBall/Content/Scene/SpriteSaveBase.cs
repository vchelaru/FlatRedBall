using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Content.Scene
{
    public class SpriteSaveBase
    {

        #region Fields
        [DefaultValue(0.0f)]
        public float X;
        [DefaultValue(0.0f)]
        public float Y;
        [DefaultValue(0.0f)]
        public float Z;

        [DefaultValue(0.0f)]
        public float XVelocity;
        [DefaultValue(0.0f)]
        public float YVelocity;
        [DefaultValue(0.0f)]
        public float ZVelocity;

        [DefaultValue(0.0f)]
        public float XAcceleration;
        [DefaultValue(0.0f)]
        public float YAcceleration;
        [DefaultValue(0.0f)]
        public float ZAcceleration;

        [DefaultValue(0.0f)]
        public float RotationX;
        [DefaultValue(0.0f)]
        public float RotationY;
        [DefaultValue(0.0f)]
        public float RotationZ;

        [DefaultValue(0.0f)]
        public float RotationZVelocity;

        [DefaultValue(1.0f)]
        public float ScaleX;
        [DefaultValue(1.0f)]
        public float ScaleY;


        [DefaultValue(0.0f)]
        public float ScaleXVelocity;
        [DefaultValue(0.0f)]
        public float ScaleYVelocity;

        [DefaultValue(0.0f)]
        public float RelativeX;
        [DefaultValue(0.0f)]
        public float RelativeY;
        [DefaultValue(0.0f)]
        public float RelativeZ;

        [DefaultValue(0.0f)]
        public float RelativeRotationX;
        [DefaultValue(0.0f)]
        public float RelativeRotationY;
        [DefaultValue(0.0f)]
        public float RelativeRotationZ;

        [DefaultValue(0.0f)]
        public float Fade;
        [DefaultValue(0.0f)]
        public float FadeRate;

        [DefaultValue(0.0f)]
        public float TintRed;
        [DefaultValue(0.0f)]
        public float TintGreen;
        [DefaultValue(0.0f)]
        public float TintBlue;

        [DefaultValue(0.0f)]
        public float TintRedRate;
        [DefaultValue(0.0f)]
        public float TintBlueRate;
        [DefaultValue(0.0f)]
        public float TintGreenRate;

        // ColorOperation and BlendOperation have to be strings to keep
        // compatability with MDX FRB.
        [DefaultValue("SelectArg1")]
        public string ColorOperation = "SelectArg1";
        [DefaultValue("REGULAR")]
        public string BlendOperation = "REGULAR"; // This is the default value - needed in case the .scnx doesn't include this property

        [DefaultValue(null)]
        public string Name;
        [DefaultValue(null)]
        public string Parent;
        [DefaultValue(null)]
        public string Texture;

        [DefaultValue(false)]
        public bool Animate;
        [DefaultValue(-1)]
        public int CurrentChain = -1;
        //        public AnimationChainList AnimationChains;
        [DefaultValue(null)]
        public string AnimationChainsFile;

        [DefaultValue(null)]
        public string Type;
        [DefaultValue(true)]
        public bool Ordered = true;

        // collisions not saved per Sprite in XNA version of FRB

        [DefaultValue(true)]
        public bool Active;
        [DefaultValue(0.0f)]
        public float ConstantPixelSize;
        [DefaultValue(true)]
        public bool Visible;

        // add info for texture address

        [DefaultValue(0.0f)]
        public float TopTextureCoordinate = 0;
        [DefaultValue(1.0f)]
        public float BottomTextureCoordinate = 1;
        [DefaultValue(0.0f)]
        public float LeftTextureCoordinate = 0;
        [DefaultValue(1.0f)]
        public float RightTextureCoordinate = 1;

        [DefaultValue(false)]
        public bool FlipHorizontal = false;
        [DefaultValue(false)]
        public bool FlipVertical = false;

        // FRB MDX has more texture address modes than XNA, so we need to account for that.
        // We'll do that by having a string that gets serialized/deserialized:
        [XmlIgnore]
        [DefaultValue(TextureAddressMode.Clamp)]
        public TextureAddressMode TextureAddressMode = TextureAddressMode.Clamp;

        [XmlAttribute("TextureAddressMode")]
        [DefaultValue("Clamp")]
        public string TextureAddressModeAsString
        {
            get { return TextureAddressMode.ToString(); }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    TextureAddressMode = TextureAddressMode.Clamp;
                }
                else
                {
                    try
                    {
                        TextureAddressMode = (TextureAddressMode)Enum.Parse(typeof(TextureAddressMode), value, true);
                    }
                    catch
                    {
                        TextureAddressMode = TextureAddressMode.Clamp;
                    }
                }
            }
        }

        #endregion


        public SpriteSaveBase()
        {
            ScaleX = 1;
            ScaleY = 1;
            Name = "";
            Visible = true;

            this.BlendOperation = "REGULAR";
            this.ColorOperation = "SelectArg1";

            this.Ordered = true;
            this.Active = true;
        }
    }
}
