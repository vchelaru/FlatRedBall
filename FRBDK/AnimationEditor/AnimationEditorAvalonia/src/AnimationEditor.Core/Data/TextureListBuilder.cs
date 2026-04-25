using FlatRedBall.Content.AnimationChain;
using System.Collections.Generic;
using System.Linq;

namespace AnimationEditor.Core.Data;

/// <summary>
/// Builds the list of textures referenced by the current .achx file (WF10).
///
/// Mirrors the logic in <c>MainWindow.RefreshTextureCombo()</c>:
/// walk all chains/frames, collect non-empty <c>TextureName</c> values,
/// return sorted and de-duplicated.
///
/// This class works with relative texture names only; path resolution to absolute
/// paths is the responsibility of the calling (UI) layer because it requires
/// knowledge of the .achx folder, which is a runtime/IO concern.
/// </summary>
public static class TextureListBuilder
{
    /// <summary>
    /// Returns a sorted, de-duplicated list of relative texture names referenced
    /// by any frame in <paramref name="acls"/>.
    /// </summary>
    /// <param name="acls">The animation chain list; may be <c>null</c>.</param>
    public static IReadOnlyList<string> GetAvailableTextures(AnimationChainListSave? acls)
    {
        if (acls is null)
            return [];

        return acls.AnimationChains
            .SelectMany(c => c.Frames)
            .Select(f => f.TextureName)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList()!;
    }
}
