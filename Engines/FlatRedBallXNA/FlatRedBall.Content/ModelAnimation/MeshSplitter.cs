using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections;

namespace FlatRedBall.Graphics.Model.Animation.Content
{

    internal class MeshSplitter
    {
        private MeshContent mesh;

        private List<MeshSplitPart> parts = new List<MeshSplitPart>();
        private int maxBones;
        private int currentIndex = 0;


        private class MeshSplitPart
        {
            private MeshSplitter splitter;
            private int index, vertexStartIndex, vertexEndIndex;
            private MeshContent newMesh;
            private SortedDictionary<int, int> oldToNewDict;
            private SortedDictionary<int, int> newToOldDict = new SortedDictionary<int, int>();
            private VertexChannel<BoneWeightCollection> weightChannel;
            private GeometryContent geom;
            private SortedDictionary<string, object> meshBones = new SortedDictionary<string, object>();
            public MeshSplitPart(
                MeshSplitter splitter,
                GeometryContent geom,
                int vertexStartIndex)
            {
                this.index = vertexStartIndex;
                this.splitter = splitter;

                this.oldToNewDict = new SortedDictionary<int, int>();
                this.vertexStartIndex = vertexStartIndex;
                this.vertexEndIndex = vertexStartIndex;
                this.geom = geom;
                GetWeightChannel();
            }

            public MeshContent Process()
            {
                newMesh = new MeshContent();
                newMesh.Name = splitter.mesh.Name + splitter.currentIndex.ToString();
                SortedDictionary<string, object> faceBones = new SortedDictionary<string, object>();
                GeometryContent newGeom = new GeometryContent();
                while (index < geom.Indices.Count - 1)
                {
                    int[] faceIndices = new int[]
                        {
                            geom.Indices[index],
                            geom.Indices[index+1],
                            geom.Indices[index+2]
                        };

                    for (int i = 0; i < 3; i++)
                    {
                        BoneWeightCollection weightCollection = weightChannel[
                            geom.Indices[index + i]];
                        foreach (BoneWeight weight in weightCollection)
                        {
                            if (!meshBones.ContainsKey(weight.BoneName) &&
                                !faceBones.ContainsKey(weight.BoneName))
                                faceBones.Add(weight.BoneName, null);
                        }
                    }
                    if (meshBones.Count + faceBones.Count > splitter.maxBones)
                    {
                        faceBones.Clear();
                        vertexEndIndex = index;
                        break;
                    }

                    foreach (string s in faceBones.Keys)
                        meshBones.Add(s, null);
                    faceBones.Clear();
                    for (int i = 0; i < 3; i++)
                    {
                        if (oldToNewDict.ContainsKey(faceIndices[i]))
                        {

                        }
                        else
                        {
                            int newIndex = newMesh.Positions.Count;
                            newMesh.Positions.Add(geom.Vertices.Positions[faceIndices[i]]);

                            oldToNewDict.Add(faceIndices[i], newIndex);
                            newGeom.Vertices.Add(newIndex);
                        }
                        newGeom.Indices.Add(oldToNewDict[faceIndices[i]]);

                    }
                    index += 3;
                    vertexEndIndex = index;
                }
                newMesh.Geometry.Add(newGeom);
                Finish();
                return newMesh;
            }

            private void Finish()
            {
                foreach (KeyValuePair<int, int> k in oldToNewDict)
                {
                    newToOldDict.Add(k.Value, k.Key);
                }

                foreach (GeometryContent newGeom in newMesh.Geometry)
                {
                    foreach (VertexChannel channel in geom.Vertices.Channels)
                    {
                        List<object> data = new List<object>();
                        foreach (int i in newGeom.Vertices.PositionIndices)
                        {
                            data.Add(channel[newToOldDict[i]]);
                        }
                        newGeom.Vertices.Channels.Add(
                            channel.Name, channel.ElementType, data);
                    }
                    newGeom.Material = geom.Material;
                    foreach (KeyValuePair<string, object> k in geom.OpaqueData)
                    {
                        if (!newGeom.OpaqueData.ContainsKey(k.Key))
                            newGeom.OpaqueData.Add(k.Key, k.Value);
                    }
                }
            }

            public int Index
            { get { return index; } }
            public int VertexStartIndex
            { get { return vertexStartIndex; } }
            public int VertexEndIndex
            { get { return vertexEndIndex; } }

            private void GetWeightChannel()
            {
                foreach (VertexChannel channel in geom.Vertices.Channels)
                {
                    if (channel.Name == VertexChannelNames.Weights())
                    {
                        weightChannel = (VertexChannel<BoneWeightCollection>)channel;
                        break;
                    }
                }
            }
        }
        public MeshSplitter(MeshContent content, int maxBones)
        {
            this.mesh = content;
            this.maxBones = maxBones;
        }

        private List<MeshContent> meshes;

        public List<MeshContent> Split()
        {
            meshes = new List<MeshContent>();
            foreach (GeometryContent geom in mesh.Geometry)
                Split(geom);
            return meshes;
        }

        public static bool NeedsSplitting(MeshContent mesh, int maxBones)
        {
            SortedDictionary<string, object> skinnedBones = new SortedDictionary<string, object>();
            foreach (GeometryContent geom in mesh.Geometry)
            {
                VertexChannel<BoneWeightCollection> weightChannel = null;
                foreach (VertexChannel channel in geom.Vertices.Channels)
                {
                    if (channel.Name == VertexChannelNames.Weights())
                    {
                        weightChannel = (VertexChannel<BoneWeightCollection>)channel;
                        break;
                    }
                }
                if (weightChannel != null)
                {
                    foreach (BoneWeightCollection weights in weightChannel)
                    {
                        foreach (BoneWeight weight in weights)
                        {
                            if (!skinnedBones.ContainsKey(weight.BoneName))
                                skinnedBones.Add(weight.BoneName, null);
                        }
                    }

                }

            }
            return skinnedBones.Keys.Count > maxBones;
        }

        private void Split(GeometryContent geom)
        {
            int vertexStart = 0;
            MeshSplitPart part;
            while (vertexStart < geom.Indices.Count)
            {
                part = new MeshSplitPart(this, geom, vertexStart);


                currentIndex++;
                MeshContent newMesh = part.Process();
                vertexStart = part.VertexEndIndex;
                newMesh.Transform = mesh.Transform;

                meshes.Add(newMesh);

            }
        }



    }

    /// <summary>
    /// Splits a model up into parts based on a max bone count.
    /// </summary>
    public class ModelSplitter
    {
        private int maxBones = 20;
        private List<MeshContent> allMeshes = new List<MeshContent>();
        private NodeContent root;
        private bool modelModified = false;

        /// <summary>
        /// Creates a new ModelSplitter.
        /// </summary>
        /// <param name="modelRoot">The root of the model content.</param>
        /// <param name="maxBonesPerMesh">The maximum number of bones per mesh.</param>
        public ModelSplitter(NodeContent modelRoot, int maxBonesPerMesh)
        {
            this.maxBones = maxBonesPerMesh;
            this.root = modelRoot;
        }


        /// <summary>
        /// Splits the model meshes up based on the max number of bones.
        /// </summary>
        /// <returns>True if any mesh was split.</returns>
        public bool Split()
        {
            Process(root);
            return modelModified;
        }
        private void Process(NodeContent input)
        {
            ProcessNode(input);
            foreach (MeshContent mesh in allMeshes)
                ProcessMesh(mesh);

        }

        private void ProcessMesh(MeshContent mesh)
        {
            if (MeshSplitter.NeedsSplitting(mesh, maxBones))
            {
                modelModified = true;
                MeshSplitter splitter = new MeshSplitter(mesh, maxBones);
                List<MeshContent> meshes = splitter.Split();
                foreach (MeshContent m in meshes)
                {
                    MeshHelper.MergeDuplicatePositions(m, 0);
                    MeshHelper.MergeDuplicateVertices(m);
                    MeshHelper.OptimizeForCache(m);
                }

                MeshContent firstMesh = meshes[0];
                NodeContent parent = mesh.Parent;
                List<NodeContent> children = new List<NodeContent>();
                foreach (NodeContent child in mesh.Children)
                    children.Add(child);

                foreach (NodeContent child in children)
                {
                    mesh.Children.Remove(child);
                }
                parent.Children.Remove(mesh);

                foreach (MeshContent m in meshes)
                {
                    parent.Children.Add(m);
                }
                foreach (NodeContent child in children)
                {
                    firstMesh.Children.Add(child);
                }
                foreach (NodeContent child in firstMesh.Children)
                {
                    ProcessNode(child);
                }
            }
        }

        private void ProcessNode(NodeContent input)
        {
            if (input is MeshContent)
            {
                allMeshes.Add((MeshContent)input);
            }

            foreach (NodeContent child in input.Children)
            {
                ProcessNode(child);
            }
            
        }

        private VertexChannel<BoneWeightCollection> GetWeightChannel(GeometryContent geom)
        {
            foreach (VertexChannel channel in geom.Vertices.Channels)
            {
                if (channel.Name == VertexChannelNames.Weights())
                {
                    return (VertexChannel<BoneWeightCollection>)channel;
                }
            }
            return null;
        }







    }
}
