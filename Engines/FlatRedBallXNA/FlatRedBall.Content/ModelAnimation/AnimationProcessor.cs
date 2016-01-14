/*
 * AnimationProcessor.cs
 * Copyright (c) 2007 David Astle, Michael Nikonov
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
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Collections;

namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Produces AnimationContentDictionary;
    /// warns of incompatibilities of model and skeleton.
    /// </summary>
    [ContentProcessor(DisplayName = "ModelAnimationCollection - Animation Library")]
    public class AnimationProcessor : ContentProcessor<BoneContent, AnimationContentDictionary>
    {

        private ContentProcessorContext context;
        private BoneContent inputSkeleton;
        private NodeContent model;
        private BoneContent modelSkeleton;


        /// <summary>
        /// The context for the processor.
        /// </summary>
        protected ContentProcessorContext Context
        { get { return context; } }

        /// <summary>
        /// The input skeleton.
        /// </summary>
        protected BoneContent InputSkeleton
        { get { return inputSkeleton; } }


        /// <summary>
        /// The model.
        /// </summary>
        protected NodeContent Model
        { get { return model; } }

        /// <summary>
        /// The skeleton contained in the model.
        /// </summary>
        protected BoneContent ModelSkeleton
        { get { return modelSkeleton; } }


        /// <summary>
        /// Produces ModelAnimationInfo from skeleton and animations.
        /// </summary>
        /// <param name="input">skeleton</param>
        /// <param name="context">The context for this processor</param>
        /// <returns>AnimationContentDictionary</returns>
        public override AnimationContentDictionary Process(BoneContent input, ContentProcessorContext context)
        {
            
            inputSkeleton = input;
            inputSkeleton.Identity.FragmentIdentifier = "";
            this.context = context;

            string modelFilePath = GetModelFilePath(inputSkeleton.Identity);
            if (modelFilePath != null)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "animation will be checked against model " + modelFilePath);
                ExternalReference<NodeContent> er = new ExternalReference<NodeContent>(modelFilePath);
                model = (NodeContent)context.BuildAndLoadAsset<NodeContent, Object>(er, "PassThroughProcessor");
                modelSkeleton = MeshHelper.FindSkeleton(model);
                CheckBones(modelSkeleton, inputSkeleton);
            }
            else
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "corresponding model not found");
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "animation filename should follow the <modelName>_<animationName>.<ext> pattern to get animation skeleton checked against model");
            }

            AnimationContentDictionary animations = Interpolate(input.Animations);
            return animations;
        }

        /// <summary>
        /// Gets the path of the model.
        /// </summary>
        /// <param name="animationId">The identity of the AnimationContent object.</param>
        /// <returns>The path of the model file.</returns>
        protected virtual string GetModelFilePath(ContentIdentity animationId)
        {
            string dir = Path.GetDirectoryName(animationId.SourceFilename);
            string animName = Path.GetFileNameWithoutExtension(animationId.SourceFilename);
            if (animName.Contains("_"))
            {
                string modelName = animName.Split('_')[0];
                return Path.GetFullPath(dir + @"\" + modelName + ".fbx");
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks the bones to make sure the skeleton is valid.
        /// </summary>
        /// <param name="modelBone">The model bone to check.</param>
        /// <param name="skeletonBone">The skeleton bone to check</param>
        protected virtual void CheckBones(BoneContent modelBone, BoneContent skeletonBone)
        {
            if (modelBone.Name != skeletonBone.Name)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                "model bone " + modelBone.Name
                    + " does not match skeletone bone " + skeletonBone.Name);
            }
            if (modelBone.Children.Count != skeletonBone.Children.Count)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "model bone " + modelBone.Name + " has " + modelBone.Children.Count
                    + " children but corresponding skeletone bone has " + skeletonBone.Children.Count + " children");
            }
            float diff = modelBone.Transform.Translation.Length() - skeletonBone.Transform.Translation.Length();
            if (diff * diff > 0.0001f)
            {
                context.Logger.LogWarning("", inputSkeleton.Identity,
                    "model bone " + modelBone.Name + " translation "
                    + "(Lenght=" + modelBone.Transform.Translation.Length() + ")"
                    + " does not match translation of skeletone bone "
                    + skeletonBone.Name
                    + " (Lenght=" + skeletonBone.Transform.Translation.Length() + ")"
                    );
            }
            Dictionary<string, BoneContent> modelBoneDict = new Dictionary<string, BoneContent>();
            foreach (BoneContent mb in modelBone.Children)
            {
                modelBoneDict.Add(mb.Name, mb);
            }
            foreach (BoneContent sb in skeletonBone.Children)
            {
                if (modelBoneDict.ContainsKey(sb.Name))
                {
                    BoneContent mb = modelBoneDict[sb.Name];
                    CheckBones(mb, sb);
                }
                else
                {
                    context.Logger.LogWarning("", inputSkeleton.Identity,
                        "skeleton bone " + sb.Name + " was not found in model");
                }
            }
        }

        /// <summary>
        /// Interpolates all the AnimationContent in the specified dictionary to 60 fps.
        /// </summary>
        /// <param name="input">The animation dictionary to interpolate.</param>
        /// <returns>An interpolated dictionary of animations.</returns>
        public virtual AnimationContentDictionary Interpolate(AnimationContentDictionary input)
        {
            AnimationContentDictionary output = new AnimationContentDictionary();
            foreach (string name in input.Keys)
            {
                output.Add(name, Interpolate(input[name]));
            }
            return output;
        }

        /// <summary>
        /// Interpolates an AnimationContent object to 60 fps.
        /// </summary>
        /// <param name="input">The AnimationContent to interpolate.</param>
        /// <returns>The interpolated AnimationContent.</returns>
        public virtual AnimationContent Interpolate(AnimationContent input)
        {
            AnimationContent output = new AnimationContent();
            long time = 0;
            long animationDuration = input.Duration.Ticks;

            // default XNA importers, due to floating point errors or TimeSpan
            // estimation, sometimes  have channels with a duration slightly longer than
            // the animation duration.  So, set the animation duration to its true
            // value
            foreach (KeyValuePair<string, AnimationChannel> c in input.Channels)
            {
                if (c.Value[c.Value.Count - 1].Time.Ticks > animationDuration)
                    animationDuration = c.Value[c.Value.Count - 1].Time.Ticks;
            }

            foreach (KeyValuePair<string, AnimationChannel> c in input.Channels)
            {
                time = 0;
                string channelName = c.Key;
                AnimationChannel channel = c.Value;
                AnimationChannel outChannel = new AnimationChannel();
                int currentFrame = 0;

                // Step through time until the time passes the animation duration
                while (time <= animationDuration)
                {
                    AnimationKeyframe keyframe;
                    // Clamp the time to the duration of the animation and make this 
                    // keyframe equal to the last animation frame.
                    if (time >= animationDuration)
                    {
                        time = animationDuration;
                        keyframe = new AnimationKeyframe(new TimeSpan(time),
                            channel[channel.Count - 1].Transform);
                    }
                    else
                    {
                        // If the channel only has one keyframe, set the transform for the current time
                        // to that keyframes transform
                        if (channel.Count == 1 || time < channel[0].Time.Ticks)
                        {
                            keyframe = new AnimationKeyframe(new TimeSpan(time), channel[0].Transform);
                        }
                        // If the current track duration is less than the animation duration,
                        // use the last transform in the track once the time surpasses the duration
                        else if (channel[channel.Count - 1].Time.Ticks <= time)
                        {
                            keyframe = new AnimationKeyframe(new TimeSpan(time), channel[channel.Count - 1].Transform);
                        }
                        else // proceed as normal
                        {
                            // Go to the next frame that is less than the current time
                            while (channel[currentFrame + 1].Time.Ticks < time)
                            {
                                currentFrame++;
                            }
                            // Numerator of the interpolation factor
                            double interpNumerator = (double)(time - channel[currentFrame].Time.Ticks);
                            // Denominator of the interpolation factor
                            double interpDenom = (double)(channel[currentFrame + 1].Time.Ticks - channel[currentFrame].Time.Ticks);
                            // The interpolation factor, or amount to interpolate between the current
                            // and next frame
                            double interpAmount = interpNumerator / interpDenom;
                            
                            // If the frames are roughly 60 frames per second apart, use linear interpolation
                            if (channel[currentFrame + 1].Time.Ticks - channel[currentFrame].Time.Ticks
                                <= ContentUtil.TICKS_PER_60FPS * 1.05)
                            {
                                keyframe = new AnimationKeyframe(new TimeSpan(time),
                                    Matrix.Lerp(
                                    channel[currentFrame].Transform,
                                    channel[currentFrame + 1].Transform,
                                    (float)interpAmount));
                            }
                            else // else if the transforms between the current frame and the next aren't identical
                                 // decompose the matrix and interpolate the rotation separately
                                if (channel[currentFrame].Transform != channel[currentFrame + 1].Transform)
                            {
                                keyframe = new AnimationKeyframe(new TimeSpan(time),
                                    ContentUtil.SlerpMatrix(
                                    channel[currentFrame].Transform,
                                    channel[currentFrame + 1].Transform,
                                    (float)interpAmount));
                            }
                            else // Else the adjacent frames have identical transforms and we can use
                                    // the current frames transform for the current keyframe.
                            {
                                keyframe = new AnimationKeyframe(new TimeSpan(time),
                                    channel[currentFrame].Transform);
                            }

                        }
                    }
                    // Add the interpolated keyframe to the new channel.
                    outChannel.Add(keyframe);
                    // Step the time forward by 1/60th of a second
                    time += ContentUtil.TICKS_PER_60FPS;
                }

                // Compensate for the time error,(animation duration % TICKS_PER_60FPS),
                // caused by the interpolation by setting the last keyframe in the
                // channel to the animation duration.
                if (outChannel[outChannel.Count - 1].Time.Ticks < animationDuration)
                {
                    outChannel.Add(new AnimationKeyframe(
                        TimeSpan.FromTicks(animationDuration),
                        channel[channel.Count - 1].Transform));
                }

                outChannel.Add(new AnimationKeyframe(input.Duration, 
                    channel[channel.Count-1].Transform));
                // Add the interpolated channel to the animation
                output.Channels.Add(channelName, outChannel);
            }
            // Set the interpolated duration to equal the inputs duration for consistency
            output.Duration = TimeSpan.FromTicks(animationDuration);
            return output;

        }





    }
}


