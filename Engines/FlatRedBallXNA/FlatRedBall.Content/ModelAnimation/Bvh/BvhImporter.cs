/*
 * BvhImporter.cs
 * Copyright (c) 2006, 2007 Michael Nikonov, David Astle
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
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;
using System.Xml;
using System.Globalization;
#endregion

namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Imports BVH (Biovision hierarchical) animation data.
    /// </summary>
    [ContentImporter(".BVH", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName = "BVH - Animation Library")]
    internal sealed class BvhImporter : ContentImporter<BoneContent>
    {
        private char[] whiteSpace = { ' ', '\t', '\r', '\n' };
        private ContentImporterContext context;
        private StreamReader reader;
        private List<BoneInfo> bones;
        private ContentIdentity contentId;
        private int currentLine = 0;
        private BoneContent root;
        int frames = 0;
        double frameTime = 0.0;
        private ContentIdentity cId
        {
            get
            {
                contentId.FragmentIdentifier = "line " + currentLine.ToString();
                return contentId;
            }
        }

        public static float ParseFloat(string data)
        {
            return float.Parse(data, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Imports BVH (Biovision hierarchical) animation data.
        /// Stores animation data in root bone.
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {
            this.context = context;
            contentId = new ContentIdentity(filename, GetType().ToString());

            AnimationContent animation = new AnimationContent();
            animation.Name = Path.GetFileNameWithoutExtension(filename);
            animation.Identity = contentId;

            bones = new List<BoneInfo>();
            reader = new StreamReader(filename);
            String line;
            if ((line = readLine()) != "HIERARCHY")
            {
                throw new InvalidContentException("no HIERARCHY found", cId);
            }
            BoneContent bone = root;
            while ((line = readLine()) != "MOTION")
            {
                if (line == null)
                    throw new InvalidContentException("premature end of file", cId);
                string keyword = line.Split(whiteSpace)[0];
                if (keyword == "ROOT" || keyword == "JOINT" || line == "End Site")
                {
                    BoneInfo boneInfo = new BoneInfo();
                    BoneContent newBone = new BoneContent();
                    if (keyword == "JOINT" || line == "End Site")
                    {
                        bone.Children.Add(newBone);
                    }
                    if (keyword == "ROOT")
                    {
                        root = newBone;
                    }
                    if (keyword == "ROOT" || keyword == "JOINT")
                    {
                        newBone.Name = line.Split(whiteSpace)[1];
                        boneInfo.bone = newBone;
                        bones.Add(boneInfo);
                    }
                    else
                    {
                        newBone.Name = bone.Name + "End";
                    }
                    if ((line = readLine()) != "{")
                    {
                        throw new InvalidContentException("expected '{' but found " + line, cId);
                    }
                    line = readLine();
                    if (line!=null && line.StartsWith("OFFSET"))
                    {
                        string[] data = line.Split(whiteSpace);
                        //couldn't get the .NET 2.0 version of Split() working, 
                        //therefore this ugly hack
                        List<string> coords = new List<string>();
                        foreach (string s in data)
                        {
                            if (s != "OFFSET" && s != "" && s != null)
                            {
                                coords.Add(s);
                            }
                        }
                        Vector3 v = new Vector3();
                        v.X = ParseFloat(coords[0]);
                        v.Y = ParseFloat(coords[1]);
                        v.Z = ParseFloat(coords[2]);
                        Matrix offset = Matrix.CreateTranslation(v);
                        newBone.Transform = offset;
                    }
                    else
                    {
                        throw new InvalidContentException("expected 'OFFSET' but found " + line, cId);
                    }
                    if (keyword == "ROOT" || keyword == "JOINT")
                    {
                        line = readLine();
                        if (line != null && line.StartsWith("CHANNELS"))
                        {
                            string[] channels = line.Split(whiteSpace);
                            for (int i = 2; i < channels.Length; i++)
                            {
                                if (channels[i]!=null && channels[i]!="")
                                    boneInfo.Add(channels[i]);
                            }
                        }
                        else
                        {
                            throw new InvalidContentException("expected 'CHANNELS' but found " + line, cId);
                        }
                    }
                    bone = newBone;
                }
                if (line == "}")
                {
                    bone = (BoneContent)bone.Parent;
                }
            }
            if ((line = readLine()) != null)
            {
                string[] data = line.Split(':');
                if (data[0] == "Frames")
                {
                    frames = int.Parse(data[1].Trim());
                }
            }
            if ((line = readLine()) != null)
            {
                string[] data = line.Split(':');
                if (data[0] == "Frame Time")
                {
                    frameTime = double.Parse(data[1].Trim());
                }
            }
            animation.Duration = TimeSpan.FromSeconds(frameTime * frames);
            root.Animations.Add(animation.Name, animation);
            foreach (BoneInfo b in bones)
            {
                animation.Channels.Add(b.bone.Name, new AnimationChannel());
            }
            int frameNumber = 0;
            while ((line = readLine()) != null)
            {
                string[] ss = line.Split(whiteSpace);
                //couldn't get the .NET 2.0 version of Split() working, 
                //therefore this ugly hack
                List<string> data = new List<string>();
                foreach (string s in ss)
                {
                    if (s != "" && s != null)
                    {
                        data.Add(s);
                    }
                }
                int i = 0;
                foreach (BoneInfo b in bones)
                {
                    foreach (string channel in b.channels)
                    {
                        b.channelValues[channel] = ParseFloat(data[i]);
                        ++i;
                    }
                }
                foreach (BoneInfo b in bones)
                {
                    // Many applications export BVH in such a way that bone translation 
                    // needs to be aplied in every frame.
                    Matrix translation = b.bone.Transform;
                    Vector3 t = new Vector3();
                    t.X = b["Xposition"];
                    t.Y = b["Yposition"];
                    t.Z = b["Zposition"];
                    if (t.Length() != 0.0f)
                    {
                        // Some applications export BVH with translation channels for every bone. 
                        // In this case, bone translation should not be applied.
                        translation = Matrix.CreateTranslation(t);
                    }
                    Quaternion r = Quaternion.Identity;
                    // get rotations in correct order
                    foreach (string channel in b.channels)
                    {
                        float angle = MathHelper.ToRadians(b[channel]);
                        if (channel.Equals("Xrotation", StringComparison.InvariantCultureIgnoreCase))
                            r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);
                        if (channel.Equals("Yrotation", StringComparison.InvariantCultureIgnoreCase))
                            r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
                        if (channel.Equals("Zrotation", StringComparison.InvariantCultureIgnoreCase))
                            r = r * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle);
                    }
                    Matrix m = Matrix.CreateFromQuaternion(r) * translation;
                    TimeSpan time = TimeSpan.FromSeconds(frameTime * frameNumber);
                    AnimationKeyframe keyframe = new AnimationKeyframe(time, m);
                    animation.Channels[b.bone.Name].Add(keyframe);
                    ++i;
                }
                ++frameNumber;
            }
            root.Identity = contentId;
            return root;
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

    internal class BoneInfo
    {
        public BoneContent bone;
        public Dictionary<string, float> channelValues=new Dictionary<string, float>();
        public List<string> channels = new List<string>();

        public void Add(string channel)
        {
            channels.Add(channel.ToLower());
            channelValues.Add(channel.ToLower(), 0.0f);
        }

        public float this[string channel]
        {
            get
            {
                if (channelValues.ContainsKey(channel.ToLower()))
                    return channelValues[channel.ToLower()];
                else
                    return 0.0f;
            }
        }
    }
}
