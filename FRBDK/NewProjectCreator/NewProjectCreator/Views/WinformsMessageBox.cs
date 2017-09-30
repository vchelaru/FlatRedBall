using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewProjectCreator.Views
{
    public interface IMessageBox
    {
        void Show(string message);

    }

    public class WinformsMessageBox : IMessageBox
    {
        public void Show(string message)
        {
            MessageBox.Show(message);
        }
    }
}
