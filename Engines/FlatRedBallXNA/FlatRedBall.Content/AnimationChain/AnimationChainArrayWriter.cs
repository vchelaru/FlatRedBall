using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using FlatRedBall.Graphics.Animation;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.AnimationChain
{
    [ContentTypeWriter]
    public class AnimationChainArrayWriter : ContentTypeWriter<AnimationChainListSaveContent>
    {
        protected override void Write(ContentWriter output, AnimationChainListSaveContent value)
        {
            if (ObjectReader.UseReflection)
            {
                ObjectWriter.WriteObject<AnimationChainListSaveContent>(output, value);
            }
            else
            {
                WriteAnimationChainListSave(output, value);
            }
			/*
            //write the list of Animation Chains
            output.Write((int)value.TimeMeasurementUnit);
            output.Write(value.AnimationChains.Count);
            for (int i = 0; i < value.AnimationChains.Count; i++)
            {
                AnimationChainSaveContent ach = value.AnimationChains[i];
                output.Write(ach.Frames.Count);
                output.Write(ach.Name);
                
                //output.Write(ach.ColorKey);

                //Write the list of frames
                for (int j = 0; j < ach.Frames.Count; j++)
                {
                    AnimationFrameSaveContent frame = ach.Frames[j];// as AnimationFrameSaveContent;
                    output.WriteExternalReference(frame.TextureReference);
                    output.Write(frame.FrameLength);
                    output.Write(frame.FlipHorizontal);
                    output.Write(frame.FlipVertical);

                    output.Write(frame.LeftCoordinate);
                    output.Write(frame.RightCoordinate);
                    output.Write(frame.TopCoordinate);
                    output.Write(frame.BottomCoordinate);

                    output.Write(frame.RelativeX);
                    output.Write(frame.RelativeY);
                }
            }
			 */
        }

        public static void WriteAnimationChainListSave(ContentWriter output, AnimationChainListSaveContent value)
        {
            output.Write(value.FileRelativeTextures);
            output.Write(System.Convert.ToInt32(value.TimeMeasurementUnit));
            output.Write(value.AnimationChains.Count);
            for (int i = 0; i < value.AnimationChains.Count; i++)
                ObjectWriter.WriteObject(output, value.AnimationChains[i]);
        }

        public static void WriteAnimationChainSave(ContentWriter output, AnimationChainSaveContent value)
        {
            if (value.Name != null)
                output.Write(value.Name);
            else
                output.Write("");
            output.Write(value.ColorKey);
            if (value.ParentFile != null)
                output.Write(value.ParentFile);
            else
                output.Write("");
            output.Write(value.Frames.Count);
            for (int i = 0; i < value.Frames.Count; i++)
                ObjectWriter.WriteObject(output, value.Frames[i]);

        }

        public static void WriteAnimationFrameSave(ContentWriter output, AnimationFrameSaveContent value)
        {
            output.Write(value.TextureReference != null);
            if (value.TextureReference != null)
                output.WriteExternalReference(value.TextureReference);
            output.Write(value.FlipHorizontal);
            output.Write(value.FlipVertical);
            if (value.TextureName != null)
                output.Write(value.TextureName);
            else
                output.Write("");
            output.Write(value.FrameLength);
            output.Write(value.LeftCoordinate);
            output.Write(value.RightCoordinate);
            output.Write(value.TopCoordinate);
            output.Write(value.BottomCoordinate);
            output.Write(value.RelativeX);
            output.Write(value.RelativeY);


        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(AnimationChainList).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(AnimationChainListReader).AssemblyQualifiedName;
        }
    }
}
