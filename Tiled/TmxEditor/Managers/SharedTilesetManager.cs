using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TmxEditor.Controllers;
using TMXGlueLib;

namespace TmxEditor.Managers
{
    public class SharedTilesetManager : Singleton<SharedTilesetManager>
    {
        public void ConvertToSharedTileset(TiledMapSave tiledMap, string tsxDestinationFolder)
        {
            if(string.IsNullOrEmpty(tiledMap.FileName))
            {
                throw new Exception("The TiledMapSave argument must have a non-null FileName property.");
            }
            string tiledMapDirectory = FileManager.GetDirectory( tiledMap.FileName);

            foreach(var tileset in tiledMap.Tilesets)
            {
                ConvertToSharedTileset(tileset, tiledMap, tsxDestinationFolder);
            }
        }

        public static void ConvertToSharedTileset(Tileset tileset, TiledMapSave tiledMap, string tsxDestinationFolder,
            bool saveTileset = true)
        {
            string tiledMapDirectory = FileManager.GetDirectory(tiledMap.FileName);

            // We're changing the type from Tileset to ExportedTileSet so we have to 
            // serialize to a string first to do a clone
            string serializedString;
            FileManager.XmlSerialize(tileset, out serializedString);
            // Set the type to:
            // tileset
            serializedString = serializedString.Replace("<mapTileset ", "<tileset ");
            serializedString = serializedString.Replace("</mapTileset>", "</tileset>");
            var toSave = FileManager.XmlDeserializeFromString<ExternalTileSet>(serializedString);

            // Now let's modify this
            // We don't use the first gid on saved tsx files, they are set in the tmx itself.
            // FRB systems will ignore this value so no need to set it or do anything:
            //toSave.Firstgid = 0;

            foreach (var image in toSave.Images)
            {
                string imageFullPath = FileManager.RemoveDotDotSlash(tiledMapDirectory + image.Source);

                string relativeToDestination = FileManager.MakeRelative(imageFullPath, tsxDestinationFolder);

                image.Source = relativeToDestination;
            }

            string tsxFileNameAbsolute = tsxDestinationFolder + tileset.Name + ".tsx";

            if (saveTileset)
            {
                FileManager.XmlSerialize(toSave, tsxFileNameAbsolute);
            }

            string tsxFileNameRelativeToTmx = FileManager.MakeRelative(tsxFileNameAbsolute, tiledMapDirectory);

            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = tiledMapDirectory;

            tileset.Source = tsxFileNameRelativeToTmx;

            FileManager.RelativeDirectory = oldRelativeDirectory;

        }
    }
}
