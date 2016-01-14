using System;
using FlatRedBall;


using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Particle;

using FlatRedBall.Gui;

using FlatRedBall.Instructions;

using ParticleEditor.GUI;

namespace ParticleEditor
{
	/// <summary>
	/// Summary description for GuiMessages.
	/// </summary>
	public class GuiMessages
    {
        #region Fields

		GuiData guiData;
		Camera camera;
		//GameForm form;

        #endregion


        public void Initialize()
		{
			#region initialize engine managers and data
			//this.form = form;
			//gameData = GameForm.gameData;
            camera = SpriteManager.Camera;

			guiData = EditorData.guiData;
			#endregion
		}


		public void updateGUIOnEmitterSelect()
		{
            GuiData.EmitterPropertyGrid.SelectedObject = AppState.Self.CurrentEmitter;


            if (AppState.Self.CurrentEmitter == null)
            {
                GuiData.ToolsWindow.scaleEmitterTime.Enabled = false;
                GuiData.ToolsWindow.copyEmitter.Enabled = false;

                return;

            }

            AppState.Self.CurrentEmitter.UpdateDependencies(TimeManager.CurrentTime);



			#region tools window


            if (AppState.Self.CurrentEmitter.Parent == null)
                GuiData.ToolsWindow.detachObject.Enabled = false;
			else
                GuiData.ToolsWindow.detachObject.Enabled = true;

            GuiData.ToolsWindow.scaleEmitterTime.Enabled = false;
            GuiData.ToolsWindow.copyEmitter.Enabled = false;

			#endregion
		}

		
		public void ExitOk(Window callingWindow)
		{
#if FRB_XNA
            FlatRedBallServices.Game.Exit();
#else
			GameForm.Exit(form);
#endif
		}


		#region emitter array list box window


		public void EmitterArrayListBoxClick(FlatRedBall.Gui.Window callingWindow)
		{
            ListBoxBase asListBoxBase = callingWindow as ListBoxBase;

            EditorData.SelectEmitter((Emitter)asListBoxBase.GetFirstHighlightedObject());
		}


		#endregion


		#region tool window messages
		
		public void DetachObjectClick(Window callingWindow)
		{
            if (AppState.Self.CurrentEmitter == null) return;

            AppState.Self.CurrentEmitter.Detach();
            GuiData.ToolsWindow.detachObject.Enabled = false;
		}


		#endregion
    }
}