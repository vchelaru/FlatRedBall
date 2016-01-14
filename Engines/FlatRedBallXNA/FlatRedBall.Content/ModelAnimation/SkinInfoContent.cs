/*
 * SkinInfoContent.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Graphics.Model.Animation.Content
{

    [ContentTypeWriter]
    internal class SkinInfoContentCollectionWriter : ContentTypeWriter<SkinInfoContentCollection>
    {
        protected override void Write(ContentWriter output, SkinInfoContentCollection value)
        {
            output.Write(value.Count);
            foreach (SkinInfoContent info in value)
            {
                output.Write(info.BoneIndex);
                output.Write(info.BoneName);
                output.Write(info.InverseBindPoseTransform);
                output.Write(info.PaletteIndex);
            }
        }


        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.ModelAnimation.SkinInfoCollectionReader).AssemblyQualifiedName;

            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
            //    return "FlatRedBall.Content.ModelAnimation.SkinInfoCollectionReader, "
            //        + "FlatRedBall, "
            //        + "Version=" + ContentUtil.VERSION + ", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Content.ModelAnimation.SkinInfoCollectionReader, "
            //        + "FlatRedBall, "
            //        + "Version=" + ContentUtil.VERSION + ", Culture=neutral, PublicKeyToken=null";
            //}

        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Graphics.Model.Animation.SkinInfoCollection).AssemblyQualifiedName;
            //if (targetPlatform == TargetPlatform.Xbox360)
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.SkinInfoCollection, FlatRedBall, "
            //        + "Version=" + ContentUtil.VERSION + ", Culture=neutral, PublicKeyToken=null";
            //}
            //else
            //{
            //    return "FlatRedBall.Graphics.Model.Animation.SkinInfoCollection, FlatRedBall, "
            //        + "Version=" + ContentUtil.VERSION + ", Culture=neutral, PublicKeyToken=null";
            //}
        }

    }

    /// <summary>
    /// A collection of SkinInfo objects that relate to a mesh.
    /// </summary>
    public class SkinInfoContentCollection : List<SkinInfoContent>
    {

    }

    /// <summary>
    /// The skinning information for a bone.
    /// </summary>
    public class SkinInfoContent
    {
        private Matrix inverseBindPose;
        private int boneIndex;
        private string boneName;
        private int paletteIndex;

        /// <summary>
        /// Gets or sets the bone index used by the matrix palette of the effect.
        /// </summary>
        public int PaletteIndex
        {
            get { return paletteIndex; }
            set { paletteIndex = value; }
        }

        /// <summary>
        /// Gets or sets the name of the bone.
        /// </summary>
        public string BoneName
        {
            get { return boneName; }
            set { boneName = value; }
        }

        /// <summary>
        /// Gets or sets the index of the bone in the model.
        /// </summary>
        public int BoneIndex
        {
            get { return boneIndex; }
            set { boneIndex = value; }
        }

        /// <summary>
        /// Gets or sets the bones inverse bind pose transform.
        /// </summary>
        public Matrix InverseBindPoseTransform
        {
            get { return inverseBindPose; }
            set { inverseBindPose = value; }
        }

    }
}
