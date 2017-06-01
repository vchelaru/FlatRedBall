using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TileGraphicsPlugin.DataTypes
{
    [XmlRoot("objecttype")]
    public class TiledObjectTypeSave
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("color")]
        public string Color { get; set; }

        [XmlElement("property")]
        public List<TiledObjectTypePropertySave> Properties { get; set; } = new List<TiledObjectTypePropertySave>();
    }
}
