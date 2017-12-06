using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Controls
{
    public class CustomNodeColorTreeView : TreeView
    {
        public CustomNodeColorTreeView()
        {
            this.DrawMode = TreeViewDrawMode.OwnerDrawText;
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            TreeNodeStates state = e.State;
            Font font = e.Node.NodeFont ?? e.Node.TreeView.Font;
            Color fore = e.Node.ForeColor;
            if (fore == Color.Empty)
                fore = e.Node.TreeView.ForeColor;
            if (e.Node == e.Node.TreeView.SelectedNode)
            {
                var color = Color.Blue;

                if((state & TreeNodeStates.Focused) != TreeNodeStates.Focused)
                {
                    color = Color.FromArgb(30, 30, 255);
                }

                fore = SystemColors.HighlightText;
                e.Graphics.FillRectangle(new SolidBrush(color), e.Bounds);
                ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, fore, color);
                TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds, fore, color, TextFormatFlags.GlyphOverhangPadding);
            }
            else
            {
                e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds, fore, TextFormatFlags.GlyphOverhangPadding);
            }
        }
    }
}
