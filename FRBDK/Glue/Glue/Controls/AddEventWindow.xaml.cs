using System;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using System.Windows;
using FlatRedBall.Glue.GuiDisplay;
using GlueFormsCore.ViewModels;
using System.Windows.Controls;
using System.Collections.Generic;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for AddEventWindow.xaml
/// (Window shown when a user adds an Event to an entity)
/// </summary>
public partial class AddEventWindow
{

    private AddEventViewModel _viewModel;
    public AddEventViewModel ViewModel
    {
        get => _viewModel;
        set
        {
            _viewModel = value;
            if (_viewModel != null)
            {
                SetFrom(_viewModel);
            }
        }
    }

    #region Initializers
    public AddEventWindow()
    {
        InitializeComponent();

        FillAvailableDelegateTypes();
        FillTunnelingObjects();
        FillTypeConverters();
        UpdateGenericTypeElementsVisibility();
    }

    /// <summary>
    /// Fills the available types combo box with the available delegate types.
    /// </summary>
    private void FillAvailableDelegateTypes()
    {
        foreach (var value in AvailableDelegateTypeConverter.GetAvailableDelegates())
        {
            this.AvailableTypesComboBox.Items.Add(value);
        }
    }

    /// <summary>
    /// Fill Tunneling Object Combo Box with the available named objects/files
    /// </summary>
    private void FillTunnelingObjects()
    {
        var availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, false, GlueState.Self.CurrentElement, null);

        foreach (string availableObject in availableObjects)
            this.TunnelingObjectComboBox.Items.Add(availableObject);

        if (TunnelingObjectComboBox.Items.Count > 0)
            TunnelingObjectComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Set TypeConverterComboBox items the user can select from.
    /// </summary>
    private void FillTypeConverters()
    {
        foreach (var converter in AvailableCustomVariableTypeConverters.GetAvailableConverters())
            TypeConverterComboBox.Items.Add(converter);

        if (TypeConverterComboBox.Items.Count > 0)
            TypeConverterComboBox.SelectedIndex = 0;
    }
    #endregion

    #region Events


    /// <summary>
    /// Event invoked when the user switches between the radio options denoting the type of event that is being created.
    /// </summary>
    private void EventTypePicked(object sender, RoutedEventArgs e)
    {
        // ignore invocation at startup, when the viewmodel isn't made yet.
        if (ViewModel == null)
            return;

        NewOptions.Visibility = Visibility.Hidden;
        ExposingOptions.Visibility = Visibility.Hidden;
        TunnelOptions.Visibility = Visibility.Hidden;

        if (sender.Equals(EventTypeNew))
        {
            ViewModel.DesiredEventType = CustomEventType.New;
            NewOptions.Visibility = Visibility.Visible;

            this.InvalidateVisual();
            return;
        }

        if (sender.Equals(EventTypeTunnel))
        {
            ViewModel.DesiredEventType = CustomEventType.Tunneled;
            TunnelOptions.Visibility = Visibility.Visible;

            this.InvalidateVisual();
            return;
        }

        if (sender.Equals(EventTypeExisting))
        {
            ViewModel.DesiredEventType = CustomEventType.Exposed;
            ExposingOptions.Visibility = Visibility.Visible;

            this.InvalidateVisual();
            return;
        }

        throw new NotImplementedException(); // only the 3 radio buttons can be used!
    }

    /// <summary>
    /// Invoked when the Available Types Combobox has been altered,
    /// because the Generic Type UI elements need to become (in)visible
    /// if the selected item contains a generic type 'T'
    /// </summary>
    private void AvailableTypesComboBox_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateGenericTypeElementsVisibility(e.AddedItems[0]!.ToString());
    }

    /// <summary>
    /// Invoked when the TunnelingEventComboBox changes value
    /// </summary>
    private void TunnelingVariableComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        AlternativeTextBox.Text =
            TunnelingObjectComboBox.Text +
            TunnelingEventComboBox.Text;
    }

    /// <summary>
    /// Invoked when the TunnelingObjectComboBox changes value
    /// </summary>
    private void TunnelingObjectComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedItemName = TunnelingObjectComboBox.SelectedItem?.ToString();

        var nos = GlueState.Self.CurrentElement.GetNamedObjectRecursively(selectedItemName);
        if (nos == null)
            return;

        var availableEvents = ExposedEventManager.GetExposableEventsFor(nos, GlueState.Self.CurrentElement);
        availableEvents.Sort();

        this.TunnelingEventComboBox.Items.Clear();

        foreach (var availableVariable in availableEvents)
        {
            this.TunnelingEventComboBox.Items.Add(availableVariable);
        }
    }

    /// <summary>
    /// Shows the GenericType element if options that can contain generic type 'T' have been selected.
    /// </summary>
    /// <param name="value"></param>
    private void UpdateGenericTypeElementsVisibility(string value = null)
    {
        var showGenericUi = value?.Contains("<T>") ?? false ? Visibility.Visible : Visibility.Hidden;

        this.GenericTypeLabel.Visibility = showGenericUi;
        this.GenericTypeTextBox.Visibility = showGenericUi;
    }

    private void SetFrom(AddEventViewModel viewModel)
    {
        if (viewModel.TunnelingObject != null)
        {
            foreach (var item in this.TunnelingEventComboBox.Items)
            {
                if (String.Equals(item.ToString(), viewModel.TunnelingEvent, StringComparison.OrdinalIgnoreCase))
                {
                    this.TunnelingEventComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        else if (this.TunnelingEventComboBox.Items.Count > 0)
        {
            this.TunnelingEventComboBox.SelectedItem = this.TunnelingEventComboBox.Items[0];
        }

        foreach (var variableName in ViewModel.ExposableEvents)
        {
            AvailableEventsComboBox.Items.Add(variableName);
        }

        if (ViewModel.ExposableEvents.Count > 0 && AvailableEventsComboBox.Items.Count > 0)
        {
            AvailableEventsComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// User presses Ok-button: perform the task of creating the new event based on the user's specifications
    /// </summary>
    /// 
    private void Submit(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
        this.Close();
    }

    /// <summary>
    /// User presses cancel button; discard all changes and close window.
    /// </summary>
    private void Cancel(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    #endregion

    #region Result Properties (for external use after window completes)

    public string ResultName
    {
        get
        {
            return ViewModel.DesiredEventType switch
            {
                CustomEventType.Exposed => this.AvailableEventsComboBox.Text,
                CustomEventType.Tunneled => this.AlternativeTextBox.Text,
                CustomEventType.New => this.EventNameTextBox.Text,
                _ => throw new NotImplementedException()
            };
        }
    }

    public string SourceVariable
    {
        get
        {
            if (ViewModel.DesiredEventType == CustomEventType.Exposed)
            {
                object selectedItem = this.AvailableEventsComboBox.SelectedItem;

                return ((ExposableEvent)selectedItem).Variable;
            }

            return null;
        }
    }

    public BeforeOrAfter BeforeOrAfter
    {
        get
        {
            object selectedItem = this.AvailableEventsComboBox.SelectedItem;

            return ((ExposableEvent)selectedItem).BeforeOrAfter;
        }
    }

    public string ResultDelegateType
    {
        get
        {
            if (ViewModel.DesiredEventType == CustomEventType.New)
            {
                string toReturn = this.AvailableTypesComboBox.Text;
                if (toReturn.Contains("<T>"))
                {
                    toReturn = toReturn.Replace("<T>",
                        "<" + GenericTypeTextBox.Text + ">");

                }
                return toReturn;
            }

            return "";

        }
    }

    public string TunnelingObject
    {
        get
        {
            if (ViewModel.DesiredEventType == CustomEventType.Tunneled)
            {
                return TunnelingObjectComboBox.Text;
            }

            return null;
        }
        set => TunnelingObjectComboBox.Text = value;
    }

    public string TunnelingEvent
    {
        get
        {
            if (ViewModel.DesiredEventType == CustomEventType.Tunneled)
            {
                return TunnelingEventComboBox.Text;
            }

            return null;
        }
    }


    public string OverridingType
    {
        get
        {
            if (OverridingPropertyTypeComboBox.SelectedIndex >= 0)
                return OverridingPropertyTypeComboBox.Text;
            else
                return null;
        }
    }
    #endregion
}
