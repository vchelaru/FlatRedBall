using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.AnimationEditorForms.Data;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.AnimationEditorForms
{
    public class SelectionSnapshot
    {
        public AnimationChainSave AnimationChainSave;
        public AnimationFrameSave AnimationFrameSave;

    }

    public class SelectedState
    {
        #region Fields

        static SelectedState mSelf;
        
        TreeView mTreeView;

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
            get
            {
                return mTreeView.SelectedNode;
            }
            set
            {
                mTreeView.SelectedNode = value;
            }
        }

        public AnimationChainSave SelectedChain
        {
            get
            {
                TreeNode node = SelectedNode;
                if (node != null && node.Tag is AnimationChainSave)
                {
                    return node.Tag as AnimationChainSave;
                }
                else if (node != null && node.Tag is AnimationFrameSave && node.Parent != null &&
                    node.Parent.Tag != null && node.Parent.Tag is AnimationChainSave)
                {
                    return node.Parent.Tag as AnimationChainSave;
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

        public AnimationFrameSave SelectedFrame
        {
            get
            {
                if (SelectedNode == null)
                {
                    return null;
                }
                else
                {
                    return SelectedNode.Tag as AnimationFrameSave;
                }
            }
            set
            {
                TreeNode treeNode = TreeViewManager.Self.GetTreeNodeFor(value);

                SelectedNode = treeNode;

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


        public void Initialize(TreeView treeView)
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
