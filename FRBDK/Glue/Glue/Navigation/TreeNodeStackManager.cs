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
        #region Fields/Properties

        System.Windows.Controls.Button BackButton;
        System.Windows.Controls.Button ForwardButton;

        Stack<ITreeNode> mTreeNodeStack = new Stack<ITreeNode>();
        Stack<ITreeNode> mForwardNodeStack = new Stack<ITreeNode>();
        bool mIgnoreNextForwardClear;

        public bool CanGoForward => mForwardNodeStack.Count != 0;
        public bool CanGoBack => mTreeNodeStack.Count != 0;

        #endregion

        public void Initialize(System.Windows.Controls.Button backButton, System.Windows.Controls.Button forwardButton)
        {
            BackButton = backButton;
            ForwardButton = forwardButton;

            UpdateNavigateButtons();
        }

        private void UpdateNavigateButtons()
        {
            BackButton.IsEnabled = CanGoBack;
            ForwardButton.IsEnabled = CanGoForward;
        }

        public void Push(ITreeNode treeNode)
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
            if (CanGoForward)
            {
                var toGoTo = mForwardNodeStack.Pop();
                //mForwardNodeStack.Push(toGoTo);
                //((TreeNodeCommands)GlueCommands.Self.TreeNodeCommands).SelectTreeNode(toGoTo);
                // the select will have added, so pop it off immediately
                // We invoke it here instead of using the commands because we need to pop right after the select
                mIgnoreNextForwardClear = true;
                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate
                {
                    // Call the normal selection which will push to bck
                    //GlueState.Self.SetCurrentTreeNode(toGoTo, recordState: false);
                    GlueState.Self.CurrentTreeNode = toGoTo;

                    UpdateNavigateButtons();
                }));
            }
        }

        public void GoBack()
        {
            if (mTreeNodeStack.Count != 0)
            {
                var toGoTo = mTreeNodeStack.Pop();
                mForwardNodeStack.Push(GlueState.Self.CurrentTreeNode) ;
                //((TreeNodeCommands)GlueCommands.Self.TreeNodeCommands).SelectTreeNode(toGoTo);
                // the select will have added, so pop it off immediately
                // We invoke it here instead of using the commands because we need to pop right after the select
                mIgnoreNextForwardClear = true;

                MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate 
                {
                    GlueState.Self.SetCurrentTreeNode(toGoTo, recordState: false);
                    UpdateNavigateButtons();

                }));
            }
        }
    }
}
