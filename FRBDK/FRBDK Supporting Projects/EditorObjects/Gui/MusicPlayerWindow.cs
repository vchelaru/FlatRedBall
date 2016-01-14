using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Audio;
using FlatRedBall;
using FlatRedBall.IO;
using FlatRedBall.Input;
using EditorObjects.Data;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Instructions;

namespace EditorObjects.Gui
{
    public class MusicPlayerWindow : Window
    {
        #region Fields

        Music mMusic;

        TextBox mSongLabel;



        Button mToStart;
        ToggleButton mPlay;
        ToggleButton mPause;
        Button mStop;
        Button mLoadSong;

        TimeLine mMusicTime;

        double mPausedTime;

        PropertyGrid<RecordingData> mRecordingDataPropertyGrid;

        Sprite mBeatSprite;
        Circle mBeatCircle;

        ComboBox mForcePlaybackComboBox;

        #endregion

        #region Properties

        public Sprite BeatSprite
        {
            get { return mBeatSprite; }
            set 
            { 
                mBeatSprite = value;
                mBeatCircle = ShapeManager.AddCircle();
                mBeatCircle.Radius = mBeatSprite.ScaleX + 2;
                mBeatCircle.Position = mBeatSprite.Position;
            }
        }

        public bool IsPaused
        {
            get 
            {
                if (mCursor.PrimaryDown && mCursor.WindowPushed == mPause && mCursor.WindowOver == mPause)
                {

                    return !mPause.IsPressed;
                }
                else
                {
                    return mPause.IsPressed;
                }
                

            }
        }

        public bool IsPlayingBack
        {
            get
            {
                if (mMusic == null)
                {
                    return false;
                }
                else
                {
                    if ((bool)mForcePlaybackComboBox.SelectedObject)
                    {
                        return true;
                    }
                    else
                    {

                        RecordingData recordingData = mRecordingDataPropertyGrid.SelectedObject;

                        return mMusic.Playing && mMusic.CurrentPosition < recordingData.LastBeatTime;
                    }
                }
            }
        }

        public Music Music
        {
            get { return mMusic; }
        }

        #endregion

        #region Event Methods

        private void OpenSongWindow(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            List<string> fileTypes = new List<string>();
            fileTypes.Add("mp3");
            fileTypes.Add("wav");
            // FRB MDX doesn't support m4a
            //fileTypes.Add("m4a");

            fileWindow.SetFileType(fileTypes);

            fileWindow.SetToLoad();

            fileWindow.OkClick += LoadSongOk;
        }

        private void LoadSongOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            mMusic = new Music(fileName, false);
            mMusic.IsPlaybackAccurate = true;

            const bool prePlay = true;

            if (prePlay)
            {
                mMusic.Play();
                mMusic.Stop();
            }

            mMusicTime.MaximumValue = mMusic.Duration;

            mMusicTime.ValueWidth = mMusic.Duration;

            mSongLabel.Text = FileManager.RemovePath(fileName);
        }

        private void PauseClick(Window callingWindow)
        {
            if (mPause.IsPressed)
            {
                mPausedTime = mMusic.CurrentPosition;
                mMusic.Stop();
            }
            else
            {
                mPlay.OnClick();
                mMusic.CurrentPosition = mPausedTime;
            }
        }

        private void PlayClick(Window callingWindow)
        {
            if (mMusic != null)
            {
                mMusic.Play();
            }
            else
            {
                mPlay.Unpress();
            }
        }

        private void SetSongTimeFromTimeLine(Window callingWindow)
        {
            if (mMusic != null)
            {
                mMusic.CurrentPosition = ((TimeLine)callingWindow).CurrentValue;
            }
        }

        private void StopClick(Window callingWindow)
        {
            if (mMusic != null)
            {
                mMusic.Stop();
            }

            mPlay.Unpress();
        }

        private void ToStartClick(Window callingWindow)
        {
            if (mMusic != null)
            {
                mMusic.CurrentPosition = 0;
            }
            mMusicTime.CurrentValue = 0;
        }

        #endregion

        #region Methods

        #region Constructor

        public MusicPlayerWindow(Cursor cursor) : base(cursor)
        {
            SetThisProperties();

            CreateButtons();

            CreateTextBox();

            CreateTimeLine();

            CreatePropertyGrid();

            CreateForcePlaybackComboBox();
        }

        #endregion

        #region Public Methods

        public void Activity()
        {
            MusicActivity();

            ListenForBeatPush();

            UpdateTimeLinePosition();

            PlaybackBeatActivity();
        }

        #endregion

        #region Private Methods

        private void CreateButtons()
        {
            #region Consts used in button creation

            const float buttonScaleX = 2.0f;
            const float buttonScaleY = 1.5f;
            const float distanceBetweenButtons = .5f;
            const float buttonY = 5;

            #endregion

            int buttonsAddedSoFar = 0;

            #region To Start
            mToStart = new Button(mCursor);
            AddWindow(mToStart);
            mToStart.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\ToStartButton.png", FlatRedBallServices.GlobalContentManager), 
                null);
            mToStart.ScaleX = buttonScaleX;
            mToStart.ScaleY = buttonScaleY;
            mToStart.X = distanceBetweenButtons + buttonScaleX + buttonsAddedSoFar * (distanceBetweenButtons + 2 * buttonScaleX);
            mToStart.Y = buttonY;
            mToStart.Click += ToStartClick;
            buttonsAddedSoFar++;
            #endregion

            #region Play 

            mPlay = new ToggleButton(mCursor);
            AddWindow(mPlay);
            mPlay.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\PlayButton.png", FlatRedBallServices.GlobalContentManager), 
                null);
            mPlay.ScaleX = buttonScaleX;
            mPlay.ScaleY = buttonScaleY;
            mPlay.X = distanceBetweenButtons + buttonScaleX + buttonsAddedSoFar * (distanceBetweenButtons + 2 * buttonScaleX);
            mPlay.Y = buttonY;
            mPlay.Click += PlayClick;
            buttonsAddedSoFar++;

            #endregion

            #region Pause

            mPause = new ToggleButton(mCursor);
            AddWindow(mPause);
            mPause.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\PauseButton.png", FlatRedBallServices.GlobalContentManager),
                null);
            mPause.ScaleX = buttonScaleX;
            mPause.ScaleY = buttonScaleY;
            mPause.X = distanceBetweenButtons + buttonScaleX + buttonsAddedSoFar * (distanceBetweenButtons + 2 * buttonScaleX);
            mPause.Y = buttonY;
            mPause.Click += PauseClick;
            buttonsAddedSoFar++;

            #endregion

            mStop = new Button(mCursor);
            AddWindow(mStop);
            mStop.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\StopButton.png", FlatRedBallServices.GlobalContentManager),
                null);
            mStop.ScaleX = buttonScaleX;
            mStop.ScaleY = buttonScaleY;
            mStop.X = distanceBetweenButtons + buttonScaleX + buttonsAddedSoFar * (distanceBetweenButtons + 2 * buttonScaleX);
            mStop.Y = buttonY;
            mStop.Click += StopClick;
            buttonsAddedSoFar++;

            mLoadSong = new Button(mCursor);
            AddWindow(mLoadSong);
            mLoadSong.SetOverlayTextures(
                FlatRedBallServices.Load<Texture2D>(@"Content\EjectButton.png", FlatRedBallServices.GlobalContentManager),
                null);
            mLoadSong.ScaleX = buttonScaleX;
            mLoadSong.ScaleY = buttonScaleY;
            mLoadSong.X = distanceBetweenButtons + buttonScaleX + buttonsAddedSoFar * (distanceBetweenButtons + 2 * buttonScaleX);
            mLoadSong.Y = buttonY;
            mLoadSong.Click += OpenSongWindow;
            buttonsAddedSoFar++;
        }

        private void CreateForcePlaybackComboBox()
        {
            mForcePlaybackComboBox = new ComboBox(mCursor);
            AddWindow(mForcePlaybackComboBox);

            mForcePlaybackComboBox.AddItem("Plaback if before last beat", false);
            mForcePlaybackComboBox.AddItem("Always Playback", true);

            mForcePlaybackComboBox.Text = "Plaback if before last beat";
            mForcePlaybackComboBox.SelectedObject = false;

            mForcePlaybackComboBox.ScaleX = 12.5f;
            mForcePlaybackComboBox.X = 13f;
            mForcePlaybackComboBox.Y = 13.2f;
        }

        private void CreatePropertyGrid()
        {
            mRecordingDataPropertyGrid = new PropertyGrid<RecordingData>(mCursor);

            mRecordingDataPropertyGrid.ExcludeMember("LastBeatTime");

            mRecordingDataPropertyGrid.GetUIElementForMember("ClosestFitBeatFrequency").ScaleX = 10;
            mRecordingDataPropertyGrid.SetMemberDisplayName("ClosestFitBeatFrequency", "Frequency");
            
            mRecordingDataPropertyGrid.GetUIElementForMember("ClosestFitOffset").ScaleX = 10;
            mRecordingDataPropertyGrid.SetMemberDisplayName("ClosestFitOffset", "Offset");



            this.AddWindow(mRecordingDataPropertyGrid);
            mRecordingDataPropertyGrid.X = mRecordingDataPropertyGrid.ScaleX + .5f;
            mRecordingDataPropertyGrid.Y = 23f;

            mRecordingDataPropertyGrid.SelectedObject = new RecordingData();
            mRecordingDataPropertyGrid.HasMoveBar = false;


        }

        private void CreateTextBox()
        {
            mSongLabel = new TextBox(mCursor);
            AddWindow(mSongLabel);
            mSongLabel.TakingInput = false;
            mSongLabel.ScaleX = 12;
            mSongLabel.X = 12.5f;
            mSongLabel.Y = 2;
        }

        private void CreateTimeLine()
        {
            mMusicTime = new TimeLine(this.mCursor);
            mMusicTime.ScaleX = 19;


            mMusicTime.GuiChange += SetSongTimeFromTimeLine;

            AddWindow(mMusicTime);
            mMusicTime.X = mMusicTime.ScaleX + .5f;
            mMusicTime.Y = 10f;

            mMusicTime.SmallVerticalBarIncrement = 5;
            mMusicTime.VerticalBarIncrement = 25;
        }

        private void ListenForBeatPush()
        {
            if (mMusic == null)
            {
                return;
            }

            if (InputManager.Keyboard.KeyPushed(Microsoft.DirectX.DirectInput.Key.Space))
            {
                mRecordingDataPropertyGrid.SelectedObject.RecordBeat(mMusic.CurrentPosition);
            }

            mRecordingDataPropertyGrid.UpdateDisplayedProperties();
        }

        private void MusicActivity()
        {
            if (mMusic != null)
            {
                mMusic.Activity();
            }
        }

        private void PlaybackBeatActivity()
        {
            if (mBeatSprite != null && IsPlayingBack)
            {
                RecordingData recordingData = mRecordingDataPropertyGrid.SelectedObject;

                double timeIntoSongThisFrame = TimeManager.CurrentTime - mMusic.TimeStarted;
                                                // Give the second difference a little extra to resolve any floating point issues.
                double timeIntoSongLastFrame = TimeManager.CurrentTime - mMusic.TimeStarted - TimeManager.SecondDifference * 1.05;

                if(recordingData.GetNumberOfBeatsIntoSong(timeIntoSongLastFrame) != 
                    recordingData.GetNumberOfBeatsIntoSong(timeIntoSongThisFrame))
                {
                     mBeatSprite.Alpha = 255;
                    mBeatSprite.AlphaRate = -400;

                    mBeatCircle.Visible = true;
                    mBeatCircle.Instructions.Add(new Instruction<Circle, bool>(mBeatCircle, "Visible", false, TimeManager.CurrentTime + .05));
                }

                // Test the beats to see if we have a new one.  If so, make the Sprite flash.
            }
        }

        private void SetThisProperties()
        {
            this.ScaleX = 22;
            this.ScaleY = 16f;
            // don't give it a move bar or close bar
        }

        private void UpdateTimeLinePosition()
        {
            if (mMusic != null)
            {
                if (!IsPaused)
                {
                    mMusicTime.CurrentValue = mMusic.CurrentPosition;
                }
            }
        }

        #endregion

        #endregion
    }
}
