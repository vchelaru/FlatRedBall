using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using EditorObjects;

using FlatRedBall;

using FlatRedBall.IO;

using FlatRedBall.ManagedSpriteGroups;

namespace PolygonEditor.Gui
{
    public class GameForm : EditorWindow
    {
        private static GameForm sGameForm;
        #region Properties

        public static string TitleText
        {
            get { return sGameForm.Text; }
            set { sGameForm.Text = value; }
        }

        #endregion

        #region Events


        private void GameForm_DragDrop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    System.Drawing.Point p = this.PointToClient(new System.Drawing.Point(e.X, e.Y));


                    foreach (string fileName in files)
                    {
                        string extension = FileManager.GetExtension(fileName);

                        switch (extension)
                        {
                            case "plylstx":
                                EditorData.LoadPolygonList(fileName);

                                this.BringToFront();
                                this.Focus();

                                break;
                            case "scnx":
                                //GameData.guiData.fileButtonWindow.AskToReplaceOrInsertNewScene(fileName);
                                break;

                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.Message);
                    return;
                }
            }

        }


        #endregion

        #region Methods

        public GameForm()
            : base()
        {
            sGameForm = this;

            // Don't give it a ShapeCollection to edit - make it null.
            EditorData.Initialize(null);
            GuiData.Initialize();
            sGameForm.Text = "PolygonEditor - untitled file";

            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.GameForm_DragDrop);

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

        public override void ProcessCommandLineArgument(string argument)
        {
            string extension = FileManager.GetExtension(argument);

            switch (extension)
            {
                case "scnx":
                    EditorData.LoadScene(argument);

                    break;
				case "plylstx":

					EditorData.LoadPolygonList(argument);
					break;
				case "shcx":
					EditorData.LoadShapeCollection(argument);
					break;

            }
        }

        protected override void ProcessDroppedFile(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);

            switch (extension)
            {

                case "scnx":
                    EditorData.LoadScene(fileName);
                    break;

            }
        }

        #endregion


    }
}
