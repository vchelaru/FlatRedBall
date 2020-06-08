using System;
using System.Xml.Serialization;

namespace TMXGlueLib
{
#if !UWP
    [Serializable]
#endif
    [XmlInclude(typeof (MapLayer))]
    [XmlInclude(typeof  (MapImageLayer))]
    [XmlInclude(typeof (mapObjectgroup))]
    public abstract class AbstractMapLayer
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        private int? visibleField;
        [XmlAttribute("visible")]
        public int visible
        {
            get
            {
                return this.visibleField.HasValue ? this.visibleField.Value : 1;
            }
            set
            {
                this.visibleField = value;
            }
        }

        public bool IsVisible => visibleField == null || visibleField == 1;
    }
}