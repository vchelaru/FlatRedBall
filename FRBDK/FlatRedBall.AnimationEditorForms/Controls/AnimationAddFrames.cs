using FlatRedBall.Content.AnimationChain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public partial class AnimationAddFrames: Form
    {
        private int NumberOfFramesLeft;
        private bool HasAFrame;
        public AnimationAddFrames(bool HasAFrame, int NumberOfFramesLeft)
        {
            InitializeComponent();

            FrameAddCount.Focus();
            FrameAddCount.Select(0, FrameAddCount.Text.Length);

            this.NumberOfFramesLeft = NumberOfFramesLeft;
            this.HasAFrame = HasAFrame;
            CheckFrameIncrementError();
        }

        public int AddCount { get { return (int)FrameAddCount.Value; } }
        public bool IncrementFrames { get { return FrameIncrement.Checked; } }

        public void CheckFrameIncrementError()
        {
            if (!HasAFrame)
            {
                FrameIncrement.Checked = false;
                FrameIncrement.Enabled = false;
                FrameIncrementError.Enabled = true;
                FrameIncrementError.Text = "Can't increment frame position if animation contains no frames!";
            }
            else if (NumberOfFramesLeft == -1)
            {
                FrameIncrementError.Text = "Unable to calculate how to increment frame...";
            }
            else if ((NumberOfFramesLeft > 0) && (FrameAddCount.Value > NumberOfFramesLeft))
            {
                FrameIncrementError.Visible = true;
                FrameIncrementError.Text = "Incrementing this many frames will exceed the texture bounds!";
            }
            else
            {
                FrameIncrementError.Visible = false;
            }
        }
    }
}
