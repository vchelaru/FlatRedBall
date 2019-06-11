using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using FlatRedBall.Glue.Events;

#if GLUE
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
#endif

namespace FlatRedBall.Glue.SaveClasses
{
    public class ScreenSave : IFileReferencer, IElement, IEventContainer, ITaggable
    {
        #region Fields

        string mBaseScreen;
        string mNextScreen;

        public List<PropertySave> Properties { get; set; } = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }

        public List<string> Tags = new List<string>();
        public string Source = "";

        #endregion

        #region Properties

        List<string> ITaggable.Tags
        {
            get { return this.Tags; }
        }

        string ITaggable.Source
        {
            get { return this.Source; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool HasChanged { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public IEnumerable<StateSave> AllStates
        {
            get
            {
                foreach (StateSave state in States)
                {
                    yield return state;
                }
                foreach (StateSaveCategory category in this.StateCategoryList)
                {
                    foreach (StateSave state in category.States)
                    {
                        yield return state;
                    }
                }
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public IEnumerable<NamedObjectSave> AllNamedObjects
        {
            get
            {
                foreach (NamedObjectSave nos in NamedObjects)
                {
                    yield return nos;

                    foreach (NamedObjectSave containedNos in nos.ContainedObjects)
                    {
                        yield return containedNos;
                    }
                }
            }
        }

        [Browsable(false)]
        public List<StateSave> States
        {
            get;
            set;
        }
        public bool ShouldSerializeStates()
        {
            return States != null && States.Count != 0;
        }

        [Browsable(false)]
        public List<StateSaveCategory> StateCategoryList
        {
            get;
            set;
        }
        public bool ShouldSerializeStateCategoryList()
        {
            return StateCategoryList != null && StateCategoryList.Count != 0;
        }

        [Browsable(false)]
        [XmlIgnore]
        public bool HasStates
        {
            get
            {
                if (States.Count != 0)
                {
                    return true;
                }

                foreach (StateSaveCategory category in StateCategoryList)
                {
                    if (category.States.Count != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [Browsable(false)]
        public List<EventResponseSave> Events
        {
            get;
            set;
        }
        public bool ShouldSerializeEvents()
        {
            return Events != null && Events.Count != 0;
        }

        // Broadcasting has been removed
//        [Browsable(false)]
//#if GLUE
//        [BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
//#endif
//        public string InitializeBroadcast
//        {
//            get;
//            set;
//        }

//        // Broadcasting has been removed 
//        [Browsable(false)]
//#if GLUE
//        [BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
//#endif
//        public string DestroyBroadcast
//        {
//            get;
//            set;
//        }

        [DefaultValue(false)]
        public bool IsOnOwnLayer
        {
            get;
            set;
        }

        public string BaseScreen
        {
            get { return mBaseScreen; }
            set
            {
                string prevValue = mBaseScreen;

                if (value == "<NONE>")
                {
                    mBaseScreen = "";
                }
                else
                {
                    mBaseScreen = value;
                }
            }
        }

        public string NextScreen
        {
            get { return mNextScreen; }
            set
            {
                mNextScreen = value;

                if (value == "<NONE>")
                {
                    mNextScreen = "";
                }
            }
        }

        [Browsable(false)]
        public List<CustomVariable> CustomVariables
        {
            get;
            set;
        }
        public bool ShouldSerializeCustomVariables()
        {
            return CustomVariables != null && CustomVariables.Count != 0;
        }

        [Browsable(false)]
        public string Name
        {
            get;
            set;
        }

        [Browsable(false)]
        public List<ReferencedFileSave> ReferencedFiles
        {
            get;
            set;
        }
        public bool ShouldSerializeReferencedFiles()
        {
            return ReferencedFiles != null && ReferencedFiles.Count != 0;
        }

        [Browsable(false)]
        public List<NamedObjectSave> NamedObjects
        {
            get;
            set;
        }
        public bool ShouldSerializeNamedObjects()
        {
            return NamedObjects != null && NamedObjects.Count != 0;
        }

        //[Browsable(false)]
        //public List<BehaviorSave> Behaviors
        //{
        //    get;
        //    set;
        //}
        //public bool ShouldSerializeBehaviors()
        //{
        //    return Behaviors != null && Behaviors.Count != 0;
        //}



        [XmlIgnore]
        [Browsable(false)]
        string INamedObjectContainer.BaseObject
        {
            get { return mBaseScreen; }
            set { mBaseScreen = value; }
        }

        [XmlIgnore]
        int INamedObjectContainer.VerificationIndex
        {
            get;
            set;
        }

        [CategoryAttribute("Performance")]
        public string ContentManagerMethod
        {
            get;
            set;
        }

        [CategoryAttribute("Performance")]
        public bool UseGlobalContent
        {
            get;
            set;
        }
        public bool ShouldSerializeUseGlobalContent()
        {
            return UseGlobalContent == true;
        }

        [XmlIgnore]
        [DisplayName("Name")]
        public string ClassName
        {
            get
            {
                return FileManager.RemovePath(Name);
            }
        }

        public string Summary
        {
            get;
            set;
        }

        [Browsable(false)]
        public bool IsRequiredAtStartup
        {
            get;
            set;
        }
        public bool ShouldSerializeIsRequiredAtStartup()
        {
            return IsRequiredAtStartup == true;
        }

        [Browsable(false)]
        [XmlIgnore]
        public string ContentManagerForCodeGeneration
        {
            get
            {
                string nameWithoutPath = FileManager.RemovePath(Name);

                string contentManagerName = "\"" + nameWithoutPath + "\"";
                if (!String.IsNullOrEmpty(ContentManagerMethod))
                {
                    contentManagerName = ContentManagerMethod;
                }
                else if (UseGlobalContent)
                {
                    contentManagerName = "\"Global\"";
                }

                return contentManagerName;
            }
        }

        string IElement.BaseElement
        {
            get { return BaseScreen; }
        }

        [XmlIgnore]
        [CategoryAttribute("Performance")]
        public bool IsLoadingScreen
        {
            get
            {
                return Properties.ContainsValue("IsLoadingScreen") && ((bool)Properties.GetValue("IsLoadingScreen"));
            }
            set
            {
                Properties.SetValue("IsLoadingScreen", value);
            }
        }

        #endregion

        #region Methods

        public ScreenSave()
        {
            Events = new List<EventResponseSave>();
            ReferencedFiles = new List<ReferencedFileSave>();
            NamedObjects = new List<NamedObjectSave>();
            //Behaviors = new List<BehaviorSave>();
            CustomVariables = new List<CustomVariable>();
            States = new List<StateSave>();
            StateCategoryList = new List<StateSaveCategory>();
        }

        public CustomVariable AddCustomVariable(string propertyType, string propertyName)
        {
            CustomVariable customVariable = new CustomVariable();
            customVariable.Type = propertyType;
            customVariable.Name = propertyName;

            CustomVariables.Add(customVariable);

            return customVariable;
        }

        public ScreenSave Clone()
        {
            return FileManager.CloneObject<ScreenSave>(this);
        }

        //public BehaviorSave GetBehavior(string behaviorName)
        //{
        //    return IBehaviorContainerHelper.GetBehavior(this, behaviorName);
        //}

        public CustomVariable GetCustomVariable(string customVariableName)
        {
            foreach (CustomVariable customVariable in CustomVariables)
            {
                if (customVariable.Name == customVariableName)
                {
                    return customVariable;
                }
            }
            return null;
        }

        //public CustomVariable GetCustomVariableRecursively(string variableName)
        //{
        //    return IBehaviorContainerHelper.GetCustomVariableRecursively(this, variableName);
        //}
        
        public EventResponseSave GetEvent(string eventName)
        {
            foreach (EventResponseSave es in Events)
            {
                if (es.EventName == eventName)
                {
                    return es;
                }
            }

            return null;
        }

        //public string GetFulfillerName(BehaviorRequirement behaviorRequirement)
        //{
        //    return IBehaviorContainerHelper.GetFulfillerName(this, behaviorRequirement);
        //}
        
        public object GetPropertyValue(string propertyName)
        {
            for (int i = 0; i < CustomVariables.Count; i++)
            {
                if (CustomVariables[i].Name == propertyName)
                {
                    return CustomVariables[i].DefaultValue;
                }
            }
            return null;

        }

        public void SetCustomVariable(string propertyName, object valueToSet)
        {
            for (int i = 0; i < CustomVariables.Count; i++)
            {
                if (CustomVariables[i].Name == propertyName)
                {
                    CustomVariable cv = CustomVariables[i];
                    cv.DefaultValue = valueToSet;
                    CustomVariables[i] = cv;
                }
            }
        }

        public bool HasMemberWithName(string memberName)
        {
            for (int i = 0; i < ReferencedFiles.Count; i++)
            {
                if (FileManager.RemovePath(FileManager.RemoveExtension(ReferencedFiles[i].Name)) == memberName)
                {
                    return true;
                }
            }

            for (int i = 0; i < NamedObjects.Count; i++)
            {
                if (NamedObjects[i].FieldName == memberName)
                {
                    return true;
                }
            }

            return false;

        }
        
        public ReferencedFileSave GetReferencedFileSave(string fileName)
        {
            return FileReferencerHelper.GetReferencedFileSave(this, fileName);
        }


        public override string ToString()
        {
            return this.Name + " (Screen)";
        }

        #endregion


    }
}
