using System;
using System.Windows;
using System.Windows.Controls;

namespace FlatRedBall.Glue.Controls;

/// <summary>
/// Interaction logic for PropertyGrid.xaml
/// </summary>
public partial class PropertyGrid
{
    #region Dependency Properties

    public string HeaderLabelProperty
    {
        get => (string)GetValue(HeaderLabelPropertyProperty);
        set => SetValue(HeaderLabelPropertyProperty, value);
    }

    public static readonly DependencyProperty HeaderLabelPropertyProperty = DependencyProperty.Register(nameof(HeaderLabelProperty), typeof(string), typeof(PropertyGrid), new PropertyMetadata("Property"));

    public string HeaderLabelValue
    {
        get => (string)GetValue(HeaderLabelValueProperty);
        set => SetValue(HeaderLabelValueProperty, value);
    }

    public static readonly DependencyProperty HeaderLabelValueProperty = DependencyProperty.Register(nameof(HeaderLabelValue), typeof(string), typeof(PropertyGrid), new PropertyMetadata("Value"));

    public Style ComboBoxEditingStyle
    {
        get => (Style)GetValue(ComboBoxEditingStyleProperty);
        set => SetValue(ComboBoxEditingStyleProperty, value);
    }

    public static readonly DependencyProperty ComboBoxEditingStyleProperty = DependencyProperty.Register(nameof(ComboBoxEditingStyle), typeof(Style), typeof(PropertyGrid), new PropertyMetadata(new Style(typeof(ComboBox))));

    public Style CheckBoxEditingStyle
    {
        get => (Style)GetValue(CheckBoxEditingStyleProperty);
        set => SetValue(CheckBoxEditingStyleProperty, value);
    }

    public static readonly DependencyProperty CheckBoxEditingStyleProperty = DependencyProperty.Register(nameof(CheckBoxEditingStyle), typeof(Style), typeof(PropertyGrid), new PropertyMetadata(new Style(typeof(CheckBox))));

    public Style TextBoxEditingStyle
    {
        get => (Style)GetValue(TextBoxEditingStyleProperty);
        set => SetValue(TextBoxEditingStyleProperty, value);
    }

    public static readonly DependencyProperty TextBoxEditingStyleProperty = DependencyProperty.Register(nameof(TextBoxEditingStyle), typeof(Style), typeof(PropertyGrid), new PropertyMetadata(new Style(typeof(TextBox))));

    #endregion

    public PropertyGrid()
    {
        InitializeComponent();
    }

    public EventHandler<SelectionChangedEventArgs> OnSelectionChanged;

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnSelectionChanged?.Invoke(sender, e);
    }
}