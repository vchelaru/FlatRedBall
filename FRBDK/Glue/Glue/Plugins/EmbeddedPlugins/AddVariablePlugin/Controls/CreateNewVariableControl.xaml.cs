using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Controls.AddVariable
{
    /// <summary>
    /// Interaction logic for CreateNewVariableControl.xaml
    /// </summary>
    public partial class CreateNewVariableControl : UserControl
    {
        public string VariableName => TextBox.Text;

        public string SelectedType
        {
            get
            {

                return (string)ListBox.SelectedValue;
            }
        }

        public CreateNewVariableControl()
        {
            InitializeComponent();
        }

        public void FillAvailableTypes(List<string> types)
        {
            foreach(var item in types)
            {
                ListBox.Items.Add(item);
            }

            ListBox.SelectedIndex = 0;
        }

        public void FocusTextBox()
        {
            TextBox.Focus();
        }
    }
}
