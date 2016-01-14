using NewProjectCreator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NewProjectCreator.Managers
{
    public class MessageBoxShower : IPopUpUi
    {
        public void Show(string message)
        {
            MessageBox.Show(message);
        }
    }
}
