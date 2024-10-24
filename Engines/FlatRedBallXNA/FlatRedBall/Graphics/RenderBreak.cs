//#if DESKTOP_GL || WINDOWS
#if WINDOWS|| MONOGAME_381
#define USE_CUSTOM_SHADER
#endif

using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.IO;

namespace FlatRedBall.Graphics
{
    public struct RenderBreak
    {
        #region Fields

        public int ItemNumber;

        public PrimitiveType PrimitiveType;

        public string LayerName;

#if DEBUG
        public object ObjectCausingBreak;

        /// <summary>
        /// Debug only: Returns detailed information about this render break.
        /// </summary>
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
        
        public float Red;
        public float Green;
        public float Blue;

        public ColorOperation ColorOperation;

        public BlendOperation BlendOperation;

        public TextureFilter TextureFilter;

        public Texture2D Texture;

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
                if (sprite.mTexture != null && sprite.mTexture.IsDisposed)
                {
                    var msg = "The Sprite with the name \"" + sprite.Name +
                        "\" references a disposed texture of the name " + sprite.Texture.Name +
                        ".  If you're using Screens you may have forgotten to remove a Sprite that was " +
                        "added in the Screen.";

                    if (sprite.Parent != null)
                    {
                        msg += $"\n\n Sprite parent found, it is: {sprite.Parent.Name} of type: {sprite.Parent.GetType()}";
                    }
                    else
                    {
                        msg += "\n\n Sprite parent was null.";
                    }

                    if(sprite.CurrentChainName != null)
                    {
                        msg += $"\n\n Sprite chain name was: {sprite.CurrentChainName}";
                    }

                    throw new ObjectDisposedException(msg);
                }

                try
                {
                    Texture = sprite.mTexture;
                }
                catch(ObjectDisposedException)
                {
                    // no big deal if it's disposed, that probably means we are switching to a new screen and the render breaks
                    // for this frame are being recorded. We can ignore this:

                    Texture = null;
                }

                ColorOperation = sprite.mColorOperation;
                BlendOperation = sprite.mBlendOperation;
                TextureFilter = sprite.TextureFilter.HasValue ? sprite.TextureFilter.Value : FlatRedBallServices.GraphicsOptions.TextureFilter;

                if (sprite.mTexture == null)
                {

                    // requirement for reach profile - this shouldn't impact anything
                    TextureAddressMode = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp;
                }
                else
                {
                    TextureAddressMode = sprite.TextureAddressMode;
                }


                Red = sprite.mRed;
                Green = sprite.mGreen;
                Blue = sprite.mBlue;
            }
            else
            {
                Red = 0;
                Green = 0;
                Blue = 0;

                Texture = null;

                ColorOperation = ColorOperation.Texture;

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


            Red = 1;
            Green = 1;
            Blue = 1;
#if !USE_CUSTOM_SHADER

            if (text.ColorOperation != Graphics.ColorOperation.Texture)
            {
                Red = text.Red;
                Green = text.Green;
                Blue = text.Blue;
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

                Texture = text.Font.Textures[textureIndex];

                ColorOperation = text.ColorOperation;
                BlendOperation = text.BlendOperation;
                TextureAddressMode = TextureAddressMode.Clamp;
            }
            else
            {
                Texture = null;
                ColorOperation = ColorOperation.Texture;
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

            Texture = texture;
            ColorOperation = colorOperation;
            BlendOperation = blendOperation;
            TextureAddressMode = textureAddressMode;
            TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
            _originalTextureFilter = TextureFilter.Linear;

            Red = 0;
            Green = 0;
            Blue = 0;
        }
        

        #endregion

        #region Public Methods

        public bool DiffersFrom(Sprite sprite)
        {

            // Some explanation on why we are doing this:
            // ColorTextureAlpha is implemented using a custom
            // shader on FRB XNA. FRB MonoGame doesn't (yet) use
            // custom shaders, so it has to rely on fixed function-
            // equivalent code (technically it is using shaders, just
            // not ones we wrote for FRB). Therefore, when using this color
            // operation, we have to change states on any color change. But to
            // make this more efficient we'll only do this if in ColorTExtureAlpha.
            // Even though FlatRedBall XNA doesn't require a state change here, we want
            // all engines to beave the same if the user uses FRB XNA for performance measurements.
            // Therefore, we'll inject a render break here on PC and eat the performance penalty to 
            // get identical behavior across platforms.
            bool isColorChangingOnColorTextureAlpha =
                sprite.mColorOperation == ColorOperation.ColorTextureAlpha &&
                (sprite.mRed != Red ||
                sprite.mGreen != Green ||
                sprite.mBlue != Blue);

            return sprite.mTexture != Texture ||
                sprite.mColorOperation != ColorOperation ||
                sprite.mBlendOperation != BlendOperation ||
                sprite.TextureAddressMode != TextureAddressMode ||
                (sprite.TextureFilter != null &&
                sprite.TextureFilter != TextureFilter) ||
                isColorChangingOnColorTextureAlpha;
        }

        public bool DiffersFrom(Text text)
        {

            return text.Font.Texture != Texture ||
                text.ColorOperation != ColorOperation ||
                text.BlendOperation != BlendOperation ||
                TextureAddressMode != TextureAddressMode.Clamp
#if !USE_CUSTOM_SHADER
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

                if (ColorOperation != ColorOperation.Color)
                {
                    Renderer.Texture = Texture;
                }

                if (Texture == null && ColorOperation == ColorOperation.Texture)
                {
                    ColorOperation = ColorOperation.Color;
                }

                Renderer.ColorOperation = ColorOperation;
                Renderer.BlendOperation = BlendOperation;
                Renderer.TextureAddressMode = TextureAddressMode;
                _originalTextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
                //if (TextureFilter != FlatRedBallServices.GraphicsOptions.TextureFilter)
                    FlatRedBallServices.GraphicsOptions.TextureFilter = TextureFilter;

#if !USE_CUSTOM_SHADER
                if (ColorOperation == Graphics.ColorOperation.ColorTextureAlpha)
                {
                    Renderer.SetFogForColorOperation(Red, Green, Blue);
                }
#endif
            }
        }


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


        public override string ToString()
        {
            var hasAlreadyIncludedType = false;
            string toReturn = "<null texture>";
            if (this.Texture?.Name != null)
            {
                if(!FlatRedBall.IO.FileManager.IsRelative(Texture.Name))
                {
                    toReturn = FileManager.MakeRelative(this.Texture.Name);

                }
                else
                {
                    toReturn = this.Texture.Name;
                }
            }
#if DEBUG
            else
            {
                hasAlreadyIncludedType = true;
                toReturn = ObjectCausingBreak?.GetType().Name;
            }


            var byPrefix = hasAlreadyIncludedType ? null : "  by";

            if (ObjectCausingBreak is INameable nameable && !string.IsNullOrEmpty(nameable.Name))
            {
                toReturn += $"{byPrefix} {nameable.Name}";
            }
            else if(!hasAlreadyIncludedType)
            {
                toReturn += $"{byPrefix} {ObjectCausingBreak?.GetType().Name}";
            }
#endif
            return toReturn;
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
