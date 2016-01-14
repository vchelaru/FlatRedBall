using System.Windows.Forms;

namespace FlatRedBall.Glue
{
    public partial class WaitingForm : Form
    {
        private static WaitingForm _myForm;

        public WaitingForm()
        {
            InitializeComponent();
        }

        public void SetText(string text)
        {
            lblText.Text = text;
        }

        public static WaitingForm Self
        {
            get
            {
                if (_myForm == null)
                {
                    _myForm = new WaitingForm();
                }

                return _myForm;
            }
        }
    }
}
