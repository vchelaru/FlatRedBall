using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

// TODO: replace this with the type you want to write out.
using TWrite = FlatRedBall.Content.Scene.TextSaveContent;
using FlatRedBall.Content.Saves;

namespace FlatRedBall.Content.Scene
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class TextSaveWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            if (ObjectReader.UseReflection)
            {
                ObjectWriter.WriteObject<TWrite>(output, value);
            }
            else
            {
                WriteTextObject(output, value);
            }
        }

        public static void WriteTextObject(ContentWriter output, TWrite value)
        {
            output.Write(value.FontTextureReference != null);
            if (value.FontTextureReference != null)
                output.WriteExternalReference(value.FontTextureReference);
            if (value.FontPatternText != null)
                output.Write(value.FontPatternText);
            else
                output.Write("");
            output.Write(value.X);
            output.Write(value.Y);
            output.Write(value.Z);
            output.Write(value.RotationX);
            output.Write(value.RotationY);
            output.Write(value.RotationZ);
            if (value.DisplayText != null)
                output.Write(value.DisplayText);
            else
                output.Write("");
            if (value.Name != null)
                output.Write(value.Name);
            else
                output.Write("");
            if (value.Parent != null)
                output.Write(value.Parent);
            else
                output.Write("");
            output.Write(value.Scale);
            output.Write(value.Spacing);
            output.Write(value.NewLineDistance);
            output.Write(value.MaxWidth);
            output.Write(System.Convert.ToInt32(value.MaxWidthBehavior));
            output.Write(System.Convert.ToInt32(value.VerticalAlignment));
            output.Write(System.Convert.ToInt32(value.HorizontalAlignment));
            output.Write(value.Visible);
            output.Write(value.CursorSelectable);
            if (value.FontTexture != null)
                output.Write(value.FontTexture);
            else
                output.Write("");
            if (value.FontFile != null)
                output.Write(value.FontFile);
            else
                output.Write("");
            output.Write(value.Red);
            output.Write(value.Green);
            output.Write(value.Blue);
            if (value.ColorOperation != null)
                output.Write(value.ColorOperation);
            else
                output.Write("");
            output.Write(value.RelativeX);
            output.Write(value.RelativeY);
            output.Write(value.RelativeZ);
            output.Write(value.RelativeRotationX);
            output.Write(value.RelativeRotationY);
            output.Write(value.RelativeRotationZ);

        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            // TODO: change this to the name of your ContentTypeReader
            // class which will be used to load this data.
            return typeof(FlatRedBall.Content.TextReader).AssemblyQualifiedName;
        }
    }
}
