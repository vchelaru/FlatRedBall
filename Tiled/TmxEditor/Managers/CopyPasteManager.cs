using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TmxEditor.Controllers;
using TMXGlueLib;

namespace TmxEditor.Managers
{
    public class CopyPasteManager : Singleton<CopyPasteManager>
    {
        public mapTilesetTile CopiedTilesetTile
        {
            get;
            private set;
        }


        public void StoreCopyOf(mapTilesetTile whatToCopy)
        {
            if (whatToCopy != null)
            {
                CopiedTilesetTile = ToolsUtilities.FileManager.CloneSaveObject(whatToCopy);
            }
        }

    }
}
