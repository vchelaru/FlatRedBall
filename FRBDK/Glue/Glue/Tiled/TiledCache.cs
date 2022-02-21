using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

    }

}
