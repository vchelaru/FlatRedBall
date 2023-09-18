using OfficialPlugins.UndoPlugin.NewFolder;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.UndoPlugin.Views
{
    /// <summary>
    /// Interaction logic for UndoDisplay.xaml
    /// </summary>
    public partial class UndoDisplay : UserControl
    {
        public UndoDisplay()
        {
            InitializeComponent();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            UndoManager.HandleUndo();
        }
    }
}
