/*
 * AsfImporter.cs
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
    /// Imports Acclaim ASF (motion capture skeleton).
    /// Stores DOF spec (degrees of freedom) as string tagged "dof" in OpaqueData for each bone.
    /// </summary>
    [ContentImporter(".ASF", CacheImportedData = true, DefaultProcessor = "AnimationProcessor",
        DisplayName = "Acclaim ASF - Animation Library")]
    internal sealed class AsfImporter : ContentImporter<BoneContent>
    {
        private ContentImporterContext context;
        private StreamReader reader;
        private NamedValueDictionary<BoneContent> bones;
        private ContentIdentity contentId;
        private int currentLine=0;
        private ContentIdentity cId
        {
            get
            {
                contentId.FragmentIdentifier = "line " + currentLine.ToString();
                return contentId;
            }
        }
        public NamedValueDictionary<BoneContent> Bones
        {
            get { return bones; }
        }

        /// <summary>
        /// Imports Acclaim AFS (motion capture skeleton).
        /// Stores dof spec (degrees of freedom) as string in OpaqueData for each bone.
        /// </summary>
        public override BoneContent Import(string filename, ContentImporterContext context)
        {
            this.context = context;
            contentId = new ContentIdentity(filename);
            BoneContent root = new BoneContent();
            root.Name = "root";
            root.Identity = cId;
            root.Transform = Matrix.Identity;
            bones = new NamedValueDictionary<BoneContent>();
            bones.Add("root", root);
            if (!File.Exists(filename)) 
                throw new InvalidContentException("file " + filename + " not found");
            reader = new StreamReader(filename);
            String line;
            while ((line = readLine()) != ":bonedata")
            {
                if (line == null) throw new InvalidContentException("no bone data found", cId);
            }
            BoneContent bone;
            while ((bone=importBone()) != null)
            {
                bones.Add(bone.Name, bone);
            }
            importHierarchy();
            foreach (BoneContent b in bones.Values)
            {
                if (b.Name != "root" && b.Parent == null)
                {
                    throw new InvalidContentException("incomplete hierarchy - bone " + b + " has no parent", cId);
                }
            }
            return root;
        }

        private string readLine()
        {
            string line=reader.ReadLine();
            ++currentLine;
            if (line != null) 
                line=line.Trim();
            return line;
        }

        private BoneContent importBone()
        {
            /* example (dof and limits are optional): 
             * 
             *   begin
                     id 2 
                     name lfemur
                     direction 0.34202 -0.939693 0  
                     length 7.16147  
                     axis 0 0 20  XYZ
                    dof rx ry rz
                    limits (-160.0 20.0)
                           (-70.0 70.0)
                           (-60.0 70.0)
                  end
                */
            BoneContent bone = new BoneContent();
            String line;
            line = readLine();
            if (line == ":hierarchy") 
                return null;
            if (line!="begin")
                throw new InvalidContentException("no hierarchy found", cId);
            line = readLine().Trim(); //id 1
            bone.Name = importStrings("name")[0];
            bone.Identity = cId;
            float[] direction = importFloats("direction");
            float length = importFloats("length")[0];
            string[] axis = importStrings("axis");

            // now skip optional "limits" as we don't need them
            for (int i = 0; i < 5; i++)
            {
                line = readLine();
                if (line == null)
                    throw new InvalidContentException("melformed bone data for bone " + bone.Name, cId);
                if (line == "end")
                    break;
                if (line.Contains("dof "))
                {
                    string[] dof = line.Substring(4).Split(' ');
                    bone.OpaqueData.Add("dof", dof);
                }
            }
            Vector3 v = new Vector3(direction[0], direction[1], direction[2]);
            Matrix m = Matrix.Identity;
            m.Translation = v * length;
            bone.Transform = m;
            return bone;
        }

        private string[] importStrings(string keyword)
        {
            string line = readLine();
            if (line == null)
                throw new InvalidContentException("premature end of file", cId);
            string[] tokens = line.Split(' ');
            if (tokens[0] != keyword)
                throw new InvalidContentException("expected '" + keyword + "' but found '" + tokens[0]+"'", cId);
            return line.Substring(tokens[0].Length + 1).Trim().Split(' ');
        }

        private float[] importFloats(string keyword)
        {
            string[] strings = importStrings(keyword);
            float[] f=new float[strings.Length];
            for (int i = 0; i < strings.Length; i++)
            {
                f[i] = float.Parse(strings[i], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat);
            }
            return f;
        }

        private void importHierarchy()
        {
            string line;
            line = readLine(); //begin
            while ((line = readLine()) != null)
            {
                if (line == "end")
                    break;
                string[] s = line.Split(' ');
                string parent = s[0];
                if (!bones.ContainsKey(parent))
                    throw new InvalidContentException("unknown bone "+parent, cId);
                string[] children = line.Substring(parent.Length+1).Split(' ');
                foreach(string child in children)
                {
                    if (!bones.ContainsKey(child))
                        throw new InvalidContentException("unknown bone " + child, cId);
                    bones[parent].Children.Add(bones[child]);
                }
            }
        }
    }
}
