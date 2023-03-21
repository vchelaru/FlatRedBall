using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Utilities;
using GlueFormsCore.Extensions;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        //List<object> allOptions = new List<object>();
        //ObservableCollection<object> filteredOptions = new ObservableCollection<object>();

        AddNewFileViewModel ViewModel => DataContext as AddNewFileViewModel;


        public List<string> NamesAlreadyUsed
        {
            get; private set;
        } = new List<string>();

        public AssetTypeInfo SelectedItem
        {
            get => ViewModel.SelectedAssetTypeInfo;
            set => ViewModel.SelectedAssetTypeInfo = value;
        }

        //public string ResultName
        //{
        //    get => TextBox.Text;
        //    set => TextBox.Text = value;
        //}

        #endregion

        #region Events

        public event SelectionChangedEventHandler SelectionChanged;

        public Func<object> GetCreationOption;

        #endregion

        public CustomizableNewFileWindow()
        {
            InitializeComponent();
            GlueCommands.Self.DialogCommands.MoveToCursor(this);
            this.Loaded += HandleLoaded;
        }


        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            if(SearchTermTextBox.IsVisible)
            {

                SearchTermTextBox.Focus();
                Keyboard.Focus(SearchTermTextBox);

                SearchTermTextBox.SelectAll();
            }
            else
            {
                this.TextBox.Focus();
                this.TextBox.SelectAll();
            }

        }



        public void AddCustomUi(System.Windows.Controls.Control controlToAdd)
        {
            AdditionalUiStack.Children.Add(controlToAdd);
        }

        public void AddOption(AssetTypeInfo option)
        {
            //FileTypeComboBox.Items.Add(option);
            ViewModel.AllOptions.Add(option);
        }

        //private void RefreshListBox()
        //{
        //    filteredOptions.Clear();

        //    var temp = allOptions
        //        .OrderBy(item => item?.ToString())
        //        .ToList();


        //    foreach(var item in temp)
        //    {
        //        var toString = item.ToString();

        //        var text = SearchTermTextBox.Text?.ToLowerInvariant();

        //        var shouldAdd = string.IsNullOrEmpty(text) ||
        //            toString?.ToLowerInvariant().Contains(text) == true;

        //        if(shouldAdd)
        //        {
        //            filteredOptions.Add(item);
        //        }
        //    }

        //    if (ListBox.SelectedIndex > 0 == false && filteredOptions.Count > 0)
        //    {
        //        ListBox.SelectedIndex = 0;
        //    }
        //}

        private string GetObjectTypeFromAti(AssetTypeInfo ati)
        {
            if (ati == null)
            {
                return null;
            }
            else
            {
                string fileType = ati.FriendlyName;
                if (fileType?.Contains("(") == true)
                {
                    fileType = fileType.Substring(0, fileType.IndexOf('('));
                }

                fileType = fileType?.Replace(" ", "")?.Replace("/", "");
                return fileType;
            }
        }

        private void FileTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(this.SelectionChanged != null)
            {
                this.SelectionChanged(this, e);
            }
            var ati = SelectedItem;

            if (ati != null)
            {
                if (ViewModel.IsNameDefault)
                {
                    string fileType = GetObjectTypeFromAti(ati);
                    // We want to make sure we don't
                    // suggest a name that is already
                    // being used.
                    //textBox1.Text = fileType + "File";
                    //This sets the IsNameDefault to false, so set it back to true:
                    ViewModel.FileName = StringFunctions.MakeStringUnique(fileType + "File", NamesAlreadyUsed, 2);

                    var relativeFile = ViewModel.FileName + "." + ViewModel.SelectedAssetTypeInfo?.Extension;
                    var absoluteFilePath = GlueCommands.Self.GetAbsoluteFilePath(relativeFile);

                    while (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(absoluteFilePath) != null)
                    {
                        ViewModel.FileName = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(ViewModel.FileName);
                        relativeFile = ViewModel.FileName + "." + ViewModel.SelectedAssetTypeInfo?.Extension;
                        absoluteFilePath = GlueCommands.Self.GetAbsoluteFilePath(relativeFile);
                    }
                    ViewModel.IsNameDefault = true;
                }
            }
            // The selection may change because the user clicked, or because the user entered new text in the filter text
            // Don't focus if the search box has focus:
            if(!SearchTermTextBox.IsFocused)
            {
                TextBox.Focus();
                TextBox.SelectAll();
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

        public object GetOptionFor(AssetTypeInfo ati)
        {
            if (GetCreationOption != null)
            {
                foreach (var deleg in GetCreationOption.GetInvocationList())
                {
                    var result = deleg.DynamicInvoke();

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        //private void SearchTermTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    RefreshListBox();
        //}

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
                    if (ListBox.SelectedIndex < ViewModel.FilteredOptions.Count - 1)
                    {
                        ListBox.SelectedIndex++;
                        ListBox.ScrollIntoView(ListBox.SelectedItem);
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(ViewModel.SelectedAssetTypeInfo != null)
            {
                DialogResult = true;
            }
        }
    }

}
