using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Instructions.Reflection;

using System.Xml.Serialization;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Interfaces;

#if GLUE
using Microsoft.Build.BuildEngine;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
#endif
namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum MembershipInfo
    {
        NotContained,
        ContainedInThis,
        ContainedInBase,
        ContainedInDerived
    }


    public enum VerticalOrHorizontal
    {
        Vertical,
        Horizontal
    }

    #endregion

    public class EntitySave : IFileReferencer, IElement, ITaggable, IPropertyListContainer
    {
        #region Fields

        bool mIsUnique;


        string mBaseEntity;

        string mCurrentStateChange;


        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }

        public List<string> Tags = new List<string>();
        public string Source = "";

        #endregion

        #region Properties

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
                    foreach(StateSave state in category.States)
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
                foreach(NamedObjectSave nos in NamedObjects)
                {
                    yield return nos;

                    foreach(NamedObjectSave containedNos in nos.ContainedObjects)
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
        public List<CustomVariable> CustomVariables
        {
            get;
            set;
        }
        public bool ShouldSerializeCustomVariables()
        {
            return CustomVariables != null && CustomVariables.Count != 0;
        }

        // This is not ever used, may be removed
        // completely from Glue, but for now let's
        // pull it out of the UI
        [Browsable(false)]
        public bool IsUnique
        {
            get
            {
                return mIsUnique;
            }
            set
            {
                mIsUnique = value;

                for (int i = 0; i < NamedObjects.Count; i++)
                {
                    NamedObjects[i].AddToManagers = !mIsUnique;
                }


            }
        }
        public bool ShouldSerializeIsUnique()
        {
            return IsUnique == true;
        }

        [CategoryAttribute("Inheritance and Interfaces")]
        public string BaseEntity
        {
            get { return mBaseEntity; }
            set
            {
                string prevValue = mBaseEntity;

                if (value == "<NONE>")
                {
                    mBaseEntity = "";
                }
                else
                {
                    mBaseEntity = value;
                }

            }
        }

        [XmlIgnore]
        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsICollidable
        {
            get
            {
                return Properties.GetValue<bool>("ImplementsICollidable");
            }
            set
            {
                Properties.SetValue("ImplementsICollidable", value);
            }
        }

        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsIClickable
        {
            get;
            set;
        }
        public bool ShouldSerializeImplementsIClickable()
        {
            return ImplementsIClickable == true;
        }

        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsIVisible
        {
            get;
            set;
        }
        public bool ShouldSerializeImplementsIVisible()
        {
            return ImplementsIVisible == true;
        }

        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsIWindow
        {
            get;
            set;
        }
        public bool ShouldSerializeImplementsIWindow()
        {
            return ImplementsIWindow == true;
        }

        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsIDrawableBatch
        {
            get
            {
                return Properties.GetValue<bool>(nameof(ImplementsIDrawableBatch)   );
            }
            set
            {
                Properties.SetValue(nameof(ImplementsIDrawableBatch), value);
            }
        }

        [Browsable(false)]
        public string Name
        {
            get;
            set;
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
            get { return mBaseEntity; }
            set { mBaseEntity = value; }
        }

        [XmlIgnore]
        int INamedObjectContainer.VerificationIndex
        {
            get;
            set;
        }

        internal int VerificationIndex
        {
            get;
            set;
        }

        [CategoryAttribute("Performance")]
        public bool CreatedByOtherEntities
        {
            get;
            set;
        }
        public bool ShouldSerializeCreatedByOtherEntities()
        {
            return CreatedByOtherEntities == true;
        }

        [CategoryAttribute("Performance")]
        public bool PooledByFactory
        {
            get;
            set;
        }
        public bool ShouldSerializePooledByFactory()
        {
            return PooledByFactory == true;
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

        [Category("Performance")]
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

        //public bool ShouldBePooled = true;



        public string Summary
        {
            get;
            set;
        }

        string IElement.BaseElement
        {
            get { return BaseEntity; }
        }


        #region ScrollableEntityList Properties

        [XmlIgnore]
        [CategoryAttribute("Scrollable Entity List")]
        public bool IsScrollableEntityList
        {
            get
            {
                return Properties.GetValue<bool>(nameof(IsScrollableEntityList));
            }
            set
            {
                Properties.SetValue(nameof(IsScrollableEntityList), value);
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        [CategoryAttribute("Scrollable Entity List")]
        public VerticalOrHorizontal VerticalOrHorizontal
        {
            get
            {
                return Properties.GetValue<VerticalOrHorizontal>("VerticalOrHorizontal");
            }
            set
            {
                Properties.SetValue("VerticalOrHorizontal", value);
            }
        }


        [XmlIgnore]
        [CategoryAttribute("Scrollable Entity List")]
        public string ItemType
        {
            get
            {
                return Properties.GetValue<string>("ItemType");
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Properties.SetValue("ItemType", null);
                }
                else
                {
                    Properties.SetValue("ItemType", value);
                }
            }
        }

        [XmlIgnore]
        [CategoryAttribute("Scrollable Entity List")]
        public float ListTopBound
        {
            get
            {
                return Properties.GetValue<float>("ListTopBound");
            }
            set
            {
                Properties.SetValue("ListTopBound", value);
            }

        }

        [XmlIgnore]
        [CategoryAttribute("Scrollable Entity List")]
        public float ListBottomBound
        {
            get
            {
                return Properties.GetValue<float>("ListBottomBound");
            }
            set
            {
                Properties.SetValue("ListBottomBound", value);
            }
        }

        [XmlIgnore]
        [CategoryAttribute("Scrollable Entity List")]
        public float SpacingBetweenItems
        {
            get
            {
                return Properties.GetValue<float>("SpacingBetweenItems");
            }
            set
            {
                Properties.SetValue("SpacingBetweenItems", value);
            }

        }

        #endregion

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

        /// <summary>
        /// Sets whether the Entity is a 2D Entity.  If this is true, then contained objects and files will default to 2D mode (pixel size will be .5, Cameras in files will be orthogonal)
        /// </summary>
        public bool Is2D
        {
            get;
            set;
        }

        List<string> ITaggable.Tags
        {
            get { return Tags; }
        }

        string ITaggable.Source
        {
            get { return Source; }
        }

        #endregion

        #region Methods

        public EntitySave()
        {
            // June 19, 2011
            // We used to have
            // PooledByFactory set
            // to true because I (Vic)
            // thought that we could simplify
            // variable resetting such that the
            // user would never have to worry about
            // reset variables - in other words, that
            // it would "just work".  Unfortunately that
            // doesn't seem to be the case.  We've simplified
            // it quite a bit, but it still requires some work
            // and can be confusing to users.  Therefore, we'll
            // make this false by default, and the user will have
            // to set it to true if pooling behavior is desired.
            //PooledByFactory = true;
            PooledByFactory = false;

            ReferencedFiles = new List<ReferencedFileSave>();
            NamedObjects = new List<NamedObjectSave>();
            CustomVariables = new List<CustomVariable>();
            //Behaviors = new List<BehaviorSave>();
            States = new List<StateSave>();
            StateCategoryList = new List<StateSaveCategory>();
            Events = new List<EventResponseSave>();
        }


        public EntitySave Clone()
        {
            return FileManager.CloneObject<EntitySave>(this);
            /*
            EntitySave entitySaveToReturn = (EntitySave)this.MemberwiseClone();

            entitySaveToReturn.CustomVariables = new List<CustomVariable>();
            for (int i = 0; i < CustomVariables.Count; i++)
            {
                entitySaveToReturn.CustomVariables.Add(CustomVariables[i].Clone());
            }

            entitySaveToReturn.NamedObjects = new List<NamedObjectSave>();
            for (int i = 0; i < NamedObjects.Count; i++)
            {
                entitySaveToReturn.NamedObjects.Add(NamedObjects[i].Clone());
            }

            entitySaveToReturn.ReferencedFiles = new List<ReferencedFileSave>();
            for (int i = 0; i < ReferencedFiles.Count; i++)
            {
                entitySaveToReturn.ReferencedFiles.Add(ReferencedFiles[i].Clone());
            }


            return entitySaveToReturn;
             */
        }


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

        public ReferencedFileSave GetReferencedFileSave(string fileName)
        {
            return FileReferencerHelper.GetReferencedFileSave(this, fileName);
        }

        // This method needs to be moved to IElementExtensionMethods.cs!!!

        


        
        public void SetCustomVariable(string customVariableName, object valueToSet)
        {
            for (int i = 0; i < CustomVariables.Count; i++)
            {
                if (CustomVariables[i].Name == customVariableName)
                {
                    CustomVariable cv = CustomVariables[i];
                    cv.DefaultValue = valueToSet;
                    CustomVariables[i] = cv;
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
