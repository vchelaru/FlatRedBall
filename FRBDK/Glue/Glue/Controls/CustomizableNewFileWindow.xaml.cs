using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for CustomizableNewFileWindow.xaml
    /// </summary>
    public partial class CustomizableNewFileWindow : Window
    {
        #region Fields/Properties

        GroupBox spreadsheetBox;
        RadioButton dictionaryRadio;

        List<object> allOptions = new List<object>();
        ObservableCollection<object> filteredOptions = new ObservableCollection<object>();

        bool mIsNameDefault = true;

        List<FileTypeOptions> mFileTypeOptions = new List<FileTypeOptions>();

        public List<string> NamesAlreadyUsed
        {
            get; private set;
        } = new List<string>();

        public object SelectedItem
        {
            get => ListBox.SelectedItem;
            set => ListBox.SelectedItem = value;
        }

        public string ResultName
        {
            get => TextBox.Text;
            set => TextBox.Text = value;
        }

        public AssetTypeInfo ResultAssetTypeInfo
        {
            get { return ListBox.SelectedItem as AssetTypeInfo; }
        }
        #endregion

        public CustomizableNewFileWindow()
        {
            InitializeComponent();

            ListBox.ItemsSource = filteredOptions;

            CreateSpreadsheetOptions();


            this.WindowStartupLocation = WindowStartupLocation.Manual;

            // uses winforms:
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            this.Left = point.X - this.Width / 2;
            // not sure why this is so high
            //this.Top = point.Y - this.Height/2;
            this.Top = point.Y - 50;

            SearchTermTextBox.Focus();
        }

        public void AddOption(object option)
        {
            //FileTypeComboBox.Items.Add(option);
            allOptions.Add(option);
            RefreshListBox();


        }

        private void RefreshListBox()
        {
            filteredOptions.Clear();

            var temp = allOptions
                .OrderBy(item => item?.ToString())
                .ToList();
            

            foreach(var item in temp)
            {
                var toString = item.ToString();

                var text = SearchTermTextBox.Text?.ToLowerInvariant();

                var shouldAdd = string.IsNullOrEmpty(text) ||
                    toString.ToLowerInvariant().Contains(text);

                if(shouldAdd)
                {
                    filteredOptions.Add(item);
                }
            }

            if (ListBox.SelectedIndex > 0 == false && filteredOptions.Count > 0)
            {
                ListBox.SelectedIndex = 0;
            }
        }


        //private void Create2D3DUI()
        //{
        //    FileTypeOptions m2D3DFileTypeOptions = new FileTypeOptions();
        //    m2D3DFileTypeOptions.ObjectType.Add("Scene");
        //    m2D3DFileTypeOptions.Options.Add("2D");
        //    m2D3DFileTypeOptions.Options.Add("3D");
        //    m2D3DFileTypeOptions.UiType = UiType.RadioButton;

        //    mFileTypeOptions.Add(m2D3DFileTypeOptions);
        //}

        private void CreateSpreadsheetOptions()
        {
            spreadsheetBox = new GroupBox();
            spreadsheetBox.Header = "Runtime Type";
            var stack = new StackPanel();
            spreadsheetBox.Content = stack;
            spreadsheetBox.Visibility = Visibility.Collapsed;
            AdditionalUiStack.Children.Add(spreadsheetBox);

            dictionaryRadio = new RadioButton();
            dictionaryRadio.Content = "Dictionary";
            dictionaryRadio.IsChecked = true;
            stack.Children.Add(dictionaryRadio);

            var listRadio = new RadioButton();
            listRadio.Content = "List";
            stack.Children.Add(listRadio);
        }

        public string GetObjectTypeFromAti(AssetTypeInfo ati)
        {
            if (ati == null)
            {
                return null;
            }
            else
            {
                string fileType = ati.FriendlyName;
                if (fileType.Contains("("))
                {
                    fileType = fileType.Substring(0, fileType.IndexOf('('));
                }

                fileType = fileType.Replace(" ", "");
                return fileType;
            }
        }


        private void FileTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AssetTypeInfo ati = SelectedItem as AssetTypeInfo;

            if(ati?.FriendlyName == "Spreadsheet (.csv)")
            {
                spreadsheetBox.Visibility = Visibility.Visible;
            }
            else
            {
                spreadsheetBox.Visibility = Visibility.Collapsed;
            }

            if (ati != null)
            {

                string fileType = GetObjectTypeFromAti(ati);

                if (mIsNameDefault)
                {
                    // We want to make sure we don't
                    // suggest a name that is already
                    // being used.
                    //textBox1.Text = fileType + "File";
                    TextBox.Text = StringFunctions.MakeStringUnique(fileType + "File", NamesAlreadyUsed, 2);

                    while (ObjectFinder.Self.GetReferencedFileSaveFromFile(TextBox.Text + "." + ResultAssetTypeInfo.Extension) != null)
                    {
                        TextBox.Text = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(TextBox.Text);
                    }
                }
            }
        }

        private void HandleOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void HandleCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape:
                    DialogResult = false;
                    e.Handled = true;
                    break;
                case Key.Enter:
                    DialogResult = true;
                    e.Handled = true;
                    break;
            }
        }

        public string GetOptionFor(AssetTypeInfo ati)
        {
            if(ati?.FriendlyName == "Spreadsheet (.csv)")
            {
                if(dictionaryRadio.IsChecked == true)
                {
                    return "Dictionary";
                }
                else
                {
                    return "List";
                }
            }
            else
            {
                return null;
            }
        }

        private void SearchTermTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshListBox();
        }

        private void SearchTermTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Escape:
                    if(string.IsNullOrEmpty(SearchTermTextBox.Text))
                    {
                        DialogResult = false;
                    }
                    else
                    {
                        SearchTermTextBox.Text = null;
                    }
                    e.Handled = true;
                    break;
                case Key.Enter:
                    DialogResult = true;
                    e.Handled = true;
                    break;
            }
        }

        private void SearchTermTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (ListBox.SelectedIndex > 0)
                    {
                        ListBox.SelectedIndex--;
                        ListBox.ScrollIntoView(ListBox.SelectedItem);
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (ListBox.SelectedIndex < filteredOptions.Count - 1)
                    {
                        ListBox.SelectedIndex++;
                        ListBox.ScrollIntoView(ListBox.SelectedItem);
                    }
                    e.Handled = true;
                    break;
            }
        }

        //public string GetOptionFor(string type)
        //{
        //    foreach (var option in mFileTypeOptions)
        //    {
        //        if (option.ObjectType.Contains(type))
        //        {
        //            return mDynamicUiHelper.GetValue(option.UiId);
        //        }
        //    }

        //    return null;
        //}
    }
}
