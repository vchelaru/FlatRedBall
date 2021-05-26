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
        private float _offsetX;
        private float _offsetY;

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

        private float? parallaxxField;
        [XmlAttribute("parallaxx")]
        public float ParallaxX
        {
            get
            {
                return this.parallaxxField.HasValue ? this.parallaxxField.Value : 1f;
            }
            set
            {
                this.parallaxxField = value;
            }
        }

        private float? parallaxyField;
        [XmlAttribute("parallaxy")]
        public float ParallaxY
        {
            get
            {
                return this.parallaxyField.HasValue ? this.parallaxyField.Value : 1f;
            }
            set
            {
                this.parallaxyField = value;
            }
        }

        [XmlAttribute("offsetx")]
        public float OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        [XmlAttribute("offsety")]
        public float OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; }
        }
    }
}