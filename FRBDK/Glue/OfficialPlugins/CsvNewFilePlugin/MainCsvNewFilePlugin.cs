using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPluginsCore.CsvNewFilePlugin
{
    [Export(typeof(PluginBase))]
    public class MainCsvNewFilePlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.AddNewFileOptionsHandler += HandleAddNewFileOptions;


        }

        private void HandleAddNewFileOptions(CustomizableNewFileWindow newFileWindow)
        {
            var spreadsheetBox = new GroupBox();
            spreadsheetBox.Header = "Runtime Type";
            var stack = new StackPanel();
            spreadsheetBox.Content = stack;
            spreadsheetBox.Visibility = Visibility.Collapsed;
            newFileWindow.AddCustomUi(spreadsheetBox);

            var dictionaryRadio = new RadioButton();
            dictionaryRadio.Content = "Dictionary";
            dictionaryRadio.IsChecked = true;
            stack.Children.Add(dictionaryRadio);

            var listRadio = new RadioButton();
            listRadio.Content = "List";
            stack.Children.Add(listRadio);

            newFileWindow.SelectionChanged += (not, used) =>
            {
                var ati = newFileWindow.SelectedItem as AssetTypeInfo;

                if (ati?.FriendlyName == "Spreadsheet (.csv)")
                {
                    spreadsheetBox.Visibility = Visibility.Visible;
                }
                else
                {
                    spreadsheetBox.Visibility = Visibility.Collapsed;
                }
            };

            newFileWindow.GetCreationOption = () =>
            {
                if (dictionaryRadio.IsChecked == true)
                {
                    return "Dictionary";
                }
                else
                {
                    return "List";
                }
            };
        }
    }
}
