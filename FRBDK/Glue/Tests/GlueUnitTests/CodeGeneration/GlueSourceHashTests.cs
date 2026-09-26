using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;

namespace GlueUnitTests.CodeGeneration;

public class GlueSourceHashTests
{
    static EntitySave CreateEntity(object debugValue)
    {
        var entity = new EntitySave { Name = "Entities\\DebuggingVariables" };
        entity.CustomVariables.Add(new CustomVariable
        {
            Name = "DebugValue",
            Type = "float",
            DefaultValue = debugValue,
            IsShared = true,
        });
        return entity;
    }

    [Fact]
    public void Compute_SameContent_ReturnsSameHash()
    {
        GlueSourceHash.Compute(CreateEntity(1f)).ShouldBe(GlueSourceHash.Compute(CreateEntity(1f)));
    }

    [Fact]
    public void Compute_ChangedVariableValue_ReturnsDifferentHash()
    {
        GlueSourceHash.Compute(CreateEntity(1f)).ShouldNotBe(GlueSourceHash.Compute(CreateEntity(2f)));
    }

    [Fact]
    public void GenerateSourceHashAttribute_WritesAssemblyMetadataKeyedByEscapedElementName()
    {
        var entity = CreateEntity(1f);
        var block = new CodeDocument(0);

        CodeWriter.GenerateSourceHashAttribute(block, entity);

        block.ToString().Trim().ShouldBe(
            "[assembly: System.Reflection.AssemblyMetadata(\"GlueSourceHash:Entities\\\\DebuggingVariables\", \"" +
            GlueSourceHash.Compute(entity) + "\")]");
    }
}
