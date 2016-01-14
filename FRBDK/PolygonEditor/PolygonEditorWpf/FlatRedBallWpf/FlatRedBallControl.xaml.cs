using System;

namespace PolygonEditor
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