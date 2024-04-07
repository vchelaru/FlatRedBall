using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Xml.Serialization;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using FlatRedBall.Glue.Events;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.SaveClasses
{
    public class ScreenSave : GlueElement, IEventContainer
    {
        #region Fields

        string mBaseScreen;
        string mNextScreen;

        #endregion

        #region Properties




        // Broadcasting has been removed
//        [Browsable(false)]
//        [BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
//        public string InitializeBroadcast
//        {
//            get;
//            set;
//        }

//        // Broadcasting has been removed 
//        [Browsable(false)]
//        [BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
//        public string DestroyBroadcast
//        {
//            get;
//            set;
//        }

        [DefaultValue(false)]
        [Obsolete("Don't use this anymore. As of April 2024 - this is very old and is likely not used anymore in modern projects." +
            " We now use explicit layers.")]
        public bool IsOnOwnLayer
        {
            get;
            set;
        }

        /// <summary>
        /// The base screen for this screen. Setting this
        /// value results in this screen being a derived screen.
        /// If set in Glue, the UpdateFromBaseType extension method
        /// should be called to propagate all named objects and variables.
        /// </summary>
        public string BaseScreen
        {
            get { return mBaseScreen; }
            set
            {
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
        [JsonIgnore]
        [Browsable(false)]
        public override string BaseObject
        {
            get { return mBaseScreen; }
            set { mBaseScreen = value; }
        }

        [CategoryAttribute("Performance")]
        public string ContentManagerMethod
        {
            get;
            set;
        }


        public bool ShouldSerializeUseGlobalContent()
        {
            return UseGlobalContent == true;
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
        [JsonIgnore]
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

        public override string BaseElement => BaseScreen;

        [XmlIgnore]
        [JsonIgnore]
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

        // Vic says - Entities use JSON for cloning, but Screens still
        // use XML. Should we migrate over to JSON? Not sure...
        [Obsolete("Use CloneJson instead")]
        public ScreenSave Clone()
        {
            var clone = FileManager.CloneObject<ScreenSave>(this);

            CopyVariablesAfterClone(clone);

            return clone;
        }

        public ScreenSave CloneJson()
        {
            var serialized = JsonConvert.SerializeObject(this, Formatting.Indented);

            var clone = JsonConvert.DeserializeObject<ScreenSave>(serialized);

            CopyVariablesAfterClone(clone);

            return clone;
        }

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
        
        public override string ToString()
        {
            return this.Name + " (Screen)";
        }

        #endregion


    }
}
