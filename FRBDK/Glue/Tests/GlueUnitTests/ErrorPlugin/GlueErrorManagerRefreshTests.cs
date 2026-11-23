using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ErrorPlugin;

/// <summary>
/// An error added straight to <see cref="GlueErrorManager"/> (the content pipeline's missing-builder error) only
/// reaches the Errors tab through a refresh, so adding one has to ask for it.
/// </summary>
public class GlueErrorManagerRefreshTests
{
    public GlueErrorManagerRefreshTests() => GlueTestBootstrap.EnsureInitialized();

    [Fact]
    public void Add_ShouldRefreshErrors()
    {
        var originalAction = RefreshCommands.RefreshErrorsAction;
        var refreshCount = 0;
        RefreshCommands.RefreshErrorsAction = () => refreshCount++;
        var manager = EditorObjects.IoC.Container.Get<GlueErrorManager>();
        try
        {
            manager.Add(new FixedError());

            refreshCount.ShouldBe(1);
        }
        finally
        {
            RefreshCommands.RefreshErrorsAction = originalAction;
            manager.ClearFixedErrors();
        }
    }

    /// <summary>Reports itself fixed so <see cref="GlueErrorManager.ClearFixedErrors()"/> removes it afterwards.</summary>
    private class FixedError : ErrorViewModel
    {
        public override string UniqueId => nameof(GlueErrorManagerRefreshTests);
        public override bool GetIfIsFixed() => true;
    }
}
