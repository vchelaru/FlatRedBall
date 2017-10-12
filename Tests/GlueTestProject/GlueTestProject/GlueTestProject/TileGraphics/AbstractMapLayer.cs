using System;
using System.Xml.Serialization;

namespace TMXGlueLib
{
#if !UWP
    [Serializable]
#endif
    [XmlInclude(typeof (MapLayer))]
    [XmlInclude(typeof (mapObjectgroup))]
    public abstract class AbstractMapLayer
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}