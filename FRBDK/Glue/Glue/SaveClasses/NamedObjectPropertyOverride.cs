using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace FlatRedBall.Glue.SaveClasses
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    // May 16, 2012
    // I don't know if
    // this is even used
    // anymore, but it seems
    // to be leftover code from
    // when States could be used
    // to load custom types of content
    // (like different .scnx files depending
    // on the phone's current resolution.  But
    // this has been removed and will likely be
    // replaced by something else later.
    public class NamedObjectPropertyOverride
    {
        string mSourceFile;

        // This is used to store off values that are set
        // in the property grids before the instance that is
        // being modified is placed in a state.  When adding here, 
        // add to ReactToStateSaveChangedValue in the PropertyGrid helper.  Look for the switch.
        public static string SourceFileBuffer;

        #region Properties

        [Browsable(false)]
        public bool IsNulledOut
        {
            get
            {
                // Add any new property tests here.
                // This is used to know whether this should be removed from the state
                return SourceFile == null;
            }
        }


        // If adding new properties, modify the IsNulledOut property and add a buffer above ^^^^^

        [Browsable(false)]
        public string Name
        {
            get;
            set;
        }

        // If adding new properties, modify the IsNulledOut property and add a buffer above ^^^^^

        public string SourceFile
        {
            get
            {
                return mSourceFile;
            }
            set
            {
                // Vic asks - do we want this here? It got removed when we moved to a core project.
#if GLUE
                if (ObjectFinder.Self.GetElementContaining(this) == null)
                {
                    SourceFileBuffer = value;
                }
#endif
                mSourceFile = value;
            }
        }

        // If adding new properties, modify the IsNulledOut property and add a buffer above ^^^^^

#endregion

#region Methods

        public NamedObjectPropertyOverride Clone()
        {
            return (NamedObjectPropertyOverride)this.MemberwiseClone();
        }

        public override string ToString()
        {
            return Name;
        }

#endregion
    }
}
