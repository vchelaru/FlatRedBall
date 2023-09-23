using System;
using System.Diagnostics;
using System.Windows;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for ComboBoxMessageBox.xaml
/// Represents a window where the user can make a selection from a combobox. This value will be returned.
/// </summary>
public partial class ComboBoxMessageBox
{
    private class ObjectWithDisplayText
    {
        public readonly string DisplayText;
        public readonly object ObjectReference;

        public ObjectWithDisplayText(string displayText, object objectReference)
        {
            DisplayText = displayText;
            ObjectReference = objectReference;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }

    /// <summary>
    /// Open a window containing a combobox.
    /// </summary>
    /// <param name="label">A line explaining the purpose of the combobox window. Cannot be left empty.</param>
    public ComboBoxMessageBox(string label)
    {
        Debug.Assert(!String.IsNullOrWhiteSpace(label));
        InitializeComponent();
        Message.Content = label;
    }

    /// <summary>
    /// Item display label selected by user
    /// </summary>
    public string SelectedText => ((UserOptions.SelectionBoxItem as ObjectWithDisplayText)!).DisplayText;

    /// <summary>
    /// Item value selected by user
    /// </summary>
    public object SelectedItem => ((UserOptions.SelectedItem as ObjectWithDisplayText)!).ObjectReference;

    /// <summary>
    /// Add <paramref name="item"/> the user can select in the combo box.
    /// </summary>
    /// <param name="item">Item to add. The ToString() method will be used on the item to derive its label.</param>
    public void Add(object item)
    {
        Add(item, item.ToString());
    }

    /// <summary>
    /// Add <paramref name="item"/> the user can select in the combo box.
    /// </summary>
    /// <param name="item">value of the item</param>
    /// <param name="displayText">human-readable display label for the item in the combo box.</param>
    public void Add(object item, string displayText)
    {
        UserOptions.Items.Add(new ObjectWithDisplayText(displayText, item));

        if (UserOptions.Items.Count == 1)
        {
            UserOptions.SelectedItem = item;
        }
    }

    /// <summary>
    /// User pressed ok button; return value.
    /// </summary>
    private void Submit(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    /// <summary>
    /// User pressed cancel button; abort task.
    /// </summary>
    private void Cancel(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}
