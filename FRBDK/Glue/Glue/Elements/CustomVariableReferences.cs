using System.Collections.Generic;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Elements;

/// <summary>
/// All known references to a <see cref="CustomVariable"/> gathered from the live project.
/// Returned by <see cref="IReferenceService.GetCustomVariableReferences"/>.
/// </summary>
public class CustomVariableReferences
{
    /// <summary>
    /// States (within the owning element) that contain an <see cref="InstructionSave"/>
    /// assigning this variable. Each entry carries the owning element so callers can
    /// display "State X in Element Y" without a second lookup.
    /// </summary>
    public List<(GlueElement Element, StateSave State)> StateReferences { get; } = new();

    /// <summary>
    /// <see cref="CustomVariable"/>s in derived elements where
    /// <see cref="CustomVariable.DefinedByBase"/> is true and the name matches.
    /// Each entry carries the derived element so callers can navigate directly to it.
    /// </summary>
    public List<(GlueElement Element, CustomVariable Variable)> DerivedVariableReferences { get; } = new();

    /// <summary>
    /// The <see cref="NamedObjectSave"/> within the owning element that this variable
    /// is tunneled through (<see cref="CustomVariable.SourceObject"/> is set).
    /// Usually zero or one entry; a list for API uniformity.
    /// </summary>
    public List<(GlueElement Element, NamedObjectSave Instance)> TunneledFromInstances { get; } = new();

    /// <summary>
    /// <see cref="NamedObjectSave"/>s in other screens/entities that are typed to the
    /// owning element AND have an <see cref="CustomVariableInNamedObject"/> explicitly
    /// setting this variable by name. These are the callers that would break on a rename.
    /// </summary>
    public List<(GlueElement Container, NamedObjectSave Instance)> InstancesOfOwnerType { get; } = new();

    /// <summary>
    /// <see cref="EventResponseSave"/>s in the owning element that are wired to fire
    /// when this variable changes.
    /// </summary>
    public List<EventResponseSave> EventReferences { get; } = new();
}
