using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Runs the test collection containing <c>LiveGameProcessTests</c> after every other collection in the
/// assembly.
///
/// <c>LiveGameProcessTests</c> loads a real gold project into the process-wide Glue statics
/// (<see cref="FlatRedBall.Glue.Managers.GlueState"/>, <c>FileManager.RelativeDirectory</c>, the STA
/// thread's <see cref="System.Threading.SynchronizationContext"/>, ...) the same way
/// <c>GoldProjectCompileTests</c> does - see <see cref="LiveEditEmbedLastOrderer"/>'s doc comment for the
/// mechanism. That class only orders test cases *within* one class; nothing stopped a completely different
/// class from running right after <c>LiveGameProcessTests</c> and inheriting whatever it left dirty. That
/// showed up two different ways depending on what happened to run next: sometimes an unrelated test failed
/// with a <c>FileNotFoundException</c> against a directory <c>LiveGameProcessTests</c> had already deleted,
/// sometimes the whole run hung forever with every thread idle (an StaFact-pumped test's
/// <c>SynchronizationContext</c> silently lost, so its continuations stopped being posted back to the pump
/// that is waiting for them). See GitHub issue #2008 and the 2026-08-07 entries in
/// <c>FRBDK/Glue/.claude/testing-incidents.md</c>.
///
/// Ordering the collection last means nothing else in the assembly ever runs after it, so whatever state it
/// leaves behind can no longer reach another test. xUnit's default collection order is not alphabetical or
/// declaration order - without this, the collection can land anywhere, including first.
/// </summary>
public class LiveGameTestsLastCollectionOrderer : ITestCollectionOrderer
{
    public const string TypeName = "GlueUnitTests.TestSupport.LiveGameTestsLastCollectionOrderer";
    public const string AssemblyName = "GlueUnitTests";

    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections) =>
        testCollections.OrderBy(collection => ContainsLiveGameProcessTests(collection) ? 1 : 0);

    static bool ContainsLiveGameProcessTests(ITestCollection collection) =>
        collection.DisplayName.Contains("LiveGameProcessTests");
}
