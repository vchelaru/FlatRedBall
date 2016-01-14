/*
 * PaletteEffectContent.cs
 * Copyright (c) 2006 David Astle
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace FlatRedBall.Graphics.Model.Animation.Content
{



    [ContentTypeWriter]
    internal class PaletteMaterialWriter : ContentTypeWriter<PaletteMaterialContent>
    {
        protected override void Write(ContentWriter output, PaletteMaterialContent value)
        {

            output.WriteRawObject<byte[]>(value.ByteCode);
            output.Write(value.PaletteSize);
            bool hasTexture = value.Textures.ContainsKey("Texture");
            output.Write(hasTexture);
            if (hasTexture)
                output.WriteExternalReference<TextureContent>(value.Textures["Texture"]);
            output.Write(value.SpecularPower != null);
            if (value.SpecularPower != null)
                output.Write((float)value.SpecularPower);
            output.Write(value.SpecularColor != null);
            if (value.SpecularColor != null)
                output.Write((Vector3)value.SpecularColor);
            output.Write(value.EmissiveColor != null);
            if (value.EmissiveColor != null)
                output.Write((Vector3)value.EmissiveColor);
            output.Write(value.DiffuseColor != null);
            if (value.DiffuseColor != null)
                output.Write((Vector3)value.DiffuseColor);
            output.Write(value.Alpha != null);
            if (value.Alpha != null)
                output.Write((float)value.Alpha);

        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.ModelAnimation.PaletteEffectReader).AssemblyQualifiedName;
            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
            //    return "FlatRedBall.Content.ModelAnimation.PaletteEffectReader, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Content.ModelAnimation.PaletteEffectReader, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Graphics.Model.Animation.BasicPaletteEffect).AssemblyQualifiedName;
            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.BasicPaletteEffect, FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.BasicPaletteEffect, FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
        }
    }


    /// <summary>
    /// Processes a PaletteInfo object into a PaletteMaterialContent object.
    /// </summary>
    [ContentProcessor]
    public class PaletteInfoProcessor : ContentProcessor<PaletteInfo,
        PaletteMaterialContent>
    {
        /// <summary>
        /// Processes a PaletteInfo object into a PaletteMaterialContent object.
        /// </summary>
        /// <param name="input">The PaletteInfo to process.</param>
        /// <param name="context">The processor context.</param>
        /// <returns>The processed PaletteMaterialContent</returns>
        public override PaletteMaterialContent Process(PaletteInfo input,
            ContentProcessorContext context)
        {
#if XNA4
            throw new NotImplementedException();
#else
            // Set all the variables based on the input.
            EffectProcessor effectProcessor = new EffectProcessor();
            EffectContent effectContent = new EffectContent();
            effectContent.EffectCode = input.SourceCode;
            CompiledEffect compiled;
            if (context.TargetPlatform == TargetPlatform.Xbox360)
            {
                compiled = Effect.CompileEffectFromSource(
                    input.SourceCode, null, null, CompilerOptions.None, TargetPlatform.Xbox360);
            }
            else
            {
                compiled = effectProcessor.Process(effectContent, context);
            }
            PaletteMaterialContent content = new PaletteMaterialContent();
            content.PaletteSize = input.PaletteSize;
            content.ByteCode = compiled.GetEffectCode();
            BasicMaterialContent basic = input.BasicContent;
            content.Alpha = basic.Alpha;
            content.DiffuseColor = basic.DiffuseColor;
            content.EmissiveColor = basic.EmissiveColor;
            content.Name = basic.Name;
            content.SpecularColor = basic.SpecularColor;
            content.SpecularPower = basic.SpecularPower;
            content.Texture = basic.Texture;
            content.VertexColorEnabled = basic.VertexColorEnabled;
            return content;
#endif
        }
    }

    /// <summary>
    /// Contains info relating to BasicPaletteEffect for processing.
    /// </summary>
    public class PaletteInfo
    {
        private string sourceCode;
        private int paletteSize;
        private BasicMaterialContent basicContent;

        /// <summary>
        /// Creats a new instance of PaletteInfo.
        /// </summary>
        /// <param name="sourceCode">The source code for the BasicPaletteEffect</param>
        /// <param name="paletteSize">The size of the matrix palette</param>
        /// <param name="basicContent">The BasicMaterialContent that stores the parameters
        /// to copy to BasicPaletteEffect</param>
        public PaletteInfo(string sourceCode, int paletteSize,
            BasicMaterialContent basicContent)
        {
            this.sourceCode = sourceCode;
            this.paletteSize = paletteSize;
            this.basicContent = basicContent;
        }

        /// <summary>
        /// The source code for BasicPaletteEffect.
        /// </summary>
        public string SourceCode
        { get { return sourceCode; } }

        /// <summary>
        /// The size of the matrix palette.
        /// </summary>
        public int PaletteSize
        { get { return paletteSize; } }

        /// <summary>
        /// The BasicMaterialContent that stores values to copy to the palette content.
        /// </summary>
        public BasicMaterialContent BasicContent
        { get { return basicContent; } }
    }

    /// <summary>
    /// Content for BasicPaletteEffect.
    /// </summary>
    public class PaletteMaterialContent : BasicMaterialContent
    {
        private byte[] byteCode;
        private int paletteSize;

        /// <summary>
        /// Creates a new instance of PaletteMaterialContent.
        /// </summary>
        public PaletteMaterialContent()
        {
        }

        /// <summary>
        /// Gets or sets the size of the matrix palette.
        /// </summary>
        public int PaletteSize
        {
            get { return paletteSize; }
            set 
            { 
                paletteSize = value; 
            }
        }

        /// <summary>
        /// Gets or sets the byte code for the effect.
        /// </summary>
        public byte[] ByteCode
        {
            get { return (byte[])byteCode.Clone(); }
            set { byteCode = value; }
        }



    }

}
