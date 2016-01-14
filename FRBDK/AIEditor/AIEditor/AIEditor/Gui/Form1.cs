using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using FlatRedBall;

using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.IO;

namespace AIEditor.Gui
{
    public partial class Form1 : EditorObjects.EditorWindow
    {

		private static Form1 sForm;
		public static string TitleText
		{
			get { return sForm.Text; }
			set { sForm.Text = value; }
		}

        public Form1()
            : base()
        {
			sForm = this;
            EditorData.Initialize();
            GuiData.Initialize();
        }

		public override void ProcessCommandLineArgument(string argument)
		{
			string extension = FileManager.GetExtension(argument);

			switch (extension)
			{
				case "nntx":
					EditorData.LoadNodeNetwork(argument, false, false, false);

					break;

			}
		}

        public override void FrameUpdate()
        {

            EditorData.Update();

            if (EditorData.Scene != null)
            {
                foreach (SpriteGrid spriteGrid in EditorData.Scene.SpriteGrids)
                {
                    spriteGrid.Manage();
                }
            }

            base.FrameUpdate();

            
        }

    }
}