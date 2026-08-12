using System;
using System.Collections.Generic;
using FlatRedBall.Glue.Controls;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Captures every <see cref="DialogService.ShowChoice"/> a test provokes, and answers each one with
/// "no button clicked" so the run continues.
/// </summary>
/// <remarks>
/// Must be installed, not merely asserted on afterwards: unstubbed, <c>ShowChoice</c> puts a real modal
/// on the developer's desktop and the test run blocks on it forever. <see cref="GlueTestBootstrap"/>
/// stubs <c>ShowMessageImpl</c> for the same reason but deliberately leaves the choice dialog alone,
/// since answering a choice changes what the code under test does next.
/// </remarks>
internal sealed class RecordedChoiceDialogs : IDisposable
{
    readonly Func<string, (string label, object value)[], object> _previous;

    public List<string> Messages { get; } = new();

    public RecordedChoiceDialogs()
    {
        _previous = DialogService.ShowChoiceImpl;
        DialogService.ShowChoiceImpl = (message, options) =>
        {
            Messages.Add(message);
            return null;
        };
    }

    public void Dispose() => DialogService.ShowChoiceImpl = _previous;
}
