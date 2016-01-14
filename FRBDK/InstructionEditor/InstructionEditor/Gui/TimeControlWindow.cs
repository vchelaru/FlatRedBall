using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Instructions;
using FlatRedBall;

namespace InstructionEditor.Gui
{
    public class TimeControlWindow : Window
    {
        #region Fields

        static Button toStartButton;
        static ToggleButton playButton;
        static Button stopButton;
        static ToggleButton mCycleButton;

        #endregion

        #region Properties

        public bool Cycling
        {
            get { return mCycleButton.IsPressed; }
        }

        #endregion

        #region Event Methods

        void PlayPushed(Window callingWindow)
        {
            #region If current keyframe list

            if (EditorData.EditorLogic.CurrentKeyframeList != null)
            {
                if (Cycling)
                {
                    EditorData.EditorLogic.CurrentKeyframeList.InstructionToExecuteAtEnd =
                        new MethodInstruction<TimeControlWindow>(
                        this, "ToStartPushed", new object[] { null }, 0);
                }
                else
                {
                    EditorData.EditorLogic.CurrentKeyframeList.InstructionToExecuteAtEnd = null;
                }

                InstructionList velocityList =
                    EditorData.EditorLogic.CurrentKeyframeList.CreateVelocityListAtTime(
                        GuiData.TimeLineWindow.CurrentValue);

                foreach (Instruction instruction in velocityList)
                {
                    // add the current time but subtract the time in the velocity list
                    instruction.TimeToExecute += TimeManager.CurrentTime - GuiData.TimeLineWindow.CurrentValue;

                }

                velocityList.ExecuteAndRemoveOrCyclePassedInstructions();

                InstructionManager.Instructions.AddRange(velocityList);

            }
            #endregion

            #region else, if there is an AnimationSequence

            else if (EditorData.EditorLogic.CurrentAnimationSequence != null)
            {
                EditorData.EditorLogic.CurrentAnimationSequence.Play( 
                    GuiData.TimeLineWindow.CurrentValue,
                    this.Cycling
                    );
            }

            #endregion

            GuiData.TimeLineWindow.IsPlaying = true;

        }

        void StopPushed(Window callingWindow)
        {
            // Not sure if this'll work in the future, but for now this'll stop everything.
            InstructionManager.PauseEngine(false);
            playButton.Unpress();

            GuiData.TimeLineWindow.IsPlaying = false;

        }

        public void ToStartPushed(Window callingWindow)
        {
            bool shouldPlay = playButton.IsPressed;

            GuiData.TimeLineWindow.timeLine.CurrentValue = 0;
            GuiData.TimeLineWindow.timeLine.Start = 0;
            GuiData.TimeLineWindow.timeLine.CallOnGUIChange();
            EditorData.instructionPlayer.ChangeTime();

            StopPushed(null);

            if (shouldPlay)
            {
                playButton.Press();
            }
        }

        #endregion
        
        #region Methods

        public TimeControlWindow()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            SetPositionTL(11.5f, 72.6f);
            ScaleX = 11.5f;
            ScaleY = 1.95f;
            HasMoveBar = true;
            Name = "Time Controls";

            toStartButton = AddButton();
            toStartButton.SetPositionTL(1.9f, 1.9f);
            toStartButton.ScaleX = 1.5f;
            toStartButton.ScaleY = 1.5f;
            toStartButton.SetOverlayTextures(10, 1);
            toStartButton.Text = "To Start of Instruction Set";
            toStartButton.Click += new GuiMessage(ToStartPushed);

            stopButton = AddButton();
            stopButton.SetPositionTL(11.5f, 1.9f);
            stopButton.ScaleX = 1.5f;
            stopButton.ScaleY = 1.5f;
            stopButton.SetOverlayTextures(11, 1);
            stopButton.Text = "Stop";
            stopButton.Click += new GuiMessage(StopPushed);


            playButton = AddToggleButton();
            playButton.SetPositionTL(14.7f, 1.9f);
            playButton.ScaleX = 1.5f;
            playButton.ScaleY = 1.5f;
            playButton.SetOverlayTextures(9, 1);
            playButton.Text = "Play";
            playButton.Click += new GuiMessage(PlayPushed);

            mCycleButton = AddToggleButton();
            mCycleButton.SetPositionTL(17.9f, 1.9f);
            mCycleButton.ScaleX = 1.5f;
            mCycleButton.ScaleY = 1.5f;
            mCycleButton.SetOverlayTextures(8, 3);
            mCycleButton.SetText("Cycle Off", "Cycle On");


        }

        #endregion

    }
}
