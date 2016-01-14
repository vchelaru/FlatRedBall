/*
 * BoneIndexer.cs
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
using System.Collections.ObjectModel;

namespace FlatRedBall.Graphics.Model.Animation.Content
{

    /// <summary>
    /// Assists in creating palette indices for mesh bones.
    /// </summary>
    public sealed class BoneIndexer
    {
        private int currentIndex = 0;
        private Dictionary<string, int> dict = new Dictionary<string, int>();
        private List<string> skinnedBoneNames = new List<string>();

        /// <summary>
        /// The names of the skinned bones that have indices attached to this indexer.
        /// </summary>
        public ReadOnlyCollection<string> SkinnedBoneNames
        {
            get { return skinnedBoneNames.AsReadOnly(); }
        }

        /// <summary>
        /// True if an index has been created for the given bone.
        /// </summary>
        /// <param name="boneName">The name of the bone.</param>
        /// <returns>True if an index has been created for the given bone.</returns>
        public bool ContainsBone(string boneName)
        {
            return dict.ContainsKey(boneName);
        }

        /// <summary>
        /// Creates an index for a bone if one doesn't exist, and returns the palette
        /// index for the given bone.
        /// </summary>
        /// <param name="boneName">The name of the bone.</param>
        /// <returns>The matrix palette index of the bone.</returns>
        public byte GetBoneIndex(string boneName)
        {
            if (!dict.ContainsKey(boneName))
            {
                dict.Add(boneName, currentIndex);
                skinnedBoneNames.Add(boneName);
                currentIndex++;
                return (byte)(currentIndex - 1);
            }
            else
                return (byte)dict[boneName];
        }
        
    }
}
