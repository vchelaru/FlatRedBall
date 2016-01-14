using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;
using FlatRedBall.Content.AnimationChain;

namespace FlatRedBall.Content
{
    #region XML docs
    /// <summary>
    /// Reader class used in the FlatRedBall Content Pipeline to convert compiled
    /// AnimationChainLists to the runtime instances.
    /// </summary>
    #endregion
    public class AnimationChainListReader : ContentTypeReader<AnimationChainList>
    {
        protected override AnimationChainList Read(ContentReader input, AnimationChainList existingInstance)
        {
            if (existingInstance!= null)
            {
                return existingInstance;
            }

            AnimationChainListSave acls = null;

            if (ObjectReader.UseReflection)
            {
                acls = ObjectReader.ReadObject<AnimationChainListSave>(input);
            }
            else
            {
                acls = ReadAnimationChainListSave(input);
            }
			acls.FileName = input.AssetName;

			return acls.ToAnimationChainList("");

			/*
            //read list of animation chains
            TimeMeasurementUnit timeMeasurement = (TimeMeasurementUnit)input.ReadInt32();
            float divisor = timeMeasurement == TimeMeasurementUnit.Millisecond ? 1000 : 1;

            int chainsCount = input.ReadInt32();
            existingInstance = new AnimationChainList(chainsCount);
            existingInstance.TimeMeasurementUnit = timeMeasurement;

            existingInstance.Name = input.AssetName;

            for (int i = 0; i < chainsCount; i++)
            {
                //read list of Frames
                int frameCount = input.ReadInt32();
                FlatRedBall.Graphics.Animation.AnimationChain ach =
                    new FlatRedBall.Graphics.Animation.AnimationChain(frameCount);

                ach.Name = input.ReadString();

                for (int j = 0; j < frameCount; j++)
                {
                    AnimationFrame frame = new AnimationFrame(
                        input.ReadExternalReference<Texture2D>(),
                        input.ReadSingle());
                    frame.FrameLength /= divisor;
                    frame.FlipHorizontal = input.ReadBoolean();
                    frame.FlipVertical = input.ReadBoolean();

                    frame.LeftCoordinate = input.ReadSingle();
                    frame.RightCoordinate = input.ReadSingle();
                    frame.TopCoordinate = input.ReadSingle();
                    frame.BottomCoordinate = input.ReadSingle();

                    frame.RelativeX = input.ReadSingle();
                    frame.RelativeY = input.ReadSingle();

                    ach.Add(frame);
                }
                existingInstance.Add(ach);
            }

            return existingInstance;
			 */
        }

        public static AnimationChainListSave ReadAnimationChainListSave(ContentReader input)
        {
            FlatRedBall.Content.AnimationChain.AnimationChainListSave newObject = new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
            newObject.FileRelativeTextures = input.ReadBoolean();
            newObject.TimeMeasurementUnit = (FlatRedBall.TimeMeasurementUnit)Enum.ToObject(typeof(FlatRedBall.TimeMeasurementUnit), (int)input.ReadInt32());
            int AnimationChainsCount = input.ReadInt32();
            for (int i = 0; i < AnimationChainsCount; i++)
                newObject.AnimationChains.Add(ObjectReader.ReadObject<FlatRedBall.Content.AnimationChain.AnimationChainSave>(input));
            return newObject;

        }

        public static AnimationChainSave ReadAnimationChainSave(ContentReader input)
        {
            FlatRedBall.Content.AnimationChain.AnimationChainSave newObject = new FlatRedBall.Content.AnimationChain.AnimationChainSave();
            newObject.Name = input.ReadString();
            newObject.ColorKey = input.ReadUInt32();
            newObject.ParentFile = input.ReadString();
            int FramesCount = input.ReadInt32();
            for (int i = 0; i < FramesCount; i++)
                newObject.Frames.Add(ObjectReader.ReadObject<FlatRedBall.Content.AnimationChain.AnimationFrameSave>(input));
            return newObject;

        }

        public static AnimationFrameSave ReadAnimationFrameSave(ContentReader input)
        {
            FlatRedBall.Content.AnimationChain.AnimationFrameSave newObject = new FlatRedBall.Content.AnimationChain.AnimationFrameSave();
            if (input.ReadBoolean())
                newObject.mTextureInstance = input.ReadExternalReference<Microsoft.Xna.Framework.Graphics.Texture2D>();
            newObject.FlipHorizontal = input.ReadBoolean();
            newObject.FlipVertical = input.ReadBoolean();
            newObject.TextureName = input.ReadString();
            newObject.FrameLength = input.ReadSingle();
            newObject.LeftCoordinate = input.ReadSingle();
            newObject.RightCoordinate = input.ReadSingle();
            newObject.TopCoordinate = input.ReadSingle();
            newObject.BottomCoordinate = input.ReadSingle();
            newObject.RelativeX = input.ReadSingle();
            newObject.RelativeY = input.ReadSingle();
            return newObject;

        }
    }
}
