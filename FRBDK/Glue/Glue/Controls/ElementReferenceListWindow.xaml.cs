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
    public ElementReferenceListWindow()
    {
        InitializeComponent();
    }

    public void PopulateWithReferencesTo(ReferencedFileSave rfs)
    {
        foreach (var item in ReferenceService.Self.GetReferencesTo(rfs))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesToElement(IElement element)
    {
        foreach (var item in ReferenceService.Self.GetReferencesToElement(element))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesTo(NamedObjectSave namedObjectSave, IElement container)
    {
        foreach (var item in ReferenceService.Self.GetReferencesTo(namedObjectSave, container))
            ItemListView.Items.Add(item);
        UpdateTextToReferenceCount();
    }

    public void PopulateWithReferencesTo(CustomVariable customVariable, IElement container)
    {
        foreach (var item in ReferenceService.Self.GetReferencesTo(customVariable, container))
            ItemListView.Items.Add(item);
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
