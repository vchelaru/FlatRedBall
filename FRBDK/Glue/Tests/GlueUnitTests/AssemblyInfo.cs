using Xunit;

// Glue is built on process-wide singletons (GlueState.Self, GlueCommands.Self, ObjectFinder.Self,
// TaskManager, PluginManager), so most test classes here have to assign shared static state to set up
// their scenario. Running them in parallel means one class can overwrite another's setup mid-test - the
// symptom is a test which passes under --filter and fails in the full run.
//
// Isolating that state properly would mean removing the singletons, which isn't happening in FRB1
// maintenance mode. Serializing the whole assembly costs a few seconds on a sub-minute suite, so it's
// the better trade than expecting every new test author to know which statics are shared.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// LiveGameProcessTests loads a real project into process-wide Glue statics the same way
// GoldProjectCompileTests does - see LiveGameTestsLastCollectionOrderer's doc comment. Running its
// collection last means nothing else in the assembly can inherit whatever state it leaves dirty.
[assembly: TestCollectionOrderer(GlueUnitTests.TestSupport.LiveGameTestsLastCollectionOrderer.TypeName, GlueUnitTests.TestSupport.LiveGameTestsLastCollectionOrderer.AssemblyName)]
