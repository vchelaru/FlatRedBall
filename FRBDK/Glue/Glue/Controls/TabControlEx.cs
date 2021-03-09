using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace FlatRedBall.Glue.Controls
{
    public delegate bool PreRemoveTab(int indx);
    public class TabControlEx : TabControl
    {
        [Description("Does not add close button to first."),
         Category("Closable Tab Control"),
         Browsable(true)]
        public bool IgnoreFirst { get; set; }


        public TabControlEx()
            : base()
        {
            PreRemoveTabPage = null;
            this.DrawMode = TabDrawMode.OwnerDrawFixed;

            this.ControlAdded += HandleControlAdded;
        }
        public PreRemoveTab PreRemoveTabPage;


        private void HandleControlAdded(object sender, ControlEventArgs e)
        {

            if(e.Control is PluginTabPage)
            {
                var pluginTab = e.Control as PluginTabPage;

                pluginTab.LastTabControl = this;
                pluginTab.RightClickCloseClicked += HandleTabRightClickClose;

            }
        }

        private void HandleTabRightClickClose(object sender, EventArgs e)
        {
            var control = sender as PluginTabPage;

            var index = TabPages.IndexOf(control);

            if(index > -1)
            {
                CloseTab(index);
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            TabPage tabPage = this.TabPages[e.Index];

            bool shouldDrawX = false;
            if (tabPage is PluginTabPage)
            {
                shouldDrawX = ((PluginTabPage)tabPage).DrawX;
            }

            Rectangle boundsRectangle = e.Bounds;
            boundsRectangle = GetTabRect(e.Index);
            boundsRectangle.Offset(2, 2);
            boundsRectangle.Width = 5;
            boundsRectangle.Height = 5;

            Brush blackBrush = new SolidBrush(Color.Black);            
            
            if (shouldDrawX)
            {


                if (e.Index != 0 || !IgnoreFirst)
                {
                    Pen p = new Pen(blackBrush);
                    e.Graphics.DrawLine(p, boundsRectangle.X, boundsRectangle.Y, boundsRectangle.X + boundsRectangle.Width, boundsRectangle.Y + boundsRectangle.Height);
                    e.Graphics.DrawLine(p, boundsRectangle.X + boundsRectangle.Width, boundsRectangle.Y, boundsRectangle.X, boundsRectangle.Y + boundsRectangle.Height);
                }
            }

            string titel = this.TabPages[e.Index].Text;
            Font f = this.Font;
            e.Graphics.DrawString(titel, f, blackBrush, new PointF(boundsRectangle.X, boundsRectangle.Y));
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            Point p = e.Location;
            int start;

            if (IgnoreFirst)
                start = 1;
            else
                start = 0;

            for (int i = start; i < TabCount; i++)
            {
                TabPage tabPage = this.TabPages[i];

                if (tabPage is PluginTabPage)
                {
                    var pluginTab = tabPage as PluginTabPage;

                    var tabRect = GetTabRect(i);

                    bool clickedTab = tabRect.Contains(p);

                    if(e.Button == System.Windows.Forms.MouseButtons.Left && clickedTab)
                    {
                        pluginTab.LastTimeClicked = DateTime.Now;
                    }

                    bool shouldDrawX = ((PluginTabPage)tabPage).DrawX;
                    if (shouldDrawX)
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            Rectangle r = GetTabRect(i);
                            r.Offset(2, 2);
                            r.Width = 5;
                            r.Height = 5;
                            if (r.Contains(p))
                            {
                                CloseTab(i);
                            }
                        }
                        else if (e.Button == MouseButtons.Middle)
                        {
                            CloseTab(i);
                        }

                    }
                    if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    {
                        if (clickedTab)
                        {
                            this.SelectedIndex = i;
                            pluginTab.RefreshRightClickCommands();
                            // not sure why we have to subtract the height, but if we don't then the menu
                            // seems to be offset by the height of the tab
                            pluginTab.ContextMenu.Show(pluginTab, new Point(e.X, e.Y - tabRect.Height));
                        }
                    }
                }
            }
        }

        private void CloseTab(int i)
        {
            if (PreRemoveTabPage != null)
            {
                bool closeIt = PreRemoveTabPage(i);
                if (!closeIt)
                    return;
            }

            TabPage tab = TabPages[i];

            TabPages.Remove(tab);

            if (tab is PluginTabPage)
                ((PluginTabPage)tab).CloseTabByUser();
        }
    }
}
