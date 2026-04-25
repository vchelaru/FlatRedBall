using FlatRedBall.Content.AnimationChain;

namespace AnimationEditor.Core.IO;

/// <summary>
/// Adjusts UV coordinates when a texture is resized (IO15 — pad to power-of-two).
///
/// Formula per coordinate:
///   pixels = Round(oldDimension × coordinate)
///   newCoordinate = pixels / newDimension
///
/// This is a straight port of <c>ResizeMethods.AdjustFrameToResize</c> /
/// <c>AdjustValue</c> with no dependency on <c>GraphicsDevice</c> or file I/O.
/// </summary>
public static class TextureResizeAdjuster
{
    /// <summary>
    /// Adjusts the UV coordinates of a single frame in-place.
    /// </summary>
    public static void AdjustFrame(
        AnimationFrameSave frame,
        int oldWidth, int oldHeight,
        int newWidth, int newHeight)
    {
        frame.LeftCoordinate   = AdjustValue(frame.LeftCoordinate,   oldWidth,  newWidth);
        frame.RightCoordinate  = AdjustValue(frame.RightCoordinate,  oldWidth,  newWidth);
        frame.TopCoordinate    = AdjustValue(frame.TopCoordinate,    oldHeight, newHeight);
        frame.BottomCoordinate = AdjustValue(frame.BottomCoordinate, oldHeight, newHeight);
    }

    /// <summary>
    /// Adjusts every frame in <paramref name="acls"/> whose
    /// <c>TextureName</c> (resolved against <paramref name="aclsDirectory"/>)
    /// matches <paramref name="absoluteTextureFileName"/>.
    ///
    /// Returns the list of frames that were modified.
    /// </summary>
    public static List<AnimationFrameSave> AdjustAll(
        AnimationChainListSave acls,
        string aclsDirectory,
        string absoluteTextureFileName,
        int oldWidth, int oldHeight,
        int newWidth, int newHeight)
    {
        var adjusted = new List<AnimationFrameSave>();

        // Normalise the reference path the same way the old app did
        absoluteTextureFileName = Standardize(absoluteTextureFileName);

        foreach (var chain in acls.AnimationChains)
        {
            foreach (var frame in chain.Frames)
            {
                var fullFramePath = Standardize(
                    CombinePaths(aclsDirectory, frame.TextureName));

                if (fullFramePath == absoluteTextureFileName)
                {
                    AdjustFrame(frame, oldWidth, oldHeight, newWidth, newHeight);
                    adjusted.Add(frame);
                }
            }
        }

        return adjusted;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static float AdjustValue(float coordinate, int oldDim, int newDim)
    {
        if (newDim == 0) return coordinate; // guard
        int pixels = (int)Math.Round(oldDim * coordinate);
        return (float)pixels / newDim;
    }

    /// <summary>Normalises path separators and lower-cases for comparison.</summary>
    private static string Standardize(string path)
        => path.Replace('\\', '/').ToLowerInvariant().TrimEnd('/');

    private static string CombinePaths(string dir, string file)
    {
        if (string.IsNullOrEmpty(dir))  return file;
        if (string.IsNullOrEmpty(file)) return dir;
        return dir.TrimEnd('/', '\\') + "/" + file.TrimStart('/', '\\');
    }
}
