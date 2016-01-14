using System;

using FlatRedBall;

using FlatRedBall.Graphics.Particle;

using FlatRedBall.Gui;

using FlatRedBall.Instructions;
#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

namespace ParticleEditor.GUI
{
	/// <summary>
	/// Summary description for ToolsWindow.
	/// </summary>
    public class ToolsWindow : EditorObjects.Gui.ToolsWindow
	{
		#region Fields

		#region engine and common game data

		GuiMessages messages;

		#endregion



		public ToggleButton moveObject = null;
		public ToggleButton attachObject = null;
		public Button detachObject = null;
		public Button copyEmitter = null;

		public Button scaleEmitterTime = null;

        Button mDownZFreeRotateButton;

		#endregion

        #region Properties

        public bool IsDownZ
        {
            get { return mDownZFreeRotateButton.ButtonPushedState != ButtonPushedState.Down; }
        }

        #endregion

        #region methods

        public ToolsWindow(Cursor cursor) : base()
		{
			messages = GuiData.Messages;

			SetPositionTL(3.5f, 63.8f);

            HasCloseButton = true;

            moveObject = AddToggleButton();
			moveObject.Text = "Move";
			moveObject.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>("Content/icons/Tools/move.tga", AppState.Self.PermanentContentManager),
                null);

            attachObject = AddToggleButton();
			attachObject.Text = "Attach";
			moveObject.AddToRadioGroup(attachObject);
			attachObject.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>("Content/icons/attach.tga", AppState.Self.PermanentContentManager),
                null);


            detachObject = AddButton();
			detachObject.Text = "Detach";
			detachObject.Enabled = false;
			detachObject.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>("Content/icons/detach.tga", AppState.Self.PermanentContentManager),             
                null);
			detachObject.Click += new GuiMessage(messages.DetachObjectClick);


            copyEmitter = AddButton();
			copyEmitter.Text = "Copy";
			copyEmitter.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>("Content/icons/duplicate.tga", AppState.Self.PermanentContentManager),             
                null);
			copyEmitter.Click += new GuiMessage(CopyEmitterClick);
            copyEmitter.Enabled = false ;

            scaleEmitterTime = AddButton();
			scaleEmitterTime.Text = "Scale Emitter Speed";
			scaleEmitterTime.SetOverlayTextures(15, 2);
			scaleEmitterTime.Click += new GuiMessage(ScaleEmitterTimeClick);
			scaleEmitterTime.Enabled = false;

            #region DownZFreeRotateButton

            this.mDownZFreeRotateButton = base.AddToggleButton();


            mDownZFreeRotateButton.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\DownZ.png", FlatRedBallServices.GlobalContentManager),
                FlatRedBallServices.Load<Texture2D>(@"Content\FreeRotation.png", FlatRedBallServices.GlobalContentManager));


            #endregion
		}

	
		public void CopyEmitterClick(Window callingWindow)
		{
            Emitter e = AppState.Self.CurrentEmitter.Clone();
			while( EditorData.Emitters.FindByName(e.Name) != null)
				e.Name = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(e.Name);

			EditorData.Emitters.Add(e);
		}


		public void ScaleEmitterTimeClick(Window callingWindow)
		{
			TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter speed percentage.  Values over 100% will speed up the Emitter.", "Scale Emitter Speed");
            tiw.Text = "100";
			tiw.OkClick += new GuiMessage(ScaleEmitterTimeOk);
		}

		
		private void ScaleEmitterTimeOk(Window callingWindow)
		{
            float scaleAmount = float.Parse(((TextInputWindow)callingWindow).Text) / 100.0f;
			float scaleSquared = scaleAmount * scaleAmount;

            Emitter e = AppState.Self.CurrentEmitter;

			e.ParticleBlueprint.AlphaRate *= scaleAmount;
            e.ParticleBlueprint.RedRate *= scaleAmount;
            e.ParticleBlueprint.GreenRate *= scaleAmount;
            e.ParticleBlueprint.BlueRate *= scaleAmount;

            e.ParticleBlueprint.ScaleXVelocity *= scaleAmount;
            e.ParticleBlueprint.ScaleYVelocity *= scaleAmount;

//			e.drag *= scaleAmount;

            e.EmissionSettings.RadialVelocity *= scaleAmount;
            e.EmissionSettings.RadialVelocityRange *= scaleAmount;

            e.EmissionSettings.XVelocity *= scaleAmount;
            e.EmissionSettings.YVelocity *= scaleAmount;
            e.EmissionSettings.ZVelocity *= scaleAmount;

            e.EmissionSettings.XVelocityRange *= scaleAmount;
            e.EmissionSettings.YVelocityRange *= scaleAmount;
            e.EmissionSettings.ZVelocityRange *= scaleAmount;

            e.EmissionSettings.XAcceleration *= scaleAmount;
            e.EmissionSettings.YAcceleration *= scaleAmount;
            e.EmissionSettings.ZAcceleration *= scaleAmount;

            e.EmissionSettings.XAccelerationRange *= scaleAmount;
            e.EmissionSettings.YAccelerationRange *= scaleAmount;
            e.EmissionSettings.ZAccelerationRange *= scaleAmount;

            e.EmissionSettings.RotationZVelocity *= scaleAmount;
            e.EmissionSettings.RotationZVelocityRange *= scaleAmount;

			e.SecondFrequency /= scaleAmount;
			e.SecondsLasting /= scaleAmount;

		}
		
		#endregion


	}
}
