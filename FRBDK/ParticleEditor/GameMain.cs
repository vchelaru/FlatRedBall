using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Audio;
using FlatRedBall.Collections;
using FlatRedBall.Input;
using FlatRedBall.Instructions;

using FlatRedBall.Utilities;

using Microsoft.DirectX.DirectInput;
using FlatRedBall.IO;



namespace ParticleEditor
{
	public class GameForm : EditorObjects.EditorWindow
	{
		#region declaration, construction, and events

		#region Variable Declaration

        #region Static Engine References

		public static FlatRedBall.Gui.Cursor cursor;
		public static Camera camera;

		public static EditorData gameData;
        #endregion

        static bool pause = false;


		#endregion
	
		#region GameForm constructor
		public GameForm() : base()
		{

			//set the window caption.  This should be the name of your game with version number.  
			//Edit your assembly info to alter your version number of your game
            this.Text = "ParticleEditor - Editing unsaved .emix";

            camera = SpriteManager.Cameras[0];

            cursor = GuiManager.Cursor;

            FlatRedBallServices.Update();


			gameData = new EditorData();

			gameData.Initialize(this);


                EditorData.guiData.Initialize();



			GuiData.Messages.Initialize(this);

            GuiManager.RefreshTextSize();
		}
		#endregion
		
		#region Form Event Handlers
		//override a couple of event handlers for this form to allow
		//termination of the game and to pause while minimized or invisible.
		//You may want to remap the esc key or take it out and terminate through game
		//menus later


		
		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			//Esc was pressed, dispose the form.  
			//This will cause the game loop to terminate and the application to close
//			if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
//			{	
//				Exit(this);
//			}
		}


		public static void Exit(GameForm form)
		{
			System.Windows.Forms.Cursor.Show();		
			form.Dispose(); 

		}

		#endregion
		#endregion

        [STAThread]
        static void Main(string[] args)
		{
            //try
            {
                GameForm frm = new GameForm();
                if (frm.IsDisposed)
                    return;

                frm.Run(args);
            }
            //catch (Exception e)
            //{
            //    string error = e.ToString();

            //    if (e.InnerException != null)
            //    {
            //        error += "\n\n";

            //        error += e.InnerException.ToString();
            //    }
            //    System.Windows.Forms.MessageBox.Show(error);
            //}
		}


        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (!SpriteManager.Exiting)
            {
                gameData.Activity();
            }
        }

		public override void ProcessCommandLineArgument(string argument)
		{
			string extension = FileManager.GetExtension(argument);

			switch (extension)
			{
				case "emix":
					EditorData.LoadEmitters(argument);

					break;
			}
		}
	}
}