using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    /// <summary>
    /// TMX file creation, which the Tiled Plugin owns. The plugin assigns its implementation to
    /// <see cref="ExportedImplementations.GlueCommands.TiledCommands"/> at startup, so the property is null when
    /// the plugin is not loaded.
    /// </summary>
    public interface ITiledCommands
    {
        /// <summary>Overwrites the TMX file with one of the premade levels that has visuals.</summary>
        /// <param name="tmxFile">The TMX file to overwrite, usually in a Level screen.</param>
        /// <param name="levelName">The premade level, such as "OverworldPlatformerA".</param>
        void SaveTmxWithVisuals(ReferencedFileSave tmxFile, string levelName);

        /// <summary>Writes the standard tileset files to the content folder.</summary>
        void SaveTilesetFilesToDisk();

        /// <summary>Adds the standard tileset to the TMX file. Does nothing when the file is not a TMX.</summary>
        void AddStandardTileset(ReferencedFileSave tmxFile);

        /// <summary>Adds a GameplayLayer to the TMX file. Does nothing when the file is not a TMX.</summary>
        void AddGameplayLayer(ReferencedFileSave tmxFile);

        /// <summary>Adds solid collision tiles around the TMX file's border. Does nothing when the file is not a TMX.</summary>
        void AddCollisionBorder(ReferencedFileSave tmxFile);
    }
}
