using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;
#if FRB_MDX
using TextureAddressMode = Microsoft.DirectX.Direct3D.TextureAddress;
using Microsoft.DirectX.Direct3D;
#else
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endif

namespace FlatRedBall.Graphics
{
    public struct RenderBreak
    {
        #region Fields

        public int ItemNumber;

        Texture2D mTexture;
        public PrimitiveType PrimitiveType;

        public string LayerName;

#if DEBUG
        public object ObjectCausingBreak;
        public string Details
        {
            get
            {
                if (ObjectCausingBreak != null)
                {
                    string toReturn = ObjectCausingBreak.ToString();

                    if(ObjectCausingBreak is PositionedObject)
                    {
                        var parent = (ObjectCausingBreak as PositionedObject).Parent;
                        if (parent != null)
                        {
                            toReturn += "\nParent: " + parent.ToString();
                        }
                    }

                    if(string.IsNullOrEmpty(toReturn))
                    {
                        toReturn = ObjectCausingBreak.GetType().FullName;
                    }

                    return toReturn;
                }
                else
                {
                    return "Unknown object";
                }
            }

        }

#endif

#if WINDOWS_PHONE || MONOGAME
        public float Red;
        public float Green;
        public float Blue;
#endif

#if FRB_MDX
        TextureOperation ColorOperation;
#else
        public ColorOperation ColorOperation;

#endif
        public BlendOperation BlendOperation;

        public TextureFilter TextureFilter;


        public Texture2D Texture
        {
            get { return mTexture; }
        }

        public TextureAddressMode TextureAddressMode;
        private static TextureFilter _originalTextureFilter;



        #endregion

        #region Methods

        #region Constructors

        public RenderBreak(int itemNumber, Sprite sprite)
        {
#if DEBUG
            ObjectCausingBreak = sprite;
#endif
            LayerName = Renderer.CurrentLayerName;
            ItemNumber = itemNumber;
            PrimitiveType = PrimitiveType.TriangleList;
            _originalTextureFilter = TextureFilter.Linear;

            if (sprite != null)
            {
                if (sprite.Texture != null && sprite.Texture.IsDisposed)
                {
                    throw new ObjectDisposedException("The Sprite with the name \"" + sprite.Name + 
                        "\" references a disposed texture of the name " + sprite.Texture.Name + 
                        ".  If you're using Screens you may have forgotten to remove a Sprite that was " +
                        "added in the Screen.");
                }

                mTexture = sprite.Texture;

                ColorOperation = sprite.ColorOperation;
                BlendOperation = sprite.BlendOperation;
                TextureFilter = sprite.TextureFilter.HasValue ? sprite.TextureFilter.Value : FlatRedBallServices.GraphicsOptions.TextureFilter;

#if FRB_XNA
                if (sprite.Texture == null)
                {

                    // requirement for reach profile - this shouldn't impact anything
                    TextureAddressMode = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp;
                }
                else
#endif
                {
                    TextureAddressMode = sprite.TextureAddressMode;
                }


#if WINDOWS_PHONE || MONOGAME

                Red = sprite.Red;
                Green = sprite.Green;
                Blue = sprite.Blue;
#endif

            }
            else
            {
#if WINDOWS_PHONE || MONOGAME
                Red = 0;
                Green = 0;
                Blue = 0;
#endif

                mTexture = null;
#if FRB_MDX
                ColorOperation = TextureOperation.SelectArg1;
#else
                ColorOperation = ColorOperation.None;
#endif
                BlendOperation = BlendOperation.Regular;
                TextureAddressMode = TextureAddressMode.Clamp;
                TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
            }
        }


        public RenderBreak(int itemNumber, Text text, int textureIndex)
        {
#if DEBUG
            ObjectCausingBreak = text;
#endif
            LayerName = Renderer.CurrentLayerName ;


#if WINDOWS_PHONE || MONOGAME

            if (text.ColorOperation != Graphics.ColorOperation.Texture)
            {
                Red = text.Red;
                Green = text.Green;
                Blue = text.Blue;
            }
            else
            {
                Red = 1;
                Green = 1;
                Blue = 1;
            }
#endif

            ItemNumber = itemNumber;

            PrimitiveType = PrimitiveType.TriangleList;
            TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
            _originalTextureFilter = TextureFilter.Linear;

            if (text != null)
            {
                if (text.Font.Texture != null && text.Font.Texture.IsDisposed)
                {
                    throw new ObjectDisposedException("Cannot create render break with disposed Texture2D");
                }

                mTexture = text.Font.Textures[textureIndex];

                ColorOperation = text.ColorOperation;
                BlendOperation = text.BlendOperation;
                TextureAddressMode = TextureAddressMode.Clamp;
            }
            else
            {
                mTexture = null;
                ColorOperation = ColorOperation.None;
                BlendOperation = BlendOperation.Regular;
                TextureAddressMode = TextureAddressMode.Clamp;
            }
        }

        public RenderBreak(int itemNumber, Texture2D texture,
            ColorOperation colorOperation, 
            BlendOperation blendOperation, TextureAddressMode textureAddressMode)
        {
#if DEBUG
            ObjectCausingBreak = null;
#endif
            LayerName = Renderer.CurrentLayerName;


            PrimitiveType = PrimitiveType.TriangleList;
            ItemNumber = itemNumber;

            if (texture != null && texture.IsDisposed)
            {
                throw new ObjectDisposedException("Cannot create render break with disposed Texture2D");
            }

            mTexture = texture;
            ColorOperation = colorOperation;
            BlendOperation = blendOperation;
            TextureAddressMode = textureAddressMode;
            TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
            _originalTextureFilter = TextureFilter.Linear;

#if MONOGAME
            Red = 0;
            Green = 0;
            Blue = 0;

#endif
        }
        

        #endregion

        #region Public Methods

        public bool DiffersFrom(Sprite sprite)
        {

            return sprite.Texture != Texture ||
                sprite.ColorOperation != ColorOperation ||
                sprite.BlendOperation != BlendOperation ||
                sprite.TextureAddressMode != TextureAddressMode ||
                (sprite.TextureFilter != null && 
                sprite.TextureFilter != TextureFilter)
#if WINDOWS_PHONE || MONOGAME
                ||
                sprite.Red != Red ||
                sprite.Green != Green ||
                sprite.Blue != Blue
#endif
                
                
                ;
        }

        public bool DiffersFrom(Text text)
        {

            return text.Font.Texture != Texture ||
                text.ColorOperation != ColorOperation ||
                text.BlendOperation != BlendOperation ||
                TextureAddressMode != TextureAddressMode.Clamp
#if WINDOWS_PHONE || MONOGAME
                ||
                text.Red != Red ||
                text.Green != Green ||
                text.Blue != Blue
#endif
;
        }


        public void SetStates()
        {
            //if (Renderer.RendererDiagnosticSettings.RenderBreaksPerformStateChanges)
            {
#if FRB_MDX
                if (Texture != null)
                    Renderer.Texture = Texture.texture;
                else
                    Renderer.Texture = null;

                if (Texture == null && ColorOperation == TextureOperation.SelectArg1)
                {
                    ColorOperation = TextureOperation.SelectArg2;
                }
#else
                if (ColorOperation != Graphics.ColorOperation.Color)
                {
                    Renderer.Texture = Texture;
                }

                if (Texture == null && ColorOperation == ColorOperation.Texture)
                {
                    ColorOperation = ColorOperation.Color;
                }
#endif

                Renderer.ColorOperation = ColorOperation;
                Renderer.BlendOperation = BlendOperation;
                Renderer.TextureAddressMode = TextureAddressMode;
                _originalTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
                if (TextureFilter != FlatRedBallServices.GraphicsOptions.TextureFilter)
                    FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter;

#if WINDOWS_PHONE || MONOGAME
            if (ColorOperation == Graphics.ColorOperation.ColorTextureAlpha)
            {
                Renderer.SetFogForColorOperation(Red, Green, Blue);
            }
            // Vic says - can we do add?  Do we have to use dual texturing?  Crappy!
            //if (ColorOperation == Graphics.ColorOperation.Add)
            //{
            //    BasicEffect effect = Renderer.CurrentEffect as BasicEffect;

                

            //    effect.LightingEnabled = true;
                
            //    effect.AmbientLightColor = Microsoft.Xna.Framework.Vector3.One;

            //    effect.DirectionalLight0.Enabled = true;
            //    effect.DirectionalLight0.DiffuseColor = new Microsoft.Xna.Framework.Vector3(Red, Green, Blue);
            //    //effect.EmissiveColor = new Microsoft.Xna.Framework.Vector3(Red, Green, Blue);


            //}
            //else
            //{
            //    BasicEffect effect = Renderer.CurrentEffect as BasicEffect;

            //    effect.EmissiveColor = new Microsoft.Xna.Framework.Vector3(0, 0, 0);


            //}
#endif
            }
        }

#if !FRB_MDX
        public void SetStates(Effect effect)
        {
            //if (Renderer.RendererDiagnosticSettings.RenderBreaksPerformStateChanges)
            {
                effect.Parameters["CurrentTexture"].SetValue(Texture);

                EffectParameter address = effect.Parameters["Address"];
                if (address != null)
                {
                    address.SetValue((int)TextureAddressMode);
                }

                Renderer.ForceSetColorOperation(this.ColorOperation);

                Renderer.BlendOperation = BlendOperation;
                _originalTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
                if (TextureFilter != FlatRedBallServices.GraphicsOptions.TextureFilter)
                    FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter;
            }
        }
#endif

        public override string ToString()
        {
            string textureName = "<null texture>";
            if (this.Texture != null)
            {
                textureName = this.Texture.Name;
            }

            return textureName;
        }

        #endregion

        #endregion

        public void Cleanup()
        {
            if (_originalTextureFilter != FlatRedBallServices.GraphicsOptions.TextureFilter)
                FlatRedBallServices.GraphicsOptions.TextureFilter = _originalTextureFilter;
        }
    }
}
