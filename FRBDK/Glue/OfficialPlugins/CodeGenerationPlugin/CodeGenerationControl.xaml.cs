using FlatRedBall.Glue.Plugins.ExportedImplementations;
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

namespace OfficialPlugins.CodeGenerationPlugin
{
    /// <summary>
    /// Interaction logic for CodeGenerationControl.xaml
    /// </summary>
    public partial class CodeGenerationControl : UserControl
    {
        public CodeGenerationControl()
        {
            InitializeComponent();
        }

        private void HandleGenerateEverythingClick(object sender, RoutedEventArgs e)
        {

            GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
        }
    }
}
