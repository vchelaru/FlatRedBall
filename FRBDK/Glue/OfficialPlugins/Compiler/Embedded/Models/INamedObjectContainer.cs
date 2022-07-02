using System;
using System.Collections.Generic;
using System.Text;


namespace GlueControl.Models
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

        int VerificationIndex
        {
            get;
            set;
        }

    }

}
