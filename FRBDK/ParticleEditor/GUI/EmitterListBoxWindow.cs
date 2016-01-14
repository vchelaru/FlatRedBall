using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Utilities;
#if FRB_XNA
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif

namespace ParticleEditor.GUI
{
	/// <summary>
	/// Summary description for EmitterListBoxWindow.
	/// </summary>
	public class EmitterListBoxWindow : CollapseWindow
	{
		#region Fields

        static int NumberOfEmittersCreated = 0;

        ListDisplayWindow mListDisplayWindow;
		Button addEmitter;
		Button deleteEmitter;


		#endregion

        #region Event Methods

        public void addEmitterClick(FlatRedBall.Gui.Window callingWindow)
        {
            TextInputWindow tempWindow = GuiManager.ShowTextInputWindow("Enter a name for the new Emitter", "Add Particle");

            tempWindow.Text = "Emitter" + NumberOfEmittersCreated;
            NumberOfEmittersCreated++;

            tempWindow.OkClick += new GuiMessage(addEmitterOkClick);
        }

        public void addEmitterOkClick(FlatRedBall.Gui.Window callingWindow)
        {

            Emitter newEmitter = new Emitter();

            SpriteManager.AddEmitter(newEmitter);

            ShapeManager.AddPolygon(newEmitter.EmissionBoundary);

            EditorData.SetDefaultValuesOnEmitter(newEmitter);

            EditorData.Emitters.Add(newEmitter);

            newEmitter.Name = ((TextInputWindow)callingWindow).Text;

            newEmitter.Texture = FlatRedBallServices.Load<Texture2D>("content/redball.bmp", 
            AppState.Self.PermanentContentManager);

        }

        private void DeleteEmitterClick(FlatRedBall.Gui.Window callingWindow)
        {
            EditorData.EditorLogic.DeleteCurrentEmitter();

        }

        private void FocusUpdate(IInputReceiver inputReceiver)
        {
            if (InputManager.Keyboard.KeyPushed(Keys.Delete))
            {
                EditorData.EditorLogic.DeleteCurrentEmitter();
                
            }

            if (InputManager.Keyboard.KeyPushed(Keys.Space) && AppState.Self.CurrentEmitter != null)
            {
                AppState.Self.CurrentEmitter.Emit(null);

            }

            if (InputManager.Keyboard.KeyPushed(Keys.C))
            {
                GuiData.ActivityWindow.ClearAllParticles();
            }
        }

        private void PositionAndScaleChildrenWindows(Window callingWindow)
        {

            addEmitter.ScaleX = (ScaleX - .5f) / 2.0f;
            addEmitter.ScaleY = 1.3f;
            addEmitter.SetPositionTL(addEmitter.ScaleX + .5f, 2 * ScaleY - 1.5f);

            deleteEmitter.ScaleX = (ScaleX - .5f) / 2.0f;
            deleteEmitter.ScaleY = 1.3f;
            deleteEmitter.SetPositionTL(addEmitter.X + addEmitter.ScaleX + deleteEmitter.ScaleX + .25f, 2 * ScaleY - 1.5f);

            mListDisplayWindow.SetPositionTL(ScaleX, ScaleY - 1.7f);
            mListDisplayWindow.ScaleX = ScaleX;
            mListDisplayWindow.ScaleY = ScaleY-1.7f;
        }

        #endregion

        #region Methods

        #region Constructor

        public EmitterListBoxWindow(GuiMessages messages) : base(GuiManager.Cursor)
		{
			#region this Properties
			GuiManager.AddWindow(this);
			SetPositionTL(95.3f, 17.6167374f);
			ScaleX = 15;
			ScaleY = 12;

            MinimumScaleX = 12.5f;
            MinimumScaleY = 7;

			HasCloseButton = true;
			mMoveBar = true;
			mName = "Emitters";
            Resizable = true;
            Resizing += PositionAndScaleChildrenWindows;
			#endregion		

            mListDisplayWindow = new ListDisplayWindow(mCursor);
            this.AddWindow(mListDisplayWindow);
            mListDisplayWindow.DrawOuterWindow = false;
            mListDisplayWindow.ListBox.Highlight += new GuiMessage(messages.EmitterArrayListBoxClick);
            mListDisplayWindow.AllowCopy = true;
            mListDisplayWindow.AfterItemsPasted += new GuiMessage(mListDisplayWindow_AfterItemsPasted);
            mListDisplayWindow.ListBox.FocusUpdate += FocusUpdate;
            mListDisplayWindow.AllowReordering = true;

            addEmitter = new Button(mCursor);
            AddWindow(addEmitter);
			addEmitter.Text = "Add Emitter";
			addEmitter.Click += new GuiMessage(addEmitterClick);

            deleteEmitter = new Button(mCursor);
            AddWindow(deleteEmitter);
			deleteEmitter.Text = "Delete Emitter";
			deleteEmitter.Click += new GuiMessage(DeleteEmitterClick);

            PositionAndScaleChildrenWindows(null);
        }

        void mListDisplayWindow_AfterItemsPasted(Window callingWindow)
        {
            foreach (object o in ((ListDisplayWindow)callingWindow).LastItemsPasted)
            {
                Emitter emitter = (Emitter)o;

                SpriteManager.AddEmitter(emitter);

                StringFunctions.MakeNameUnique<Emitter>(emitter, EditorData.Emitters);
            }
        }

        #endregion

        #region Public Methods

        public CollapseItem GetItem(object referenceObject)
        {
            return mListDisplayWindow.ListBox.GetItem(referenceObject);
        }

        public object GetFirstHighlightedObject()
        {
            return mListDisplayWindow.GetFirstHighlightedObject();
        }

        public void Sort()
        {
            mListDisplayWindow.ListBox.Sort();
        }

        public void Update()
        {
            mListDisplayWindow.ListShowing = EditorData.Emitters;
        }

        #endregion

        #region Private Methods



        #endregion

        #endregion
    }
}
