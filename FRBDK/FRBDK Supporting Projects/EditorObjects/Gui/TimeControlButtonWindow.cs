using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    public class TimeControlButtonWindow : Window
    {
        #region Fields

		public Button toStartButton;
		public ToggleButton playButton;
		public Button stopButton;

        #endregion    
    
        #region Events

        public event GuiMessage ToStartClick;
        public event GuiMessage PlayClick;
        public event GuiMessage StopClick;

        #endregion

        #region Event and Delegate Methods

        void toStartButtonClick(Window callingWindow)
        {
            if (ToStartClick != null)
            {
                ToStartClick(this);
            }
        }

        void stopButtonClick(Window callingWindow)
        {
            if (playButton.IsPressed)
            {
                playButton.Unpress();
            }

            if (StopClick != null)
            {
                StopClick(this);
            }
        }

        void playButtonClick(Window callingWindow)
        {
            if (PlayClick != null)
            {
                PlayClick(this);
            }
        }

        #endregion


        #region Methods

        public TimeControlButtonWindow(Cursor cursor) : base(cursor)
        {
			//GuiManager.AddWindow(this);

			SetPositionTL(5.2f, 40);
			ScaleX = 5.2f;
			ScaleY = 1.95f;
			mName = "Time Controls";
			mMoveBar = true;


            toStartButton = new Button(mCursor);
            AddWindow(toStartButton);
			toStartButton.SetPositionTL(1.9f, 1.9f);
			toStartButton.ScaleX = 1.5f;
			toStartButton.ScaleY = 1.5f;
			toStartButton.SetOverlayTextures(10, 1);
			toStartButton.Click += new GuiMessage(toStartButtonClick);
            toStartButton.Text = "To Start";


            stopButton = new Button(mCursor);
            AddWindow(stopButton);
			stopButton.SetPositionTL(5.1f, 1.9f);
			stopButton.ScaleX = 1.5f;
			stopButton.ScaleY = 1.5f;
			stopButton.SetOverlayTextures(11, 1);
			stopButton.Click += new GuiMessage(stopButtonClick);
            stopButton.Text = "Stop";

            playButton = new ToggleButton(mCursor);
            AddWindow(playButton);
			playButton.SetPositionTL(8.3f, 1.9f);
			playButton.ScaleX = 1.5f;
			playButton.ScaleY = 1.5f;
			playButton.SetOverlayTextures(9, 1);
			playButton.Click += new GuiMessage(playButtonClick);
            playButton.Text = "Play";

        }

        #endregion

    }
}
