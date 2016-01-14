/*
 * AmcImporter.cs
 * Copyright (c) 2006, 2007 Michael Nikonov
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
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;
using System.Globalization;
#endregion

namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Imports Acclaim AMC (motion capture data).
    /// For a foo_bar.amc, expects skeleton in a file named foo.asf.
    /// Returns a skeleton with Animations in root bone. 
    /// </summary>
    [ContentImporter(".AMC", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName="Acclaim AMC - Animation Library")]
    internal sealed class AmcImporter : ContentImporter<BoneContent>
    {

        #region Member Variables
        private long ticksPerFrame = TimeSpan.TicksPerSecond/24;
        private NamedValueDictionary<BoneContent> bones;
        private ContentImporterContext context;
        private StreamReader reader;
        private ContentIdentity contentId;
        private int currentLine = 0;

        private ContentIdentity cId
        {
            get
            {
                contentId.FragmentIdentifier = "line " + currentLine.ToString();
                return contentId;
            }
        }
        #endregion            


        /// <summary>
        /// Imports Acclaim AMC (motion capture data).
        /// For a foo_bar.amc, expects skeleton in a file named foo.asf.
        /// Returns a skeleton with Animations in root bone. 
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {
            this.context = context;
            contentId = new ContentIdentity(filename, GetType().ToString());
            reader = new StreamReader(filename);

            AnimationContent animation = new AnimationContent();
            animation.Name = Path.GetFileNameWithoutExtension(filename);
            animation.Identity = contentId;

            string dir=Path.GetDirectoryName(filename);
            string asfFilename=animation.Name+".asf";
            if (animation.Name.Contains("_"))
            {
                //asfFilename = animation.Name.Split('_')[0] + ".asf";
            }
            asfFilename = dir+@"\"+asfFilename;
            context.Logger.LogWarning("", contentId, "using skeleton from {0}", asfFilename);
            AsfImporter asfImporter = new AsfImporter();
            BoneContent root = asfImporter.Import(asfFilename, context);
            bones = asfImporter.Bones;

            int frameNumber = 1;
            int maxFrameNumber = 0;
            string line;
            animation.Channels.Add("root", new AnimationChannel());
            while ((line = readLine()) != null)
            {
                if (line[0]!='#' && line[0]!=':')
                {
                    int fn = 0;
                    if (int.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out fn))
                    {
                        ++frameNumber;// = int.Parse(line, NumberStyles.Int, CultureInfo.InvariantCulture.NumberFormat);
                        maxFrameNumber = System.Math.Max(frameNumber, maxFrameNumber);
                        TimeSpan time = TimeSpan.FromTicks(frameNumber * ticksPerFrame);
                        animation.Channels["root"].Add(new AnimationKeyframe(time, Matrix.Identity));
                    }
                    else
                    {
                        string[] s=line.Split(' ');
                        string bone = s[0];
                        if (!animation.Channels.ContainsKey(bone))
                        {
                            animation.Channels.Add(bone, new AnimationChannel());
                        }
                        AnimationChannel channel = animation.Channels[bone];
                        AnimationKeyframe keyframe = importKeyframe(s, frameNumber);
                        if (keyframe!=null) 
                            channel.Add(keyframe);
                    }
                }
            }
            animation.Duration = TimeSpan.FromTicks(maxFrameNumber * ticksPerFrame);
            context.Logger.LogImportantMessage("imported {0} animation frames for {1} bones", 
                maxFrameNumber, animation.Channels.Count);
            root.Animations.Add(animation.Name, animation);
            return root;
        }

        /// <summary>
        ///  Imports a keyframe
        /// </summary>
        private AnimationKeyframe importKeyframe(string[] s, int frameNumber)
        {
            AnimationKeyframe keyframe = null;
            if (!bones.ContainsKey(s[0]))
                throw new InvalidContentException("skeleton does not have bone " + s[0], cId);
            BoneContent bone = bones[s[0]];
            if (bone.OpaqueData.ContainsKey("dof"))
            {
                string[] dof = (string[])bone.OpaqueData["dof"];
                int dataLength = s.Length - 1;
                if (dataLength != dof.Length)
                    throw new InvalidContentException("AFS DOF specifies "+dof.Length
                        +" values but AMC has "+dataLength, cId);
                Matrix transform = Matrix.Identity;
                Vector3 t=new Vector3();
                Quaternion r=Quaternion.Identity;
                for (int i = 0; i < dataLength; i++)
                {
                    float data = float.Parse(s[i + 1], 
                        NumberStyles.Float, 
                        CultureInfo.InvariantCulture.NumberFormat);
                    if (dof[i] == "tx")
                        t.X = data;
                    else if (dof[i] == "ty")
                        t.Y = data;
                    else if (dof[i] == "tz")
                        t.Z = data;
                    else if (dof[i] == "rx")
                        r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(data));
                    else if (dof[i] == "ry")
                        r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(data));
                    else if (dof[i] == "rz")
                        r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(data));
                }
                if (t.Length() == 0)
                {
                    t = bone.Transform.Translation;
                }
                transform = Matrix.CreateFromQuaternion(r) * Matrix.CreateTranslation(t);
                TimeSpan time = TimeSpan.FromTicks(frameNumber * ticksPerFrame);
                keyframe = new AnimationKeyframe(time, transform);
            }
            return keyframe;
        }

        private string readLine()
        {
            string line = reader.ReadLine();
            ++currentLine;
            if (line != null)
                line = line.Trim();
            return line;
        }
    }
}
