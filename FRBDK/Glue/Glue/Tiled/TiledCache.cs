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

    public class DatedFile
    {
        public FilePath FilePath;
        public DateTime LastTimeChanged;
    }

    public class CachedTiledMapSave
    {
        public FilePath FilePath;
        public DateTime LastTimeChanged;
        public TiledMapSave TiledMapSave;

        public List<DatedFile> AdditionalFiles = new List<DatedFile>();

        internal bool IsUpToDate()
        {
            var isUpToDate = FilePath.Exists() && System.IO.File.GetLastWriteTime(FilePath.FullPath) <= LastTimeChanged;

            if(isUpToDate)
            {
                foreach(var file in AdditionalFiles)
                {
                    var isAdditionalFileUpToDate =
                        file.FilePath.Exists() && System.IO.File.GetLastWriteTime(file.FilePath.FullPath) <= file.LastTimeChanged;
                    if(!isAdditionalFileUpToDate)
                    {
                        isUpToDate = false;
                    }
                }
            }

            return isUpToDate;
        }
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
                    TiledMapSave tms = null;

                    try
                    {
                        tms = TiledMapSave.FromFile(fileName.FullPath);
                    }
                    catch (Exception e) 
                    {
                        GlueCommands.Self.PrintError($"Could not load TMX file {fileName} because of error: {e.ToString()}");
                    }
                    if(tms != null)
                    {
                        CachedTiledMapSave cachedTiledMapSave = CreateCachedTiledMapSave(fileName, tms);

                        dictionary[fileName] = cachedTiledMapSave;
                    }

                }
            });

            CachedTiledMapSaves.Clear();
            foreach (var kvp in dictionary)
            {
                CachedTiledMapSaves[kvp.Key] = kvp.Value;
            }
        }

        private static CachedTiledMapSave CreateCachedTiledMapSave(FilePath fileName, TiledMapSave tms)
        {
            var cachedTiledMapSave = new CachedTiledMapSave
            {
                LastTimeChanged = DateTime.Now,
                FilePath = fileName,
                TiledMapSave = tms
            };

            foreach (var tileset in tms.Tilesets)
            {
                FilePath fullFile = fileName.GetDirectoryContainingThis() + tileset.Source;
                if (fullFile.Exists())
                {
                    cachedTiledMapSave.AdditionalFiles.Add(new DatedFile
                    {
                        FilePath = fullFile,
                        LastTimeChanged = DateTime.Now,
                    });
                }
            }

            return cachedTiledMapSave;
        }

        public TiledMapSave GetTiledMap(FilePath filePath)
        {
            TiledMapSave tms = null;
            if (CachedTiledMapSaves.ContainsKey(filePath))
            {
                var cached = CachedTiledMapSaves[filePath];

                var date = cached.LastTimeChanged;

                if (cached.IsUpToDate())
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

                    CachedTiledMapSaves[filePath] = CreateCachedTiledMapSave(filePath, tms);
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

        public CroppedBitmap GetBitmapForStandardTilesetId(int tileId, string tileType)
        {
            if(standardTilesetImage == null)
            {
                return null;
            }

            var unwrappedX = tileId * 16;
            var y = 16 * (unwrappedX / (int)standardTilesetImage.PixelWidth);
            var x = unwrappedX % (int)standardTilesetImage.PixelWidth;

            CroppedBitmap croppedBitmap = new CroppedBitmap();

            try
            {
                croppedBitmap.BeginInit();
                croppedBitmap.SourceRect = new Int32Rect(x, y, 16, 16);
                croppedBitmap.Source = standardTilesetImage;
                croppedBitmap.EndInit();
            }
            catch (Exception e)
            {
                throw new Exception($"Error creating cropped bitmap at X,Y {x},{y} for tile ID {tileId} with type {tileType}");
            }


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

        internal void ReactToClosedProject()
        {
            standardTilesetImage = null;
            standardTileset = null;
            StandardTilesetFilePath = null;
            CachedTiledMapSaves?.Clear();
        }
    }




}
