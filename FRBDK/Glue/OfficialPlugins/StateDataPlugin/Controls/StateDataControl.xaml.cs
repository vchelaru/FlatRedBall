using OfficialPlugins.StateDataPlugin.ViewModels;
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

namespace OfficialPlugins.StateDataPlugin.Controls
{
    /// <summary>
    /// Interaction logic for StateDataControl.xaml
    /// </summary>
    public partial class StateDataControl : UserControl
    {
        StateCategoryViewModel ViewModel
        {
            get
            {
                return DataContext as StateCategoryViewModel;
            }
        }

        public StateDataControl()
        {
            InitializeComponent();
            {
                var column = CreateColumnForType("string", false);

                column.Header = "Name";
                DataGridInstance.Columns.Add(column);
            }

        }

        public void RefreshColumns()
        {
            // get rid of all the extra columns
            while(DataGridInstance.Columns.Count > 1)
            {
                DataGridInstance.Columns.RemoveAt(DataGridInstance.Columns.Count - 1);
            }

            foreach(var viewModelColumn in ViewModel.Columns)
            {
                var column = CreateColumnForType("string", false);

                column.Header = viewModelColumn;
                DataGridInstance.Columns.Add(column);
            }
        }

        public DataGridColumn CreateColumnForType(string typeName, bool isEnum)
        {
            //var column = new DataGridTemplateColumn();

            //DataTemplate dataTemplate = new DataTemplate();

            //dataTemplate.DataType = typeof(string);

            ////set up the stack panel
            //FrameworkElementFactory textBox = new FrameworkElementFactory(typeof(TextBox));
            //textBox.Name = "textBox";

            //dataTemplate.VisualTree = textBox;

            //column.CellTemplate = dataTemplate;
            //DataGridInstance.Columns.Add(column);

            //return column;

            var column = new DataGridTextColumn();
            column.Binding = new Binding("Name");
            return column;
        }
    }
}
