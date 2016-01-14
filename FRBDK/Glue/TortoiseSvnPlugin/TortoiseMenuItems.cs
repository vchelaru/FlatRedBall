using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace PluginTestbed.TortoiseSvnPlugin
{
    [Export(typeof(IMenuStripPlugin)) ]
    public class TortoiseMenuItems : IMenuStripPlugin
    {
        ToolStripMenuItem mUpdateItem;
        ToolStripMenuItem mCommitItem;
        ToolStripSeparator mSplitter;
        MenuStrip mMenuStrip;

        TortoiseSvnManager mTortoiseSvnManager;

        void OnSvnUpdateClick(object sender, EventArgs e)
        {
            mTortoiseSvnManager.PerformSvnUpdate();
        }

        void OnSvnCommitClick(object sender, EventArgs e)
        {
            mTortoiseSvnManager.PerformSvnCommit();
        }

        public void InitializeMenu(System.Windows.Forms.MenuStrip menuStrip)
        {
            
            mMenuStrip = menuStrip;
            ToolStripMenuItem itemToAddTo = mMenuStrip.GetItem("Update");

            mUpdateItem = new ToolStripMenuItem("Svn Update");
            mCommitItem = new ToolStripMenuItem("Svn Commit");
            mSplitter = new ToolStripSeparator();

            itemToAddTo.DropDownItems.Add(mSplitter);
            itemToAddTo.DropDownItems.Add(mUpdateItem);
            itemToAddTo.DropDownItems.Add(mCommitItem);


            mUpdateItem.Click += new EventHandler(OnSvnUpdateClick);
            mCommitItem.Click += new EventHandler(OnSvnCommitClick);
        }

        public string FriendlyName
        {
            get { return "Tortoise SVN Plugin"; }
        }

        public Version Version
        {
            get { return new Version(); }
        }

        public void StartUp()
        {
            mTortoiseSvnManager = new TortoiseSvnManager();
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            mTortoiseSvnManager = null;

            ToolStripMenuItem itemToRemoveFrom = mMenuStrip.GetItem("Plugins");
            itemToRemoveFrom.DropDownItems.Remove(mUpdateItem);
            itemToRemoveFrom.DropDownItems.Remove(mCommitItem);
            itemToRemoveFrom.DropDownItems.Remove(mSplitter);
            return true;// We are okay to shut down
        }
    }
}
