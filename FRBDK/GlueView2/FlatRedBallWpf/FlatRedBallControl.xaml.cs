using System;

namespace FlatRedBallWpf
{
    public partial class FlatRedBallControl
    {
        public FlatRedBallControl()
        {
            InitializeComponent();
        }

        public IntPtr Handle
        {
            get { return GamePanel.Handle; }
        }
    }
}