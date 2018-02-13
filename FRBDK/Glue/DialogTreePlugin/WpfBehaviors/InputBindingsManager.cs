using DialogTreePlugin.Controllers;
using DialogTreePlugin.ViewModels;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DialogTreePlugin.WpfBehaviors
{
    public static class InputBindingsManager
    {
        public static readonly DependencyProperty UpdatePropertySourceWhenEnterPressedProperty = DependencyProperty.RegisterAttached(
                "UpdatePropertySourceWhenEnterPressed", typeof(DependencyProperty), typeof(InputBindingsManager), new PropertyMetadata(null, OnUpdatePropertySourceWhenEnterPressedPropertyChanged));

        static InputBindingsManager()
        {

        }

        public static void SetUpdatePropertySourceWhenEnterPressed(DependencyObject dp, DependencyProperty value)
        {
            dp.SetValue(UpdatePropertySourceWhenEnterPressedProperty, value);
        }

        public static DependencyProperty GetUpdatePropertySourceWhenEnterPressed(DependencyObject dp)
        {
            return (DependencyProperty)dp.GetValue(UpdatePropertySourceWhenEnterPressedProperty);
        }

        private static void OnUpdatePropertySourceWhenEnterPressedPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = dp as UIElement;

            if (element == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                element.PreviewKeyDown -= HandlePreviewKeyDown;
            }

            if (e.NewValue != null)
            {
                element.PreviewKeyDown += new KeyEventHandler(HandlePreviewKeyDown);
            }
        }

        static void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoUpdateSource(e.Source);
            }
        }

        static void DoUpdateSource(object source)
        {
            DependencyProperty property =
                GetUpdatePropertySourceWhenEnterPressed(source as DependencyObject);

            if (property == null)
            {
                return;
            }

            UIElement elt = source as UIElement;

            if (elt == null)
            {
                return;
            }

            BindingExpression binding = BindingOperations.GetBindingExpression(elt, property);

            if (binding != null)
            {
                binding.UpdateSource();
            }
        }
    }

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
            if (e.PropertyName == nameof(LocaliztionDbViewModel.CsvHeader) && MainController.Self.LocalizationDb != null)
            {
                var headers = MainController.Self.LocalizationDb.Headers;
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
