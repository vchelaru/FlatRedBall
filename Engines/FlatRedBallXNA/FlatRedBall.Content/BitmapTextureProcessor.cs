using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
using System.Windows.Forms;
using System.Diagnostics;
using FlatRedBall.IO;
#endif

namespace FlatRedBall.Content
{
    class BitmapTextureProcessor
    {
        #region Fields


        static OpaqueDataDictionary dictionary;
    //    dictionary.Add("TextureFormat", TextureProcessorOutputFormat);

        static bool UseBlackAsAlpha;

        public static bool ResizeToPowerOfTwo
        {
            set
            {
                CreateDictionaryIfNecessary();

                if (!dictionary.ContainsKey("ResizeToPowerOfTwo"))
                {
                    dictionary.Add("ResizeToPowerOfTwo", value);
                }
                else
                {
                    dictionary["ResizeToPowerOfTwo"] = value;
                }
                
            }
            get
            {
                if (!dictionary.ContainsKey("ResizeToPowerOfTwo"))
                {
                    return false;
                }
                else
                {
                    return (bool)dictionary["ResizeToPowerOfTwo"];
                }
            }
        }

        public static TextureProcessorOutputFormat TextureProcessorOutputFormat
        {
            set
            {
                CreateDictionaryIfNecessary();


                if (!dictionary.ContainsKey("TextureFormat"))
                {
                    dictionary.Add("TextureFormat", value);
                }
                else
                {
                    dictionary["TextureFormat"] = value;
                }
            }
            get
            {
                if (!dictionary.ContainsKey("TextureFormat"))
                {
                    return TextureProcessorOutputFormat.Color;
                }
                else
                {
                    return (TextureProcessorOutputFormat)dictionary["TextureFormat"];
                }

            }
        }

        public static bool GenerateMipmaps
        {
            set
            {
                CreateDictionaryIfNecessary();

                if (!dictionary.ContainsKey("GenerateMipmaps"))
                {
                    dictionary.Add("GenerateMipmaps", value);
                }
                else
                {
                    dictionary["GenerateMipmaps"] = value;
                }
            }
            get
            {
                if (!dictionary.ContainsKey("GenerateMipmaps"))
                {
                    return true;
                }
                else
                {
                    return (bool)dictionary["GenerateMipmaps"];
                }

            }
        }

        public static TextureProcessor TextureProcessor = new TextureProcessor();

        #endregion

        #region Methods

        /// <summary>
        /// builds an external reference to a texture
        /// </summary>
        internal static ExternalReference<TextureContent> BuildTexture(string file, ContentProcessorContext context)
        {
            ExternalReference<Texture2DContent> tex = new ExternalReference<Texture2DContent>(file);

            tex.Name = Path.GetFileNameWithoutExtension(file);

            CreateDictionaryIfNecessary();

            //string extension = Path.GetExtension(file);
            //UseBlackAsAlpha = extension.ToLower() == ".bmp";


            //if (UseBlackAsAlpha)
            //{
            //    TextureProcessor.ColorKeyColor = Color.Black;
            //    TextureProcessor.ColorKeyEnabled = true;
            //}
            //else
            //{
            //    TextureProcessor.ColorKeyEnabled = false;
            //}


            return context.BuildAsset<Texture2DContent, TextureContent>(
                    tex,
                    typeof(TextureProcessor).Name,
                    dictionary,
                    "TextureImporter",
                    null // Passing null lets the content pipeline decide on the default name, which is what we want.
                    );
        }


        static int MakePowerOfTwo(int value)
        {
            int bit = 1;

            while (bit < value)
                bit *= 2;

            return bit;
        }



        private static void CreateDictionaryIfNecessary()
        {
            if (dictionary == null)
            {
                dictionary = new OpaqueDataDictionary();

#if XNA4
                dictionary.Add("ColorKeyColor", Color.Transparent);
#else
                dictionary.Add("ColorKeyColor", Color.TransparentBlack);
#endif
                dictionary.Add("ColorKeyEnabled", false);
                dictionary.Add("GenerateMipmaps", true);
                dictionary.Add("ResizeToPowerOfTwo", false);
                dictionary.Add("TextureFormat", TextureProcessorOutputFormat.Color);
            }


        }


        #endregion
    }
}
