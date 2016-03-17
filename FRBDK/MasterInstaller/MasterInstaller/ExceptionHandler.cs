using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MasterInstaller
{
    class ExceptionHandler
    {
        public static void HandleException(Exception e)
        {
            MessageBox.Show("Error installing FlatRedBall: " + e.ToString());
        }
    }
}
