using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace TiledPluginCore.Views
{
    /// <summary>
    /// Interaction logic for TileNodeNetworkProperties.xaml
    /// </summary>
    public partial class TileNodeNetworkProperties : UserControl
    {
        public TileNodeNetworkProperties()
        {
            InitializeComponent();
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if(sender is TextBox tBox)
                {
                    DependencyProperty prop = TextBox.TextProperty;

                    BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                    if (binding != null) { binding.UpdateSource(); }
                }
                else if(sender is ComboBox comboBox)
                {
                    DependencyProperty prop = ComboBox.TextProperty;

                    BindingExpression binding = BindingOperations.GetBindingExpression(comboBox, prop);
                    if (binding != null) { binding.UpdateSource(); }
                }
            }
        }
    }
}
