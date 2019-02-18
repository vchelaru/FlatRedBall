using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.CodeGeneration;

namespace FlatRedBall.Glue.Controls
{
	public class ScreenTreeNode : BaseElementTreeNode<ScreenSave>
	{
        #region Properties

        public ScreenSave ScreenSave
        {
            get { return mSaveObject as ScreenSave; }
            set { mSaveObject = value; Tag = mSaveObject; }
        }

		#endregion

		#region Methods

        public ScreenTreeNode(string text) : base(text)
        {
            if (BaseElementTreeNode.UseIcons)
            {
                ImageKey = "screen.png";
                SelectedImageKey = "screen.png";
            }
        }
               
        #endregion
    }
}
