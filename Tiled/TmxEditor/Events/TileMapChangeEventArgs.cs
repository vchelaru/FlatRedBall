using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TmxEditor.Events
{
    public enum ChangeType
    {
        Tileset,
        Other
    }

    public class TileMapChangeEventArgs : EventArgs
    {
        public ChangeType ChangeType
        {
            get;
            set;
        }
    }
}
