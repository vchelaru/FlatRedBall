using System.Xml.Serialization;

namespace AtlasPlugin
{
    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStruct
    {
        private object[] itemsField;
        private ItemsChoiceType2[] itemsElementNameField;
        private string typeField;

        [XmlElement("QSize", typeof(dataStructQSize))]
        [XmlElement("array", typeof(dataStructArray))]
        [XmlElement("enum", typeof(dataStructEnum))]
        [XmlElement("false", typeof(object))]
        [XmlElement("filename", typeof(string))]
        [XmlElement("int", typeof(int))]
        [XmlElement("key", typeof(string))]
        [XmlElement("map", typeof(dataStructMap))]
        [XmlElement("string", typeof(string))]
        [XmlElement("struct", typeof(dataStructStruct))]
        [XmlElement("true", typeof(object))]
        [XmlElement("uint", typeof(int))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore()]
        public ItemsChoiceType2[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructQSize
    {
        private object[] itemsField;

        [XmlElement("int", typeof(int))]
        [XmlElement("key", typeof(string))]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructArray
    {
        private string filenameField;
        private dataStructArrayStruct[] structField;

        public string filename
        {
            get { return filenameField; }
            set { filenameField = value; }
        }

        [XmlElement("struct")]
        public dataStructArrayStruct[] @struct
        {
            get { return structField; }
            set { structField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructArrayStruct
    {
        private object[] itemsField;
        private ItemsChoiceType[] itemsElementNameField;
        private string typeField;

        [XmlElement("QSize", typeof(dataStructQSize))]
        [XmlElement("double", typeof(byte))]
        [XmlElement("false", typeof(object))]
        [XmlElement("key", typeof(string))]
        [XmlElement("string", typeof(string))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore()]
        public ItemsChoiceType[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructEnum
    {
        private string typeField;
        private string valueField;

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }

        [XmlText()]
        public string Value
        {
            get { return valueField; }
            set { valueField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructMap
    {
        private object[] itemsField;
        private string typeField;


        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("key", typeof(string))]
        [System.Xml.Serialization.XmlElementAttribute("struct", typeof(dataStructMapStruct))]
        public object[] items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        /// <remarks/>
        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructMapStruct
    {
        private string keyField;
        private string filenameField;
        private string typeField;

        public string key
        {
            get { return keyField; }
            set { keyField = value; }
        }

        public string filename
        {
            get { return filenameField; }
            set { filenameField = value; }
        }

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructStruct
    {
        private object[] itemsField;
        private ItemsChoiceType1[] itemsElementNameField;
        private string typeField;

        [XmlElement("double", typeof(byte))]
        [XmlElement("enum", typeof(dataStructEnum))]
        [XmlElement("false", typeof(object))]
        [XmlElement("int", typeof(int))]
        [XmlElement("key", typeof(string))]
        [XmlElement("string", typeof(string))]
        [XmlElement("struct", typeof(dataStructStructStruct))]
        [XmlElement("uint", typeof(int))]
        [XmlChoiceIdentifier("ItemsElementName")]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlElement("ItemsElementName")]
        [XmlIgnore()]
        public ItemsChoiceType1[] ItemsElementName
        {
            get { return itemsElementNameField; }
            set { itemsElementNameField = value; }
        }

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }

    [System.Serializable()]
    [XmlType(AnonymousType = true)]
    public class dataStructStructStruct
    {
        private object[] itemsField;
        private string typeField;

        [XmlElement("enum", typeof(dataStructEnum))]
        [XmlElement("key", typeof(string))]
        public object[] Items
        {
            get { return itemsField; }
            set { itemsField = value; }
        }

        [XmlAttribute()]
        public string type
        {
            get { return typeField; }
            set { typeField = value; }
        }
    }
}