using System;
using System.Collections.Generic;

using FlatRedBall;



using FlatRedBall.Content;
using FlatRedBall.Content.Particle;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.ManagedSpriteGroups;

using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using FlatRedBall.Utilities;


using ParticleEditor.GUI;
using FlatRedBall.IO;

using EditorObjects;
using System.Windows.Forms;

using Cursor = FlatRedBall.Gui.Cursor;
using System.IO;

#if FRB_MDX
using FlatRedBall.Collections;

using Microsoft.DirectX.DirectInput;
#endif

namespace ParticleEditor
{
	/// <summary>
	/// Summary description for GameData.
	/// </summary>
	public static class EditorData
	{
		#region Fields
		#region reference to engine managers and data
		public static GuiData guiData;

		public static Camera camera;
        public static Cursor cursor;
		//public GameForm frm;
		#endregion

        static EditorLogic mEditorLogic;

		public static PositionedObjectList<Emitter> Emitters = new PositionedObjectList<Emitter>();
		//public List<EmitterSave> lastLoadedFile;
        public static EmitterSaveList lastLoadedFile;
        public static string CurrentEmixFileName = "";

        private static Scene mScene;

		public static bool guiVisible = true;


        public static EditorProperties EditorProperties = new EditorProperties();
		#endregion

        #region Properties

        public static EditorLogic EditorLogic
        {
            get { return mEditorLogic; }
        }

        public static Scene Scene
        {
            get { return mScene; }
        }

        #endregion

        #region Methods

        #region Public Methods

        public static void Initialize()
		{
			#region initialize engine managers and data
			guiData = new GuiData();
            camera = SpriteManager.Camera;
            cursor = GuiManager.Cursor;

			SpriteManager.Camera.FarClipPlane = 1800;

            mEditorLogic = new EditorLogic();

            EditorProperties = new EditorProperties();

			#endregion

//            SpriteManager.AddParticleSprite(
  //              FlatRedBallServices.Load<Texture2D>("redball.bmp", "PermanentContentManager"));
		}


        public static void Update()
		{
            mEditorLogic.Update();

            if (mScene != null)
                mScene.ManageAll();
			
			#region if there is a mEditorLogic.CurrentEmitter
            if (AppState.Self.CurrentEmitter != null)
			{
				if(GuiData.ActivityWindow.TimedEmitCurrent)
				{
                    AppState.Self.CurrentEmitter.TimedEmit(null);
				}
			}
			#endregion

            if (GuiData.ActivityWindow.TimedEmitAll)
            {
                foreach (Emitter emitter in Emitters)
                    emitter.TimedEmit();
            }

            GuiData.Update();

            UndoManager.EndOfFrameActivity();

            
            //			if(guiData.propWindow.textureButton.CurrentChain != null)
//				sprMan.AnimateWAnimateWindow(guiData.propWindow.textureButton);

        }


        public static void CopyCurrentEmitter()
        {
            if (AppState.Self.CurrentEmitter == null) return;

            Emitter tempEmitter = AppState.Self.CurrentEmitter.Clone();
            tempEmitter.Name = StringFunctions.IncrementNumberAtEnd(tempEmitter.Name);
            while (Emitters.FindWithNameContaining(tempEmitter.Name) != null)
            {
                tempEmitter.Name = StringFunctions.IncrementNumberAtEnd(tempEmitter.Name);
            }

            Emitters.Add(tempEmitter);
            SpriteManager.AddEmitter(tempEmitter);
        }


        public static void CreateNewWorkspace()
        {
            if (mScene != null)
            {
                mScene.RemoveFromManagers();
                mScene.Clear();
            }

            while (Emitters.Count != 0)
            {
                SpriteManager.RemoveEmitter(Emitters[Emitters.Count - 1]);
            }

            AppState.Self.CurrentEmitter = null;

            GuiData.EmitterPropertyGrid.SelectedObject = null;
        }


        public static void HandleDragDrop(object sender, DragEventArgs e)
        {

            string filename = ((string[]) ((DataObject) e.Data).GetData("FileName"))[0];
            string fullname = new FileInfo(filename).FullName;

            string extension = FileManager.GetExtension(fullname);


            switch (extension)
            {
                case "scnx":
                    LoadScene(fullname);

                    break;
                case "emix":
                    AppCommands.Self.File.LoadEmitters(fullname);
                    break;
                default:
                    
                    break;
                    

            }
            
        }

        
        public static void LoadScene(string fileName)
        {
            if (mScene != null)
            {
                mScene.RemoveFromManagers();
                mScene.Clear();
            }

            SpriteEditorScene ses = SpriteEditorScene.FromFile(fileName);
            mScene = ses.ToScene(AppState.Self.PermanentContentManager);

            mScene.AddToManagers();

			FileMenuWindow.AttemptEmitterAttachment(fileName);
		}


        public static void SelectEmitter(Emitter emitterToSelect)
        {
            #region attaching an emitter
            if (GuiData.ToolsWindow.attachObject.IsPressed && AppState.Self.CurrentEmitter != emitterToSelect)
            {
                GuiData.ToolsWindow.attachObject.Unpress();

            }
            #endregion
            else
            {
                AppState.Self.CurrentEmitter = emitterToSelect;
                GuiData.Messages.updateGUIOnEmitterSelect();
            }
        }

        #endregion
		
		#endregion

        internal static void SetDefaultValuesOnEmitter(Emitter newEmitter)
        {
            newEmitter.TimedEmission = true;
            newEmitter.SecondFrequency = .2f;
            newEmitter.SecondsLasting = 6;
            newEmitter.RemovalEvent = Emitter.RemovalEventType.Timed;

            if (Camera.Main.Orthogonal)
            {
                newEmitter.EmissionSettings.PixelSize = .5f;
                newEmitter.EmissionSettings.RadialVelocity = 35;
            }
        }
    }
}
