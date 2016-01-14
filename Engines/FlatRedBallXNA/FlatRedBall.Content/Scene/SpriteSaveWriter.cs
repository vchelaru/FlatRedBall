using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using FlatRedBall.Content.AnimationChain;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.Scene
{
    [ContentTypeWriter]
    class SpriteSaveWriter : ContentTypeWriter<SpriteSaveContent>
    {
        protected override void Write(ContentWriter output, SpriteSaveContent value)
        {
            if (ObjectReader.UseReflection)
            {
                ObjectWriter.WriteObject<SpriteSaveContent>(output, value);
            }
            else
            {
                WriteUsingGeneratedCode(output, value);
            }
            
        }

        public static void WriteUsingGeneratedCode(ContentWriter output, SpriteSaveContent value)
        {


            output.Write(value.TextureReference != null);
            if (value.TextureReference != null)
                output.WriteExternalReference(value.TextureReference);
            output.Write(value.AnimationChainReference != null);
            if (value.AnimationChainReference != null)
                output.WriteExternalReference(value.AnimationChainReference);
            output.Write(value.X);
            output.Write(value.Y);
            output.Write(value.Z);
            output.Write(value.XVelocity);
            output.Write(value.YVelocity);
            output.Write(value.ZVelocity);
            output.Write(value.XAcceleration);
            output.Write(value.YAcceleration);
            output.Write(value.ZAcceleration);
            output.Write(value.RotationX);
            output.Write(value.RotationY);
            output.Write(value.RotationZ);
            output.Write(value.RotationZVelocity);
            output.Write(value.ScaleX);
            output.Write(value.ScaleY);
            output.Write(value.ScaleXVelocity);
            output.Write(value.ScaleYVelocity);
            output.Write(value.RelativeX);
            output.Write(value.RelativeY);
            output.Write(value.RelativeZ);
            output.Write(value.RelativeRotationX);
            output.Write(value.RelativeRotationY);
            output.Write(value.RelativeRotationZ);
            output.Write(value.Fade);
            output.Write(value.FadeRate);
            output.Write(value.TintRed);
            output.Write(value.TintGreen);
            output.Write(value.TintBlue);
            output.Write(value.TintRedRate);
            output.Write(value.TintBlueRate);
            output.Write(value.TintGreenRate);
            if (value.ColorOperation != null)
                output.Write(value.ColorOperation);
            else
                output.Write("");
            if (value.BlendOperation != null)
                output.Write(value.BlendOperation);
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
            if (value.Texture != null)
                output.Write(value.Texture);
            else
                output.Write("");
            output.Write(value.Animate);
            output.Write(value.CurrentChain);
            if (value.AnimationChainsFile != null)
                output.Write(value.AnimationChainsFile);
            else
                output.Write("");
            if (value.Type != null)
                output.Write(value.Type);
            else
                output.Write("");
            output.Write(value.Ordered);
            output.Write(value.Active);
            output.Write(value.ConstantPixelSize);
            output.Write(value.Visible);
            output.Write(value.TopTextureCoordinate);
            output.Write(value.BottomTextureCoordinate);
            output.Write(value.LeftTextureCoordinate);
            output.Write(value.RightTextureCoordinate);
            output.Write(value.FlipHorizontal);
            output.Write(value.FlipVertical);
            output.Write(System.Convert.ToInt32(value.TextureAddressMode));


            
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(SpriteReader).AssemblyQualifiedName;
        }
    }
}
