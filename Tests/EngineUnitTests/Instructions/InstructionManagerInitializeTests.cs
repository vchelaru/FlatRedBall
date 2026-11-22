using EngineUnitTests.TestSupport;
using FlatRedBall.Instructions;
using Shouldly;
using Xunit;

namespace EngineUnitTests.Instructions;

// Glue's test host initializes InstructionManager the way Glue.exe does, and a gold-project test then
// initializes the whole engine in the same process, so Initialize runs twice (#2429).
[Collection(nameof(EngineStatefulTestCollection))]
public class InstructionManagerInitializeTests
{
    [Fact]
    public void Initialize_CalledAgainAfterEngineInitialization_DoesNotThrowOrDuplicateTables()
    {
        EngineTestBootstrap.EnsureInitialized();
        var relationshipCount = InstructionManager.AbsoluteRelativeValueRelationships.Count;

        Should.NotThrow(() => InstructionManager.Initialize());

        InstructionManager.AbsoluteRelativeValueRelationships.Count.ShouldBe(relationshipCount);
        InstructionManager.GetRelativeForAbsolute("Z").ShouldBe("RelativeZ");
    }
}
