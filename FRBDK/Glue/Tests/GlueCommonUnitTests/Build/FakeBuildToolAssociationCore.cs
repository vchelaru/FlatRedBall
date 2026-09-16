using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;

namespace GlueCommonUnitTests.Build;

/// <summary>
/// Hand-rolled test double for <see cref="IBuildToolAssociationCore"/> - the real implementation
/// (<c>BuildToolAssociationManager</c>) lives in Glue.csproj and isn't reachable from this net8.0
/// test project.
/// </summary>
public class FakeBuildToolAssociationCore : IBuildToolAssociationCore
{
    readonly Dictionary<ReferencedFileSave, string> _buildToolProcessedByFile = new();

    public void SetBuildToolProcessed(ReferencedFileSave referencedFileSave, string buildToolProcessed) =>
        _buildToolProcessedByFile[referencedFileSave] = buildToolProcessed;

    public string GetBuildToolProcessed(ReferencedFileSave referencedFileSave) =>
        _buildToolProcessedByFile.TryGetValue(referencedFileSave, out var buildToolProcessed) ? buildToolProcessed : null;
}
