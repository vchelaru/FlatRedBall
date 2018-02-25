using DialogTreePlugin.Controllers;
using DialogTreePlugin.ViewModels;
using FlatRedBall.IO.Csv;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DialogTreePlugin.WpfBehaviors
{
    public class DialogTreeDataGrid : DataGrid
    {

        public DataTemplateSelector CellTemplateSelector
        {
            get { return (DataTemplateSelector)GetValue(CellTemplateSelectorProperty); }
            set { SetValue(CellTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty CellTemplateSelectorProperty =
            DependencyProperty.Register("Selector", typeof(DataTemplateSelector), typeof(DialogTreeDataGrid),
            new FrameworkPropertyMetadata(null));



        protected override void OnAutoGeneratingColumn(DataGridAutoGeneratingColumnEventArgs e)
        {

            e.Cancel = true;
            if (e.PropertyName == nameof(LocaliztionDbViewModel.CsvHeader) && DialogTreeFileController.Self.LocalizationDb != null)
            {
                var headers = DialogTreeFileController.Self.LocalizationDb.Headers;
                if (ShouldRegenerateColumns(headers))
                {
                    Columns.Clear();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        Columns.Add(new DataGridTextColumn
                        {
                            Header = headers[i].Name,
                            Binding = new Binding($"{nameof(LocaliztionDbViewModel.LocalizedText)}[{i}].{nameof(LocalizedTextViewModel.Text)}"),
                            IsReadOnly = i == 0
                        });
                    }
                }
            }
        }

        private bool ShouldRegenerateColumns(CsvHeader[] headers)
        {
            var toReturn = Columns.Count != headers.Length;
            if(toReturn == false)
            {
                for(int i = 0; i < headers.Length; i ++)
                {
                    var headerAsString = Columns[i].Header as string;
                    if (headers[i].Name != headerAsString)
                    {
                        toReturn = true;
                        break;
                    }
                }
            }

            return toReturn;
        }
    }
}
