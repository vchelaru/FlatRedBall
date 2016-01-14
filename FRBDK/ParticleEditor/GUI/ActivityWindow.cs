using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;
using FlatRedBall.Graphics.Particle;

namespace ParticleEditor.GUI
{
    public class ActivityWindow : Window
    {
        #region Fields
        Button emitCurrent;
		Button emitAll;
		ToggleButton startStopCurrent;
		ToggleButton startStopAll;
		Button clearAllButton;
        TextDisplay numberOfSpritesDisplay;

        #endregion

        #region Properties

        public bool TimedEmitAll
        {
            get { return startStopAll.IsPressed; }
        }

        public bool TimedEmitCurrent
        {
            get { return startStopCurrent.IsPressed; }
        }

        #endregion

        #region Event Methods
        private void emitCurrentClick(FlatRedBall.Gui.Window callingWindow)
        {
            if (AppState.Self.CurrentEmitter != null)
            {
                AppState.Self.CurrentEmitter.Emit(null);
            }
        }

        private void emitAllClick(FlatRedBall.Gui.Window callingWindow)
        {
            foreach (Emitter emitter in EditorData.Emitters)
            {
                emitter.Emit();
            }
        }

        private void clearAllButtonClick(FlatRedBall.Gui.Window callingWindow)
        {
            SpriteManager.RemoveAllParticleSprites();
        }
        #endregion

        #region Methods
        public ActivityWindow()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            ScaleX = 10;
            ScaleY = 6.5f;
            HasCloseButton = true;
            HasMoveBar = true;
            SetPositionTL(10, 52f);

            emitCurrent = new Button(mCursor);
            AddWindow(emitCurrent);
            emitCurrent.ScaleX = 4.8f;
            emitCurrent.SetPositionTL(ScaleX - 4.6f, 2);
            emitCurrent.ScaleY = 1f;
            emitCurrent.Text = "Emit Current";
            emitCurrent.Click += new GuiMessage(emitCurrentClick);

            emitAll = new Button(mCursor);
            AddWindow(emitAll);
            emitAll.ScaleX = 4.5f;
            emitAll.SetPositionTL(ScaleX + 5, 2);
            emitAll.ScaleY = 1f;
            emitAll.Text = "Emit All";
            emitAll.Click += new GuiMessage(emitAllClick);


            startStopCurrent = new ToggleButton(mCursor);
            AddWindow(startStopCurrent);
            startStopCurrent.ScaleX = 4.8f;
            startStopCurrent.SetText("Start Current", "Stop Current");
            startStopCurrent.SetPositionTL(ScaleX - 4.6f, 5);

            startStopAll = new ToggleButton(mCursor);
            AddWindow(startStopAll);
            startStopAll.ScaleX = 4.5f;
            startStopAll.SetText("Start All", "Stop All");
            startStopAll.SetPositionTL(ScaleX + 5, 5);

            clearAllButton = new Button(mCursor);
            AddWindow(clearAllButton);
            clearAllButton.ScaleX = 8.5f;
            clearAllButton.ScaleY = 1.5f;
            clearAllButton.SetPositionTL(ScaleX, 8);
            clearAllButton.Text = ("Clear All");
            clearAllButton.Click += new GuiMessage(clearAllButtonClick);

            numberOfSpritesDisplay = new TextDisplay(mCursor);
            AddWindow(numberOfSpritesDisplay);
            numberOfSpritesDisplay.Text = "Number of Sprites: 0";
            numberOfSpritesDisplay.SetPositionTL(ScaleX - 10, 11);
        }

        public void ClearAllParticles()
        {
            clearAllButtonClick(null);
        }

        public void Update()
        {
            numberOfSpritesDisplay.Text = "Number of Particles: " +
                SpriteManager.ParticleCount;
        }

        #endregion
    }
}
