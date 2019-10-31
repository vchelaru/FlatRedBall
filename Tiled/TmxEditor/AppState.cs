using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TmxEditor.Controllers;
using TmxEditor.GraphicalDisplay.Tilesets;
using TMXGlueLib;

namespace TmxEditor
{
    public class AppState : Singleton<AppState>
    {
        public string TmxFileName
        {
            get { return ProjectManager.Self.LastLoadedFile; }
        }

        public string TmxFolder
        {
            get
            {
                if (string.IsNullOrEmpty(TmxFileName))
                {
                    return null;
                }
                else
                {
                    return FileManager.GetDirectory(TmxFileName);
                }
            }
        }

        public MapLayer CurrentMapLayer
        {
            get
            {
                return LayersController.Self.CurrentMapLayer;
            }
        }

        public property CurrentLayerProperty
        {
            get
            {
                return LayersController.Self.CurrentLayerProperty;
            }
        }

        public property CurrentTilesetTileProperty
        {
            get
            {
                return TilesetController.Self.CurrentTilesetTileProperty;
            }
        }

        public Tileset CurrentTileset
        {
            get
            {
                return TilesetController.Self.CurrentTileset;
            }
        }

        public mapTilesetTile CurrentMapTilesetTile
        {
            get
            {
                return TilesetController.Self.CurrentTilesetTile;
            }
        }

        public TiledMapSave CurrentTiledMapSave
        {
            get
            {
                return ProjectManager.Self.TiledMapSave;
            }
        }


        public ProvidedContext ProvidedContext
        {
            get;
            set;

        }

        public AppState()
        {

            ProvidedContext = new ProvidedContext();

        }


    }
}
