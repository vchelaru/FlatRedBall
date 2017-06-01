using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TMXGlueLib;

namespace TileGraphicsPlugin.DataTypes
{
    public class TiledObjectTypePropertySave : property
    {
        [XmlAttribute("default")]
        public string DefaultAsString { get; set; }

    }
}
