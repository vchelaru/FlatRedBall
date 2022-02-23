using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using TMXGlueLib;

namespace FlatRedBall.Glue.Tiled
{
    #region CachedTiledMapSave
    public class CachedTiledMapSave
    {
        public FilePath FilePath;
        public DateTime LastTimeChanged;
        public TiledMapSave TiledMapSave;
    }
    #endregion

    public class TiledCache
    {
        #region Fields/properties
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
        FilePath StandardTilesetFilePath;

        public IEnumerable<TiledMapSave> AllTiledMaps => CachedTiledMapSaves.Values.Select(item => item.TiledMapSave);
        public IEnumerable<CachedTiledMapSave> AllCachedTiledMapSaveInstances => CachedTiledMapSaves.Values;


        #endregion

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
                    StandardTilesetFilePath = tmxFilePath.GetDirectoryContainingThis() + standardTileset.Source;

                    if (standardTileset != null)
                    {
                        var source = standardTileset.Images.FirstOrDefault()?.Source;

                        pngFilePath = StandardTilesetFilePath.GetDirectoryContainingThis() + source;
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

        public CroppedBitmap GetBitmapForStandardTilesetId(int tileId)
        {
            if(standardTilesetImage == null)
            {
                return null;
            }

            var unwrappedX = tileId * 16;
            var y = 16 * (unwrappedX / (int)standardTilesetImage.Width);
            var x = unwrappedX % (int)standardTilesetImage.Width;

            CroppedBitmap croppedBitmap = new CroppedBitmap();
            croppedBitmap.BeginInit();
            croppedBitmap.SourceRect = new Int32Rect(x, y, 16, 16);
            croppedBitmap.Source = standardTilesetImage;
            croppedBitmap.EndInit();

            return croppedBitmap;
        }

        public int? GetTileIdFromType(string type)
        {
            if(standardTileset == null)
            {
                return null;
            }

            return standardTileset.Tiles.FirstOrDefault(item => item.Type == type)?.id;
        }

        public void SaveStandardTileset()
        {
            if (StandardTilesetFilePath == null)
            {
                return;
            }

            // Not sure why this has a source. If it does, the XML won't serialize properly so null it out
            StandardTileset.Source = null;

            try
            {
                FileManager.XmlSerialize(StandardTileset, out string serialized);

                // Here we're serializing a Tileset (capitalized) but we want to save it as a lower-case tileset which
                // is the actual matching class. One way to do this would be to convert one type to another, but that's
                // dangerous if we forget one property especially as we add more properties. Instead, we can just modify the 
                // xml manually:
                serialized = serialized
                    .Replace("<mapTileset ", "<tileset ")
                    .Replace("</mapTileset>", "</tileset>")
                    .Replace("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", "");

                FileWatchManager.IgnoreNextChangeOnFile(StandardTilesetFilePath);
                // save it
                FileManager.SaveText(serialized, StandardTilesetFilePath.FullPath);
            }
            catch(Exception ex)
            {
                GlueCommands.Self.PrintError(ex.ToString());
            }
        }

    }




}
