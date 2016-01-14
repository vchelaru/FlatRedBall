/*
 * XMeshImporter.cs
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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System.IO;
#endregion

namespace FlatRedBall.Graphics.Model.Animation.Content
{

    public partial class XModelImporter
    {
        /// <summary>
        /// A helper for XModelImporter that loads Mesh nodes in .X files
        /// </summary>
        private class XMeshImporter
        {
            /// <summary>
            /// Represents a face of the model.  Used internally to buffer mesh data so that
            /// the mesh can be properly split up into ModelMeshParts such that there is
            /// 1 part per material
            /// </summary>
            private struct Face
            {
                // An array that represents the indices of the verts on the mesh that
                // this face contains
                public int[] VertexIndices;
                // The index of materials that determines what material is attached to
                // this face
                public int MaterialIndex;

                // Converts a face with 4 verts into 
                public Face[] ConvertQuadToTriangles()
                {
                    Face[] triangles = new Face[2];
                    triangles[0].VertexIndices = new int[3];
                    triangles[1].VertexIndices = new int[3];
                    for (int i = 0; i < 3; i++)
                    {
                        triangles[0].VertexIndices[i] = VertexIndices[i];
                        triangles[1].VertexIndices[i] = VertexIndices[(i + 2) % 4];
                    }

                    triangles[0].MaterialIndex = MaterialIndex;
                    triangles[1].MaterialIndex = MaterialIndex;
                    return triangles;
                }
            }

            #region Member Variables
            private Face[] faces;
            private Vector2[] texCoords = null;
            private Vector3[] normals = null;
            // We will calculate our own normals if there is not a 1:1 normal to face ratio
            private bool hasNormals;
            // The materials of the mesh
            private MaterialContent[] materials = new MaterialContent[0];
            // The blend weights
            List<Vector4[]> weights = new List<Vector4[]>();
            // The blend weight indices
            List<Byte4[]> weightIndices = new List<Byte4[]>();

            private bool isSkinned = false;
            private XModelImporter model;
            private XFileTokenizer tokens;
            // This will eventually turn into the Mesh
            private MeshContent mesh;
            // Contains a list of BoneWeightCollections, one for each vertex.
            // Each BoneWeightCollection contains a list of weights and the name
            // of the bone to which that weight belongs.
            private List<BoneWeightCollection> skinInfo =
                new List<BoneWeightCollection>();
            private SortedList<string, Matrix> skinTransformDictionary =
                new SortedList<string, Matrix>();
            private List<SkinTransformContent> skinTransforms =
                new List<SkinTransformContent>();

            // We will give each mesh a unique name because the default model processor
            // doesn't apply the correct transform to meshes that have a null name
            private static int meshID = 0;
            #endregion

            #region Constructors

            /// <summary>
            /// Creates a new instance of XMeshImporter
            /// </summary>
            /// <param name="model">The object that is importing the model from
            /// the current .X file</param>
            public XMeshImporter(XModelImporter model)
            {
                this.tokens = model.tokens;
                this.model = model;
                model.meshes.Add(this);
            }
            #endregion

            #region Vertex Importation Methods

            // "This template is instantiated on a per-mesh basis. Within a mesh, a sequence of n 
            // instances of this template will appear, where n is the number of bones (X file frames) 
            // that influence the vertices in the mesh. Each instance of the template basically defines
            // the influence of a particular bone on the mesh. There is a list of vertex indices, and a 
            // corresponding list of weights.
            // template SkinWeights 
            // { 
            //     STRING transformNodeName; 
            //     DWORD nWeights; 
            //     array DWORD vertexIndices[nWeights]; 
            //     array float weights[nWeights]; 
            //     Matrix4x4 matrixOffset; 
            // }
            // - The name of the bone whose influence is being defined is transformNodeName, 
            // and nWeights is the number of vertices affected by this bone.
            // - The vertices influenced by this bone are contained in vertexIndices, and the weights for 
            // each of the vertices influenced by this bone are contained in weights.
            // - The matrix matrixOffset transforms the mesh vertices to the space of the bone. When concatenated 
            // to the bone's transform, this provides the world space coordinates of the mesh as affected by the bone."
            // (http://msdn.microsoft.com/library/default.asp?url=/library/en-us/
            //  directx9_c/dx9_graphics_reference_x_file_format_templates.asp)
            // 


            // Reads in a bone that contains skin weights.  It then adds one bone weight
            //  to every vertex that is influenced by ths bone, which contains the name of the bone and the
            //  weight.
            public void ImportSkinWeights()
            {
                isSkinned = true;
                string boneName = tokens.SkipName().NextString();
                // an influence is an index to a vertex that is affected by the current bone
                int numInfluences = tokens.NextInt();
                List<int> influences = new List<int>();
                List<float> weights = new List<float>();
                for (int i = 0; i < numInfluences; i++)
                {
                    influences.Add(tokens.NextInt());
                }
                for (int i = 0; i < numInfluences; i++)
                {
                    weights.Add(tokens.NextFloat());
                    if (weights[i] == 0)
                    {
                        influences[i] = -1;
                    }
                }
                influences.RemoveAll(delegate(int i) { return i == -1; });
                weights.RemoveAll(delegate(float f) { return f == 0; });

                while (tokens.Peek == ";")
                {
                    tokens.NextToken();
                }

                // Add the matrix that transforms the vertices to the space of the bone.
                // we will need this for skinned animation.
                Matrix blendOffset = tokens.NextMatrix();
                ContentUtil.ReflectMatrix(ref blendOffset);
                skinTransformDictionary.Add(boneName, blendOffset);
                SkinTransformContent trans = new SkinTransformContent();
                trans.BoneName = boneName;
                trans.Transform = blendOffset;
                skinTransforms.Add(trans);
                // end of skin weights
                tokens.SkipToken();

                // add a new name/weight pair to every vertex influenced by the bone
                for (int i = 0; i < influences.Count; i++)
                    skinInfo[influences[i]].Add(new BoneWeight(boneName,
                        weights[i]));
            }

            /// <summary>
            /// Reads in the vertex positions and vertex indices for the mesh
            /// </summary>
            private void InitializeMesh()
            {
                // This will turn into the ModelMeshPart
                //geom = new GeometryContent();
               // mesh.Geometry.Add(geom);
                int numVerts = -1;

                int i = -1;

                try
                {

                    numVerts = tokens.NextInt();
                    // read the verts and create one boneweight collection for each vert
                    // which will represent that vertex's skinning info (which bones influence it
                    // and the weights of each bone's influence on the vertex)
                    for (i = 0; i < numVerts; i++)
                    {
                        skinInfo.Add(new BoneWeightCollection());
                        Vector3 v = tokens.NextVector3();
                        // Reflect each vertex across z axis to convert it from left hand to right
                        // hand
                        v.Z *= -1;
                        mesh.Positions.Add(v);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Error trying to parse the verts.  The model claims to have " + numVerts + " verts.  We were able to parse " + i + " verts");

                }

                

                // Add the indices that describe the order in which
                // the vertices are rendered
                int numFaces = -1;
                i = -1;

                try
                {

                    numFaces = tokens.NextInt();
                    faces = new Face[numFaces];
                    for (i = 0; i < numFaces; i++)
                    {
                        int numVertsPerFace = tokens.NextInt();
                        faces[i].VertexIndices = new int[numVertsPerFace];
                        for (int j = 0; j < numVertsPerFace; j++)
                            faces[i].VertexIndices[j] = tokens.NextInt();
                        tokens.SkipToken();
                    }
                }
                catch (Exception e)
                {
                    string error = "Error trying to parse the faces.  The model claims to have " + numFaces + " faces.  We were able to parse " + i + " faces";

                    error += "\nThe parser parsed " + numVerts + " verts.  Should there really be that many?";

                    throw new Exception(error);
                }

            }

            // template MeshTextureCoords
            // {
            //      DWORD nTextureCoords;
            //      array Coords2d textureCoords[nTextureCoords] ;
            // } 
            /// <summary>
            /// Imports the texture coordinates associated with the current mesh.
            /// </summary>
            private void ImportTextureCoords()
            {
                tokens.SkipName();
                int numCoords = tokens.NextInt();
                texCoords = new Vector2[numCoords];
                for (int i = 0; i < numCoords; i++)
                    texCoords[i] = tokens.NextVector2();
                // end of vertex coordinates
                tokens.SkipToken();
            }

            // template MeshNormals
            // {
            //     DWORD nNormals;
            //     array Vector normals[nNormals];
            //     DWORD nFaceNormals;
            //     array MeshFace faceNormals[nFaceNormals];
            // } 
            /// <summary>
            /// Imports the normals associated with the current mesh.
            /// </summary>
            private void ImportNormals()
            {
                tokens.ReadName();
                hasNormals = true;

                int numNormals = tokens.NextInt();
                if (numNormals == mesh.Positions.Count)
                    normals = new Vector3[numNormals];
                else
                    hasNormals = false;

                for (int i = 0; i < numNormals; i++)
                {
                    Vector3 norm = tokens.NextVector3();
                    if (numNormals == mesh.Positions.Count)
                    {
                        normals[i] = norm;
                        normals[i].Z *= -1;
                    }
                }

                int numFaces = tokens.NextInt();
                for (int i = 0; i < numFaces; i++)
                {
                    int numNormalsPerFace = tokens.NextInt();
                    tokens.SkipTokens(2*numNormalsPerFace+1);
                }
                // end of mesh normals
                tokens.SkipToken();
            }



            // template Mesh
            // {
            //      DWORD nVertices;
            //      array Vector vertices[nVertices];
            //      DWORD nFaces;
            //      array MeshFace faces[nFaces];
            //      [...]
            // }
            /// <summary>
            /// Imports a mesh.
            /// </summary>
            /// <returns>The imported mesh</returns>
            public NodeContent ImportMesh()
            {
                // Initialize mesh
                mesh = new MeshContent();
                mesh.Name = tokens.ReadName();
                if (mesh.Name == null)
                {
                    mesh.Name = "Mesh" + meshID.ToString();
                    meshID++;
                }
                // Read in vertex positions and vertex indices
                InitializeMesh();

                // Fill in the geometry channels and materials for this mesh
                for (string next = tokens.NextToken(); next != "}"; next = tokens.NextToken())
                {
                    if (next == "MeshNormals")
                        ImportNormals();
                    else if (next == "MeshTextureCoords")
                        ImportTextureCoords();
                    else if (next == "SkinWeights")
                        ImportSkinWeights();
                    else if (next == "MeshMaterialList")
                        ImportMaterialList();
                    else if (next == "Frame")
                        mesh.Children.Add(model.ImportNode());
                    else if (next == "{")
                        tokens.SkipNode();
                    else if (next == "}")
                        break;
                }
                return mesh;
            }
            #endregion

            #region Material Importation Methods
            // template MeshMaterialList
            // {
            //      DWORD nMaterials;
            //      DWORD nFaceIndexes;
            //      array DWORD faceIndexes[nFaceIndexes];
            //      [Material]
            // } 
            /// <summary>
            /// Imports a material list that contains the materials used by the current mesh.
            /// </summary>
            private void ImportMaterialList()
            {
                int numMaterials = tokens.SkipName().NextInt();
                materials = new MaterialContent[numMaterials];
                int numFaces = tokens.NextInt();

                // skip all the indices and their commas/semicolons since
                // we are just going to apply this material to the entire mesh
                for (int i = 0; i < numFaces; i++)
                    faces[i].MaterialIndex = tokens.NextInt();
                // account for blenders mistake of putting an extra semicolon here
                if (tokens.Peek == ";")
                    tokens.SkipToken();
                for (int i = 0; i < numMaterials; i++)
                {
                    if (tokens.Peek == "{")
                    {
                        tokens.SkipToken();
                        string materialReference = tokens.NextToken();
                        tokens.SkipToken();
                        materials[i] = model.materials[materialReference];
                    }
                    else
                    {
                        tokens.SkipToken();
                        materials[i] = model.ImportMaterial();
                    }
                }
                // end of material list
                tokens.SkipToken();
            }


            #endregion

            #region Other Methods

            /// <summary>
            /// Adds all the buffered channels to the mesh and merges duplicate positions/verts
            /// </summary>
            private void AddAllChannels()
            {


                bool recalcNormal = false;
               if (model.AnimationOptions.Contains("RecalcNormals") || (normals == null && !hasNormals))
                   recalcNormal=true;
               else
                    AddChannel<Vector3>(VertexElementUsage.Normal.ToString() + "0", normals);


                if (texCoords != null)
                    AddChannel<Vector2>("TextureCoordinate0", texCoords);

                //for (int i = 0; i < weightIndices.Count; i++)
                //{
                //    AddChannel<Byte4>(VertexElementUsage.BlendIndices.ToString() + i.ToString(),
                //        weightIndices[i]);
                //}
                //for (int i = 0; i < weights.Count; i++)
                //{
                //    AddChannel<Vector4>(VertexElementUsage.BlendWeight.ToString() + i.ToString(),
                //        weights[i]);
                //}
                bool isSkinned = false;
                foreach (BoneWeightCollection bwc in skinInfo)
                {
                    if (bwc.Count > 0)
                    {
                        isSkinned = true;
                        break;
                    }
                }
                if (isSkinned)
                {
                    AddChannel<BoneWeightCollection>(VertexChannelNames.Weights(), skinInfo.ToArray());
                }

                MeshHelper.MergeDuplicatePositions(mesh, 0);
                MeshHelper.MergeDuplicateVertices(mesh);
                if (recalcNormal)
                    MeshHelper.CalculateNormals(mesh, true);
                MeshHelper.OptimizeForCache(mesh);



                
            }

            /// <summary>
            /// Adds a channel to the mesh
            /// </summary>
            /// <typeparam name="T">The structure that stores the channel data</typeparam>
            /// <param name="channelName">The type of channel</param>
            /// <param name="channelItems">The buffered items to add to the channel</param>
            private void AddChannel<T>(string channelName, T[] channelItems)
            {
                foreach (GeometryContent geom in mesh.Geometry)
                {
                    T[] channelData = new T[geom.Vertices.VertexCount];
                    for (int i = 0; i < channelData.Length; i ++)
                        channelData[i] = channelItems[geom.Vertices.PositionIndices[i]];
                    geom.Vertices.Channels.Add<T>(channelName, channelData);
                }
            }
            /// <summary>
            /// Converts the bone weight collections into working vertex channels by using the
            /// provided bone index dictionary.  Converts bone names into indices.
            /// </summary>
            /// <param name="boneIndices">A dictionary that maps bone names to their indices</param>
            public void AddWeights(Dictionary<string, int> boneIndices)
            {

                if (!isSkinned)
                    return;

                Dictionary<string, int> meshBoneIndices = new Dictionary<string, int>();
                int currentIndex = 0;
                // The bone indices are already sorted by index
                foreach (KeyValuePair<string, int> k in boneIndices)
                {
                    if (skinTransformDictionary.ContainsKey(k.Key))
                    {
                        meshBoneIndices.Add(k.Key, currentIndex++);
                        SkinTransformContent transform = new SkinTransformContent();
                        transform.BoneName = k.Key;
                        transform.Transform = skinTransformDictionary[k.Key];
                        skinTransforms.Add(transform);
                    }
                }

            }
            #endregion

            /// <summary>
            /// Creates the ModelMeshParts-to-be (geometry) by splitting up the mesh
            /// via materials
            /// </summary>
            public void CreateGeometry()
            {
                // Number of geometries to create
                int numPartions = materials.Length == 0
                    ? 1 : materials.Length;
                // An array of the faces that each geometry will contain
                List<Face>[] partitionedFaces = new List<Face>[numPartions];

                // Partion the faces.  Each face has a material index, and
                // each geometry has its own material, so the material index
                // refers to the geometry index for the face.
                for (int i = 0; i < partitionedFaces.Length; i++)
                    partitionedFaces[i] = new List<Face>();
                for (int i = 0; i < faces.Length; i++)
                {
                    if (faces[i].VertexIndices.Length == 4)
                        partitionedFaces[faces[i].MaterialIndex].AddRange(
                            faces[i].ConvertQuadToTriangles());
                    else
                        partitionedFaces[faces[i].MaterialIndex].Add(faces[i]);
                }

                // Add the partioned faces to their respective geometries
                int index = 0;
                foreach (List<Face> faceList in partitionedFaces)
                {
                    GeometryContent geom = new GeometryContent();
                    mesh.Geometry.Add(geom);
                    for (int i = 0; i < faceList.Count * 3; i++)
                        geom.Indices.Add(i);
                    foreach (Face face in faceList)
                        geom.Vertices.AddRange(face.VertexIndices);              
                    if (materials.Length > 0)
                        geom.Material = materials[index++];
                }

                // Add the channels to the geometries
                AddAllChannels();
                

            }

            public SkinTransformContent[] SkinTransforms
            {
                get { return skinTransforms.Count > 0 ? skinTransforms.ToArray() : null; }
            }

            public MeshContent Mesh
            {
                get { return mesh; }
            }
        }


    }


}
