using System;
using System.Linq;
using Npc;
using Npc.Data;
using Shouldly;

namespace GlueUnitTests.Projects;

// The New Project wizard's platform dropdown groups its rows by engine version, so an entry's name is
// split in three: a short FriendlyName, the Category that becomes the group header, and the muted
// Details line. Splitting it that way is what stops the rows being unreadably long, but it also means a
// row on its own no longer says which engine it belongs to -- these pin the parts of that split which
// are not visible until someone opens the dropdown. See GitHub issue #2127.
public class NewProjectPlatformNamingTests
{
    [Fact]
    public void EveryWizardEntry_ShouldDeclareACategory()
    {
        foreach (var project in EmptyTemplates.Projects)
        {
            project.Category.ShouldNotBeNullOrEmpty(
                $"'{project.FriendlyName}' has no Category, so it falls under a blank group header and " +
                "its name never says which engine version it creates.");
        }
    }

    // Both engines offer a MonoGame desktop template, so the short names collide by design. The
    // qualified name is the only thing separating them in the closed ComboBox.
    [Fact]
    public void NoTwoWizardEntries_ShouldShareAQualifiedName()
    {
        var duplicates = EmptyTemplates.Projects
            .GroupBy(project => project.QualifiedFriendlyName)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        duplicates.ShouldBeEmpty(
            $"These names appear more than once in the platform dropdown: {string.Join(", ", duplicates)}. " +
            "Rows that read identically once the dropdown is closed cannot be told apart.");
    }

    // The category is prepended to build the qualified name, so an entry carrying it in FriendlyName too
    // renders as "FlatRedBall 1 - FlatRedBall 1 Desktop".
    [Fact]
    public void NoWizardEntryFriendlyName_ShouldRepeatItsEngineVersion()
    {
        foreach (var project in EmptyTemplates.Projects)
        {
            project.FriendlyName.ShouldNotContain("FlatRedBall 1", Case.Insensitive);
            project.FriendlyName.ShouldNotContain("FlatRedBall 2", Case.Insensitive);
        }
    }

    // Web and FNA are FlatRedBall 1 engines. Listing them outside the FlatRedBall 1 group reads as though
    // they were a third and fourth product.
    [Theory]
    [InlineData("FlatRedBallDesktopGlMonoGameTemplate")]
    [InlineData("FlatRedBallWebTemplate")]
    [InlineData("FlatRedBallDesktopFnaTemplate")]
    public void ZipTemplates_ShouldBeCategorizedUnderFlatRedBall1(string templateNamespace)
    {
        var project = EmptyTemplates.Projects
            .FirstOrDefault(item => item.Namespace == templateNamespace);

        project.ShouldNotBeNull($"The wizard no longer offers '{templateNamespace}'.");
        project.Category.ShouldBe(PlatformProjectInfo.Frb1Category);
    }

    [Fact]
    public void TheFlatRedBall2Template_ShouldBeCategorizedUnderFlatRedBall2()
    {
        var project = EmptyTemplates.Projects.OfType<DotnetNewProjectInfo>().Single();

        project.Category.ShouldBe(PlatformProjectInfo.Frb2Category);
    }
}
