using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FlatRedBall.Input;

namespace FlatRedBall.Glue.SaveClasses
{
    public interface INamedObjectContainer
    {
        List<NamedObjectSave> NamedObjects
        {
            get;
        }

        IEnumerable<NamedObjectSave> AllNamedObjects { get; }


        string BaseObject
        {
            get;
            set;
        }

        [XmlIgnore]
        int VerificationIndex
        {
            get;
            set;
        }

    }

}
