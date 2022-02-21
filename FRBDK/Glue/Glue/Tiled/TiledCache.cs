using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TMXGlueLib;

namespace FlatRedBall.Glue.Tiled
{

    public class CachedTiledMapSave
    {
        public FilePath FilePath;
        public DateTime LastTimeChanged;
        public TiledMapSave TiledMapSave;
    }

    public class TiledCache
    {
        Dictionary<FilePath, CachedTiledMapSave> CachedTiledMapSaves = new Dictionary<FilePath, CachedTiledMapSave>();

        BitmapImage standardTilesetImage;
        public BitmapImage StandardTilesetImage 
        { 
            get
            {
                if(standardTilesetImage == null)
                {
                    FindStandardTileset();
                }
                return standardTilesetImage;
            }
        }
        Tileset standardTileset;
        public Tileset StandardTileset
        {
            get
            {
                if(standardTileset == null)
                {
                    FindStandardTileset();
                }
                return standardTileset;
            }
        }

        public void RefreshCache()
        {
            List<FilePath> tmxFiles = new List<FilePath>();

            var allRfses = ObjectFinder.Self.GetAllReferencedFiles().ToArray();

            foreach (var rfs in allRfses)
            {
                if (rfs.Name.ToLowerInvariant().EndsWith(".tmx"))
                {
                    var fileName = GlueCommands.Self.GetAbsoluteFilePath(rfs);
                    tmxFiles.Add(fileName);
                }
            }

            ConcurrentDictionary<FilePath, CachedTiledMapSave> dictionary = new System.Collections.Concurrent.ConcurrentDictionary<FilePath, CachedTiledMapSave>();

            foreach (var item in CachedTiledMapSaves)
            {
                dictionary[item.Key] = item.Value;
            }

            Parallel.ForEach(tmxFiles, fileName =>
            {
                if (!CachedTiledMapSaves.ContainsKey(fileName) && fileName.Exists())
                {
                    var tms = TiledMapSave.FromFile(fileName.FullPath);

                    var cachedTiledMapSave = new CachedTiledMapSave
                    {
                        LastTimeChanged = DateTime.Now,
                        FilePath = fileName,
                        TiledMapSave = tms
                    };

                    dictionary[fileName] = cachedTiledMapSave;

                }
            });

            CachedTiledMapSaves.Clear();
            foreach (var kvp in dictionary)
            {
                CachedTiledMapSaves[kvp.Key] = kvp.Value;
            }
        }


        public TiledMapSave GetTiledMap(FilePath filePath)
        {
            TiledMapSave tms = null;
            if (CachedTiledMapSaves.ContainsKey(filePath))
            {
                var cached = CachedTiledMapSaves[filePath];

                var date = cached.LastTimeChanged;

                if (filePath.Exists() && System.IO.File.GetLastWriteTime(filePath.FullPath) <= cached.LastTimeChanged)
                {
                    tms = cached.TiledMapSave;
                }
            }

            // unlikely, but possible that the file changed since it was cached so we re-check here.
            if (tms == null)
            {
                if (filePath.Exists())
                {
                    tms = TiledMapSave.FromFile(filePath.FullPath);

                    CachedTiledMapSaves[filePath] = new CachedTiledMapSave
                    {
                        LastTimeChanged = DateTime.Now,
                        FilePath = filePath,
                        TiledMapSave = tms
                    };
                }
            }

            return tms;
        }

        public IEnumerable<TiledMapSave> AllTiledMaps => CachedTiledMapSaves.Values.Select(item => item.TiledMapSave);
        public IEnumerable<CachedTiledMapSave> AllCachedTiledMapSaveInstances => CachedTiledMapSaves.Values;

        public void FindStandardTileset()
        {
            TiledMapSave foundTiledMapSave = null;
            FilePath tmxFilePath = null;
            // find one with a GameplayLayerb
            foreach (var cachedMap in GlueState.Self.TiledCache.AllCachedTiledMapSaveInstances)
            {
                var gameplayLayer = cachedMap.TiledMapSave.Layers.FirstOrDefault(item => item.Name?.ToLowerInvariant() == "gameplaylayer");

                if (gameplayLayer != null)
                {
                    foundTiledMapSave = cachedMap.TiledMapSave;
                    tmxFilePath = cachedMap.FilePath;
                    break;
                }
            }

            FilePath pngFilePath = null;
            
            if (foundTiledMapSave != null)
            {
                standardTileset = foundTiledMapSave.Tilesets.FirstOrDefault(item => item.Name == "TiledIcons");
                if (standardTileset != null)
                {
                    FilePath tsxFilePath = tmxFilePath.GetDirectoryContainingThis() + standardTileset.Source;

                    if (standardTileset != null)
                    {
                        var source = standardTileset.Images.FirstOrDefault()?.Source;

                        pngFilePath = tsxFilePath.GetDirectoryContainingThis() + source;
                    }
                }
            }

            if (pngFilePath?.Exists() == true)
            {
                standardTilesetImage = new BitmapImage();
                standardTilesetImage.BeginInit();
                standardTilesetImage.CacheOption = BitmapCacheOption.OnLoad;
                standardTilesetImage.UriSource = new Uri(pngFilePath.FullPath, UriKind.Relative);
                standardTilesetImage.EndInit();

            }
        }
    }

}
