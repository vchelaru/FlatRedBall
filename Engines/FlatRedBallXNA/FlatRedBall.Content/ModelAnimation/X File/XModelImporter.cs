/*
 * XModelImporter.cs
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
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using System.IO;
using System.Globalization;
#endregion

namespace FlatRedBall.Graphics.Model.Animation.Content
{
    /// <summary>
    /// Imports a directx model that contains skinning info.
    /// </summary>
    [ContentImporter(".Xmmmmmmmmmmmmmmmmmmmmmmm", CacheImportedData = true, DefaultProcessor="AnimatedModelProcessor",
        DisplayName="X File - Animation Library")]
    public partial class XModelImporter : ContentImporter<NodeContent>
    {
        #region Member Variables
        // The file that we are importing
        private string fileName;
        private XFileTokenizer tokens;
        // Stores the root frame
        private NodeContent root;

        private Dictionary<string, NodeContent> boneDict = new Dictionary<string, NodeContent>();

        // Stores the number of units that represent one second in animation data
        // Is null if the file contains no information, and a default value will be used
        private int? animTicksPerSecond;
        private const int DEFAULT_TICKS_PER_SECOND = 3500;
        // Stores information about the current build for the content pipeline
        private ContentImporterContext context;
        // A list of meshes that have been imported
        private List<XMeshImporter> meshes = new List<XMeshImporter>();
        private Dictionary<string, MaterialContent> materials = new Dictionary<string, MaterialContent>();
        private List<string> animationOptions = new List<string>();

        public List<string> AnimationOptions
        {
            get { return animationOptions; }
        }
        #endregion

        #region Non Animation Importation Methods
        public override NodeContent Import(string filename, ContentImporterContext context)
        {

            System.Threading.Thread currentThread = System.Threading.Thread.CurrentThread;
            CultureInfo culture = new CultureInfo("en-US");
            currentThread.CurrentCulture = culture;
            currentThread.CurrentUICulture = culture;
            this.fileName = filename;
            this.context = context;
            // Create an instance of a class that splits a .X file into tokens and provides
            // functionality for iterating and parsing the tokens
            tokens = new XFileTokenizer(filename);

            // skip header
            tokens.SkipTokens(3);
            root = new NodeContent();
            root.Identity = new ContentIdentity(fileName, "Animation Library XImporter");
            // fill in the tree
            ImportRoot();

            Matrix absoluteMeshTransform=Matrix.Identity;
            List<SkinTransformContent[]> skinTransforms = new List<SkinTransformContent[]>();
            List<Matrix> absMeshTransforms = new List<Matrix>();

            // Now that we have mapped bone names to their indices, we can create the vertices
            // in each mesh so that they contain indices and weights
            foreach (XMeshImporter mesh in meshes)
            {
                mesh.CreateGeometry();
                SkinTransformContent[] meshSkinTransforms = mesh.SkinTransforms;
                skinTransforms.Add(mesh.SkinTransforms);
                if (mesh.SkinTransforms!=null && mesh.SkinTransforms.Length > 0)
                {
                    absoluteMeshTransform = mesh.Mesh.AbsoluteTransform;
                }
                absMeshTransforms.Add(mesh.Mesh.AbsoluteTransform);
            }

            FindBones(root);
            root.OpaqueData.Add("AbsoluteMeshTransforms", absMeshTransforms);
            // Calculates bind pose as required for compatibility with Xna Content DOM
            Dictionary<string, Matrix> absTransformsDict = new Dictionary<string, Matrix>(skinTransforms.Count);
            int k = 0;
            foreach (SkinTransformContent[] sst in skinTransforms)
            {

                if (sst != null)
                {
                    absoluteMeshTransform = meshes[k].Mesh.AbsoluteTransform;
                    foreach (SkinTransformContent st in sst)
                    {
                        Matrix abs = Matrix.Invert(Matrix.Invert(absoluteMeshTransform) * st.Transform);

                        if (absTransformsDict.ContainsKey(st.BoneName) == false)
                        {
                            absTransformsDict.Add(st.BoneName, abs);
                        }
                    }
                }
                k++;
            }






            FixBindPose(root, absTransformsDict);

            return root;
        }

        private void FindBones(NodeContent root)
        {
            if (root.Name != null && !boneDict.ContainsKey(root.Name))
                boneDict.Add(root.Name, root);
            foreach (NodeContent child in root.Children)
                FindBones(child);
        }
        // Calculates bind pose as required for compatibility with Xna Content DOM
        private void FixBindPose(NodeContent bone, Dictionary<string, Matrix> absTransformsDict)
        {

            if (bone.Parent!=null && absTransformsDict.ContainsKey(bone.Name))
            {
                Matrix abs = absTransformsDict[bone.Name];
                bone.Transform = abs * Matrix.Invert(bone.Parent.AbsoluteTransform);
            }
             
            foreach (NodeContent child in bone.Children)
            {
                FixBindPose(child, absTransformsDict);
            }
        }

        /// <summary>
        /// Imports the root and animation data associated with it
        /// </summary>
        private void ImportRoot()
        {
            // Read all tokens in the file
            if (!tokens.AtEnd)
            {
                do
                {
                    string next = tokens.NextToken();
                    // If nodes are found in the same scope as the root, add them
                    // as children because the Model class only supports one root
                    // frame
                    if (next == "Frame")
                        if (root == null)
                        {
                            root = ImportNode();
                            root.Identity = new ContentIdentity();
                            root.Identity.SourceFilename = fileName;
                            root.Identity.SourceTool = this.GetType().ToString();
                        }
                        else
                        {
                            root.Children.Add(ImportNode());
                        }
                    //template AnimTicksPerSecond
                    // {
                    //     DWORD AnimTicksPerSecond;
                    // } 
                    else if (next == "AnimTicksPerSecond")
                    {
                        animTicksPerSecond = tokens.SkipName().NextInt();
                        tokens.SkipToken();
                    }
                    // See ImportAnimationSet for template info
                    else if (next == "AnimationSet")
                        ImportAnimationSet();
                    else if (next == "Material")
                        ImportMaterial();
                    else if (next == "template")
                        tokens.SkipName().SkipNode();
                    else if (next == "AnimationLibOptions")
                    {
                        tokens.SkipName();
                        int numOptions = tokens.NextInt();
                        for (int i = 0; i < numOptions; i++)
                        {
                            string animationOption = tokens.NextString();
                            animationOptions.Add(animationOption);
                        }
                        tokens.SkipToken().SkipToken();
                    }
                }
                while (!tokens.AtEnd);
            }
    
        }

        /// <summary>
        /// Loads a custom material.  That is, loads a material with a custom effect.
        /// </summary>
        /// <returns>The custom material</returns>
        private MaterialContent ImportCustomMaterial()
        {
            EffectMaterialContent content = new EffectMaterialContent();
            tokens.SkipName();
            string effectName = GetAbsolutePath(tokens.NextString());
            content.Effect = new ExternalReference<EffectContent>(effectName);

            // Find value initializers for the effect parameters and set the values
            // as indicated
            for (string token = tokens.NextToken(); token != "}"; token = tokens.NextToken())
            {
                if (token == "EffectParamFloats")
                {
                    tokens.SkipName();
                    string floatsParamName = tokens.NextString();
                    int numFloats = tokens.NextInt();
                    float[] floats = new float[numFloats];
                    for (int i = 0; i < numFloats; i++)
                        floats[i] = tokens.NextFloat();
                    tokens.SkipToken();
                    content.OpaqueData.Add(floatsParamName, floats);
                }
                else if (token == "EffectParamDWord")
                {
                    tokens.SkipName();
                    string dwordParamName = tokens.NextString();
                    float dword = tokens.NextFloat();
                    tokens.SkipToken();
                    content.OpaqueData.Add(dwordParamName, dword);
                }
                else if (token == "EffectParamString")
                {
                    tokens.SkipName();
                    string stringParamName = tokens.NextString();
                    string paramValue = tokens.NextString();
                    tokens.SkipToken();
                    content.OpaqueData.Add(stringParamName, paramValue);
                }
                if (token == "{")
                    tokens.SkipNode();
            }
            return content;

        }

        // template Material
        // {
        //      ColorRGBA faceColor;
        //      FLOAT power;
        //      ColorRGB specularColor;
        //      ColorRGB emissiveColor;
        //      [...]
        // } 
        /// <summary>
        /// Imports a material, which defines the textures that a mesh uses and the way in which
        /// light reflects off the mesh
        /// </summary>
        private MaterialContent ImportMaterial()
        {
            ExternalReference<TextureContent> texRef = null;
            BasicMaterialContent basicMaterial = new BasicMaterialContent();
            MaterialContent returnMaterial = basicMaterial;
            // make sure name isn't null
            string materialName = tokens.ReadName();
            if (materialName == null)
                materialName = "";
            // Diffuse color describes how diffuse (directional) light
            // reflects off the mesh
            Vector4 diffuseColor4 = tokens.NextVector4();
            Vector3 diffuseColor = new Vector3(
                diffuseColor4.X, diffuseColor4.Y, diffuseColor4.Z);

            // Old code
            //Vector3 diffuseColor = new Vector3(tokens.NextFloat(),
            //    tokens.NextFloat(), tokens.NextFloat());

            // Specular power is inversely exponentially proportional to the
            // strength of specular light
            float specularPower = tokens.NextFloat();

            // Specular color describes how specular (directional and shiny)
            // light reflects off the mesh
            Vector3 specularColor = tokens.NextVector3();
            Vector3 emissiveColor = tokens.NextVector3();


            // Import any textures associated with this material
            for (string token = tokens.NextToken();
                token != "}"; )
            {
                // Milkshape exports with capital N on name
                if (token == "TextureFilename" || token=="TextureFileName")
                {
                    // Get the absolute path of the texture
                    string fileName = tokens.SkipName().NextString();
                    if (fileName.TrimStart(' ', '"').TrimEnd(' ', '"') != "")
                    {
                        string path = GetAbsolutePath(fileName);
                        if (Path.IsPathRooted(fileName) && !System.IO.File.Exists(path))
                        {
                            context.Logger.LogWarning("", new ContentIdentity(),
                                "An absolute texture path that does not exist is stored in an .X file: " +
                                path + "\n  Attempting to find texture via relative path.");
                            path = GetAbsolutePath(Path.GetFileName(fileName));
                        }

                        texRef =
                            new ExternalReference<TextureContent>(path);
                    }
                    tokens.SkipToken();
                }
                else if (token == "EffectInstance")
                    returnMaterial = ImportCustomMaterial();
                else if (token == "{")
                    tokens.SkipNode();
                token = tokens.NextToken();
            }

            if (returnMaterial is BasicMaterialContent)
            {
                basicMaterial.Texture = texRef;
                basicMaterial.DiffuseColor = diffuseColor;
                basicMaterial.EmissiveColor = emissiveColor;
                basicMaterial.SpecularColor = specularColor;
                basicMaterial.SpecularPower = specularPower;
            }
            returnMaterial.Name = materialName;

            if (returnMaterial.Name != null)
            {
                if (materials.ContainsKey(returnMaterial.Name))
                    materials.Remove(returnMaterial.Name);
                materials.Add(returnMaterial.Name, returnMaterial);
            }
                
            return returnMaterial;

        }


        /// <summary>
        /// Gets an absolute path of a content item
        /// </summary>
        /// <param name="contentItem">The content item's local filename path</param>
        /// <returns>The absolute filename of the item</returns>
        public string GetAbsolutePath(string contentItem)
        {
            if (Path.IsPathRooted(contentItem))
                return contentItem;
            string path = Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar
                + contentItem;
            return Path.GetFullPath(path);
            
        }



        // A frame can store any data, but is constrained such that each frame must haveB
        // a transform matrix for .X meshes.
        // template Frame
        // {
        //    [...]			
        // } 
        /// <summary>
        /// Imports a data Node in a directx file, usually a Frame node.
        /// </summary>
        /// <returns>The imported node</returns>
        private NodeContent ImportNode()
        {
            NodeContent c;
            if (meshes.Count==0)
            {
                c = new NodeContent();
            }
            else
            {
                c = new BoneContent();
            }
            c.Name = tokens.ReadName();
            if (c.Name == null)
                c.Name = "Node" + c.GetHashCode();
            // process all of this frame's children
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {

                if (next == "Frame")
                    c.Children.Add(ImportNode());
                else if (next == "FrameTransformMatrix")
                    c.Transform = ImportFrameTransformMatrix();
                else if (next == "Mesh")
                {
                    XMeshImporter meshImporter = new XMeshImporter(this);
                    c.Children.Add(meshImporter.ImportMesh());
                }
                // Ignore templates, which define new structure that store data
                // for now, we only will work with the standard directx templates
                else if (next == "template")
                    tokens.SkipName().SkipNode();
                // Skin weight nodes can exist either inside meshes or inside frames.
                // When they exist in a frame, they will refer to a universal mesh
                else if (next == "SkinWeights")
                    meshes[0].ImportSkinWeights();
                // an data node that we are uninterested in
                else if (next == "{")
                    tokens.SkipNode();
            }
            return c;
        }

        // template FrameTransformMatrix
        // {
        //     Matrix4x4 frameMatrix;
        // } 
        /// <summary>
        /// Imports a transform matrix attached to a ContentNode
        /// </summary>
        /// <returns>The transform matrix attached to the current ContentNode</returns>
        private Matrix ImportFrameTransformMatrix()
        {
            Matrix m = tokens.SkipName().NextMatrix();
            // Reflect the matrix across the Z axis to swap from left hand to right hand
            // coordinate system
            ContentUtil.ReflectMatrix(ref m);
            // skip the "}" at the end of the node
            tokens.SkipToken();
            return m;
        }
        #endregion

        #region Animation Importation Methods

        // template AnimationSet
        // {
        //     [ Animation ]
        // } 
        /// <summary>
        /// Imports an animation set that is added to the AnimationContentDictionary of
        /// the root frame.
        /// </summary>
        private void ImportAnimationSet()
        {

            AnimationContent animSet = new AnimationContent();
            animSet.Name = tokens.ReadName();
            // Give each animation a unique name
            if (animSet.Name == null)
                animSet.Name = "Animation" + root.Animations.Count.ToString();

            // Fill in all the channels of the animation.  Each channel refers to 
            // a single bone's role in the animation.
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {
                if (next == "Animation")
                {
                    string boneName;
                    AnimationChannel anim = ImportAnimationChannel(out boneName);
                    // Every channel must be attached to a bone!
                    if (boneName == null)
                        throw new Exception("Animation in file is not attached to any joint");
                    // Make sure that the duration of the animation is set to the 
                    // duration of the longest animation channel
                    if (anim[anim.Count - 1].Time > animSet.Duration)
                        animSet.Duration = anim[anim.Count - 1].Time;
                    animSet.Channels.Add(boneName, anim);
                }
                // skip nodes we are uninterested in
                else if (next == "{")
                    tokens.SkipNode();
            }
            //skeletonRoot = MeshHelper.FindSkeleton(root);
            //skeletonRoot.Animations.Add(animSet.Name, animSet);
            if (root.Animations.ContainsKey(animSet.Name))
            {
                string error = "Attempting to add " + animSet.Name + " but it already exists.";



                throw new Exception(error);
            }
            root.Animations.Add(animSet.Name, animSet);

        }




        /*
         * template AnimationKey 
         * {
         *     DWORD keyType;
         *     DWORD nKeys;
         *     array TimedFloatKeys keys[nKeys];
         * } 
         * 
         * 
         * template TimedFloatKeys 
         * { 
         *     DWORD time; 
         *     FloatKeys tfkeys; 
         * } 
         * 
         * template FloatKeys
         * {
         *     DWORD nValues;
         *     array float values[nValues];
         * }        
         */
        /// <summary>
        ///  Imports a key frame list associated with an animation channel
        /// </summary>
        /// <param name="keyType">The type of animation keys used by the current channel</param>
        /// <returns>The list of key frames for the given key type in the current channel</returns>
        private AnimationKeyframe[] ImportAnimationKey(out int keyType)
        {
            // These keys can be rotation (0),scale(1),translation(2), or matrix(3 or 4) keys.
            keyType = tokens.SkipName().NextInt();
            // Number of frames in channel
            int numFrames = tokens.NextInt();
            AnimationKeyframe[] frames = new AnimationKeyframe[numFrames];
            // Find the ticks per millisecond that defines how fast the animation should go
            double ticksPerMS = animTicksPerSecond == null ? DEFAULT_TICKS_PER_SECOND /1000.0 
                : (double)animTicksPerSecond / 1000.0;

            // fill in the frames
            for (int i = 0; i < numFrames; i++)
            {
                // Create a timespan object that represents the time that the current keyframe
                // occurs
                TimeSpan time = new TimeSpan(0, 0, 0, 0,
                    (int)(tokens.NextInt() / ticksPerMS));
                // The number of keys that represents the transform for this keyframe. 
                // Quaternions (rotation keys) have 4,
                // Vectors (scale and translation) have 3,
                // Matrices have 16
                int numKeys = tokens.NextInt();
                Matrix transform = new Matrix();
                if (numKeys == 16)
                    transform = tokens.NextMatrix();
                else if (numKeys == 4)
                {
                    Vector4 v = tokens.NextVector4();
                    Quaternion q = new Quaternion(
                        new Vector3(-v.Y,-v.Z,-v.W),
                        v.X);

                    transform = Matrix.CreateFromQuaternion(q);
                }
                else if (numKeys == 3)
                {
                    Vector3 v = tokens.NextVector3();

                    if (keyType == 1)
                    {
                        Matrix.CreateScale(ref v, out transform);
                    }
                    else
                    {
                        Matrix.CreateTranslation(ref v, out transform);
                    }
                }
                tokens.SkipToken();
                frames[i] = new AnimationKeyframe(time, transform);
            }
            tokens.SkipToken();
            return frames;
        }



        /*
         * template Animation
         * {
         * [...]
         * }
         */
        /// <summary>
        /// Fills in all the channels of an animation.  Each channel refers to 
        /// a single bone's role in the animation.
        /// </summary>
        /// <param name="boneName">The name of the bone associated with the channel</param>
        /// <returns>The imported animation channel</returns>
        private AnimationChannel ImportAnimationChannel(out string boneName)
        {
            AnimationChannel anim = new AnimationChannel();
            // Store the frames in an array, which acts as an intermediate data set
            // This will allow us to more easily provide support for non Matrix
            // animation keys at a later time
            AnimationKeyframe[] rotFrames = null;
            AnimationKeyframe[] transFrames = null;
            AnimationKeyframe[] scaleFrames = null;
            List<AnimationKeyframe> matrixFrames = null;
            boneName = null;
            tokens.SkipName();
            for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
            {
                // A set of animation keys
                if (next == "AnimationKey")
                {
                    // These keys can be rotation (0),scale(1),translation(2), or matrix(3 or 4) keys.
                    int keyType;
                    AnimationKeyframe[] frames = ImportAnimationKey(out keyType);

                    if (keyType == 0)
                        rotFrames = frames;
                    else if (keyType == 1)
                        scaleFrames = frames;
                    else if (keyType == 2)
                        transFrames = frames;
                    else
                        matrixFrames = new List<AnimationKeyframe>(frames);

                }
                // A possible bone name
                else if (next == "{")
                {
                    string token = tokens.NextToken();
                    if (tokens.NextToken() != "}")
                        tokens.SkipNode();
                    else
                        boneName = token;
                }
            }
            // Fill in the channel with the frames
            if (matrixFrames != null)
            {
                matrixFrames.Sort(new Comparison<AnimationKeyframe>(delegate(AnimationKeyframe one,
                    AnimationKeyframe two)
                    {
                        return one.Time.CompareTo(two.Time);
                    }));
                if (matrixFrames[0].Time != TimeSpan.Zero)
                    matrixFrames.Insert(0, new AnimationKeyframe(new TimeSpan(),
                        matrixFrames[0].Transform));
                for (int i = 0; i < matrixFrames.Count; i++)
                {
                    Matrix m = matrixFrames[i].Transform;
                    ContentUtil.ReflectMatrix(ref m);
                    matrixFrames[i].Transform = m;
                    anim.Add(matrixFrames[i]);
                }
            }
            else
            {
                List<AnimationKeyframe> combinedFrames = ContentUtil.MergeKeyFrames(
                    scaleFrames, transFrames, rotFrames);
                for (int i = 0; i < combinedFrames.Count; i++)
                {
                    Matrix m = combinedFrames[i].Transform;
                    ContentUtil.ReflectMatrix(ref m);
                    combinedFrames[i].Transform = m; //* Matrix.CreateRotationX(MathHelper.PiOver2);
                    anim.Add(combinedFrames[i]);
                    
                }

            }
            return anim;
        }
        #endregion


    }
}
