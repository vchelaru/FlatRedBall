using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Utilities;

namespace FlatRedBall.Glue.Controls
{
    public class NewFileWindow : Window
    {
        #region Fields

        bool mIsNameDefault = true;

        ComboBox mOptionsComboBox;
        Label ComboBoxLabel;
        TextBox textBox1;
        StackPanel DynamicUiPanel;

        int nextDynamicId;
        Dictionary<int, FrameworkElement> mDynamicControls = new Dictionary<int, FrameworkElement>();

        List<FileTypeOptions> mFileTypeOptions = new List<FileTypeOptions>();
        List<string> mNamesAlreadyUsed = new List<string>();

        #endregion

        #region Properties

        public List<string> NamesAlreadyUsed => mNamesAlreadyUsed;

        public AssetTypeInfo ResultAssetTypeInfo => mOptionsComboBox.SelectedItem as AssetTypeInfo;

        public object SelectedItem
        {
            get => mOptionsComboBox.SelectedItem;
            set => mOptionsComboBox.SelectedItem = value;
        }

        public string ResultName
        {
            get => textBox1.Text;
            set => textBox1.Text = value;
        }

        public string ComboBoxMessage
        {
            get => (string)ComboBoxLabel.Content;
            set => ComboBoxLabel.Content = value;
        }

        #endregion

        public NewFileWindow()
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;

            var outerStack = new StackPanel { Margin = new Thickness(8) };
            Content = outerStack;

            ComboBoxLabel = new Label { Content = "Select the file type:", Padding = new Thickness(0, 0, 0, 2) };
            outerStack.Children.Add(ComboBoxLabel);

            mOptionsComboBox = new ComboBox { MinWidth = 210 };
            mOptionsComboBox.SelectionChanged += OptionsComboBox_SelectionChanged;
            outerStack.Children.Add(mOptionsComboBox);

            var nameLabel = new Label
            {
                Content = "Enter the new file's name:",
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(0, 0, 0, 2)
            };
            outerStack.Children.Add(nameLabel);

            textBox1 = new TextBox { MinWidth = 210 };
            outerStack.Children.Add(textBox1);

            DynamicUiPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            outerStack.Children.Add(DynamicUiPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            outerStack.Children.Add(buttonPanel);

            var okButton = new Button { Content = "OK", MinWidth = 80, Margin = new Thickness(0, 0, 6, 0), IsDefault = true };
            okButton.Click += (s, e) => DialogResult = true;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true };
            cancelButton.Click += (s, e) => DialogResult = false;
            buttonPanel.Children.Add(cancelButton);

            GlueCommands.Self.DialogCommands.MoveToCursor(this);

            Create2D3DUI();
            CreateSpreadsheetOptions();
            foreach (var option in mFileTypeOptions)
                CreateUiForOptions(option);
        }

        private void Create2D3DUI()
        {
            var opts = new FileTypeOptions();
            opts.ObjectType.Add("Scene");
            opts.Options.Add("2D");
            opts.Options.Add("3D");
            mFileTypeOptions.Add(opts);
        }

        private void CreateSpreadsheetOptions()
        {
            var opts = new FileTypeOptions();
            opts.ObjectType.Add("Spreadsheet");
            opts.Options.Add("Dictionary");
            opts.Options.Add("List");
            mFileTypeOptions.Add(opts);
        }

        private void CreateUiForOptions(FileTypeOptions options)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var optionText in options.Options)
            {
                var rb = new RadioButton { Content = optionText, Margin = new Thickness(0, 0, 8, 0) };
                if (panel.Children.Count == 0)
                    rb.IsChecked = true;
                panel.Children.Add(rb);
            }
            panel.Visibility = Visibility.Collapsed;
            DynamicUiPanel.Children.Add(panel);

            options.UiId = nextDynamicId;
            mDynamicControls[nextDynamicId] = panel;
            nextDynamicId++;
        }

        public void AddOption(object option)
        {
            mOptionsComboBox.Items.Add(option);
        }

        private void OptionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mOptionsComboBox.SelectedItem is AssetTypeInfo ati)
            {
                string fileType = GetObjectTypeFromAti(ati);

                foreach (var option in mFileTypeOptions)
                {
                    mDynamicControls[option.UiId].Visibility = option.ObjectType.Contains(fileType)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                if (mIsNameDefault)
                {
                    textBox1.Text = StringFunctions.MakeStringUnique(fileType + "File", mNamesAlreadyUsed, 2);
                    while (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(
                        textBox1.Text + "." + ResultAssetTypeInfo.Extension) != null)
                    {
                        textBox1.Text = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(textBox1.Text);
                    }
                }
            }
        }

        public string GetObjectTypeFromAti(AssetTypeInfo ati)
        {
            if (ati == null) return null;
            string fileType = ati.FriendlyName;
            if (fileType.Contains("("))
                fileType = fileType.Substring(0, fileType.IndexOf('('));
            fileType = fileType.Replace(" ", "");
            return fileType;
        }

        public int AddTextBox(string label)
        {
            DynamicUiPanel.Children.Add(new Label
            {
                Content = label,
                Padding = new Thickness(0, 6, 0, 2)
            });

            var textBox = new TextBox { MinWidth = 210 };
            DynamicUiPanel.Children.Add(textBox);

            mDynamicControls[nextDynamicId] = textBox;
            return nextDynamicId++;
        }

        internal string GetValueFromId(int valueId)
        {
            if (mDynamicControls.TryGetValue(valueId, out var control))
            {
                if (control is TextBox tb) return tb.Text;
                if (control is StackPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is RadioButton rb && rb.IsChecked == true)
                            return rb.Content as string;
                    }
                }
            }
            return string.Empty;
        }
    }

    #region FileTypeOptions

    class FileTypeOptions
    {
        public List<string> ObjectType = new List<string>();
        public List<string> Options = new List<string>();
        public int UiId;
    }

    #endregion
}
