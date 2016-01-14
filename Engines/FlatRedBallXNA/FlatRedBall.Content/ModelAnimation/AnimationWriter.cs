/*
 * AnimationWriter.cs
 * Copyright (c) 2006, 2007 David Astle, Michael Nikonov
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

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif
#endregion

namespace FlatRedBall.Graphics.Model.Animation.Content
{

    /// <summary>
    /// Writes ModelInfo data so it can be read into an object during runtime
    /// </summary>
    [ContentTypeWriter]
    internal sealed class AnimationWriter : ContentTypeWriter<AnimationContentDictionary>
    {



        /// <summary>
        /// Writes a ModelInfo object into XNB data
        /// </summary>
        /// <param name="output">The stream that contains the written data</param>
        /// <param name="value">The instance to be serialized</param>
        protected override void Write(ContentWriter output, AnimationContentDictionary value)
        {
            AnimationContentDictionary animations = value;
            output.Write(animations.Count);

            foreach (KeyValuePair<string, AnimationContent> k in animations)
            {
                output.Write(k.Key);

                output.Write(k.Value.Channels.Count);
                foreach (KeyValuePair<string, AnimationChannel> chan in k.Value.Channels)
                {
                    output.Write(chan.Key);
                    output.Write(chan.Value.Count);

                    foreach (AnimationKeyframe keyframe in chan.Value)
                    {
                        output.Write(keyframe.Transform);
                        output.Write(keyframe.Time.Ticks);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the string that describes the reader used to convert the
        /// stream of data into a ModelInfo object
        /// </summary>
        /// <param name="targetPlatform">The current platform</param>
        /// <returns>The string that describes the reader used for a ModelInfo object</returns>
        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.ModelAnimation.AnimationReader).AssemblyQualifiedName;
            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
                
            //    return "FlatRedBall.Content.ModelAnimation.AnimationReader, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Content.ModelAnimation.AnimationReader, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            

        }
        
        /// <summary>
        /// Returns the string that describes what type of object the stream
        /// will be converted into at runtime (ModelInf)
        /// </summary>
        /// <param name="targetPlatform">The current platform</param>
        /// <returns>The string that describes the run time type for the object written into
        /// the stream</returns>
        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Graphics.Model.Animation.AnimationInfoCollection).AssemblyQualifiedName;
            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.AnimationInfoCollection, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.AnimationInfoCollection, "
            //        + "FlatRedBall, "
            //        + "Version="+ContentUtil.VERSION+", Culture=neutral, PublicKeyToken=null";
            //}



        }
    }
 
}
