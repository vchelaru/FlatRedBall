using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.Elements
{
    #region PlatformSpecificType class
    public struct PlatformSpecificType
    {
        public const string AllPlatform = "All";

        public string QualifiedType;
        public string Platform;

        /// <summary>
        /// An optional function which can return the qualified name. The Object is the NamedObjectSave or ReferencedFileSave.
        /// </summary>
        [XmlIgnore]
        public Func<object, string> PlatformFunc;
    }
    #endregion
}
