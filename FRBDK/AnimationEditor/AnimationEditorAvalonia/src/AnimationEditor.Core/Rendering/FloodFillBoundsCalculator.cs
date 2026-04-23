using System;
using System.Collections.Generic;

namespace AnimationEditor.Core.Rendering;

/// <summary>
/// Pure BFS flood-fill that finds the bounding box of contiguous pixels that
/// satisfy a caller-supplied predicate (typically "is opaque").
/// No SkiaSharp or graphics dependency — the caller provides the pixel accessor.
/// </summary>
public static class FloodFillBoundsCalculator
{
    /// <summary>
    /// BFS flood fill starting from (<paramref name="startX"/>, <paramref name="startY"/>).
    /// Visits every pixel reachable from the seed without crossing a pixel where
    /// <paramref name="isOpaque"/> returns <c>false</c>.
    /// </summary>
    /// <param name="startX">Seed column (0-based).</param>
    /// <param name="startY">Seed row (0-based).</param>
    /// <param name="width">Bitmap width in pixels.</param>
    /// <param name="height">Bitmap height in pixels.</param>
    /// <param name="isOpaque">
    ///   Predicate — return <c>true</c> for pixels that belong to the filled region.
    ///   Signature: <c>isOpaque(x, y)</c>.
    /// </param>
    /// <param name="minX">Inclusive left edge of the bounding box.</param>
    /// <param name="minY">Inclusive top edge of the bounding box.</param>
    /// <param name="maxX">Inclusive right edge of the bounding box.</param>
    /// <param name="maxY">Inclusive bottom edge of the bounding box.</param>
    /// <returns>
    ///   <c>true</c> if at least one pixel was flooded (i.e. the seed was opaque
    ///   and in-bounds); <c>false</c> otherwise.
    /// </returns>
    public static bool TryGetBounds(
        int startX, int startY,
        int width,  int height,
        Func<int, int, bool> isOpaque,
        out int minX, out int minY,
        out int maxX, out int maxY)
    {
        minX = int.MaxValue; minY = int.MaxValue;
        maxX = int.MinValue; maxY = int.MinValue;

        if (width <= 0 || height <= 0)                                    return false;
        if (startX < 0 || startX >= width || startY < 0 || startY >= height) return false;
        if (!isOpaque(startX, startY))                                    return false;

        var visited = new bool[width * height];
        var stack   = new Stack<(int x, int y)>(512);
        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            int idx = y * width + x;
            if (visited[idx]) continue;
            if (!isOpaque(x, y)) continue;

            visited[idx] = true;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

            stack.Push((x - 1, y));
            stack.Push((x + 1, y));
            stack.Push((x,     y - 1));
            stack.Push((x,     y + 1));
        }

        return maxX >= minX;
    }
}
