using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Math.Geometry;
using FlatRedBall;
using FlatRedBall.Glue.SaveClasses;
using GlueView.Facades;
using FlatRedBall.Utilities;
using FlatRedBall.Graphics;

namespace GlueViewOfficialPlugins.ObjectCreator
{
    public partial class AddObjectToolbox : UserControl
    {
        #region Fields

        TreeNode mRootEntityTreeNode;
        TreeNode mFlatRedBallTypeTreeNode;

        ObjectCreator mObjectCreator = new ObjectCreator();

        #endregion


        public AddObjectToolbox()
        {
            InitializeComponent();

            PopulateWithFrbTypes();
            PopulateWithEntityTypes();
        }

        public void PopulateWithEntityTypes()
        {
            if (mRootEntityTreeNode == null)
            {
                mRootEntityTreeNode = TreeView.Nodes.Add("Entities");
            }

            mRootEntityTreeNode.Nodes.Clear();

            if (GlueViewState.Self.CurrentGlueProject != null)
            {
                foreach (EntitySave entitySave in GlueViewState.Self.CurrentGlueProject.Entities)
                {
                    mRootEntityTreeNode.Nodes.Add(entitySave.Name);
                }
            }

        }

        private void PopulateWithFrbTypes()
        {
            mFlatRedBallTypeTreeNode = TreeView.Nodes.Add("FlatRedBall Types");
            mFlatRedBallTypeTreeNode.Nodes.Add(typeof(AxisAlignedRectangle).Name);
            mFlatRedBallTypeTreeNode.Nodes.Add(typeof(Circle).Name);

            mFlatRedBallTypeTreeNode.Nodes.Add(typeof(Sprite).Name);
            mFlatRedBallTypeTreeNode.Nodes.Add(typeof(Text).Name);

        }

        private void TreeView_DoubleClick(object sender, EventArgs e)
        {
            string nodeText = TreeView.SelectedNode.Text;

            SourceType sourceType;

            if (TreeView.SelectedNode.Parent != null)
            {
                if (TreeView.SelectedNode.Parent == mFlatRedBallTypeTreeNode)
                {
                    sourceType = SourceType.FlatRedBallType;
                }
                else
                {
                    sourceType = SourceType.Entity;
                }
                mObjectCreator.CreateNamedObject(sourceType, nodeText);
            }
        }
    }
}
