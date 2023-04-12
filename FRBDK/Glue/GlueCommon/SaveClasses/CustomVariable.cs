using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum BeforeOrAfter
    {
        Before,
        After
    }


    public enum Scope
    {
        Public,
        Private,
        Protected,
        Internal
    }

    #endregion

    #region Delegates

    public delegate string CustomVariableToString(CustomVariable cv);

    #endregion

    /// <summary>
    /// Variable which is stored directly in a GlueElement. These are not directly stored in NamedObjectSaves.
    /// </summary>
    public class CustomVariable 
    {
        #region Fields
        [XmlIgnore]
        [JsonIgnore]
        public static CustomVariableToString ToStringDelegate;

        public List<PropertySave> Properties = new List<PropertySave>();

        string mSourceObject;
        string mSourceObjectProperty;
        object mDefaultValue;

        #endregion

        #region Properties

        [CategoryAttribute("\t")]
        public string Name
        {
            get;
            set;
        }

        [XmlIgnore]
        [JsonIgnore]
        [ReadOnlyAttribute(true)]
		public string Type
		{
            get => Properties.GetValue<string>("Type");
			set => Properties.SetValue("Type", value);
		}

        [BrowsableAttribute(false)]
        public ReferencedFileReference SourceFile
        {
            get;
            set;
        }



		[ReadOnlyAttribute(false)]
        [CategoryAttribute("Access")]
		public object DefaultValue
		{
            get => mDefaultValue; 
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

		public string FulfillsRequirement
		{
			get;
			set;
		}
        public bool ShouldSerializeFulfillsRequirement()
        {
            return !string.IsNullOrEmpty(FulfillsRequirement) && FulfillsRequirement != "<NONE>";
        }

		[CategoryAttribute("Access"), DefaultValue(false)]
		public bool SetByDerived
		{
			get;
			set;
		}
        

		[CategoryAttribute("Access"), DefaultValue(false)]
		public bool IsShared
		{
			get;
			set;
		}

		[Browsable(false), DefaultValue(false)]
		public bool DefinedByBase
		{
			get;
			set;
		}

        [CategoryAttribute("Tunneling")]
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

        [CategoryAttribute("Tunneling")]
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

        public string Summary
        {
            get;
            set;
        }


        [CategoryAttribute("Event"), DefaultValue(false)]
        public bool CreatesEvent
        {
            get;
            set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool HasAccompanyingVelocityProperty
        {
            
            get
            {
                return Properties.ContainsValue("HasAccompanyingVelocityProperty") && ((bool)Properties.GetValue("HasAccompanyingVelocityProperty"));
            }
            set
            {
                Properties.SetValue("HasAccompanyingVelocityProperty", value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [CategoryAttribute("Type Conversion")]
        public string OverridingPropertyType
        {
            get
            {
                if (Properties.ContainsValue("OverridingPropertyType"))
                {
                    return Properties.GetValue<string>("OverridingPropertyType");
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value == "<NONE>")
                {
                    Properties.SetValue<string>("OverridingPropertyType", null);
                }
                else
                {
                    Properties.SetValue("OverridingPropertyType", value);
                }
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [CategoryAttribute("Type Conversion")]
        public string TypeConverter
        {
            get
            {
                if (Properties.ContainsValue("TypeConverter"))
                {
                    return Properties.GetValue<string>("TypeConverter");
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Properties.SetValue("TypeConverter", value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public bool IsTunneling
        {
            get
            {
                return !string.IsNullOrEmpty(this.SourceObject) && !string.IsNullOrEmpty(this.SourceObjectProperty);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [CategoryAttribute("Access")]
        public bool CreatesProperty
        {
            get
            {
                return Properties.GetValue<bool>("CreatesProperties");
            }
            set
            {
                Properties.SetValue("CreatesProperties", value);

            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [CategoryAttribute("Access")]
        public Scope Scope
        {
            get => Properties.GetValue<Scope>(nameof(Scope));
            set => Properties.SetValue(nameof(Scope), value);
        }

        // Don't XML ignore this!
        // doing so seems to strip all
        // categories out of objects. I
        // didn't expect that, I thought that
        // the Properties would handle it but it 
        // seems like they don't. Maybe because it's a field?
        public string Category
        {
            get => Properties.GetValue<string>(nameof(Category));
            set => Properties.SetValue(nameof(Category), value);
        }

        //[XmlIgnore]
        //public Scope
        public string PreferredDisplayerTypeName
        {
            get; set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public Func<GlueElement, List<string>> CustomGetForcedOptionsFunc;

        #endregion

        #region Methods

        public CustomVariable()
		{
			FulfillsRequirement = "<NONE>";
		}

		public CustomVariable Clone()
		{
			CustomVariable newCustomVariable = this.MemberwiseClone() as CustomVariable;
            newCustomVariable.Properties = new List<PropertySave>();
            newCustomVariable.Properties.AddRange(this.Properties);

			return newCustomVariable;
		}


        public object GetValue()
        {
            return this.DefaultValue;
        }


        public override string ToString()
        {
            if (ToStringDelegate != null)
            {
                return ToStringDelegate(this);
            }
            else
            {
                if (string.IsNullOrEmpty(SourceObject))
                {
                    return "" + Type + " " + Name + " = " + DefaultValue;
                }
                else
                {
                    return "" + Type + " " + SourceObject + "." + SourceObjectProperty + " = " + DefaultValue;
                }
            }
        }

        #endregion

    }
}
