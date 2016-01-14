using System;

namespace FlatRedBallWpf
{
    public partial class FlatRedBallControl
    {
        public FlatRedBallControl()
        {
            InitializeComponent();

            this.Focusable = true;
        }

        public IntPtr Handle
        {
            get { return GamePanel.Handle; }
        }

        public bool IsPanelFocused
        {
            get { return Panel.IsFocused; }
        }

    }
}