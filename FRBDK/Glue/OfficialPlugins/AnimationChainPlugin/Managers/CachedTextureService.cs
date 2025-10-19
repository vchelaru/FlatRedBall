using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.IO;
using SkiaSharp;

namespace OfficialPlugins.AnimationChainPlugin.Managers;

public class CachedTexture
{
    public SKBitmap Texture { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CachedTextureService
{
    public Dictionary<FilePath, CachedTexture> CachedTextures { get; private set; } = new();

    public void RefreshCacheFor(FilePath filePath)
    {
        var shouldRefresh = CachedTextures.ContainsKey(filePath) == false ||
            System.IO.File.GetLastWriteTime(filePath.FullPath) > CachedTextures[filePath].LastUpdated;

        if(shouldRefresh)
        {
            if(filePath.IsDirectory == false && filePath.Exists())
            {
                try
                {
                    using var stream = System.IO.File.OpenRead(filePath.FullPath);

                    var bitmap = SKBitmap.Decode(stream);
                    CachedTextures[filePath] = new CachedTexture
                    {
                        Texture = bitmap,
                        LastUpdated = DateTime.Now
                    };
                }
                catch (Exception e)
                {
                    // for debugging:
                    throw;
                }
            }
            else if(CachedTextures.ContainsKey(filePath))
            {
                CachedTextures.Remove(filePath);
            }
        }
    }

    public SKBitmap? TryGetTexture(FilePath filePath)
    {
        if(CachedTextures.ContainsKey(filePath))
        {
            return CachedTextures[filePath].Texture;
        }
        return null;
    }
}
