using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;


using FlatRedBall.IO;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Instructions;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Events;
using FlatRedBall.Utilities;
using System.Collections.ObjectModel;
using System.Drawing;

namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum ContainerType
    {
        Screen,
        Entity,
        None
    }

    public enum SourceType
    {
        File,
        Entity,
        FlatRedBallType,
        // October 7, 2012
        // Decided to permanently
        // remove the Screen option
        // for objects.
        //Screen
        CustomType
    }

    public enum LayerCoordinateType
    {
        MatchCamera,
        MatchScreenResolution,
        Custom
    }

    public enum LayerCoordinateUnit
    {
        Pixel,
        Percent
    }

    #endregion


    #region Delegates

    public delegate string NamedObjectToString(NamedObjectSave nos);
    
    #endregion

    public class NamedObjectSave : INameable
    {
        #region Fields
        [XmlIgnore]
        public static NamedObjectToString ToStringDelegate;

        public List<PropertySave> Properties = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }

        private List<TypedMemberBase> mTypedMembers = new List<TypedMemberBase>();
        private ReadOnlyCollection<TypedMemberBase> mTypedMembersReadOnly;

        [XmlElementAttribute("CustomProperty")]
        public List<CustomVariableInNamedObject> InstructionSaves = new List<CustomVariableInNamedObject>();


        private List<String> mVariablesToRest = new List<String>();

        private string mSourceFile;
        private SourceType mSourceType;
        private bool mSetByDerived;
        private bool mExposedInDerived;
        private bool mSetByContainer;
        public string FileCreatedBy;
        private string mLayerOn;

        string mClickEvent;
        string mCurrentState;
        

        #endregion


        #region Properties

        public bool IsNewCamera
        {
            get;
            set;
        }
        public bool ShouldSerializeIsNewCamera()
        {
            return IsNewCamera;
        }

        /// <summary>
        /// A list of typed members which is populated according to the SourceType and related variables.
        /// This is public so that runtime libraries can populate it.  You should not modify this unless
        /// you really know what you're doing.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public List<TypedMemberBase> TypedMembers
        {
            get { return mTypedMembers; }
        }

        #region XML Docs
        /// <summary>
        /// Defines the source type of this NamedObjectSave.  The SourceType is the broadest type of categorization.
        /// </summary>
        /// <remarks>
        /// The SourceType cannot be Screen if
        /// this NamedObjectSave is contained in
        /// an Entity.  This restriction will be enforced
        /// in the PropertyGridHelper in Glue.
        /// </remarks>
        #endregion
        [CategoryAttribute("Source")]
        public SourceType SourceType
        {
            get { return mSourceType; }
            set
            {
                mSourceType = value;
            }
        }

        [CategoryAttribute("Source")]
        public string SourceFile
        {
            get
            {
                return mSourceFile;
            }
            set
            {
                if (!String.IsNullOrEmpty(value) && value.ToLower().Replace("\\", "/").StartsWith("content/"))
                    value = value.Substring(8);

                mSourceFile = value;
            }
        }

        string mSourceName;

        [CategoryAttribute("Source")]
        public string SourceName
        {
            get
            {
                return mSourceName;
            }
            set
            {
                mSourceName = value;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string SourceNameWithoutParenthesis
        {
            get
            {
                if (string.IsNullOrEmpty(mSourceName))
                {
                    return mSourceName;
                }
                else if (mSourceName == "<NONE>")
                {
                    return null;
                }
                else
                {
                    // -1 to take out the space before the (

                    return mSourceName.Substring(0, mSourceName.LastIndexOf('(') - 1);
                }
            }
        }

        string mSourceClassType;

        [CategoryAttribute("Source")]
        public string SourceClassType
        {
            get { return mSourceClassType; }
            set
            {
                mSourceClassType = value;

            }
        }


        string mSourceClassGenericType;

        [CategoryAttribute("Source")]
        public string SourceClassGenericType
        {
            get { return mSourceClassGenericType; }
            set
            {
                mSourceClassGenericType = value;

                if (mSourceClassGenericType == "<NONE>")
                {
                    mSourceClassGenericType = null;
                }
            }
        }

        /// <summary>
        /// Returns the effective type of the instance, depending on whether it's a 
        /// file, entity, or FRB type.
        /// </summary>
        [XmlIgnore]
        [CategoryAttribute("\t\tInstance")]
        public string InstanceType
        {
            get
            {
                if (SourceType == SourceType.File)
                {
                    // special case - if the file is a .srgx 
                    if (!string.IsNullOrEmpty(SourceFile) && FileManager.GetExtension(SourceFile) == "srgx")
                    {
                        return "SpriteRig";
                    }

                    else if (string.IsNullOrEmpty(SourceName) || SourceName == "<NONE>")
                    {
                        return "";
                    }
                    else
                    {
                        int openParenthesis = SourceName.LastIndexOf("(");
                        int length = SourceName.Length - 1 - openParenthesis;

                        return SourceName.Substring(openParenthesis + 1, length - 1);
                    }
                }
                else if (SourceType == SourceType.Entity)
                {
                    if (SourceClassType == null)
                    {
                        return null;
                    }
                    else
                    {
                        // Entities are stored as "Entity\WhateverEntity".  Let's remove tghe Entity part
                        return FlatRedBall.IO.FileManager.RemovePath(SourceClassType);
                    }
                }
                else
                {
                    return SourceClassType;
                }
            }
        }

        [CategoryAttribute("\t\tInstance")]
        public string InstanceName
        {
            get;
            set;
        }

        [XmlIgnore]
        [Browsable(false)]
        string INameable.Name
        {
            get { return InstanceName; }
            set { InstanceName = value; }
        }

        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool HasPublicProperty
        {
            get;
            set;
        }

        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool SetByDerived
        {
            get { return mSetByDerived; }
            set
            {
                mSetByDerived = value;
            }
        }

        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool ExposedInDerived
        {
            get { return mExposedInDerived; }
            set
            {
                mExposedInDerived = value;
            }
        }

        [CategoryAttribute("Access"), DefaultValue(false)]
        public bool SetByContainer
        {
            get { return mSetByContainer; }
            set
            {
                mSetByContainer = value;

            }
        }
        
        [CategoryAttribute("Creation")]
        public bool AttachToContainer
        {
            get;
            set;
        }

        [CategoryAttribute("Creation"), DefaultValue(false)]
        public bool AttachToCamera
        {
            get;
            set;
        }

        [CategoryAttribute("Creation"), DefaultValue(true)]
        public bool Instantiate
        {
            get;
            set;
        } = true;

        [CategoryAttribute("Creation"), DefaultValue(true)]
        public bool AddToManagers
        {
            get;
            set;
        }


        [CategoryAttribute("Creation"), DefaultValue(false)]
        public bool RemoveFromManagersWhenInvisible
        {
            get;
            set;
        }

        public bool ShouldSerializeAddToManagers()
        {
            return AddToManagers == false;
        }

        [CategoryAttribute("Creation")]
        public string ConditionalCompilationSymbols
        {
            get
            {
                return Properties.GetValue<string>(nameof(ConditionalCompilationSymbols));
            }
            set
            {
                Properties.SetValue(nameof(ConditionalCompilationSymbols), value);
            }
        }

        // This used to use the Properties list to store its value, but it caused a bug.
        // Since this defaulted to true in the constructor, the constructor would add a "true"
        // value for this. The constructor is called whenever XML deserialization occurs. After
        // this value was set to true, deserialization would add properties, and "true" would be
        // in the XML file too, causing a duplicate entry.
        // The moreal is - if the starting default for a value does not match the default for the type,
        // then the property should use get;set; instead of the properties.
        public bool GenerateTimedEmit
        {
            get;
            set;
        }



        bool mIsPixelPerfect = true;
        [Category("Creation")]
        public bool IsPixelPerfect
        {
            get{ return mIsPixelPerfect;}
            set{ mIsPixelPerfect = value;}
        }
        public bool ShouldSerializeIsPixelPerfect()
        {
            return IsPixelPerfect == false;
        }

        [CategoryAttribute("Interface"), DefaultValue(true)]
        public bool IncludeInIVisible
        {
            get;
            set;
        }

        [CategoryAttribute("Interface"), DefaultValue(true)]
        public bool IncludeInICollidable
        {
            get;
            set;
        }


        [CategoryAttribute("Interface"), DefaultValue(true)]
        public bool IncludeInIClickable
        {
            get;
            set;
        }

        // Behaviors are on their way out - do we need this anymore?
        [ DefaultValue("<NONE>")]    
        public string FulfillsRequirement
        {
            get;
            set;
        }

        [XmlIgnore]
        [Browsable(false)]
        public string FieldName
        {
            get 
            {

                if (HasPublicProperty || SetByContainer)
                {
                    return "m" + InstanceName;
                }
                else
                {
                    return InstanceName;
                }
                
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public bool IsGenericType
        {
            get
            {
                return !string.IsNullOrEmpty(InstanceType) && InstanceType.Contains("<T>");
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string ClassType
        {
            get
            {
                if (SourceType == SourceType.FlatRedBallType && !string.IsNullOrEmpty(InstanceType) &&
                    InstanceType.Contains("<T>"))
                {
                    string genericType = SourceClassGenericType;

                    if (genericType == null)
                    {
                        return null;
                    }
                    else
                    {

                        if (genericType.Contains("\\"))
                        {
                            // The namespace is part of it, so let's remove it
                            int lastSlash = genericType.LastIndexOf('\\');
                            genericType = genericType.Substring(lastSlash + 1);
                        }

                        return InstanceType.Replace("<T>", "<" + genericType + ">");
                    }
                }
                else
                {
                    return InstanceType;
                }
            }
        }


        [Browsable(false), DefaultValue(false)]
        public bool DefinedByBase
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool InstantiatedByBase
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public bool IsDisabled
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsNodeHidden
        {
            get
            {
                return Properties.ContainsValue("IsNodeHidden") && ((bool)Properties.GetValue("IsNodeHidden"));
            }
            set
            {
                Properties.SetValue("IsNodeHidden", value);
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public bool IsEntireFile
        {
            get
            {
                return SourceType == SourceType.File &&
                    SourceName != null &&
                    SourceName.StartsWith("Entire File (");
            }
        }


        [Browsable(false)]
        public List<string> VariablesToReset
        {
            get { return mVariablesToRest; }
            set { mVariablesToRest = value; }
        }
        public bool ShouldSerializeVariablesToReset()
        {
            return VariablesToReset != null && VariablesToReset.Count != 0;
        }

        public string Summary
        {
            get;
            set;
        }

        [CategoryAttribute("Activity"), DefaultValue(false)]
        public bool IgnoresPausing
        {
            get;
            set;
        }

        [CategoryAttribute("Activity"), DefaultValue(true)]
        public bool CallActivity
        {
            get;
            set;
        }

        [CategoryAttribute("Activity"), DefaultValue(false)]
        [XmlIgnore]
        public bool IsManuallyUpdated
        {
            get
            {
                return Properties.GetValue<bool>(nameof(IsManuallyUpdated));
            }
            set
            {
                Properties.SetValue(nameof(IsManuallyUpdated), value);
            }
        }

        [Browsable(false)]
        public List<NamedObjectSave> ContainedObjects
        {
            get;
            set;
        }
        public bool ShouldSerializeContainedObjects()
        {
            return ContainedObjects != null && ContainedObjects.Count != 0;
        }

        // I don't think we use these anymore.  Events are handled in IElements like variables
        //[Browsable(false)]
        //public List<EventSave> Events
        //{
        //    get;
        //    set;
        //}
        //public bool ShouldSerializeEvents()
        //{
        //    return Events != null && Events.Count != 0;
        //}

        // May 19, 2012
        // We now have events to handle
        // click as well as all kinds of
        // other types of events, so we don't
        // need this explicitly defined anymore.
//        [Browsable(false)]
//#if GLUE
//        [BroadcastAttribute(BroadcastStaticOrInstance.Instance)]
//        [TypeConverter(typeof(BroadcastAutofillTypeConverter))]
//#endif

//        [CategoryAttribute("Active Events")]
//        public string ClickEvent
//        {
//            get { return mClickEvent; }
//            set 
//            {
//                mClickEvent = value;
//                if (mClickEvent == "<NONE>")
//                {
//                    mClickEvent = "";
//                }
//            }
//        }

        [Browsable(false)]
        [XmlIgnore]
        public bool IsList
        {
            get
            {
                return SourceType == SaveClasses.SourceType.FlatRedBallType && SourceClassType == "PositionedObjectList<T>";
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool IsFullyDefined
        {
            get
            {
                switch (SourceType)
                {
                    case SaveClasses.SourceType.File:
                        return !string.IsNullOrEmpty(this.SourceFile) && 
                            !string.IsNullOrEmpty(this.SourceName) &&
                            this.SourceName != "<NONE>";

                        //break;
                    case SaveClasses.SourceType.FlatRedBallType:
                        if (IsList)
                        {
                            return !string.IsNullOrEmpty(this.SourceClassGenericType);
                        }
                        else
                        {
                            return !string.IsNullOrEmpty(this.SourceClassType);
                        }


                        //break;
                    case SaveClasses.SourceType.Entity:
                        return !string.IsNullOrEmpty(this.SourceClassType);
                        //break;

                }

                return true;
            }
        }

        [CategoryAttribute("Creation")]
        public string CurrentState
        {
            get { return mCurrentState; }
            set 
            { 
                mCurrentState = value;
                if (mCurrentState == "<NONE>")
                {
                    mCurrentState = null;
                }
            }
        }

        [CategoryAttribute("Creation")]
        public bool IsZBuffered
        {
            get;
            set;
        }
        public bool ShouldSerializeIsZBuffered()
        {
            return IsZBuffered;
        }

        // August 28, 2012
        // I initially
        // put this in so
        // Lists in Entities
        // wouldn't destroy the
        // factory that fills them.
        // I then found out that we should
        // only Destroy factories in Screens,
        // so I no longer need this.  But I may
        // need it in the future if a Screen contains
        // multiple lists of a given type...but I'll cross
        // that bridge when I get to it.
        //public bool InitializesFactory
        //{
        //    get;
        //    set;
        //}
        //public bool ShouldSerializeInitializesFactory()
        //{
        //    return InitializesFactory == false;
        //}

        #region Layer Properties

        public bool IndependentOfCamera
        {
            get;
            set;
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool IsLayer
        {
            get
            {
                return SourceType == SaveClasses.SourceType.FlatRedBallType && SourceClassType == "Layer";
            }
        }

        public string LayerOn
        {
            get { return mLayerOn; }
            set
            {
                if (value == "<NONE>")
                {
                    mLayerOn = "";
                }
                else
                {
                    mLayerOn = value;
                }
            }
        }

        public bool Is2D
        {
            get;
            set;
        }
        public bool ShouldSerializeIs2D()
        {
            return IsLayer;
        }

        public LayerCoordinateUnit LayerCoordinateUnit
        {
            get;
            set;
        }
        public bool ShouldSerializeLayerCoordinateUnit()
        {
            return LayerCoordinateUnit != SaveClasses.LayerCoordinateUnit.Pixel;
        }


        [XmlIgnore]
        public GlueSaveClasses.FloatRectangle? DestinationRectangle
        {
            get
            {
                if (Properties.ContainsValue("DestinationRectangle"))
                {
                    return Properties.GetValue<GlueSaveClasses.FloatRectangle?>("DestinationRectangle");
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    Properties.SetValue("DestinationRectangle", value.Value);
                }
                else
                {
                    Properties.Remove("DestinationRectangle");
                }
            }
        }


        [XmlIgnore]
        [CategoryAttribute("Creation")]
        public bool IsContainer
        {
            get
            {
                return Properties.GetValue<bool>("IsContainer");
            }
            set
            {
                Properties.SetValue("IsContainer", value);
            }

        }

        // I'm not sure if we want this anymore...
        // I had it before we had the IndepenedentOfCamera property.
        // Now maybe this is not important?
        // No, it is important.  IndependentOfCamera is regarding the 
        // DestinationRectangle.  This is regarding the orthognal resolution.
        public LayerCoordinateType LayerCoordinateType
        {
            get;
            set;
        }
        
        
        public bool ShouldSerializeLayerCoordinateType()
        {
            return IsLayer && LayerCoordinateType != SaveClasses.LayerCoordinateType.MatchCamera;
        }

        #endregion


        #endregion


        #region Methods

        #region Constructor

        public NamedObjectSave()
        {
            GenerateTimedEmit = true;
            Instantiate = true;
            mTypedMembersReadOnly = new ReadOnlyCollection<TypedMemberBase>(mTypedMembers);
            //Events = new List<EventSave>();

            IncludeInIVisible = true;
            IncludeInIClickable = true;
            IncludeInICollidable = true;
            CallActivity = true;

            AttachToContainer = true;
            AddToManagers = true;

            FulfillsRequirement = "<NONE>";

            ContainedObjects = new List<NamedObjectSave>();
            IndependentOfCamera = true;

        }

        #endregion



        public CustomVariableInNamedObject GetInstructionFromMember(string memberName)
        {
            for (int i = 0; i < InstructionSaves.Count; i++)
            {
                if (InstructionSaves[i].Member == memberName)
                {
                    return InstructionSaves[i];
                }
            }
            return null;
        }

        public NamedObjectSave GetContainedNamedObjectRecursively(string name)
        {
            if (this.InstanceName == name)
            {
                return this;
            }

            foreach (NamedObjectSave nos in ContainedObjects)
            {
                NamedObjectSave foundNos = nos.GetContainedNamedObjectRecursively(name);

                if (foundNos != null)
                {
                    return foundNos;
                }
            }

            return null;
        }

        public object GetPropertyValue(string propertyName)
        {
            InstructionSave instruction = GetInstructionFromMember(propertyName);
            if (instruction == null)
            {
                return null;
            }
            else
            {
                return instruction.Value;
            }
        }

        //public EventSave GetEvent(string eventName)
        //{
        //    foreach (EventSave es in Events)
        //    {
        //        if (es.EventName == eventName)
        //        {
        //            return es;
        //        }
        //    }

        //    return null;
        //}

        public CustomVariableInNamedObject GetCustomVariable(string name)
        {
            foreach (CustomVariableInNamedObject customVariable in this.InstructionSaves)
            {
                if (customVariable.Member == name)
                {
                    return customVariable;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns whether this instance has the argument variable. This controls whether the variable should appear in the property grid.
        /// </summary>
        /// <param name="customVariableName"></param>
        /// <returns></returns>
        public bool HasCustomVariable(string customVariableName)
        {
            for (int i = 0; i < mTypedMembers.Count; i++)
            {
                if (mTypedMembers[i].MemberName == customVariableName)
                {
                    return true;
                }
            }

            return false;
        }

        public void ReplaceLayerRecursively(string oldLayer, string newLayer)
        {
            if (LayerOn == oldLayer)
            {
                LayerOn = newLayer;
            }

            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.ReplaceLayerRecursively(oldLayer, newLayer);
            }

        }

        #region XML Docs
        /// <summary>
        /// Renames the custom variable setting both the typed member (which represents all possible variables) as well
        /// as the InstructionSaves (which represent the variables with assigned values).
        /// </summary>
        /// <param name="oldVariable">The name to search for.</param>
        /// <param name="newVariable">The name to replace with</param>
        /// <returns>Whether any renaming has occurred.</returns>
        #endregion
        public bool RenameVariable(string oldVariable, string newVariable)
        {
            bool returnValue = false;
            foreach (CustomVariableInNamedObject cvino in InstructionSaves)
            {
                if (cvino.Member == oldVariable)
                {
                    cvino.Member = newVariable;
                    returnValue = true;
                }
            }

            foreach (TypedMemberBase typedMember in mTypedMembers)
            {
                if (typedMember.MemberName == oldVariable)
                {
                    typedMember.MemberName = newVariable;
                    returnValue = true;
                }
            }

            return returnValue;
        }

        public void SetVariableValue(string variableName, object value)
        {
            var instructionToSet = GetCustomVariable(variableName);

            if(instructionToSet == null)
            {
                instructionToSet = new CustomVariableInNamedObject();
                instructionToSet.Member = variableName;
                this.InstructionSaves.Add(instructionToSet);
            }

            instructionToSet.Value = value;
        }

        public void SetAttachToCameraRecursively(bool value)
        {
            AttachToCamera = value;

            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.SetAttachToCameraRecursively(value);
            }
        }

        public void SetDefinedByBaseRecursively(bool value)
        {
            DefinedByBase = value;

            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.SetDefinedByBaseRecursively(value);
            }
        }

        public void SetInstantiatedByBaseRecursively(bool value)
        {
            InstantiatedByBase = value;

            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.SetInstantiatedByBaseRecursively(value);
            }

        }

        public void SetLayerRecursively(string layerName)
        {
            LayerOn = layerName;


            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.SetLayerRecursively(layerName);
            }
        }

        public void SetSetByDerivedRecursively(bool value)
        {
            SetByDerived = value;

            foreach (NamedObjectSave containedNos in ContainedObjects)
            {
                containedNos.SetSetByDerivedRecursively(value);
            }
        }

        public override string ToString()
        {
            if (ToStringDelegate != null)
            {
                return ToStringDelegate(this);
            }
            else
            {
                return ClassType + " " + FieldName;
            }
        }

        public bool RemoveSelfFromNamedObjectList(List<NamedObjectSave> namedObjects)
        {
            if (namedObjects.Contains(this))
            {
                namedObjects.Remove(this);
                return true;
            }

            foreach (NamedObjectSave nos in namedObjects)
            {
                if (RemoveSelfFromNamedObjectList(nos.ContainedObjects))
                {
                    return true;
                }
            }

            return false;

        }


        #endregion
    }
}
