using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace TiledPlugin.Managers;

/// <summary>The Tiled Plugin's <see cref="ITiledCommands"/>.</summary>
internal class TiledCommands : ITiledCommands
{
    public void SaveTmxWithVisuals(ReferencedFileSave tmxFile, string levelName) =>
        TmxCreationManager.Self.SaveTmxWithVisuals(tmxFile, levelName);

    public void SaveTilesetFilesToDisk() => TmxCreationManager.Self.SaveTilesetFilesToDisk();

    public void AddStandardTileset(ReferencedFileSave tmxFile)
    {
        if (IsTmx(tmxFile))
        {
            TmxCreationManager.Self.IncludeDefaultTilesetOn(tmxFile);
        }
    }

    public void AddGameplayLayer(ReferencedFileSave tmxFile)
    {
        if (IsTmx(tmxFile))
        {
            TmxCreationManager.Self.IncludeGameplayLayerOn(tmxFile);
        }
    }

    public void AddCollisionBorder(ReferencedFileSave tmxFile)
    {
        if (IsTmx(tmxFile))
        {
            TmxCreationManager.Self.AddCollisionBorderOn(tmxFile);
        }
    }

    static bool IsTmx(ReferencedFileSave file) => file?.GetAssetTypeInfo()?.Extension == "tmx";
}
