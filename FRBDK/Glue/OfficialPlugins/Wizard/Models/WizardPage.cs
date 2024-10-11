using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shapes;
using ToolsUtilities;

namespace OfficialPlugins.Wizard.Models
{
    #region Enums

    public enum ViewType
    {
        TextBlock,
        TextBox,
        MultiLineTextBox,
        IntTextBox,
        CheckBox,
        Group,
        RadioButton,
        Button,
        View
    }

    public enum StackOrFill
    {
        Stack,
        Fill
    }

    #endregion

    #region DataItem Class

    class DataItem
    {
        public ViewType ViewType { get; set; }

        public StackOrFill StackOrFill { get; set; }

        public int? LabelFontSize { get; set; }
        /// <summary>
        /// The string value to display. This is only applied if LabelBinding is not set.
        /// </summary>
        public string LabelText { get; set; }

        /// <summary>
        /// The property to bind to on the view model, allowing for labels to have dynamic text
        /// </summary>
        public string LabelBinding { get; set; }
        public object Value { get; set; }

        public string ViewModelProperty { get; set; }
        public string VisibilityBinding { get; set; }
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

        public OptionContainer Add(string displayText, object value)
        {
            var option = new Option();
            option.OptionName = displayText;
            option.OptionValue = value;

            Options.Add(option);
            return this;
        }
    }

    #endregion

    class WizardPage
    {
        #region Events

        public event Action Shown;
        public event Action NextClicked;
        public event Action BackClicked;

        public Func<GeneralResponse> Validate;

        #endregion

        Button nextButton;

        public bool IsNextButtonEnabled
        {
            get => nextButton?.IsEnabled == true;
            set => nextButton.IsEnabled = value;
        }

        List<DataItem> DataItems = new List<DataItem>();

        public object ViewModel { get; set; }
        public Func<bool> Predicate;

        public WizardPage(object viewModel, Func<bool> predicate = null)
        {
            this.ViewModel = viewModel;
            this.Predicate = predicate;
        }

        public bool HasNextButton { get; set; } = true;

        DataItem Add(DataItem item)
        {
            DataItems.Add(item);
            return item;
        }

        public OptionContainer AddOptions(string label, string vmPropertyName = null, string visibilityBinding = null)
        {
            var item = new OptionContainer { LabelText = label };
            item.ViewModelProperty = vmPropertyName;
            item.ViewType = ViewType.Group;
            item.VisibilityBinding = visibilityBinding;
            return (OptionContainer)Add(item);
        }

        public void AddTitle(string title, string visibilityBinding = null)
        {
            var dataItem = new DataItem { LabelText = title, LabelFontSize = 24 };
            dataItem.VisibilityBinding = visibilityBinding;
            Add(dataItem);
        }

        public void AddText(string text, string visibilityBinding = null)
        {
            var dataItem = new DataItem { LabelText = text };
            dataItem.VisibilityBinding = visibilityBinding;
            Add(dataItem);
        }

        public void AddBoundText(string dataBinding)
        {
            var dataItem = new DataItem { LabelText = dataBinding };
            dataItem.ViewType = ViewType.TextBlock;
            dataItem.LabelBinding = dataBinding;
            Add(dataItem);
        }

        public void AddAction(string text, Action clickAction)
        {
            var dataItem = new DataItem { LabelText = text };
            dataItem.ViewType = ViewType.Button;
            dataItem.Value = clickAction;
            Add(dataItem);
        }

        public DataItem AddView(FrameworkElement userControl)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.View;
            dataItem.Value = userControl;
            return Add(dataItem);
        }

        public DataItem AddIntValue(string label, string vmPropertyName = null, string visibilityBinding = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.IntTextBox;
            dataItem.LabelText = label;
            dataItem.ViewModelProperty = vmPropertyName;
            dataItem.VisibilityBinding = visibilityBinding;


            return Add(dataItem);
        }

        public DataItem AddBoolValue(string label, string vmPropertyName = null, string visibilityBinding = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.CheckBox;
            dataItem.LabelText = label;
            dataItem.ViewModelProperty = vmPropertyName;
            dataItem.VisibilityBinding = visibilityBinding;

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

        public DataItem AddMultiLineStringValue(string label, string vmPropertyName = null)
        {
            var dataItem = new DataItem();
            dataItem.ViewType = ViewType.MultiLineTextBox;
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
            stackPanel.DataContext = ViewModel;
            scrollView.Content = stackPanel;

            void AddRectangle(DataItem dataItem)
            {
                var rectangle = new Rectangle() { Height = 12 };
                stackPanel.Children.Add(rectangle);

                if (!string.IsNullOrEmpty(dataItem.VisibilityBinding))
                {
                    var binding = new Binding(dataItem.VisibilityBinding) { Converter = new BooleanToVisibilityConverter() };
                    rectangle.SetBinding(Rectangle.VisibilityProperty, binding);
                }

            }
            foreach (var dataItem in DataItems)
            {
                AddItem(stackPanel, grid, dataItem);

                AddRectangle(dataItem);
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

            if(HasNextButton)
            {
                nextButton = new Button();
                nextButton.Content = isNextButtonDone ? "Done" : "Next >";
                nextButton.VerticalAlignment = VerticalAlignment.Bottom;
                nextButton.Width = 150;
                nextButton.Height = 30;
                nextButton.HorizontalAlignment = HorizontalAlignment.Right;
                nextButton.Click += (not, used) => CallNext();
                Grid.SetRow(nextButton, 1);
                grid.Children.Add(nextButton);
            }

            Shown?.Invoke();
        }

        private void AddItem(StackPanel stackPanel, Grid grid, DataItem dataItem)
        {
            var vmType = ViewModel.GetType();
            PropertyInfo vmProperty = null;
            if (!string.IsNullOrEmpty(dataItem.ViewModelProperty))
            {
                vmProperty = vmType.GetProperty(dataItem.ViewModelProperty);
            }
            var vmValue = vmProperty?.GetValue(ViewModel);

            void TryBindVisibility(FrameworkElement element)
            {
                if(!string.IsNullOrEmpty(dataItem.VisibilityBinding ))
                {
                    var binding = new Binding(dataItem.VisibilityBinding) { Converter = new BooleanToVisibilityConverter() };
                    element.SetBinding(UIElement.VisibilityProperty, binding);
                }
            }
            switch (dataItem.ViewType)
            {
                case ViewType.CheckBox:
                    var checkBox = new CheckBox();
                    checkBox.Content = dataItem.LabelText;
                    checkBox.HorizontalAlignment = HorizontalAlignment.Left;
                    checkBox.VerticalContentAlignment = VerticalAlignment.Center;
                    checkBox.SetBinding(CheckBox.IsCheckedProperty, dataItem.ViewModelProperty);
                        //IsChecked = vmValue is bool asBool && asBool;

                    TryBindVisibility(checkBox);

                    stackPanel.Children.Add(checkBox);

                    if(!string.IsNullOrEmpty(dataItem.Subtext))
                    {
                        var subTextBlock = new TextBlock();
                        subTextBlock.Text = dataItem.Subtext;
                        subTextBlock.TextWrapping = TextWrapping.Wrap;
                        subTextBlock.FontSize = 9;
                        TryBindVisibility(subTextBlock);

                        stackPanel.Children.Add(subTextBlock);
                    }

                    break;
                case ViewType.Button:
                    var button = new Button();
                    button.Content = dataItem.LabelText;
                    button.Click += (not, used) => ((Action)dataItem.Value)();
                    button.HorizontalAlignment = HorizontalAlignment.Left;
                    button.MinWidth = 200;
                    button.MinHeight = 36;
                    stackPanel.Children.Add(button);

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
                    TryBindVisibility(group);

                    foreach (var child in asOptionContainer.Options)
                    {
                        var radioButton = new RadioButton();
                        radioButton.IsChecked = vmValue?.Equals(child.OptionValue) == true;
                        radioButton.Content = child.OptionName;
                        var optionValue = child.OptionValue;
                        radioButton.Click += (not, used) => vmProperty.SetValue(ViewModel, optionValue);
                        innerStack.Children.Add(radioButton);
                    }
                    break;
                case ViewType.TextBlock:
                    var textBlock = new TextBlock();
                    if (!string.IsNullOrEmpty(dataItem.LabelBinding))
                    {
                        textBlock.SetBinding(TextBlock.TextProperty, dataItem.LabelBinding);
                    }
                    else
                    {
                        textBlock.Text = dataItem.LabelText;
                    }
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    if (dataItem.LabelFontSize != null)
                    {
                        textBlock.FontSize = dataItem.LabelFontSize.Value;
                    }
                    TryBindVisibility(textBlock);

                    stackPanel.Children.Add(textBlock);
                    break;
                case ViewType.IntTextBox:
                    {
                        var label = new TextBlock();
                        label.Text = dataItem.LabelText;
                        TryBindVisibility(label);
                        stackPanel.Children.Add(label);

                        var textBox = new TextBox();
                        textBox.MinWidth = 150;
                        textBox.HorizontalAlignment = HorizontalAlignment.Left;
                        textBox.SetBinding(TextBox.TextProperty, dataItem.ViewModelProperty);
                        TryBindVisibility(textBox);
                        stackPanel.Children.Add(textBox);
                    }


                    break;
                case ViewType.TextBox:
                case ViewType.MultiLineTextBox:
                    {
                        var label = new TextBlock();
                        label.Text = dataItem.LabelText;
                        TryBindVisibility(label);
                        stackPanel.Children.Add(label);

                        var textBox = new TextBox();
                        textBox.SetBinding(TextBox.TextProperty, dataItem.ViewModelProperty);
                        textBox.MinWidth = 150;
                        textBox.HorizontalAlignment = HorizontalAlignment.Left;
                        TryBindVisibility(textBox);

                        if(dataItem.ViewType == ViewType.MultiLineTextBox)
                        {
                            textBox.AcceptsReturn = true;
                            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;

                            textBox.Height = 240;
                        }

                        stackPanel.Children.Add(textBox);
                    }
                    break;
                case ViewType.View:
                    var userControl = dataItem.Value as FrameworkElement;
                    var oldStackPanel = userControl.Parent as StackPanel;

                    if(oldStackPanel != null)
                    {
                        oldStackPanel.Children.Remove(userControl);
                    }

                    if(dataItem.StackOrFill == StackOrFill.Stack)
                    {
                        stackPanel.Children.Add(userControl);
                    }
                    else
                    {
                        grid.Children.Add(userControl);
                    }
                    break;
            }

        }

        public void CallNext()
        {
            var response = Validate?.Invoke() ??  GeneralResponse.SuccessfulResponse;

            if(response.Succeeded == false)
            {
                MessageBox.Show(response.Message);
            }
            else
            {
                NextClicked();
            }
        }
    }
}
