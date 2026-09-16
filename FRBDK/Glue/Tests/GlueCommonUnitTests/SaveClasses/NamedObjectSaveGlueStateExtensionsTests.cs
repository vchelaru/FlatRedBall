using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.SaveClasses;

// Reads the shared static GlueStateCore.Self, so this can't run concurrently with any other test
// class that swaps it out - see ObjectFinderCoreCollection in NamedObjectSaveElementExtensionsTests.cs.
[Collection(nameof(ObjectFinderCoreCollection))]
public class NamedObjectSaveGlueStateExtensionsTests
{
    readonly FakeGlueStateCore _glueState = new();

    public NamedObjectSaveGlueStateExtensionsTests()
    {
        GlueStateCore.Self = _glueState;
    }

    [Fact]
    public void GetMemberMembershipInfo_NoCurrentScreenOrEntity_ReturnsNotContained()
    {
        Assert.Equal(MembershipInfo.NotContained,
            NamedObjectSaveGlueStateExtensions.GetMemberMembershipInfo("SomeMember"));
    }

    [Fact]
    public void GetMemberMembershipInfo_CurrentScreenHasMember_ReturnsContainedInThis()
    {
        _glueState.CurrentScreenSave = new ScreenSave
        {
            NamedObjects = { new NamedObjectSave { InstanceName = "SomeMember" } }
        };

        Assert.Equal(MembershipInfo.ContainedInThis,
            NamedObjectSaveGlueStateExtensions.GetMemberMembershipInfo("SomeMember"));
    }

    [Fact]
    public void GetMemberMembershipInfo_CurrentScreenLacksMember_ReturnsNotContained()
    {
        _glueState.CurrentScreenSave = new ScreenSave();

        Assert.Equal(MembershipInfo.NotContained,
            NamedObjectSaveGlueStateExtensions.GetMemberMembershipInfo("SomeMember"));
    }

    [Fact]
    public void GetMemberMembershipInfo_CurrentEntityHasMember_DelegatesToEntitySave()
    {
        _glueState.CurrentEntitySave = new EntitySave
        {
            NamedObjects = { new NamedObjectSave { InstanceName = "SomeMember" } }
        };

        Assert.Equal(MembershipInfo.ContainedInThis,
            NamedObjectSaveGlueStateExtensions.GetMemberMembershipInfo("SomeMember"));
    }

    [Fact]
    public void GetMemberMembershipInfo_CurrentScreenTakesPriorityOverEntity()
    {
        // GlueState.CurrentScreenSave/CurrentEntitySave are mutually exclusive in the real
        // implementation (both set from the same CurrentElement), but the extension method only
        // checks CurrentScreenSave first - pin that order explicitly.
        _glueState.CurrentScreenSave = new ScreenSave();
        _glueState.CurrentEntitySave = new EntitySave
        {
            NamedObjects = { new NamedObjectSave { InstanceName = "SomeMember" } }
        };

        Assert.Equal(MembershipInfo.NotContained,
            NamedObjectSaveGlueStateExtensions.GetMemberMembershipInfo("SomeMember"));
    }
}
