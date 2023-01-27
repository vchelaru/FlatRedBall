using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.Data;
using Microsoft.Xna.Framework.Graphics;
using CommonFormsAndControls;
using FlatRedBall.Content.Math.Geometry;

namespace FlatRedBall.AnimationEditorForms
{
    #region SelectionSnapshot

    public class SelectionSnapshot
    {
        public AnimationChainSave AnimationChainSave;
        public AnimationFrameSave AnimationFrameSave;
    }

    #endregion

    public class SelectedState
    {
        #region Fields

        static SelectedState mSelf;
        
        MultiSelectTreeView mTreeView;

        SelectionSnapshot mSnapshot = new SelectionSnapshot();

        #endregion

        #region Properties

        public static SelectedState Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new SelectedState();
                }
                return mSelf;
            }
        }

        public AnimationChainListSave AnimationChainListSave
        {
            get
            {
                return ProjectManager.Self.AnimationChainListSave;
            }
        }

        public string SelectedTextureName
        {
            get
            {
                if (SelectedFrame != null)
                {
                    return SelectedFrame.TextureName;
                }
                else if (SelectedChain != null && SelectedChain.Frames.Count != 0)
                {
                    return SelectedChain.Frames[0].TextureName;
                }
                else
                {
                    return null;
                }
            }

        }

        public TreeNode SelectedNode
        {
            get => mTreeView.SelectedNode;
            set => mTreeView.SelectedNode = value;
        }

        public List<TreeNode> SelectedNodes
        {
            get => mTreeView.SelectedNodes;
            set => mTreeView.SelectedNodes = value;
        }

        public AnimationChainSave SelectedChain
        {
            get
            {
                TreeNode node = SelectedNode;
                if (node?.Tag is AnimationChainSave tagAsAnimationChainSave)
                {
                    return tagAsAnimationChainSave;
                }
                else if (node?.Tag is AnimationFrameSave && 
                    node?.Parent?.Tag is AnimationChainSave parentTagAsAnimationChainSave)
                {
                    return parentTagAsAnimationChainSave;
                }
                // could be a shape?
                else if(node?.Parent?.Parent.Tag is AnimationChainSave grandparentChain)
                {
                    return grandparentChain;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                TreeNode treeNode = TreeViewManager.Self.GetTreeNodeFor(value);

                SelectedNode = treeNode;
            }
        }

        public List<AnimationChainSave> SelectedChains
        {
            get
            {
                List<AnimationChainSave> toReturn = new List<AnimationChainSave>();
                var treeNodes = SelectedNodes;

                foreach(var treeNode in treeNodes)
                {
                    if(treeNode.Tag is AnimationChainSave animationChainSave)
                    {
                        toReturn.Add(animationChainSave);
                    }
                }

                return toReturn;
            }
            set
            {
                List<TreeNode> treeNodes = new List<TreeNode>();

                if(value == null)
                {
                    SelectedNodes = new List<TreeNode>();
                }
                else
                {
                    SelectedNodes = value.Select(item => TreeViewManager.Self.GetTreeNodeFor(item)).ToList();
                }
            }
        }

        public AnimationFrameSave SelectedFrame
        {
            get
            {
                var node = SelectedNode;
                if (node?.Tag is AnimationFrameSave asFrame)
                {
                    return asFrame;
                }
                else if (node?.Parent?.Tag is AnimationFrameSave parentAsFrame)
                {
                    return parentAsFrame;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var treeNode = TreeViewManager.Self.GetTreeNodeFor(value);

                SelectedNode = treeNode;
            }
        }

        public AxisAlignedRectangleSave SelectedAxisAlignedRectangle
        {
            get
            {
                return SelectedNode?.Tag as AxisAlignedRectangleSave;
            }
            set
            {
                TreeNode treeNode = null;
                if (value != null)
                {
                    treeNode = TreeViewManager.Self.GetTreeNodeByTag(value);

                }

                if(treeNode != null)
                {
                    SelectedNode = treeNode;
                }
            }
        }

        public List<AnimationFrameSave> SelectedFrames
        {
            get
            {
                var toReturn = new List<AnimationFrameSave>();
                var treeNodes = SelectedNodes;

                foreach (var treeNode in treeNodes)
                {
                    if (treeNode.Tag is AnimationFrameSave frame)
                    {
                        toReturn.Add(frame);
                    }
                }
                if(toReturn.Count == 0 && SelectedFrame != null)
                {
                    toReturn.Add(SelectedFrame);
                }

                return toReturn;
            }
        }

        public TileMapInformation SelectedTileMapInformation
        {
            get
            {
                string fileName = null;

                if (SelectedFrame != null)
                {
                    fileName = SelectedState.Self.SelectedFrame.TextureName;
                }
                else if(SelectedChain != null && SelectedChain.Frames.Count > 0)
                {
                    fileName = SelectedState.Self.SelectedChain.Frames[0].TextureName;
                }

                if(!string.IsNullOrEmpty(fileName))
                {
                    TileMapInformation tileMapInfo = ProjectManager.Self.TileMapInformationList.GetTileMapInformation(fileName);

                    return tileMapInfo;
                }
                else
                {
                    return null;
                }
            }
        }

        public object SelectedShape => (object)SelectedRectangle ?? SelectedCircle;

        public AxisAlignedRectangleSave SelectedRectangle
        {
            get => SelectedNode?.Tag as AxisAlignedRectangleSave;
            set
            {
                if(value != null)
                {
                    TreeNode treeNode = TreeViewManager.Self.GetTreeNodeByTag(value);
                    if(treeNode != null)
                    {
                        SelectedNode = treeNode;
                    }
                }

            }
        }

        public List<AxisAlignedRectangleSave> SelectedRectangles
        {
            get
            {
                var toReturn = new List<AxisAlignedRectangleSave>();
                var treeNodes = SelectedNodes;

                foreach (var treeNode in treeNodes)
                {
                    if (treeNode.Tag is AxisAlignedRectangleSave rectangle)
                    {
                        toReturn.Add(rectangle);
                    }
                }

                return toReturn;
            }
            set
            {
                if(value?.Count > 0)
                {
                    List<TreeNode> treeNodesToSelect = new List<TreeNode>();
                    foreach(var item in value)
                    {
                        var treeNode = TreeViewManager.Self.GetTreeNodeByTag(item);
                        if(treeNode != null)
                        {
                            treeNodesToSelect.Add(treeNode);
                        }
                    }
                    SelectedNodes = treeNodesToSelect;
                }
            }
        }

        public CircleSave SelectedCircle
        {
            get => SelectedNode?.Tag as CircleSave;
            set
            {
                if(value != null)
                {
                    var treeNode = TreeViewManager.Self.GetTreeNodeByTag(value);
                    if(treeNode != null)
                    {
                        SelectedNode = treeNode;
                    }
                }
            }
        }

        public List<CircleSave> SelectedCircles
        {
            get
            {
                var toReturn = new List<CircleSave>();
                var treeNodes = SelectedNodes;

                foreach(var treeNode in treeNodes)
                {
                    if(treeNode.Tag is CircleSave circle)
                    {
                        toReturn.Add(circle);
                    }
                }

                return toReturn;
            }
            set
            {
                if(value?.Count > 0)
                {
                    var treeNodesToSelect = new List<TreeNode>();
                    foreach(var item in value)
                    {
                        var treeNode = TreeViewManager.Self.GetTreeNodeByTag(item);
                        if(treeNode != null)
                        {
                            treeNodesToSelect.Add(treeNode);
                        }
                    }
                    SelectedNodes = treeNodesToSelect;
                }
            }
        }

        public Texture2D SelectedTexture
        {
            get
            {
                return WireframeManager.Self.Texture;
            }
        }

        public SelectionSnapshot Snapshot
        {
            get { return mSnapshot; }
        }

        #endregion

        public void Initialize(MultiSelectTreeView treeView)
        {
            mTreeView = treeView;

        }

        public void TakeSnapshot()
        {
            mSnapshot = new SelectionSnapshot();
            mSnapshot.AnimationChainSave = this.SelectedChain;
            mSnapshot.AnimationFrameSave = this.SelectedFrame;
        }
    }
}
