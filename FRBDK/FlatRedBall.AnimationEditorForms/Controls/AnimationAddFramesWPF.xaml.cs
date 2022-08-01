using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    /// <summary>
    /// Interaction logic for AnimationAddFrames.xaml
    /// </summary>
    public partial class AnimationAddFramesWPF: Window
    {
        private int NumberOfFramesLeft;
        private bool HasAFrame;

        public AnimationAddFramesWPF(bool HasAFrame, int NumberOfFramesLeft)
        {
            InitializeComponent();

            FrameAddCount.Focus();
            FrameAddCount.Select(0, FrameAddCount.Text.Length);
            this.NumberOfFramesLeft = NumberOfFramesLeft;
            this.HasAFrame = HasAFrame;
            CheckFrameIncrementError();
        }

        public int GetCount()
        {
            if (int.TryParse(FrameAddCount.Text, out int result)) return result;
            return 0;
        }
        public int AddCount { get { return GetCount(); } }
        public bool IncrementFrames { get { return (bool)FrameIncrement.IsChecked; } }

        public void CheckFrameIncrementError()
        {
            if (!HasAFrame)
            {
                FrameIncrement.IsChecked = false;
                FrameIncrement.IsEnabled = false;
                FrameIncrementError.IsEnabled = true;
                FrameIncrementError.Content = "Can't increment frame position if animation contains no frames!";
            }
            else if (NumberOfFramesLeft == -1)
            {
                FrameIncrementError.Content = "Unable to calculate how to increment frame...";
            }
            else if ((NumberOfFramesLeft > 0) && (GetCount() > NumberOfFramesLeft))
            {
                FrameIncrementError.Visibility = Visibility.Visible;
                FrameIncrementError.Content = "Incrementing this many frames will exceed the texture bounds!";
            }
            else
            {
                FrameIncrementError.Visibility = Visibility.Hidden;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
