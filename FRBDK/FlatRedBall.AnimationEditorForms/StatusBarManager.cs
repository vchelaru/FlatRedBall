using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms
{
    class StatusBarManager : Singleton< StatusBarManager >
    {
        StatusStrip mStatusStrip;
        ToolStripStatusLabel mCursorPositionLabel;
        public void Initialize(StatusStrip statusStrip, ToolStripStatusLabel cursorPositionLabel)
        {
            mStatusStrip = statusStrip;
            mCursorPositionLabel = cursorPositionLabel;
        }

        public void SetCursorPosition(float x, float y)
        {
            mCursorPositionLabel.Text = string.Format("Cursor({0}, {1})", x, y);

        }
    }
}
