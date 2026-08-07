using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// Runs a gold project test whose name contains "WithLiveEditCode" after every other test in its class.
///
/// Embedding live edit's GlueControl closure writes ~40 files into the loaded project and saves it, and a
/// *subsequent* real project load in the same process then hangs inside GumPlugin's HandleGluxLoad: that
/// handler awaits inside a TaskManager task, TaskManager.SynchronousMode (forced on process-wide by
/// GlueTestBootstrap) runs it by blocking the calling thread on the awaited task, and the continuation needs
/// the STA thread the blocked call is sitting on. Nothing times out - the run sits there until the blame
/// collector kills it 15 minutes later, which reads as a slow build rather than a deadlock.
///
/// That fragility is in Glue's own async-void-plus-blocking-TaskManager plumbing, not in anything the gold
/// tests do wrong, and fixing it is well beyond adding compile coverage. Ordering these tests last means no
/// load ever follows an embed. xUnit's default ordering is a hash of the test case's unique ID, so without
/// this, renaming an unrelated test in the class is enough to reshuffle it back into the hang.
/// </summary>
public class LiveEditEmbedLastOrderer : ITestCaseOrderer
{
    public const string TypeName = "GlueUnitTests.TestSupport.LiveEditEmbedLastOrderer";
    public const string AssemblyName = "GlueUnitTests";

    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase =>
        testCases
            .OrderBy(testCase => Embeds(testCase) ? 1 : 0)
            .ThenBy(testCase => testCase.TestMethod.Method.Name, StringComparer.Ordinal);

    static bool Embeds(ITestCase testCase) =>
        testCase.TestMethod.Method.Name.Contains("WithLiveEditCode", StringComparison.Ordinal);
}
