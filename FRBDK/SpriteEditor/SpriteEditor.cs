//#define SHOW_ASSERT_ERRORS

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;
using FlatRedBall.Collections;
using FlatRedBall.Utilities;
using FlatRedBall.Input;

using System.Drawing;
using System.Windows.Forms;
using System.Collections; // for arrayList of sprites in spriteManager
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using FileManager = FlatRedBall.IO.FileManager;
using SpriteEditor.Gui;
using EditorObjects;

namespace SpriteEditor
{
	public class GameForm : EditorObjects.EditorWindow
	{
		#region Fields
		
		public static SECursor cursor;
		public static SpriteEditor.SEPositionedObjects.EditorCamera camera;


		#endregion

        #region Methods

        #region GameForm constructor
        public GameForm() : base()
		{
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.GameForm_DragDrop);
       
			this.Text = "SpriteEditor - untitled scene";

            camera = new SpriteEditor.SEPositionedObjects.EditorCamera(FlatRedBallServices.GlobalContentManager);
            SpriteManager.Cameras[0] = camera;
            camera.DestinationRectangle = 
                new Rectangle(0, 0, FlatRedBallServices.ClientWidth, FlatRedBallServices.ClientHeight);
            camera.FixAspectRatioYConstant();
           
            if(this.IsDisposed)	return;
			
            // this is a little different than what is normally done in the FRBTemplate;
            // instead of using the SpriteManager's regularly created Camera, make a new 
            // EditorCamera and throw that in the SpriteManager's array.

			cursor = new SECursor(camera, this);

            //GuiManager.Initialize(camera, "genGfx/defaultText.tga", this, cursor);
            GuiManager.Cursors[0] = cursor;

            FlatRedBallServices.Update(null);

            SpriteEditorSettings.Initialize();
            SpriteManager.MaxParticleCount = 10000;
			GameData.Initialize();
			GuiData.Initialize();
			GuiData.messages.Initialize(this);
		}


		#endregion

        [STAThread]
        static void Main(string[] args)
		{
            try
            {
                GameForm frm = new GameForm();
                if (frm.IsDisposed)
                    return;

                ProcessCommandLineArguments(args);

                EditorWindow.MinimumFrameLength = 1 / 60.0f;

                frm.Run(args);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString());
            }
		}

        public override void FrameUpdate()
        {
            base.FrameUpdate();

            GameData.Activity();


            DrawShapesAndLines();
        }

        static void DrawShapesAndLines()
        {
            Vector3 spriteVector;

            #region outline current Sprites
            foreach (Sprite s in GameData.EditorLogic.CurrentSprites)
            {
                spriteVector = s.Position;

                Vector3[] vector3 = { 
                    new Vector3(-s.ScaleX, s.ScaleY, 0),
                    new Vector3( s.ScaleX, s.ScaleY, 0),
                    new Vector3( s.ScaleX,-s.ScaleY, 0),
                    new Vector3(-s.ScaleX,-s.ScaleY, 0)};

                vector3[0].TransformCoordinate(s.RotationMatrix);
                vector3[1].TransformCoordinate(s.RotationMatrix);
                vector3[2].TransformCoordinate(s.RotationMatrix);
                vector3[3].TransformCoordinate(s.RotationMatrix);

                vector3[0] = spriteVector + vector3[0];
                vector3[1] = spriteVector + vector3[1];
                vector3[2] = spriteVector + vector3[2];
                vector3[3] = spriteVector + vector3[3];
            }
            #endregion

            foreach (SpriteFrame sf in GameData.EditorLogic.CurrentSpriteFrames)
            {
                spriteVector = sf.Position;

                Vector3[] vector3 = { 
                    new Vector3(-sf.ScaleX, sf.ScaleY, 0),
                    new Vector3( sf.ScaleX, sf.ScaleY, 0),
                    new Vector3( sf.ScaleX,-sf.ScaleY, 0),
                    new Vector3(-sf.ScaleX,-sf.ScaleY, 0)};

                vector3[0].TransformCoordinate(sf.RotationMatrix);
                vector3[1].TransformCoordinate(sf.RotationMatrix);
                vector3[2].TransformCoordinate(sf.RotationMatrix);
                vector3[3].TransformCoordinate(sf.RotationMatrix);

                vector3[0] = spriteVector + vector3[0];
                vector3[1] = spriteVector + vector3[1];
                vector3[2] = spriteVector + vector3[2];
                vector3[3] = spriteVector + vector3[3];
            }
        

        }

        static void ProcessCommandLineArguments(string[] args)
        {
            //VerifyScnRegistry();
            bool replace = true;
            foreach (string s in args)
            {
                if (FileManager.GetExtension(s) == "scn")
                {
                    GuiData.MenuStrip.PerformLoadScn(s, replace);
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("scn", FileManager.GetDirectory(s));
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("bmp", FileManager.GetDirectory(s));
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("srgx", FileManager.GetDirectory(s));
                    replace = false;
                }
                else if (FileManager.GetExtension(s) == "scnx")
                {
                    GuiData.MenuStrip.PerformLoadScn(s, replace);
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("scn", FileManager.GetDirectory(s));
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("scnx", FileManager.GetDirectory(s));
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("bmp", FileManager.GetDirectory(s));
                    FlatRedBall.Gui.FileWindow.SetLastDirectory("srgx", FileManager.GetDirectory(s));

                    replace = false;
                }
            }
        }

        /// <summary>
        /// Checks to see if the currently running instance of the sprite editor
        /// is associated with the .scn file extension.
        /// </summary>
        /// <remarks>Currently only attempts to check/associate if the currently
        /// logged in user is a windows administrator.  Have not fully
        /// investigated whether you really need unrestricted access to the registry
        /// to create/edit the required registry keys (in HKEY_CLASSES_ROOT), or
        /// if there is a way to do the association as a limited user. In
        /// the mean time, we will err on the side of caution.</remarks>
        static void VerifyScnRegistry()
        {
            System.Security.Principal.WindowsIdentity winIdent = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal winPrincipal = new System.Security.Principal.WindowsPrincipal(winIdent);
            if (winPrincipal.IsInRole(
                System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                FileAssociationHelper file = new FileAssociationHelper(".scn");
                if (!file.IsOpener || !file.IsEditor)
                {
                    DialogResult res = System.Windows.Forms.MessageBox.Show(
                        "This application is currently not associated with the .scn file extension.\n\nWould you like it to be?",
                        "File Association",
                         MessageBoxButtons.YesNo,
                         MessageBoxIcon.Question,
                          MessageBoxDefaultButton.Button1);
                    if (res == DialogResult.Yes)
                    {
                        file.Associate();
                    }
                }
            }
        }

        private void GameForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    System.Drawing.Point p = this.PointToClient(new Point(e.X, e.Y));


                    foreach (string fileName in files)
                    {
                        string extension = FileManager.GetExtension(fileName);

                        switch (extension)
                        {
                            case "bmp":
                            case "jpg":
                            case "tga":
                            case "png":
                            case "dds":
                                GameData.AddSprite(fileName, "");
                                this.BringToFront();
                                this.Focus();
                                
                                break;
                            case "scnx":
                                GuiData.MenuStrip.AskToReplaceOrInsertNewScene(fileName);
                                break;
                            case "x":
                                GameData.AddModel(fileName);
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
    }
}
