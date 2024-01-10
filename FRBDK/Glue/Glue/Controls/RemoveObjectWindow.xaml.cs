using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// Interaction logic for RemoveObjectWindow.xaml
    /// </summary>
    public partial class RemoveObjectWindow : Window
    {
        public RemoveObjectWindow()
        {
            InitializeComponent();

            Loaded += HandleLoaded;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            GlueCommands.Self.DialogCommands.MoveToCursor(this);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

    }
}
