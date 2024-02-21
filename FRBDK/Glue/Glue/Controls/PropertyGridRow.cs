using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlatRedBall.Glue.Controls;

public interface IPropertyGrid
{
    ObservableCollection<PropertyGridRow> PropertiesValues
    {
        get;
    }
}

public class PropertyGridRow : INotifyPropertyChanged
{
    public object? Value
    {
        get => value;
        set
        {
            if (this.value == null || !this.value.Equals(value))
            {
                this.value = value;
                NotifyPropertyChanged();
            }
        }
    }
    private object? value;

    public string Property
    {
        get => property;
        set
        {
            if (!property.Equals(value, StringComparison.Ordinal))
            {
                property = value;
                NotifyPropertyChanged();
            }
        }
    }
    private string property = string.Empty;

    public ObservableCollection<object?> SelectableValues { get; } = new();

    public Type? PropertyType
    {
        get;
    }

    #region INotifyPropertyChanged
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    internal PropertyGridRow()
    {
    }

    public PropertyGridRow(string propertyName, object value)
    {
        Value = value;
        Property = propertyName;
        PropertyType = value.GetType();
        if (value is Enum)
        {
            foreach (var enumValue in PropertyType.GetEnumValues())
            {
                SelectableValues.Add(enumValue);
            }
        }
    }
}