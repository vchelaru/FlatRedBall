/*
 * ContentUtil.cs
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
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Info on how a model is skinned.
    /// </summary>
    public enum SkinningType
    {
        /// <summary>
        /// No skinning.
        /// </summary>
        None,
        /// <summary>
        /// A max of four influences per vertex.
        /// </summary>
        FourBonesPerVertex,
        /// <summary>
        /// A max of eight influences per vertex.
        /// </summary>
        EightBonesPerVertex,
        /// <summary>
        /// A max of twelve influences per vertex.
        /// </summary>
        TwelveBonesPerVertex
    }

    /// <summary>
    /// Contains utility functions for the content pipeline relating to animation.
    /// </summary>
    public static class ContentUtil
    {
        // The current version of the library
        internal const string VERSION = "1.0.2.0";
        /// <summary>
        /// Ticks per frame at 60 frames per second.
        /// </summary>
        public const long TICKS_PER_60FPS = TimeSpan.TicksPerSecond / 60;


        /// <summary>
        /// Gets info on what skinning info a vertex element array contains.
        /// </summary>
        /// <param name="elements">The vertex elements.</param>
        /// <returns>Info on what type of skinning the elements contain.</returns>
#if XNA4
        public static SkinningType GetSkinningType(System.Collections.ObjectModel.Collection<VertexElement> elements)
#else
        public static SkinningType GetSkinningType(VertexElement[] elements)
#endif
        {
            int numIndexChannels = 0;
            int numWeightChannels = 0;
            foreach (VertexElement e in elements)
            {
                if (e.VertexElementUsage == VertexElementUsage.BlendIndices)
                    numIndexChannels++;
                else if (e.VertexElementUsage == VertexElementUsage.BlendWeight)
                    numWeightChannels++;
            }
            if (numIndexChannels == 3 || numWeightChannels == 3)
                return SkinningType.TwelveBonesPerVertex;
            else if (numIndexChannels == 2 || numWeightChannels == 2)
                return SkinningType.EightBonesPerVertex;
            else if (numIndexChannels == 1 || numWeightChannels == 1)
                return SkinningType.FourBonesPerVertex;
            return SkinningType.None;

        }



        /// <summary>
        /// Reflects a matrix across the Z axis by multiplying both the Z
        /// column and the Z row by -1 such that the Z,Z element stays intact.
        /// </summary>
        /// <param name="m">The matrix to be reflected across the Z axis</param>
        public static void ReflectMatrix(ref Matrix m)
        {
            m.M13 *= -1;
            m.M23 *= -1;
            m.M33 *= -1;
            m.M43 *= -1;
            m.M31 *= -1;
            m.M32 *= -1;
            m.M33 *= -1;
            m.M34 *= -1;
        }


        private static void SortFrames(ref AnimationKeyframe[] frames)
        {

            Array.Sort<AnimationKeyframe>(frames, new Comparison<AnimationKeyframe>(
                delegate(AnimationKeyframe one, AnimationKeyframe two)
                {
                    return one.Time.CompareTo(two.Time);
                }));

        }

        /// <summary>
        /// Merges scale, translation, and rotation keyframes into matrix keyframes.
        /// </summary>
        /// <param name="scale">The scale keyframes.</param>
        /// <param name="translation">The translation keyframes.</param>
        /// <param name="rotation">The rotation keyframes.</param>
        /// <returns>The merged matrix keyframes.</returns>
        public static List<AnimationKeyframe> MergeKeyFrames(AnimationKeyframe[] scale,
    AnimationKeyframe[] translation, AnimationKeyframe[] rotation)
        {

            if (scale == null)
            {
                scale = new AnimationKeyframe[] {new AnimationKeyframe(new TimeSpan(0),
                    Matrix.Identity)};
            }
            if (translation == null)
                throw new Exception("Animation data is not stored as matrices and " +
                    "has no translation component.");
            if (rotation == null)
                throw new Exception("Animation data is not stored as matrices and " +
                    "has no rotation component");

            // Sort the frames by time and make sure they start at time 0 and
            // have length >= 1
            InitializeFrames(ref scale);
            InitializeFrames(ref translation);
            InitializeFrames(ref rotation);

            // Get a sorted list of the timespans for all 3 keyframe types,
            // not counting duplicates
            SortedList<TimeSpan, object> keyframeTimes
                = new SortedList<TimeSpan, object>();
            foreach (AnimationKeyframe frame in scale)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            foreach (AnimationKeyframe frame in translation)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);
            foreach (AnimationKeyframe frame in rotation)
                if (!keyframeTimes.ContainsKey(frame.Time))
                    keyframeTimes.Add(frame.Time, null);


            IList<TimeSpan> times = keyframeTimes.Keys;

            // Allocate the interpolated frame matrices
            Matrix[] newScales = new Matrix[keyframeTimes.Count];
            Matrix[] newTrans = new Matrix[keyframeTimes.Count];
            Matrix[] newRot = new Matrix[keyframeTimes.Count];
            List<AnimationKeyframe> returnFrames = new List<AnimationKeyframe>();

            // Interpolate the frames based on the times
            InterpFrames(ref scale, ref newScales, times);
            InterpFrames(ref translation, ref newTrans, times);
            InterpFrames(ref rotation, ref newRot, times);

            // Merge the 3 keyframe types into one.
            for (int i = 0; i < times.Count; i++)
            {
                Matrix m = newRot[i];
                m = m * newTrans[i];
                m = newScales[i] * m;
                returnFrames.Add(new AnimationKeyframe(times[i], m));
            }

            return returnFrames;

        }


        private static void InitializeFrames(ref AnimationKeyframe[] frames)
        {
            SortFrames(ref frames);
            if (frames[0].Time != TimeSpan.Zero)
            {
                AnimationKeyframe[] newFrames = new AnimationKeyframe[frames.Length + 1];
                Array.ConstrainedCopy(frames, 0, newFrames, 1, frames.Length);
                newFrames[0] = frames[0];
                frames = newFrames;
            }
        }


        private static Quaternion qStart, qEnd, qResult;
        private static Vector3 curTrans, nextTrans, lerpedTrans;
        private static Vector3 curScale, nextScale, lerpedScale;
        private static Matrix startRotation, endRotation;
        private static Matrix returnMatrix;

        /// <summary>
        /// Roughly decomposes two matrices and performs spherical linear interpolation
        /// </summary>
        /// <param name="start">Source matrix for interpolation</param>
        /// <param name="end">Destination matrix for interpolation</param>
        /// <param name="slerpAmount">Ratio of interpolation</param>
        /// <returns>The interpolated matrix</returns>
        public static Matrix SlerpMatrix(Matrix start, Matrix end,
            float slerpAmount)
        {
            // Get rotation components and interpolate (not completely accurate but I don't want 
            // to get into polar decomposition and this seems smooth enough)
            Quaternion.CreateFromRotationMatrix(ref start, out qStart);
            Quaternion.CreateFromRotationMatrix(ref end, out qEnd);
            Quaternion.Lerp(ref qStart, ref qEnd, slerpAmount, out qResult);

            // Get final translation components
            curTrans.X = start.M41;
            curTrans.Y = start.M42;
            curTrans.Z = start.M43;
            nextTrans.X = end.M41;
            nextTrans.Y = end.M42;
            nextTrans.Z = end.M43;
            Vector3.Lerp(ref curTrans, ref nextTrans, slerpAmount, out lerpedTrans);

            // Get final scale component
            Matrix.CreateFromQuaternion(ref qStart, out startRotation);
            Matrix.CreateFromQuaternion(ref qEnd, out endRotation);
            curScale.X = start.M11 - startRotation.M11;
            curScale.Y = start.M22 - startRotation.M22;
            curScale.Z = start.M33 - startRotation.M33;
            nextScale.X = end.M11 - endRotation.M11;
            nextScale.Y = end.M22 - endRotation.M22;
            nextScale.Z = end.M33 - endRotation.M33;
            Vector3.Lerp(ref curScale, ref nextScale, slerpAmount, out lerpedScale);

            // Create the rotation matrix from the slerped quaternions
            Matrix.CreateFromQuaternion(ref qResult, out returnMatrix);

            // Set the translation
            returnMatrix.M41 = lerpedTrans.X;
            returnMatrix.M42 = lerpedTrans.Y;
            returnMatrix.M43 = lerpedTrans.Z;

            // And the lerped scale component
            returnMatrix.M11 += lerpedScale.X;
            returnMatrix.M22 += lerpedScale.Y;
            returnMatrix.M33 += lerpedScale.Z;
            return returnMatrix;
        }

        private static Matrix CalculateSquareRoot(Matrix Y, Matrix Z, int iterations)
        {
            if (iterations == 0)
                return Y;
            return CalculateSquareRoot(
                (Y + Matrix.Invert(Z)) / 2.0f,
                (Z + Matrix.Invert(Y)) / 2.0f,
                iterations - 1);
        }
        /// <summary>
        /// Calculates the square root of a matrix.
        /// </summary>
        /// <param name="A">The matrix.</param>
        /// <param name="iterations">The number of recursive iterations used by the
        /// calculation algorithm.</param>
        /// <returns>The calculated square root of the matrix.</returns>
        public static Matrix CalculateSquareRoot(Matrix A, int iterations)
        {


            return CalculateSquareRoot(A, Matrix.Identity, iterations);
        }

        // Interpolates a set of animation key frames to align with 
        // a set of times and copies it into the destination array.
        private static void InterpFrames(
            ref AnimationKeyframe[] source,
            ref Matrix[] dest,
            IList<TimeSpan> times)
        {
            // The index of hte source frame
            int sourceIndex = 0;
            for (int i = 0; i < times.Count; i++)
            {
                // Increment the index till the next index is greater than the current time
                while (sourceIndex != source.Length-1 && source[sourceIndex + 1].Time < times[i])
                {
                    sourceIndex++;
                }
                // If we are at the last index use the last transform for the rest of the times
                if (sourceIndex==source.Length-1)
                {
                    dest[i] = source[sourceIndex].Transform;
                    continue;
                }
                // If the keyframe time is equal to the current time use the keyframe transform
                if (source[sourceIndex].Time == times[i])
                {
                    dest[i] = source[sourceIndex].Transform;
                }
                else // else interpolate
                {
                    double interpAmount = ((double)times[i].Ticks - source[sourceIndex].Time.Ticks) /
                        ((double)source[sourceIndex + 1].Time.Ticks - source[sourceIndex].Time.Ticks);
                    Matrix m1 = source[sourceIndex].Transform;
                    Matrix m2 = source[sourceIndex + 1].Transform;
                    if (m1 == m2)
                        dest[i] = m1;
                    else
                        dest[i] = Matrix.Lerp(m1, m2, (float)interpAmount);

                }

            }

        }
    }
}
