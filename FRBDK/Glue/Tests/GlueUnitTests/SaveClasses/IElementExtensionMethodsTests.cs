using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using Shouldly;
using Xunit;

namespace GlueUnitTests.SaveClasses;

/// <summary>
/// GitHub issue #2140: MainPropertyGridPlugin threw a NullReferenceException in
/// <see cref="IElementExtensionMethods.GetAllReferencedFileSavesRecursively"/> during RefreshGrid,
/// reported alongside the right panel's tab selection breaking (issue #2139). The crash comes from
/// AssetTypeInfoManager's "does this element already have an .achx" check
/// (IsVariableVisibleInEditor), which is called with whatever element is currently selected - during
/// a selection transition that element can momentarily be null.
/// </summary>
public class IElementExtensionMethodsTests
{
    [Fact]
    public void GetAllReferencedFileSavesRecursively_ShouldReturnEmpty_WhenInstanceIsNull()
    {
        IElement instance = null;

        var result = instance.GetAllReferencedFileSavesRecursively().ToList();

        result.ShouldBeEmpty();
    }
}
