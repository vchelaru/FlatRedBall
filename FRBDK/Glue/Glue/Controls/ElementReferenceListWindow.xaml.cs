using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Elements;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using L = Localization;

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

    /// <summary>
    /// Populates the list view with references to <paramref name="rfs"/>.
    /// </summary>
    public void PopulateWithReferencesTo(ReferencedFileSave rfs)
    {
        var elements = ObjectFinder.Self.GetAllElementsReferencingFile(rfs.Name);

        foreach (var element in elements)
        {
            var rfsInThisElement = element.GetReferencedFileSave(rfs.Name);
            ItemListView.Items.Add(rfsInThisElement);

            foreach (var namedObject in element.AllNamedObjects)
            {
                if (namedObject.SourceType == SourceType.File &&
                    namedObject.SourceFile == rfsInThisElement.Name)
                {
                    ItemListView.Items.Add(namedObject);
                }
            }
        }

        // If this is a CSV, then loop through all of the variables and see if any of them use this type
        if (rfs.IsCsvOrTreatedAsCsv)
        {
            var className = rfs.Name;
            var customClass = ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(rfs.Name);
            if (customClass != null)
            {
                className = customClass.Name;
            }

            foreach (var customVariable in ObjectFinder.Self.GlueProject.Screens
                         .Cast<IElement>()
                         .SelectMany(element => element.CustomVariables.Where(customVariable =>
                             String.Equals(customVariable.Type, className, StringComparison.OrdinalIgnoreCase))))
            {
                ItemListView.Items.Add(customVariable);
            }

            foreach (var customVariable in ObjectFinder.Self.GlueProject.Entities
                         .Cast<IElement>()
                         .SelectMany(element => element.CustomVariables.Where(customVariable =>
                         String.Equals(customVariable.Type, className, StringComparison.OrdinalIgnoreCase))))
            {
                ItemListView.Items.Add(customVariable);
            }
        }

    }

    /// <summary>
    /// Populates the list view with references to the supplied <paramref name="element"/>.
    /// </summary>
    public void PopulateWithReferencesToElement(IElement element)
    {
        #region Get all named objects

        List<NamedObjectSave> referencedNamedObjectSaves = null;

        if (element is EntitySave save)
        {
            referencedNamedObjectSaves = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(save);
        }

        // TODO:  Handle inheritance here
        if (referencedNamedObjectSaves != null)
        {
            foreach (var nos in referencedNamedObjectSaves)
            {
                ItemListView.Items.Add(nos);
            }
        }

        #endregion

        if (element is ScreenSave screenSave)
        {
            var screens = ObjectFinder.Self.GetAllScreensThatInheritFrom(screenSave);

            // See if any Screens link to this as their next Screen in Glue
            foreach (var screen in ObjectFinder.Self.GlueProject.Screens.Where(screen => screen.NextScreen == element.Name && !screens.Contains(screen)))
            {
                screens.Add(screen);
            }

            foreach (var screen in screens)
            {
                ItemListView.Items.Add(screen);
            }
        }
        else if (element is EntitySave entitySave)
        {
            var entities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entitySave);

            foreach (var entity in entities)
            {
                ItemListView.Items.Add(entity);
            }
        }
        UpdateTextToReferenceCount();
    }

    private void UpdateTextToReferenceCount()
    {
        switch (ItemListView.Items.Count)
        {
            case 0:
                this.Text.Content = L.Texts.ReferenceFoundNone;
                return;
            case 1:
                this.Text.Content = L.Texts.ReferenceFoundOne;
                return;
            default:
                this.Text.Content = String.Format(L.Texts.ReferenceFoundAmount, ItemListView.Items.Count);
                break;
        }
    }

    private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        var highlightedObject = ItemListView.SelectedItem;

        var glueState = GlueState.Self;
        switch (highlightedObject)
        {
            case null:
                // do nothing
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

    /// <summary>
    /// Populates the list view with references to the supplied <paramref name="container"/>'s CustomVariables if they have <paramref name="namedObjectSave"/> as SourceObject.
    /// </summary>
    internal void PopulateWithReferencesTo(NamedObjectSave namedObjectSave, IElement container)
    {
        foreach (var variable in container.CustomVariables.Where(item => item.SourceObject == namedObjectSave.InstanceName))
        {
            ItemListView.Items.Add(variable);
        }

        var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(container);

        foreach (var nos in derivedElements
                     .SelectMany(element => element.NamedObjects
                         .Where(item => item.DefinedByBase && String.Equals(item.InstanceName, namedObjectSave.InstanceName, StringComparison.OrdinalIgnoreCase))))
        {
            ItemListView.Items.Add(nos);
        }
    }

    /// <summary>
    /// Populates the list view with references to the supplied <paramref name="container"/>'s States' instructions if they have <paramref name="customVariable"/> as Member.
    /// </summary>
    internal void PopulateWithReferencesTo(CustomVariable customVariable, IElement container)
    {
        foreach (var state in container.AllStates)
        {
            if (state.InstructionSaves.Any(instruction => String.Equals(instruction.Member, customVariable.Name, StringComparison.OrdinalIgnoreCase)))
            {
                ItemListView.Items.Add(state);
            }
        }

        foreach (var variable in ObjectFinder.Self.GetAllElementsThatInheritFrom(container)
                     .SelectMany(element => element.CustomVariables
                                            .Where(item => item.DefinedByBase 
                                                    && String.Equals(item.Name, customVariable.Name, StringComparison.OrdinalIgnoreCase))))
        {
            ItemListView.Items.Add(variable);
        }

        foreach (var ers in container.GetEventsOnVariable(customVariable.Name))
        {
            ItemListView.Items.Add(ers);
        }
    }
    
    /// <summary>
    /// Closes the screen
    /// </summary>
    private void CloseScreen(object sender, EventArgs e)
    {
        this.Close();
    }
}
