using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public class CustomVariable
    {
        string mSourceObject;
        string mSourceObjectProperty;
        object mDefaultValue;

        public string Name
        {
            get;
            set;
        }

        public List<PropertySave> Properties = new List<PropertySave>();

        public object DefaultValue
        {
            get { return mDefaultValue; }
            set
            {
                // We used to do this, but it turns out the user
                // may want to set a value like a Sprite's Texture
                // back to NULL.  Therefore, we allow "<NONE>" and 
                // we just use null in the code generation.
                // UPDATE:  Actually, the user can't choose to not set
                // a CustomVariable - it's always set to something.  And 
                // the user may want to make a variable for setting a Sprite's
                // Texture, but may not want to necessarily override it in the 
                // custom variable.  So we'll handle "NONE" as nothing.
                if (value is string && (string)value == "<NONE>")
                {
                    mDefaultValue = null;
                }
                else
                {
                    mDefaultValue = value;
                }
            }
        }

        public bool SetByDerived
        {
            get;
            set;
        }

        public bool IsShared
        {
            get;
            set;
        }

        public bool DefinedByBase
        {
            get;
            set;
        }


        public string SourceObject
        {
            get { return mSourceObject; }
            set
            {
                mSourceObject = value;

                if (mSourceObject == "<NONE>")
                {
                    mSourceObject = "";
                }
            }

        }

        public string SourceObjectProperty
        {
            get { return mSourceObjectProperty; }
            set
            {
                mSourceObjectProperty = value;
                if (mSourceObjectProperty == "<NONE>")
                {
                    mSourceObjectProperty = "";
                }
            }
        }


    }
}
