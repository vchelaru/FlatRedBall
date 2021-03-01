using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace OfficialPluginsCore.Wizard.Models
{
    #region Enums

    public enum ViewType
    {
        TextBlock,
        TextBox,
        IntTextBox,
        CheckBox,
        Group,
        RadioButton,
    }

    #endregion

    #region DataItem Class

    class DataItem
    {
        public ViewType ViewType { get; set; }

        public int? LabelFontSize { get; set; }
        public string LabelText { get; set; }
        public object Value { get; set; }

        public string ViewModelProperty { get; set; }

        public string Subtext { get; set; }
    }

    class Option
    {
        public string OptionName { get; set; }
        public object OptionValue { get; set; }
    }

    class OptionContainer : DataItem
    {
        public List<Option> Options = new List<Option>();

        public OptionContainer Add(string optionName, object value)
        {
            var option = new Option();
            option.OptionName = optionName;
            option.OptionValue = value;

            Options.Add(option);
            return this;
        }
    }

    #endregion

    class FormsData
    {
        public event Action NextClicked;
        public event Action BackClicked;

        List<DataItem> DataItems = new List<DataItem>();

        object viewModel;
        public Func<bool> Predicate;

        public FormsData(object viewModel, Func<bool> predicate = null)
        {
            this.viewModel = viewModel;
            this.Predicate = predicate;
        }

        DataItem Add(DataItem item)
        {
            DataItems.Add(item);
            return item;
        }

        public OptionContainer AddOptions(string label, string vmPropertyName = null)
        {
            var item = new OptionContainer { LabelText = label };
            item.ViewModelProperty = vmPropertyName;
            item.ViewType = ViewType.Group;
            return (OptionContainer)Add(item);
        }

        public void AddTitle(string title)
        {
            Add(new DataItem { LabelText = title, LabelFontSize = 24 });
        }

        public void AddText(string text)
        {
            Add(new DataItem { LabelText = text });
        }

        public DataItem AddIntValue(string label, string vmPropertyName = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.IntTextBox;
            dataItem.LabelText = label;
            dataItem.ViewModelProperty = vmPropertyName;

            return Add(dataItem);
        }

        public DataItem AddBoolValue(string label, string vmPropertyName = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.CheckBox;
            dataItem.LabelText = label;
            dataItem.ViewModelProperty = vmPropertyName;

            return Add(dataItem);
        }

        public DataItem AddStringValue(string label, string vmPropertyName = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.TextBox;
            dataItem.LabelText = label;
            dataItem.ViewModelProperty = vmPropertyName;

            return Add(dataItem);
        }

        public void Fill(Grid grid, bool showBack, bool isNextButtonDone)
        {
            grid.RowDefinitions.Clear();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var scrollView = new ScrollViewer();
            scrollView.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            grid.Children.Add(scrollView);


            var stackPanel = new StackPanel();
            stackPanel.DataContext = viewModel;
            scrollView.Content = stackPanel;

            void AddRectangle() =>
                stackPanel.Children.Add(new Rectangle() { Height = 12 });
            foreach (var dataItem in DataItems)
            {
                AddItem(stackPanel, dataItem);

                AddRectangle();
            }

            if (showBack)
            {
                var backButton = new Button();
                backButton.Content = "< Back";
                backButton.VerticalAlignment = VerticalAlignment.Bottom;
                backButton.Width = 150;
                backButton.Height = 30;
                backButton.HorizontalAlignment = HorizontalAlignment.Left;
                backButton.Click += (not, used) => BackClicked();
                Grid.SetRow(backButton, 1);
                grid.Children.Add(backButton);
            }

            var nextButton = new Button();
            nextButton.Content = isNextButtonDone ? "Done" : "Next >";
            nextButton.VerticalAlignment = VerticalAlignment.Bottom;
            nextButton.Width = 150;
            nextButton.Height = 30;
            nextButton.HorizontalAlignment = HorizontalAlignment.Right;
            nextButton.Click += (not, used) => NextClicked();
            Grid.SetRow(nextButton, 1);
            grid.Children.Add(nextButton);

        }

        private void AddItem(StackPanel stackPanel, DataItem dataItem)
        {
            var vmType = viewModel.GetType();
            PropertyInfo vmProperty = null;
            if (!string.IsNullOrEmpty(dataItem.ViewModelProperty))
            {
                vmProperty = vmType.GetProperty(dataItem.ViewModelProperty);
            }
            var vmValue = vmProperty?.GetValue(viewModel);
            switch (dataItem.ViewType)
            {
                case ViewType.CheckBox:
                    var checkBox = new CheckBox();
                    checkBox.Content = dataItem.LabelText;
                    checkBox.IsChecked = vmValue is bool asBool && asBool;
                    stackPanel.Children.Add(checkBox);

                    break;
                case ViewType.Group:
                    var group = new GroupBox();
                    group.Header = dataItem.LabelText;
                    stackPanel.Children.Add(group);
                    var asOptionContainer = dataItem as OptionContainer;
                    group.HorizontalAlignment = HorizontalAlignment.Left;
                    var innerStack = new StackPanel();
                    group.Content = innerStack;
                    group.MinWidth = 150;
                    foreach (var child in asOptionContainer.Options)
                    {
                        var radioButton = new RadioButton();
                        radioButton.IsChecked = vmValue?.Equals(child.OptionValue) == true;
                        radioButton.Content = child.OptionName;
                        var optionValue = child.OptionValue;
                        radioButton.Click += (not, used) => vmProperty.SetValue(viewModel, optionValue);
                        innerStack.Children.Add(radioButton);
                    }
                    break;
                //case ViewType.RadioButton:

                //    break;
                case ViewType.TextBlock:
                    var textBlock = new TextBlock();
                    textBlock.Text = dataItem.LabelText;
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    if (dataItem.LabelFontSize != null)
                    {
                        textBlock.FontSize = dataItem.LabelFontSize.Value;
                    }
                    stackPanel.Children.Add(textBlock);
                    break;
                case ViewType.IntTextBox:
                    {
                        var label = new TextBlock();
                        label.Text = dataItem.LabelText;
                        stackPanel.Children.Add(label);

                        var textBox = new TextBox();
                        textBox.MinWidth = 150;
                        textBox.HorizontalAlignment = HorizontalAlignment.Left;
                        textBox.SetBinding(TextBox.TextProperty, dataItem.ViewModelProperty);
                        stackPanel.Children.Add(textBox);
                    }


                    break;
                case ViewType.TextBox:
                    {
                        var label = new TextBlock();
                        label.Text = dataItem.LabelText;
                        stackPanel.Children.Add(label);

                        var textBox = new TextBox();
                        textBox.SetBinding(TextBox.TextProperty, dataItem.ViewModelProperty);
                        textBox.MinWidth = 150;
                        textBox.HorizontalAlignment = HorizontalAlignment.Left;
                        stackPanel.Children.Add(textBox);
                    }
                    break;
            }

        }
    }
}
