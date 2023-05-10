using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace TMXGlueLib
{
#if !UWP
    [Serializable]
#endif
    public partial class MapLayer : AbstractMapLayer
    {
        #region Fields

        private IDictionary<string, string> propertyDictionaryField = null;


        List<property> mProperties = new List<property>();

        public List<property> properties
        {
            get { return mProperties; }
            set
            {
                mProperties = value;
            }
        }


        private mapLayerData[] dataField;

        private string nameField;

        private int widthField;

        private int heightField;
        #endregion

        [XmlIgnore]
        public IDictionary<string, string> PropertyDictionary
        {
            get
            {
                lock (this)
                {
                    if (propertyDictionaryField == null)
                    {
                        propertyDictionaryField = TiledMapSave.BuildPropertyDictionaryConcurrently(properties);
                    }
                    if (!propertyDictionaryField.Any(p => p.Key.Equals("name", StringComparison.OrdinalIgnoreCase)))
                    {
                        propertyDictionaryField.Add("name", this.Name);
                    }
                    return propertyDictionaryField;
                }
            }
        }

        /// <remarks/>
        [XmlElement("data", Form = System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable = true)]
        public mapLayerData[] data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
                if (dataField != null)
                {
                    foreach (mapLayerData layerData in dataField)
                    {
                        layerData.length = width * height;
                    }
                }
            }
        }

        /// <remarks/>
        [XmlAttribute("width")]
        public int width
        {
            get
            {
                return this.widthField;
            }
            set
            {
                this.widthField = value;
                if (this.data != null)
                {
                    foreach (mapLayerData layerData in data)
                    {
                        layerData.length = width * height;
                    }
                }
            }
        }

        /// <remarks/>
        [XmlAttribute("height")]
        public int height
        {
            get
            {
                return this.heightField;
            }
            set
            {
                this.heightField = value;
                if (this.data != null)
                {
                    foreach (mapLayerData layerData in data)
                    {
                        layerData.length = width * height;
                    }
                }
            }
        }
        
        [XmlIgnore]
        public TiledMapSave.LayerVisibleBehavior VisibleBehavior = TiledMapSave.LayerVisibleBehavior.Ignore;

        public override string ToString()
        {
            return Name;
        }

    }
}
