using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMXGlueLib;
using FlatRedBall.IO;

namespace TmxEditor
{
    public class ProjectManager
    {
        #region Fields

        static ProjectManager mSelf;

        #endregion

        #region Properties

        public static ProjectManager Self
        {
            get { return mSelf ?? (mSelf = new ProjectManager()); }
        }

        public TiledMapSave TiledMapSave { get; private set; }

        public string LastLoadedFile
        {
            get;
            private set;
        }
        #endregion

        public string MakeAbsolute(string fileName)
        {
            return FileManager.GetDirectory(LastLoadedFile) + fileName;
        }

        public void LoadTiledMapSave(string fileName)
        {
            LastLoadedFile = fileName;
            TiledMapSave = TiledMapSave.FromFile(fileName);


        }

        public void SaveTiledMapSave(bool saveTsxFiles = true)
        {
            string fileName = this.LastLoadedFile;

            SaveTiledMapSave(fileName, saveTsxFiles);
        }

        public void SaveTiledMapSave(string fileName, bool saveTsxFiles = true)
        {
            if (TiledMapSave == null)
            {
                System.Windows.Forms.MessageBox.Show("No tile map loaded");
            }
            else
            {
                string directoryToSave = FileManager.GetDirectory(fileName);

                if (saveTsxFiles)
                {

                    bool oldLoadFromSource = Tileset.ShouldLoadValuesFromSource;
                    Tileset.ShouldLoadValuesFromSource = false;

                    // First let's save out the shared map (if necessary)
                    foreach (var tileset in TiledMapSave.Tilesets.Where(item => !string.IsNullOrEmpty(item.Source)))
                    {

                        string absoluteTilesetFilename = directoryToSave + tileset.Source;
                        string oldSource = tileset.Source;
                        tileset.Source = null;


                        // We're going to clone the tileset and remove the "source" property, because tilesets
                        // in .tsx files shouldn't have sources:
                        var forSaving = CloneAndCast(tileset);


                        FileManager.XmlSerialize(forSaving, absoluteTilesetFilename);

                        tileset.Source = oldSource;

                    }

                    Tileset.ShouldLoadValuesFromSource = oldLoadFromSource;
                }

                TiledMapSave.Save(fileName);
            }
        }

        private ExternalTileSet CloneAndCast(Tileset tileset)
        {
            string container;

            FileManager.XmlSerialize(tileset, out container);

            container = container.Replace("<mapTileset", "<tileset");
            container = container.Replace(@"</mapTileset", "</tileset");

            return FileManager.XmlDeserializeFromString<ExternalTileSet>(container);
        }

        internal void LoadTilesetFrom(string fileName, out string output)
        {
            TiledMapSave toCopyFrom = TiledMapSave.FromFile(fileName);

            var stringBuilder = new StringBuilder();


            foreach (var tileset in toCopyFrom.Tilesets)
            {
                var copyTo = GetTilesetByName(TiledMapSave, tileset.Name);

                if (copyTo != null)
                {
                    copyTo.RefreshTileDictionary();

                    stringBuilder.AppendLine("Modified " + tileset.Name + " count before: " + copyTo.Tiles.Count + ", count after: " + tileset.Tiles.Count);

                    copyTo.Tiles = tileset.Tiles;

                }
            }

            output = stringBuilder.ToString();
        }


        internal Tileset GetTilesetByName(TiledMapSave tms, string name)
        {
            return tms.Tilesets.FirstOrDefault(tileset => tileset != null && tileset.Name == name);
        }
    }
}
