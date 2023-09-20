using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;
using System.Windows.Controls;

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
