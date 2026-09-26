using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.Managers;
using Shouldly;

namespace GlueUnitTests.GameCommunicationPlugin;

public class CompiledElementSweepTests
{
    static EntitySave CreateEntity(string name, object value)
    {
        var entity = new EntitySave { Name = name };
        entity.CustomVariables.Add(new CustomVariable { Name = "DebugValue", Type = "float", DefaultValue = value, IsShared = true });
        return entity;
    }

    [Fact]
    public void GetElementsToResend_HashDiffersFromCompiled_IncludesElement()
    {
        var entity = CreateEntity("Entities\\A", 2f);
        var compiled = new Dictionary<string, string> { [entity.Name] = GlueSourceHash.Compute(CreateEntity("Entities\\A", 1f)) };

        CompiledElementSweep.GetElementsToResend(compiled, new[] { entity }).ShouldBe(new GlueElement[] { entity });
    }

    [Fact]
    public void GetElementsToResend_HashMatchesCompiled_ExcludesElement()
    {
        var entity = CreateEntity("Entities\\A", 1f);
        var compiled = new Dictionary<string, string> { [entity.Name] = GlueSourceHash.Compute(entity) };

        CompiledElementSweep.GetElementsToResend(compiled, new[] { entity }).ShouldBeEmpty();
    }

    // An element with no compiled hash has no code in the running game (added after the build, or the game
    // predates source hashes), so there is nothing a live variable push could land on.
    [Fact]
    public void GetElementsToResend_ElementMissingFromCompiled_ExcludesElement()
    {
        var entity = CreateEntity("Entities\\A", 1f);

        CompiledElementSweep.GetElementsToResend(new Dictionary<string, string>(), new[] { entity }).ShouldBeEmpty();
    }
}
