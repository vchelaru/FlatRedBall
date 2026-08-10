using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows;

namespace GlueFormsCore.Controls
{
    /// <summary>
    /// The single dialog every element delete asks its questions through. Its content is entirely driven by
    /// the DeleteOptionsViewModel it is given, so plugins extend it by adding options to that view model
    /// rather than by showing a dialog of their own. See GitHub issue #429.
    /// </summary>
    public partial class DeleteOptionsWindow : Window
    {
        public DeleteOptionsWindow()
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
