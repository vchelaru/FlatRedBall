using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueTestProject.CustomDataTypes
{
    public enum CustomEnumType
    {
        Enum1,
        Enum2
    }


    public class CustomSubtype
    {
        public CustomEnumType CustomEnumType { get; private set; }
        public int IntValue { get; private set; }


    }

    public class CustomDataType
    {
        public string Name;

        public float X;
        public float Y;
        public float Z;

        public string StringProperty
        {
            get;
            set;
        }

        public List<string> StringListField = new List<string>();
        public List<string> StringListProperty
        {
            get;
            set;
        }

        public List<CustomSubtype> ComplexCustomTypeListField;

        public List<CustomSubtype> ComplexCustomTypeListProperty
        {
            get;
            set;
        }

    }
}
