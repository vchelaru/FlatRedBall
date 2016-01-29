using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WpfDataUi.Controls;

namespace OfficialPlugins.VariableDisplay.Controls
{
    public class FileReferenceComboBox : ComboBoxDisplay
    {
        public FileReferenceComboBox() : base()
        {
            AddRightColumn();
        }

        private void AddRightColumn()
        {
            var grid = base.Grid;

            var columnDefinition = new ColumnDefinition();
            columnDefinition.Width = new System.Windows.GridLength(30);

            grid.ColumnDefinitions.Add(columnDefinition);

            var button = new Button();
            button.Content = "View";
            Grid.SetColumn(button, 2);
            grid.Children.Add(button);
        }
    }
}
