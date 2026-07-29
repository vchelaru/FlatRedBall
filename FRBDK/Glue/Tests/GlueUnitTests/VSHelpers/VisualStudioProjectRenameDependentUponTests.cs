using System;
using System.IO;
using FlatRedBall.Glue.VSHelpers.Projects;
using GlueUnitTests.TestSupport;
using Shouldly;

namespace GlueUnitTests.VSHelpers;

// #1771: renaming an element (e.g. F2 rename of an Entity) updates the .Generated.cs project item's
// Include, but left its DependentUpon metadata pointing at the old (pre-rename) .cs file name -
// VisualStudioProject.RenameItem's Generated.cs detection was matching the literal ".generated.cs"
// against the real, capitalized ".Generated.cs" file names Glue actually writes, so the block never ran.
public class VisualStudioProjectRenameDependentUponTests
{
    [Fact]
    public void RenameItem_ShouldUpdateDependentUpon_WhenRenamingAGeneratedCodeFile()
    {
        string? directory = null;
        try
        {
            var project = TestVisualStudioProjectFactory.CreateInNewTempDirectory(out directory);

            project.AddCodeBuildItem("SeaGoatCopy.cs");
            var generatedItem = project.AddCodeBuildItem("SeaGoatCopy.Generated.cs");
            generatedItem.SetMetadataValue("DependentUpon", "SeaGoatCopy.cs");

            project.RenameItem("SeaGoatCopy.cs", "DeepOne_Swarm.cs");
            project.RenameItem("SeaGoatCopy.Generated.cs", "DeepOne_Swarm.Generated.cs");

            generatedItem.GetMetadataValue("DependentUpon").ShouldBe("DeepOne_Swarm.cs");
        }
        finally
        {
            if (directory != null) Directory.Delete(directory, recursive: true);
        }
    }
}
