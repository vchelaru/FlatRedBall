using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using FlatRedBall.Glue.Elements;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for ElementReferenceListWindow.xaml
/// This window is shown if references to an item (entity, screen, etc) need to be listed.
/// </summary>
public partial class ElementReferenceListWindow
{
    readonly IReferenceService _referenceService;

    public ElementReferenceListWindow(IReferenceService referenceService)
    {
        _referenceService = referenceService;
        InitializeComponent();
    }

    public void PopulateWithReferencesTo(ReferencedFileSave rfs)
    {
        foreach (var item in _referenceService.GetReferencesTo(rfs))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesToElement(IElement element)
    {
        foreach (var item in _referenceService.GetReferencesToElement(element))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesTo(NamedObjectSave namedObjectSave, IElement container)
    {
        foreach (var item in _referenceService.GetReferencesTo(namedObjectSave, container))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesTo(CustomVariable customVariable, IElement container)
    {
        var refs = _referenceService.GetCustomVariableReferences(customVariable, container);
        foreach (var (_, state) in refs.StateReferences)
            ItemListView.Items.Add(state);
        foreach (var (_, variable) in refs.DerivedVariableReferences)
            ItemListView.Items.Add(variable);
        foreach (var (_, nos) in refs.TunneledFromInstances)
            ItemListView.Items.Add(nos);
        foreach (var (_, nos) in refs.InstancesOfOwnerType)
            ItemListView.Items.Add(nos);
        foreach (var ers in refs.EventReferences)
            ItemListView.Items.Add(ers);
        UpdateTextToReferenceCount();
    }

    private void UpdateTextToReferenceCount()
    {
        this.Text.Content = ItemListView.Items.Count switch
        {
            0 => "No references found",
            1 => "1 reference found",
            var n => $"{n} references found"
        };
    }

    private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        var glueState = GlueState.Self;
        switch (ItemListView.SelectedItem)
        {
            case null:
                break;
            case ScreenSave screenSave:
                glueState.CurrentScreenSave = screenSave;
                break;
            case EntitySave entitySave:
                glueState.CurrentEntitySave = entitySave;
                break;
            case NamedObjectSave namedObjectSave:
                glueState.CurrentNamedObjectSave = namedObjectSave;
                break;
            case ReferencedFileSave referencedFileSave:
                glueState.CurrentReferencedFileSave = referencedFileSave;
                break;
            case CustomVariable customVariable:
                glueState.CurrentCustomVariable = customVariable;
                break;
            case StateSave state:
                glueState.CurrentStateSave = state;
                break;
            case EventResponseSave eventResponse:
                glueState.CurrentEventResponseSave = eventResponse;
                break;
        }
    }

    private void CloseScreen(object sender, EventArgs e) => this.Close();
}
