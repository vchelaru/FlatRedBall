/*
 * AnimatedModelProcessor.cs
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
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.IO;
using System.Collections;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content;
using System.Globalization;
using System.Xml;
using System.Collections.ObjectModel;
using System.ComponentModel;


namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Processes a NodeContent object that was imported by SkinnedModelImporter
    /// and attaches animation data to its tag
    /// </summary>
    [ContentProcessor(DisplayName="Model - Animation Library")]
    public class AnimatedModelProcessor : ModelProcessor
    {
        private ContentProcessorContext context;
        
        // stores all animations for the model
        private AnimationContentDictionary animations = new AnimationContentDictionary();
        private NodeContent input;
        private SkinInfoContentCollection[] skinInfo = null;
        private bool modelSplit = false;
        private BoneIndexer[] indexers = null;
        List<Matrix> absoluteMeshTransforms = null;
        List<MeshContent> meshes = new List<MeshContent>();

        private int numMeshes = 0;

        const int maximumNumberOfPCBones = 56;
        const int maximumNumberOfWiiBones = 40;

        /// <summary>Processes a SkinnedModelImporter NodeContent root</summary>
        /// <param name="input">The root of the X file tree</param>
        /// <param name="context">The context for this processor</param>
        /// <returns>A model with animation data on its tag</returns>
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            //try
            //{
                ModelSplitter splitter;
                if (context.TargetPlatform != TargetPlatform.Xbox360)
                {
                    splitter = new ModelSplitter(input, maximumNumberOfPCBones);
                }
                else
                {
                    splitter = new ModelSplitter(input, maximumNumberOfWiiBones);
                }
                modelSplit = splitter.Split();
                splitter = null;
                this.input = input;
                this.context = context;
                FindMeshes(input);
                indexers = new BoneIndexer[numMeshes];
                for (int i = 0; i < indexers.Length; i++)
                {
                    indexers[i] = new BoneIndexer();
                }
                foreach (MeshContent meshContent in meshes)
                {
                    CreatePaletteIndices(meshContent);
                }

                // Get the process model minus the animation data
                ModelContent c = base.Process(input, context);

                if (!modelSplit && input.OpaqueData.ContainsKey("AbsoluteMeshTransforms"))
                {
                    absoluteMeshTransforms =
                        (List<Matrix>)input.OpaqueData["AbsoluteMeshTransforms"];
                }
                else
                {

                    foreach (MeshContent mesh in meshes)
                    {
                        if (!ValidateMeshSkeleton(mesh))
                        {
                            context.Logger.LogWarning(null, mesh.Identity, "Warning: Mesh found that has a parent that exists as "
                                + "one of the bones in the skeleton attached to the mesh.  Change the mesh "
                                + "skeleton structure or use X - File Animation Library importer if transforms are incorrect.");
                        }
                    }

                }

                Dictionary<string, object> dict = new Dictionary<string, object>();



                // Attach the animation and skinning data to the models tag
                FindAnimations(input);
                // Test to see if any animations have zero duration
                foreach (AnimationContent anim in animations.Values)
                {
                    string errorMsg = "One or more AnimationContent objects have an extremely small duration.  If the animation "
                            + "was intended to last more than one frame, please add \n AnimTicksPerSecond \n{0} \nY; \n{1}\n to your .X "
                            + "file, where Y is a positive integer.";
                    if (anim.Duration.Ticks < ContentUtil.TICKS_PER_60FPS)
                    {
                        context.Logger.LogWarning("", anim.Identity, errorMsg, "{", "}");

                        break;
                    }
                }

                XmlDocument xmlDoc = ReadAnimationXML(input);

                if (xmlDoc != null)
                {
                    SubdivideAnimations(animations, xmlDoc);
                }

                AnimationContentDictionary processedAnims
                    = new AnimationContentDictionary();
                try
                {
                    foreach (KeyValuePair<string, AnimationContent> animKey in animations)
                    {
                        AnimationContent processedAnim = ProcessAnimation(animKey.Value);
                        processedAnims.Add(animKey.Key, processedAnim);

                    }
                    dict.Add("Animations", processedAnims);
                }
                catch
                {
                    throw new Exception("Error processing animations.");
                }

                foreach (ModelMeshContent meshContent in c.Meshes)
                    ReplaceBasicEffects(meshContent);
                skinInfo = ProcessSkinInfo(c);
                dict.Add("SkinInfo", skinInfo);
                c.Tag = dict;

                return c;
            //}
            //catch (Exception e)
            //{
            //    System.IO.File.WriteAllText(@"AnimatedModelProcessorError.txt", e.Message);
            //    throw e;
            //}

            return null;
        }


        private void FindMeshes (NodeContent root)
        {
            if (root is MeshContent)
            {
                MeshContent mesh = (MeshContent)root;
                mesh.OpaqueData.Add("MeshIndex", numMeshes);
                numMeshes++;
                meshes.Add(mesh);
            }
            foreach (NodeContent child in root.Children)
                FindMeshes(child);
        }

        private void CreatePaletteIndices(MeshContent mesh)
        {
            foreach (GeometryContent meshPart in mesh.Geometry)
            {
                int meshIndex = (int)mesh.OpaqueData["MeshIndex"];
                BoneIndexer indexer = indexers[meshIndex];
                foreach (VertexChannel channel in meshPart.Vertices.Channels)
                {
                    if (channel.Name == VertexChannelNames.Weights())
                    {
                        VertexChannel<BoneWeightCollection> vc =
                            (VertexChannel<BoneWeightCollection>)channel;
                        foreach (BoneWeightCollection boneWeights in vc)
                        {
                            foreach (BoneWeight weight in boneWeights)
                            {
                                indexer.GetBoneIndex(weight.BoneName);
                            }
                        }
                    }
                }
            }
        }


        // returns true if the model contains meshes that have a parent bone as a child of 
        // a bone in the skeleton attached to the mesh.
        private bool ValidateMeshSkeleton (MeshContent meshContent)
        {
            List<string> meshParentHierarchy = new List<string>();
            int meshIndex = (int)meshContent.OpaqueData["MeshIndex"];
            BoneIndexer indexer = indexers[meshIndex];
            if (meshContent.Parent != null && indexer.SkinnedBoneNames.Contains(meshContent.Parent.Name))
            {
                // Warning
                return false;
            }
            // skeleton is fine
            return true;

        }


        private void CalculateAbsoluteTransforms(ModelBoneContent bone, Matrix[] transforms)
        {
            if (bone.Parent == null)
                transforms[bone.Index] = bone.Transform;
            else
            {
                transforms[bone.Index] = bone.Transform * transforms[bone.Parent.Index];
            }
            foreach (ModelBoneContent child in bone.Children)
                CalculateAbsoluteTransforms(child, transforms);
        }



        private SkinInfoContentCollection[] ProcessSkinInfo(ModelContent model)
        {
            SkinInfoContentCollection[] info = new SkinInfoContentCollection[model.Meshes.Count];
            Dictionary<string, int> boneDict = new Dictionary<string,int>();

            //if (model.Bones.Count > maximumNumberOfPCBones)
            //{
            //    throw new Exception("There are too many bones! You have " + model.Bones.Count + " and you can only have " + maximumNumberOfPCBones);
            //}

            foreach (ModelBoneContent b in model.Bones)
            {
                if (b.Name != null && !boneDict.ContainsKey(b.Name))
                    boneDict.Add(b.Name, b.Index);
            }

            for (int i = 0; i < info.Length; i++)
            {
                info[i] = new SkinInfoContentCollection();
                BoneIndexer indexer = indexers[i];
                ReadOnlyCollection<string> skinnedBoneNames = indexer.SkinnedBoneNames;

                Matrix[] absoluteTransforms = new Matrix[model.Bones.Count];
                CalculateAbsoluteTransforms(model.Bones[0], absoluteTransforms);

                Matrix absoluteMeshTransform;
                if (absoluteMeshTransforms == null)
                {
                    absoluteMeshTransform = absoluteTransforms[model.Meshes[i].ParentBone.Index];
                }
                else
                {
                    absoluteMeshTransform = absoluteMeshTransforms[i];
                }

                for (int j = 0; j < skinnedBoneNames.Count; j++)
                {
                    string name = skinnedBoneNames[j];
                    SkinInfoContent content = new SkinInfoContent();

                    try
                    {
                        content.BoneIndex = boneDict[name];
                    }
                    catch (KeyNotFoundException knfe)
                    {
                        string error = "Could not find a bone by the name of " + name + ".  This is skinned bone index " + j + ".";

                        error += "\nThere are " + skinnedBoneNames.Count + " skinned bones:";

                        for (int boneIndex = 0; boneIndex < skinnedBoneNames.Count; boneIndex++)
                        {
                            error += "\n " + skinnedBoneNames[boneIndex];
                        }

                        error += "\nThere are " + model.Bones.Count + " bones:";

                        foreach (ModelBoneContent b in model.Bones)
                        {
                            error += "\n " + b.Name;
                        }

                        throw new KeyNotFoundException(error);
                    }
                        
                    content.PaletteIndex = indexer.GetBoneIndex(name);
                    content.InverseBindPoseTransform = absoluteMeshTransform *
                        Matrix.Invert(absoluteTransforms[boneDict[name]]);
                    content.BoneName = name;
                    info[i].Add(content);
                }

            }
            return info;
        }

        /// <summary>
        /// Gets the names of the bones that should be used by the palette.
        /// </summary>
        protected ReadOnlyCollection<SkinInfoContentCollection> SkinnedBones
        { get { return new ReadOnlyCollection<SkinInfoContentCollection>(skinInfo); } }



        /// <summary>
        /// Gets the processor context.
        /// </summary>
        protected ContentProcessorContext ProcessorContext
        { get { return context; } }

        /// <summary>
        /// Called when an AnimationContent is processed.
        /// </summary>
        /// <param name="animation">The AnimationContent to be processed.</param>
        /// <returns>The processed AnimationContent.</returns>
        protected virtual AnimationContent
            ProcessAnimation(AnimationContent animation)
        {
            // M * M = F
            // 
            AnimationProcessor ap = new AnimationProcessor();
            AnimationContent newAnim = ap.Interpolate(animation);
            newAnim.Name = animation.Name;
            return newAnim;
; 
        }

        /// <summary>
        /// Called when an XML document is read that specifies how animations
        /// should be split.
        /// </summary>
        /// <param name="animDict">The dictionary of animation name/AnimationContent
        /// pairs. </param>
        /// <param name="doc">The Xml document that contains info on how to split
        /// the animations.</param>
        protected virtual void SubdivideAnimations(
            AnimationContentDictionary animDict, XmlDocument doc)
        {
            string[] animNames = new string[animDict.Keys.Count];
            animDict.Keys.CopyTo(animNames, 0);
            if (animNames.Length == 0)
                return;

            // Traverse each xml node that represents an animation to be subdivided
            foreach (XmlNode node in doc)
            {
                XmlElement child = node as XmlElement;
                if (child == null || child.Name != "animation")
                    continue;

                string animName = null;
                if (child["name"] != null)
                {
                    // The name of the animation to be split
                    animName = child["name"].InnerText;
                }
                else if (child["index"] != null)
                {
                    animName = animNames[int.Parse(child["index"].InnerText)];
                }
                else
                {
                    animName = animNames[0];
                }

                // If the tickspersecond node is filled, use that to calculate seconds per tick
                double animTicksPerSecond = 1.0, secondsPerTick = 0;
                try
                {
                    if (child["tickspersecond"] != null)
                    {
                        animTicksPerSecond = double.Parse(child["tickspersecond"].InnerText);
                    }
                }
                catch
                {
                    throw new Exception("Error parsing tickspersecond in xml file.");
                }
                if (animTicksPerSecond <= 0)
                    throw new InvalidDataException("AnimTicksPerSecond in XML file must be " +
                        "a positive number.");
                secondsPerTick = 1.0 / animTicksPerSecond;

                AnimationContent anim = null;
                // Get the animation and remove it from the dict
                // Check to see if the animation specified in the xml file exists
                try
                {
                    anim = animDict[animName];
                }
                catch
                {
                    throw new Exception("Animation named " + animName + " specified in XML file does not exist in model.");
                }
                animDict.Remove(anim.Name);
                // Get the list of new animations
                XmlNodeList subAnimations = child.GetElementsByTagName("animationsubset");

                foreach (XmlElement subAnim in subAnimations)
                {
                    // Create the new sub animation
                    AnimationContent newAnim = new AnimationContent();
                    XmlElement subAnimNameElement = subAnim["name"];

                    if (subAnimNameElement != null)
                        newAnim.Name = subAnimNameElement.InnerText;

                    // If a starttime node exists, use that to get the start time
                    long startTime, endTime;
                    if (subAnim["starttime"] != null)
                    {
                        try
                        {
                            startTime = TimeSpan.FromSeconds(double.Parse(subAnim["starttime"].InnerText)).Ticks;
                        }
                        catch
                        {
                            throw new Exception("Error parsing starttime node in XML file.  Node inner text "
                                + "must be a non negative number.");
                        }
                    }
                    else if (subAnim["startframe"] != null)// else use the secondspertick combined with the startframe node value
                    {
                        try
                        {
                            double seconds =
                                double.Parse(subAnim["startframe"].InnerText) * secondsPerTick;

                            startTime = TimeSpan.FromSeconds(
                                seconds).Ticks;
                        }
                        catch
                        {
                            throw new Exception("Error parsing startframe node in XML file.  Node inner text "
                              + "must be a non negative number.");
                        }
                    }
                    else
                        throw new Exception("Sub animation in XML file must have either a starttime or startframe node.");

                    // Same with endtime/endframe
                    if (subAnim["endtime"] != null)
                    {
                        try
                        {
                            endTime = TimeSpan.FromSeconds(double.Parse(subAnim["endtime"].InnerText)).Ticks;
                        }
                        catch
                        {
                            throw new Exception("Error parsing endtime node in XML file.  Node inner text "
                                + "must be a non negative number.");
                        }
                    }
                    else if (subAnim["endframe"] != null)
                    {
                        try
                        {
                            double seconds = double.Parse(subAnim["endframe"].InnerText)
                                * secondsPerTick;
                            endTime = TimeSpan.FromSeconds(
                                seconds).Ticks;
                        }
                        catch
                        {
                            throw new Exception("Error parsing endframe node in XML file.  Node inner text "
                                + "must be a non negative number.");
                        }
                    }
                    else
                        throw new Exception("Sub animation in XML file must have either an endtime or endframe node.");

                    if (endTime < startTime)
                        throw new Exception("Start time must be <= end time in XML file.");

                    // Now that we have the start and end times, we associate them with
                    // start and end indices for each animation track/channel
                    foreach (KeyValuePair<string, AnimationChannel> k in anim.Channels)
                    {
                        // The current difference between the start time and the
                        // time at the current index
                        long currentStartDiff;
                        // The current difference between the end time and the
                        // time at the current index
                        long currentEndDiff;
                        // The difference between the start time and the time
                        // at the start index
                        long bestStartDiff=long.MaxValue;
                        // The difference between the end time and the time at
                        // the end index
                        long bestEndDiff=long.MaxValue;

                        // The start and end indices
                        int startIndex = -1;
                        int endIndex = -1;

                        // Create a new channel and reference the old channel
                        AnimationChannel newChan = new AnimationChannel();
                        AnimationChannel oldChan = k.Value;

                        // Iterate through the keyframes in the channel
                        for (int i = 0; i < oldChan.Count; i++)
                        {
                            // Update the startIndex, endIndex, bestStartDiff,
                            // and bestEndDiff
                            long ticks = oldChan[i].Time.Ticks;
                            currentStartDiff = System.Math.Abs(startTime - ticks);
                            currentEndDiff = System.Math.Abs(endTime - ticks);
                            if (startIndex == -1 || currentStartDiff<bestStartDiff)
                            {
                                startIndex = i;
                                bestStartDiff = currentStartDiff;
                            }
                            if (endIndex == -1 || currentEndDiff<bestEndDiff)
                            {
                                endIndex = i;
                                bestEndDiff = currentEndDiff;
                            }
                        }


                        // Now we have our start and end index for the channel
                        for (int i = startIndex; i <= endIndex; i++)
                        {
                            AnimationKeyframe frame = oldChan[i];
                            long time;
                            // Clamp the time so that it can't be less than the
                            // start time
                            if (frame.Time.Ticks < startTime)
                                time = 0;
                            // Clamp the time so that it can't be greater than the
                            // end time
                            else if (frame.Time.Ticks > endTime)
                                time = endTime - startTime;
                            else // Else get the time
                                time = frame.Time.Ticks - startTime;

                            // Finally... create the new keyframe and add it to the new channel
                            AnimationKeyframe keyframe = new AnimationKeyframe(
                                TimeSpan.FromTicks(time),
                                frame.Transform);
                            
                            newChan.Add(keyframe);
                        }
                        
                        // Add the channel and update the animation duration based on the
                        // length of the animation track.
                        newAnim.Channels.Add(k.Key, newChan);
                        if (newChan[newChan.Count - 1].Time > newAnim.Duration)
                            newAnim.Duration = newChan[newChan.Count - 1].Time;


                    }
                    try
                    {
                        // Add the subdived animation to the dictionary.
                        animDict.Add(newAnim.Name, newAnim);
                    }
                    catch
                    {
                        throw new Exception("Attempt to add an animation when one by the same name already exists. " +
                            "Name: " + newAnim.Name);
                    }
                }
                
            }
        }

        // Reads the XML document associated with the model if it exists.
        private XmlDocument ReadAnimationXML(NodeContent root)
        {
            XmlDocument doc = null;
            string filePath = Path.GetFullPath(root.Identity.SourceFilename);
            string fileName = Path.GetFileName(filePath);
            fileName = Path.GetDirectoryName(filePath);
            if (fileName!="")
                fileName += "\\";
            fileName += System.IO.Path.GetFileNameWithoutExtension(filePath)
                + "animation.xml";
            bool animXMLExists = File.Exists(fileName);
            if (animXMLExists)
            {
                doc = new XmlDocument();
                doc.Load(fileName);
            }
            return doc;
        }



        /// <summary>
        /// Called when a basic effect is encountered and potentially replaced by
        /// BasicPaletteEffect (if not overridden).  This is called afer effects have been processed.
        /// </summary>
        /// <param name="skinningType">The the skinning type of the meshpart.</param>
        /// <param name="meshPart">The MeshPart that contains the BasicMaterialContent.</param>
        protected virtual void ReplaceBasicEffect(SkinningType skinningType,
            ModelMeshPartContent meshPart)
        {
            BasicMaterialContent basic = meshPart.Material as BasicMaterialContent;
            if (basic != null)
            {
                // Create a new PaletteSourceCode object and set its palette size
                // based on the platform since xbox has fewer registers.
                PaletteSourceCode source;
                if (context.TargetPlatform != TargetPlatform.Xbox360)
                {
                    source = new PaletteSourceCode(56);
                }
                else
                {
                    source = new PaletteSourceCode(40);
                }
                // Process the material and set the meshPart material to the new
                // material.
                PaletteInfoProcessor processor = new PaletteInfoProcessor();
                meshPart.Material = processor.Process(
                    new PaletteInfo(source.SourceCode4BonesPerVertex,
                    source.PALETTE_SIZE, basic), context);
            }
        }

        // Go through the modelmeshes and replace all basic effects for skinned models
        // with BasicPaletteEffect.
        private void ReplaceBasicEffects(ModelMeshContent input)
        {
            foreach (ModelMeshPartContent part in input.MeshParts)
            {
#if XNA4
                SkinningType skinType = ContentUtil.GetSkinningType(part.VertexBuffer.VertexDeclaration.VertexElements);
#else
                SkinningType skinType = ContentUtil.GetSkinningType(part.GetVertexDeclaration());
#endif
                if (skinType != SkinningType.None)
                {
                    ReplaceBasicEffect(skinType, part);
                }
            }
        }


        /// <summary>
        /// Searches through the NodeContent tree for all animations and puts them in
        /// one AnimationContentDictionary
        /// </summary>
        /// <param name="node">The root of the tree</param>
        private void FindAnimations(NodeContent node)
        {
            
            foreach (KeyValuePair<string, AnimationContent> k in node.Animations)
            {
                if (animations.ContainsKey(k.Key))
                {
                    foreach (KeyValuePair<string, AnimationChannel> c in k.Value.Channels)
                    {
                        animations[k.Key].Channels.Add(c.Key, c.Value);
                    }
                }
                else
                {
                    animations.Add(k.Key, k.Value);
                }
            }
            
            foreach (NodeContent child in node.Children)
                FindAnimations(child);
        }
        
        /// <summary>
        /// Go through the vertex channels in the geometry and replace the 
        /// BoneWeightCollection objects with weight and index channels.
        /// </summary>
        /// <param name="geometry">The geometry to process.</param>
        /// <param name="vertexChannelIndex">The index of the vertex channel to process.</param>
        /// <param name="context">The processor context.</param>
        protected override void ProcessVertexChannel(GeometryContent geometry, int vertexChannelIndex, ContentProcessorContext context)
        {
            bool boneCollectionsWithZeroWeights = false;
            if (geometry.Vertices.Channels[vertexChannelIndex].Name == VertexChannelNames.Weights())
            {
                int meshIndex = (int)geometry.Parent.OpaqueData["MeshIndex"];
                BoneIndexer indexer = indexers[meshIndex];
                // Skin channels are passed in from importers as BoneWeightCollection objects
                VertexChannel<BoneWeightCollection> vc = 
                    (VertexChannel<BoneWeightCollection>)
                    geometry.Vertices.Channels[vertexChannelIndex];
                int maxBonesPerVertex = 0;
                for (int i = 0; i < vc.Count; i++)
                {
                    int count = vc[i].Count;
                    if (count > maxBonesPerVertex)
                        maxBonesPerVertex = count;
                }

                // Add weights as colors (Converts well to 4 floats)
                // and indices as packed 4byte vectors.
                Color[] weightsToAdd = new Color[vc.Count];
                Byte4[] indicesToAdd = new Byte4[vc.Count];

                // Go through the BoneWeightCollections and create a new
                // weightsToAdd and indicesToAdd array for each BoneWeightCollection.
                for (int i = 0; i < vc.Count; i++)
                {
                    
                    BoneWeightCollection bwc = vc[i];

                    if (bwc.Count == 0)
                    {
                        boneCollectionsWithZeroWeights = true;
                        continue;
                    }

                    bwc.NormalizeWeights(4);
                    int count = bwc.Count;
                    if (count>maxBonesPerVertex)
                        maxBonesPerVertex = count;

                    // Add the appropriate bone indices based on the bone names in the
                    // BoneWeightCollection
                    Vector4 bi = new Vector4();
                    bi.X = count > 0 ? indexer.GetBoneIndex(bwc[0].BoneName) : (byte)0;
                    bi.Y = count > 1 ? indexer.GetBoneIndex(bwc[1].BoneName) : (byte)0;
                    bi.Z = count > 2 ? indexer.GetBoneIndex(bwc[2].BoneName) : (byte)0;
                    bi.W = count > 3 ? indexer.GetBoneIndex(bwc[3].BoneName) : (byte)0;


                    indicesToAdd[i] = new Byte4(bi);
                    Vector4 bw = new Vector4();
                    bw.X = count > 0 ? bwc[0].Weight : 0;
                    bw.Y = count > 1 ? bwc[1].Weight : 0;
                    bw.Z = count > 2 ? bwc[2].Weight : 0;
                    bw.W = count > 3 ? bwc[3].Weight : 0;
                    weightsToAdd[i] = new Color(bw);
                }

                // Remove the old BoneWeightCollection channel
                geometry.Vertices.Channels.Remove(vc);
                // Add the new channels
                geometry.Vertices.Channels.Add<Byte4>(VertexElementUsage.BlendIndices.ToString(), indicesToAdd);
                geometry.Vertices.Channels.Add<Color>(VertexElementUsage.BlendWeight.ToString(), weightsToAdd);
            }
            else
            {
                // No skinning info, so we let the base class process the channel
                base.ProcessVertexChannel(geometry, vertexChannelIndex, context);
            }
            if (boneCollectionsWithZeroWeights)
                context.Logger.LogWarning("", geometry.Identity,
                    "BonesWeightCollections with zero weights found in geometry.");
        }
    }
}


