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
using Newtonsoft.Json;

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

    public class EntitySave : GlueElement, IPropertyListContainer
    {
        #region Fields

        string mBaseEntity;


        #endregion

        #region Properties

        [CategoryAttribute("Inheritance and Interfaces")]
        public string BaseEntity
        {
            get { return mBaseEntity; }
            set
            {
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
        [JsonIgnore]
        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsICollidable
        {
            get
            {
                return Properties.GetValue<bool>(nameof(ImplementsICollidable));
            }
            set
            {
                Properties.SetValue(nameof(ImplementsICollidable), value);
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

        [CategoryAttribute("Inheritance and Interfaces")]
        public bool ImplementsITiledTileMetadata {
            get;
            set;
        }
        public bool ShouldSerializeImplementsITiledTileSprite() {
            return ImplementsITiledTileMetadata == true;
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
            get { return mBaseEntity; }
            set { mBaseEntity = value; }
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

        public bool ShouldSerializeUseGlobalContent()
        {
            return UseGlobalContent == true;
        }

        [Category("Performance")]
        [XmlIgnore]
        [JsonIgnore]
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

        // JsonIgnore because it's handled by BaseEntity
        [JsonIgnore]
        public override string BaseElement => BaseEntity; 

        #region ScrollableEntityList Properties

        [XmlIgnore]
        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonIgnore]
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
                    Properties.SetValue<string>("ItemType", null);
                }
                else
                {
                    Properties.SetValue("ItemType", value);
                }
            }
        }

        [XmlIgnore]
        [JsonIgnore]
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
        [JsonIgnore]
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
        [JsonIgnore]
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


        /// <summary>
        /// Sets whether the Entity is a 2D Entity.  If this is true, then contained objects and files will default to 2D mode (pixel size will be .5, Cameras in files will be orthogonal)
        /// </summary>
        public bool Is2D
        {
            get;
            set;
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
        }


        


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
