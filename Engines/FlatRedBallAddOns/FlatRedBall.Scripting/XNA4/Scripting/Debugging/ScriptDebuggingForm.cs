#if WINDOWS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Scripting;

namespace FlatRedBall.Scripting
{
    public partial class ScriptDebuggingForm : Form
    {
        ScriptEngine mScriptEngine;


        List<Script> mOriginals = new List<Script>();
        int mLastCount;


        public ScriptDebuggingForm()
        {
            InitializeComponent();
        }

        public void ShowWithScripts(ScriptEngine scriptEngine)
        {

            mScriptEngine = scriptEngine;
            mScriptEngine.EndActiveIf();
            foreach (Script script in mScriptEngine.Scripts)
            {
                mOriginals.Add(script);
            }



            UpdateListToScriptEngine();
            this.Show();
        }

        private void UpdateListToScriptEngine()
        {
            int dismissed = 0;

            mLastCount = mScriptEngine.Scripts.Count;

            //this.TreeView.Nodes.Clear();
            foreach (Script script in mOriginals)
            {
                // Get tree node for this script:
                TreeNode treeNode = GetTreeNodeForScript(script);

                if (treeNode == null)
                {
                    treeNode = this.TreeView.Nodes.Add(script.ToString());
                    treeNode.Tag = script;
                }

                bool hasExecuted = !mScriptEngine.Scripts.Contains(script);

                foreach (IScriptAction action in script.Actions)
                {
                    TreeNode subNode = GetTreeNodeForAction(action, treeNode);
                    if (subNode == null)
                    {
                        subNode = treeNode.Nodes.Add(action.ToString());
                        subNode.Tag = action;
                    }

                    if (hasExecuted)
                    {
                        subNode.ForeColor = Color.Green;
                    }
                }

                if (hasExecuted)
                {
                    treeNode.ForeColor = Color.Green;
                    dismissed++;
                }
            }

            this.Text = dismissed.ToString() + " / " + mOriginals.Count;
        }

        TreeNode GetTreeNodeForScript(Script script)
        {
            foreach (TreeNode node in TreeView.Nodes)
            {
                if (node.Tag == script)
                {
                    return node;
                }
            }
            return null;
        }

        TreeNode GetTreeNodeForAction(IScriptAction action, TreeNode parentNode)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (node.Tag == action)
                {
                    return node;
                }
            }
            return null;            
        }

        public void Activity()
        {
            if (mLastCount != mScriptEngine.Scripts.Count)
            {
                UpdateListToScriptEngine();

            }


        }


    }
}
#endif