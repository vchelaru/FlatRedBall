using OfficialPlugins.VariableDisplay.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
            columnDefinition.Width = new System.Windows.GridLength(36);

            // January 18, 2022
            // We allow this to be
            // editable so references
            // to unavailable files still 
            // show up
            IsEditable = true;

            grid.ColumnDefinitions.Add(columnDefinition);

            var button = new Button();
            button.Margin = new System.Windows.Thickness(3, 0, 0, 0);
            button.Click += HandleViewButtonClicked;
            button.Content = "View";
            Grid.SetColumn(button, 2);
            grid.Children.Add(button);
        }

        private void HandleViewButtonClicked(object sender, RoutedEventArgs e)
        {
            if(this.InstanceMember is IFileInstanceMember fileInstanceMember)
            {
                fileInstanceMember.OnView();
            }
        }
    }
}
