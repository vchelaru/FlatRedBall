using System.Drawing;
using System.Windows.Forms;

namespace MasterInstaller
{
    public sealed partial class LineSeparator : UserControl
    {
        public LineSeparator()
        {
            InitializeComponent();
            Paint += LineSeparator_Paint;
            MaximumSize = new Size(2000, 2);
            MinimumSize = new Size(0, 2);
        }

        private void LineSeparator_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawLine(Pens.DarkGray, new Point(0, 0), new Point(this.Width, 0));
            g.DrawLine(Pens.White, new Point(0, 1), new Point(this.Width, 1));
        }
    }
}
