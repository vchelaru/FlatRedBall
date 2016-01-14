using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileGraphicsPlugin
{
    /// <summary>
    /// This represents the class that will be created by Glue
    /// and into which CSVs will be deserialized.  
    /// </summary>
    public class TileMapInfo
    {
        public string Name { get; set;}
        public bool HasCollision { get; set;}
        public string EntityType { get; set; }
        public List<TMXGlueLib.TileAnimationFrame> EmbeddedAnimation { get; set; }
    }
}
