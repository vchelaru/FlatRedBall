#if XBOX

using System;

namespace System.Runtime.Serialization
{
    /*
     * This exists only for compilation support on XBox. These attributes are used in
     * WP7 tombstoning, so to avoid conditional compilation statements around the 
     * attribute declarations
     */

    public class DataContractAttribute : Attribute
    {
    }

    public class DataMemberAttribute : Attribute
    {
    }

    public class IgnoreDataMember : Attribute
    {
    }
}

#endif