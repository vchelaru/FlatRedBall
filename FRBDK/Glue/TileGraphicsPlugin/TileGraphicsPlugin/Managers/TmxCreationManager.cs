using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiledPlugin.ViewModels;
using TileGraphicsPlugin;
using TMXGlueLib;

namespace TiledPlugin.Managers
{
    public class TmxCreationManager : Singleton<TmxCreationManager>
    {
        internal void HandleNewTmx(ReferencedFileSave newFile)
        {
            var creationOptions = newFile.GetProperty<string>("CreationOptions");

            if (!string.IsNullOrWhiteSpace(creationOptions))
            {
                var viewModel = JsonConvert.DeserializeObject<NewTmxViewModel>(creationOptions);

                if (viewModel != null)
                {
                    HandleNewTmx(newFile, viewModel);

                }
            }
        }

        private void HandleNewTmx(ReferencedFileSave newFile, NewTmxViewModel viewModel)
        {
            if (viewModel.WithVisualType == WithVisualType.WithVisuals)
            {
                HandleNewTmxWithVisuals(newFile, viewModel);
            }
            else
            {
                if (viewModel.IncludeDefaultTileset)
                {
                    IncludeDefaultTilesetOn(newFile);
                }
                if (viewModel.IncludeGameplayLayer)
                {
                    IncludeGameplayLayerOn(newFile);
                }
                if (viewModel.ShouldAddCollisionBorder)
                {
                    AddCollisionBorderOn(newFile);
                }
            }
        }

        enum TilesetType
        {
            FrbVisualTiles,
            Zoria
        }

        private void HandleNewTmxWithVisuals(ReferencedFileSave newFile, NewTmxViewModel viewModel)
        {
            var selectedLevel = viewModel.SelectedLevel;

            string levelName = null;

            switch (selectedLevel)
            {
                case TmxLevels.OverworldPlatformerA:
                    levelName = "OverworldPlatformerA";
                    break;
                case TmxLevels.OverworldPlatformerB:
                    levelName = "OverworldPlatformerB";
                    break;
                case TmxLevels.OverworldPlatformerC:
                    levelName = "OverworldPlatformerC";
                    break;
                case TmxLevels.OverworldTopDownA:
                    levelName = "OverworldTopDownA";
                    break;
                case TmxLevels.OverworldTopDownB:
                    levelName = "OverworldTopDownB";
                    break;
                case TmxLevels.OverworldTopDownC:
                    levelName = "OverworldTopDownC";
                    break;
            }

            if (string.IsNullOrEmpty(levelName))
            {
                throw new NotImplementedException();
            }


            SaveTmxWithVisuals(newFile, levelName);
        }



        public void SaveTmxWithVisuals(ReferencedFileSave newFile, string levelName)
        {
            var newFilePath = GlueCommands.Self.GetAbsoluteFilePath(newFile);

            TilesetType tilesetType = TilesetType.FrbVisualTiles;
            if (levelName == nameof(TmxLevels.OverworldTopDownA) ||
                levelName == nameof(TmxLevels.OverworldTopDownB) ||
                levelName == nameof(TmxLevels.OverworldTopDownC))
            {
                tilesetType = TilesetType.Zoria;
            }

            void SaveIfNotExist(string resourceName, FilePath targetFile)
            {
                if (!targetFile.Exists())
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        FileManager.SaveEmbeddedResource(
                            this.GetType().Assembly,
                            resourceName,
                            targetFile.FullPath);
                    });
                }
            }

            FilePath targetVisualTsxFile = null;
            if (tilesetType == TilesetType.FrbVisualTiles)
            {
                var visualTsxResourceName = "TiledPlugin.Content.Tilesets.FrbVisualTiles.tsx";
                targetVisualTsxFile = new FilePath(GlueState.Self.ContentDirectory + "FrbVisualTiles.tsx");
                SaveIfNotExist(visualTsxResourceName, targetVisualTsxFile);

                var visualPngResourceName = "TiledPlugin.Content.Tilesets.FrbVisualTiles.png";
                var targetVisualPngFile = new FilePath(GlueState.Self.ContentDirectory + "FrbVisualTiles.png");
                SaveIfNotExist(visualPngResourceName, targetVisualPngFile);
            }
            else if (tilesetType == TilesetType.Zoria)
            {
                var visualTsxResourceName = "TiledPlugin.Content.Tilesets.ZoriaOverworld.tsx";
                targetVisualTsxFile = new FilePath(GlueState.Self.ContentDirectory + "ZoriaOverworld.tsx");
                SaveIfNotExist(visualTsxResourceName, targetVisualTsxFile);

                var visualPngResourceName = "TiledPlugin.Content.Tilesets.ZoriaOverworld.png";
                var targetVisualPngFile = new FilePath(GlueState.Self.ContentDirectory + "ZoriaOverworld.png");
                SaveIfNotExist(visualPngResourceName, targetVisualPngFile);
            }
            var standardTsxResourceName = "TiledPlugin.Content.Tilesets.StandardTileset.tsx";
            var targetStandardTsxFile = new FilePath(GlueState.Self.ContentDirectory + "StandardTileset.tsx");
            SaveIfNotExist(standardTsxResourceName, targetStandardTsxFile);

            var startTilesetPngResourceName = "TiledPlugin.Content.Tilesets.StandardTilesetIcons.png";
            var targetStartTilesetPngFile = new FilePath(GlueState.Self.ContentDirectory + "StandardTilesetIcons.png");
            SaveIfNotExist(startTilesetPngResourceName, targetStartTilesetPngFile);


            var wasLoadingSource = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            {
                var tmxResourceName = "TiledPlugin.Content.Levels." + levelName + ".tmx";
                var byteArray = FileManager.GetByteArrayFromEmbeddedResource(this.GetType().Assembly, tmxResourceName);
                var tmxString = Encoding.UTF8.GetString(byteArray);
                //TiledMapSave.FromFile(byteArrayString);
                var tiledMapSave = FileManager.XmlDeserializeFromString<TiledMapSave>(tmxString);


                var newVisualTsxRelative = targetVisualTsxFile.RelativeTo(newFilePath.GetDirectoryContainingThis());
                var newStandardTsxRelative = targetStandardTsxFile.RelativeTo(newFilePath.GetDirectoryContainingThis());

                foreach (var tileset in tiledMapSave.Tilesets)
                {
                    if (tileset.Source == "../Tilesets/FrbVisualTiles.tsx" ||
                        tileset.Source == "../Tilesets/ZoriaOverworld.tsx")
                    {
                        tileset.Source = newVisualTsxRelative;
                    }

                    else if (tileset.Source == "../Tilesets/StandardTileset.tsx")
                    {
                        tileset.Source = newStandardTsxRelative;
                    }
                }

                tiledMapSave.Save(newFilePath.FullPath);
            }
            Tileset.ShouldLoadValuesFromSource = wasLoadingSource;
        }

        public void IncludeGameplayLayerOn(ReferencedFileSave newFile)
        {
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            var fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));
            var tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);



            var layer = new MapLayer();
            layer.Name = "GameplayLayer";

            var existingLayer = tileMapSave.Layers.First();
            layer.width = existingLayer.width;
            layer.height = existingLayer.height;
            layer.data = existingLayer.data;

            // remove existing layers, replace it with this layer so the user doesn't accidentally place tiles on the wrong layer
            tileMapSave.MapLayers.Clear();
            tileMapSave.MapLayers.Add(layer);

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;
        }

        public void IncludeDefaultTilesetOn(ReferencedFileSave newFile)
        {
            FilePath fullTmxFile, existingDefaultTilesetFile;
            TiledMapSave tileMapSave;
            SaveTilesetFilesToDisk(out existingDefaultTilesetFile);
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;
            fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));

            tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);

            var standardTileset = new Tileset();

            var tmxDirectory = fullTmxFile.GetDirectoryContainingThis();
            standardTileset.Source = FileManager.MakeRelative(existingDefaultTilesetFile.FullPath, tmxDirectory.FullPath);


            tileMapSave.Tilesets.Add(standardTileset);

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;

        }

        public void SaveTilesetFilesToDisk()
        {
            SaveTilesetFilesToDisk(out FilePath _);
        }

        private void SaveTilesetFilesToDisk(out FilePath existingDefaultTilesetFile)
        {
            existingDefaultTilesetFile = null;
            if (existingDefaultTilesetFile == null)
            {
                var folder = new FilePath(
                    GlueState.Self.ContentDirectory);

                var assembly =
                    this.GetType().Assembly;

                var destinationTsx = new FilePath(folder + "StandardTileset.tsx");
                var destinationPng = new FilePath(folder + "StandardTilesetIcons.png");

                if (destinationTsx.Exists() == false)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        // save the tsx
                        FileManager.SaveEmbeddedResource(
                            assembly,
                            "TiledPlugin.Content.Tilesets.StandardTileset.tsx",
                            destinationTsx.FullPath
                            );

                    });
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationTsx}, file already exists.");
                }

                existingDefaultTilesetFile = destinationTsx;

                if (destinationPng.Exists() == false)
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        // save the png
                        FileManager.SaveEmbeddedResource(
                        assembly,
                        "TiledPlugin.Content.Tilesets.StandardTilesetIcons.png",
                        destinationPng.FullPath);
                    });
                }
                else
                {
                    GlueCommands.Self.PrintOutput($"Did not save {destinationPng}, file already exists.");
                }
            }
        }

        public void AddCollisionBorderOn(ReferencedFileSave newFile)
        {
            var old = Tileset.ShouldLoadValuesFromSource;
            Tileset.ShouldLoadValuesFromSource = false;

            var fullTmxFile = new FilePath(GlueCommands.Self.FileCommands.GetFullFileName(newFile));
            var tileMapSave = TMXGlueLib.TiledMapSave.FromFile(fullTmxFile.FullPath);

            var tiles = tileMapSave.Layers[0].data[0].tiles;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    if (x == 0 || y == 0 || x == 31 || y == 31)
                    {
                        var absoluteValue = x + 32 * y;

                        tiles[absoluteValue] = 1;
                    }
                }
            }

            tileMapSave.Layers[0].data[0].SetTileData(tiles, "base64", "gzip");

            tileMapSave.Tilesets[0].Firstgid = 1;

            tileMapSave.Save(fullTmxFile.FullPath);
            Tileset.ShouldLoadValuesFromSource = old;
        }
    }
}
