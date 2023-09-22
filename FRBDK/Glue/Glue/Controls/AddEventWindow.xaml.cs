using System;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using System.Windows;
using FlatRedBall.Glue.GuiDisplay;
using GlueFormsCore.ViewModels;
using System.Windows.Controls;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for AddEventWindow.xaml
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
        UpdateGenericUiVisibility();

    }

    private void FillAvailableDelegateTypes()
    {
        foreach (var value in AvailableDelegateTypeConverter.GetAvailableDelegates())
        {
            this.AvailableTypesComboBox.Items.Add(value);
        }
    }


    private void FillTunnelingObjects()
    {
        var availableObjects = AvailableNamedObjectsAndFiles.GetAvailableObjects(false, false, GlueState.Self.CurrentElement, null);

        foreach (string availableObject in availableObjects)
            this.TunnelingObjectComboBox.Items.Add(availableObject);

        if (TunnelingObjectComboBox.Items.Count > 0)
            TunnelingObjectComboBox.SelectedIndex = 0;
    }

    private void FillTunnelableEventsFor(NamedObjectSave nos)
    {
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
    private void AvailableTypesComboBox_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateGenericUiVisibility(e.AddedItems[0]!.ToString());
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
        NamedObjectSave nos = GlueState.Self.CurrentElement.GetNamedObjectRecursively(TunnelingObjectComboBox.Text);
        FillTunnelableEventsFor(nos);
    }

    private void UpdateGenericUiVisibility(string value = null)
    {
        var showGenericUi = value?.Contains("<T>") ?? false ? Visibility.Visible : Visibility.Hidden;

        this.GenericTypeLabel.Visibility = showGenericUi;
        this.GenericTypeTextBox.Visibility = showGenericUi;
    }

    private void SetFrom(AddEventViewModel viewModel)
    {
        this.TunnelingObjectComboBox.SelectedItem = viewModel.TunnelingObject;

        foreach (var item in this.TunnelingEventComboBox.Items)
        {
            if (String.Equals(item.ToString(), viewModel.TunnelingEvent, StringComparison.OrdinalIgnoreCase))
            {
                this.TunnelingEventComboBox.SelectedItem = item;
                break;
            }
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
