using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using Glue;

namespace FlatRedBall.Glue.Navigation
{
    public class TreeNodeStackManager : Singleton<TreeNodeStackManager>
    {
        TreeNode mCurentTreeNode;
        Button mBackButton;
        Button mForwardButton;

        Stack<TreeNode> mTreeNodeStack = new Stack<TreeNode>();
        Stack<TreeNode> mForwardNodeStack = new Stack<TreeNode>();
        bool mIgnoreNextForwardClear;

        public void Initialize(Button backButton, Button forwardButton)
        {
            mBackButton = backButton;
            mForwardButton = forwardButton;

            UpdateNavigateButtons();
        }

        private void UpdateNavigateButtons()
        {
            mBackButton.Enabled = mTreeNodeStack.Count > 0;
            mForwardButton.Enabled = mForwardNodeStack.Count > 0;
        }

        public void Push(TreeNode treeNode)
        {
            mTreeNodeStack.Push(treeNode);
            if (!mIgnoreNextForwardClear)
            {
                mForwardNodeStack.Clear();
            }
            else
            {
                mIgnoreNextForwardClear = false;
            }
            UpdateNavigateButtons();

        }



        public void GoForward()
        {
            if (mForwardNodeStack.Count != 0)
            {
                var toGoTo = mForwardNodeStack.Pop();
                //mForwardNodeStack.Push(toGoTo);
                //((TreeNodeCommands)GlueCommands.Self.TreeNodeCommands).SelectTreeNode(toGoTo);
                // the select will have added, so pop it off immediately
                // We invoke it here instead of using the commands because we need to pop right after the select
                mIgnoreNextForwardClear = true;
                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate
                {
                    ElementViewWindow.SelectedNode = toGoTo;
                    // We want it to go to the back
                    //mTreeNodeStack.Pop();
                    UpdateNavigateButtons();

                }));
            }
        }

        public void GoBack()
        {
            if (mTreeNodeStack.Count != 0)
            {
                var toGoTo = mTreeNodeStack.Pop();
                mForwardNodeStack.Push(ElementViewWindow.SelectedNode);
                //((TreeNodeCommands)GlueCommands.Self.TreeNodeCommands).SelectTreeNode(toGoTo);
                // the select will have added, so pop it off immediately
                // We invoke it here instead of using the commands because we need to pop right after the select
                mIgnoreNextForwardClear = true;

                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate 
                    { 
                        ElementViewWindow.SelectedNode = toGoTo; 
                    
                        mTreeNodeStack.Pop();
                        UpdateNavigateButtons();

                    }));
            }
        }


    }
}
