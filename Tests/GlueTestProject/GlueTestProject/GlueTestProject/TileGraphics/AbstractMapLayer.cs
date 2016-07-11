using System;
using System.Xml.Serialization;

namespace TMXGlueLib
{
    [Serializable]
    [XmlInclude(typeof (MapLayer))]
    [XmlInclude(typeof (mapObjectgroup))]
    public abstract class AbstractMapLayer
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}