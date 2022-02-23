using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace FlatRedBall.Glue.ViewModels
{
    public class TilesetTileSelectorFullViewModel : ViewModel
    {
        public int TileId
        {
            get => Get<int>();
            set => Set(value);
        }

        [DependsOn(nameof(TileId))]
        public string TileType
        {
            get
            {
                return GlueState.Self.TiledCache.StandardTileset.Tiles.FirstOrDefault(item => item.id == TileId)?.Type;
            }
        }

        public int? ExistingId
        {
            get => Get<int?>();
            set => Set(value);
        }

        public string ExistingType =>
            ExistingId != null
            ? GlueState.Self.TiledCache.StandardTileset.Tiles.FirstOrDefault(item => item.id == ExistingId.Value)?.Type
            : null;
        public string TileShapeCollectionName
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(ExistingId))]
        [DependsOn(nameof(ExistingType))]
        [DependsOn(nameof(TileShapeCollectionName))]
        [DependsOn(nameof(TileId))]
        public bool IsNullingOutExisting => ExistingId != null && TileId != ExistingId && ExistingType != null && ExistingType == TileShapeCollectionName;

        [DependsOn(nameof(TileId))]
        [DependsOn(nameof(TileType))]
        [DependsOn(nameof(ExistingId))]
        public bool WillReferenceExistingTile => !string.IsNullOrEmpty(TileType) && TileId != ExistingId;

        [DependsOn(nameof(TileType))]
        public bool WillSetNewTileType => string.IsNullOrEmpty(TileType);

        [DependsOn(nameof(IsNullingOutExisting))]
        [DependsOn(nameof(WillReferenceExistingTile))]
        [DependsOn(nameof(WillSetNewTileType))]
        public string WarningLabelText
        {
            get
            {
                string toReturn = null;
                if (IsNullingOutExisting)
                {
                    toReturn += $"The tile {ExistingType} with ID {ExistingId} will have its type set to <NULL>.\n";
                }

                if(WillReferenceExistingTile)
                {
                    toReturn += $"This object will reference tile ID {TileId} with the name {TileType}\n";
                }
                if (WillSetNewTileType)
                {
                    toReturn += $"The tile with ID {TileId} will be given the type {TileShapeCollectionName}\n";
                }

                return toReturn;
            }
        }

        [DependsOn(nameof(WarningLabelText))]
        public Visibility WarningVisibility => (!string.IsNullOrEmpty(WarningLabelText)).ToVisibility();
    }
}
