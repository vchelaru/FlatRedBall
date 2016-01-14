using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using GlueView.Forms.PropertyGrids;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Instructions.Reflection;
using System.ComponentModel;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Parsing;
using System.Reflection;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

//using FlatRedBall.Glue.FormHelpers.StringConverters;
//using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.GuiDisplay
{



    public class NamedObjectPropertyGridDisplayer : PropertyGridDisplayer
    {

        #region Enums

        public enum DisplayModes
        {
            Regular,
            VariablesOnly,
            Debug
        }



        #endregion

        #region Fields

        static Attribute[] mCustomVariableAttribute = new Attribute[] { new CategoryAttribute("\t\tVariable") };
        static Attribute[] mUnsetCustomVariableAttribute = new Attribute[] { new CategoryAttribute("\tUnset Variable") };

        DisplayModes mDisplayMode;

        static GlueStateSnapshot mGlueStateSnapshot;

        #endregion

        #region Properties

        public DisplayModes DisplayMode
        {
            get { return mDisplayMode; }
            set
            {
                mDisplayMode = value;
                UpdateIncludedAndExcluded(Instance as NamedObjectSave);

                base.Instance = base.Instance; // refreshes the PropertyGrid
            }
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                throw new Exception("Use UpdateToState instead");

            }
        }

        public IElement CurrentElement
        {
            get { return mGlueStateSnapshot.CurrentElement; }
        }

        public NamedObjectSave CurrentNamedObject
        {
            get
            {
                return mGlueStateSnapshot.CurrentNamedObjectSave;
            }
        }

        public CustomVariable CurrentCustomVariable
        {
            get
            {
                return mGlueStateSnapshot.CurrentCustomVariable;
            }
        }

        public StateSave CurrentStateSave
        {
            get
            {
                return mGlueStateSnapshot.CurrentStateSave;
            }
        }


        public override System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get
            {
                return base.PropertyGrid;
            }
            set
            {
                GridItem gridItem = null;
                if (base.PropertyGrid != null)
                {
                    gridItem = base.PropertyGrid.SelectedGridItem;
                }
                base.PropertyGrid = value;
                // There's a bug where the PropertyGrid scrolls down.  Let's fix that
                // Update - now it's a bug that it 
                // scrolls to the top.  We just need to select whatever
                // was selected before
                //ScrollToTop();

                if (base.PropertyGrid != null && gridItem != null && base.PropertyGrid.SelectedGridItem != null)
                {
                    var parent = PropertyGrid.SelectedGridItem.Parent;
                    if (parent != null)
                    {
                        foreach (GridItem child in parent.GridItems)
                        {
                            if (child.Label == gridItem.Label)
                            {
                                base.PropertyGrid.SelectedGridItem = child;
                                break;
                            }
                        }
                    }

                }
                
            }
        }


        #endregion

        #region Constructor

        public NamedObjectPropertyGridDisplayer()
            : base()
        {
            mGlueStateSnapshot = new GlueStateSnapshot();
            DisplayMode = DisplayModes.Regular;
        }

        #endregion

        #region Methods

        private bool ShouldMemberBeSkipped(TypedMemberBase typedMemberBase)
        {
            // This variable is only available for exposing, not for setting on the NOS itself
            if (typedMemberBase.MemberName == "SourceFile")
            {
                return true;
            }
            return false;
        }
        

        void AfterChangeValueThatConflictsWithPixelSize(object sender, MemberChangeArgs e)
        {
            float textureScale = GetTextureScale();

            if (textureScale != 0)
            {
                string memberChanged = e.Member;
                MessageBox.Show("Setting " + memberChanged + " when TextureScale is not 0 may result in the " + 
                    memberChanged + " value not applying");
            }
        }

        private float GetTextureScale()
        {
            float textureScale = 0;
            NamedObjectSave nos = Instance as NamedObjectSave;

            var instruction = nos.GetInstructionFromMember("TextureScale");
            if (instruction != null && instruction.Value != null)
            {
                if (instruction.Value is int)
                {
                    textureScale = (float)((int)instruction.Value);
                }
                else
                {
                    textureScale = (float)instruction.Value;
                }
            }
            return textureScale;
        }




        private bool IsMemberInTypedReferenceList(string memberName, List<TypedMemberBase> typedMembers)
        {
            if (typedMembers != null)
            {
                foreach (var tmb in typedMembers)
                {
                    if (tmb.MemberName == memberName)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public static void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value)
        {

            if (memberType != null &&
                value is string &&
                memberType != typeof(Microsoft.Xna.Framework.Color) &&
                !CustomVariableExtensionMethods.GetIsFile(memberType) // If it's a file, we just want to set the string value and have the underlying system do the loading                            
                )
            {
                bool isCsv = NamedObjectPropertyGridDisplayer.GetIfIsCsv(nos, memberName);
                bool shouldConvertValue = !isCsv &&
                    memberType != typeof(object) &&
                    // variable could be an object
                    memberType != typeof(PositionedObject);
                // If the MemberType is object, then it's something we can't convert to - it's likely a state
                if (shouldConvertValue)
                {
                    value = PropertyValuePair.ConvertStringToType((string)value, memberType);
                }
            }

            nos.SetPropertyValue(memberName, value);
        }

        private Type ConvertListTypes(Type memberType)
        {
            if (memberType == typeof(IList<FlatRedBall.Math.Geometry.Point>))
            {
                memberType = typeof(List<FlatRedBall.Glue.FormHelpers.PropertyGrids.Vector2WithProperties>);
            }

            return memberType;
        }

        // Made public for testing
        public static bool GetIfIsCsv(NamedObjectSave instance, string memberName)
        {
            bool isCsv = false;
            // Wait!  Is this thing a CSV?  If so, we just want to use the string value and not try to translate:
            if (instance.SourceType == SaveClasses.SourceType.Entity && !string.IsNullOrEmpty(instance.SourceClassType))
            {
                EntitySave entity = ObjectFinder.Self.GetEntitySave(instance.SourceClassType);

                if (entity != null)
                {
                    CustomVariable customVariable = entity.GetCustomVariableRecursively(memberName);

                    // This may not actually be a CustomVariable, but instead could be an Object that is SetByContainer.
                    // Therefore we should tolerate a null:
                    if (customVariable != null)
                    {
                        isCsv = customVariable.GetIsCsv();
                    }
                }
            }
            return isCsv;
        }

        public bool UpdateToState(IGlueState glueState)
        {
            bool didInstanceChange = glueState.CurrentNamedObjectSave != this.CurrentNamedObject;

            mGlueStateSnapshot.SetFrom(glueState);

            mInstance = glueState.CurrentNamedObjectSave;
            UpdateIncludedAndExcluded(mInstance as NamedObjectSave);

            base.Instance = mInstance;


            return didInstanceChange;
        }

        private void UpdateIncludedAndExcluded(NamedObjectSave instance)
        {
            ////////////////////Early Out/////////////////////////
            if (instance == null)
            {
                return;
            }
            ///////////////////End Early Out///////////////////////
            ResetToDefault();

            ExcludeAndIncludeGlueVariables(instance);
            
        }

        private void ExcludeAndIncludeGlueVariables(NamedObjectSave instance)
        {

            bool shouldIncludeSourceClassType = true;
            bool shouldIncludeSourceFile = true;
            bool shouldIncludeSourceName = true;
            bool shouldIncludeSourceClassGenericType = true;
            bool shouldShowCurrentState = true;
            bool shouldIncludeIncludeInIVisible = true;
            bool shouldIncludeIncludeInIClickable = true;
            bool shouldIncludeIsContainer = true;
            bool shouldShowIsZBuffered = false;
            bool shouldIncludeSetByContainer = true;

            ExcludeMember("InstructionSaves");
            ExcludeMember("Properties");
            ExcludeMember("FileCreatedBy");
            ExcludeMember("FulfillsRequirement");
            ExcludeMember("IsNodeHidden");


            if (DisplayMode == DisplayModes.VariablesOnly)
            {
                ExcludeAllMembers();
            }
            else
            {

                if (DisplayMode != DisplayModes.Debug)
                {
                    ExcludeMember("InstantiatedByBase");
                }

                var containerType = instance.GetContainerType();

                // Screens can't be IVisible/IClickable so no need to show these properties
                // in screens
                shouldIncludeIncludeInIVisible = containerType == ContainerType.Entity;
                shouldIncludeIncludeInIClickable = containerType == ContainerType.Entity;

                bool shouldShowAttachToContainer = containerType == ContainerType.Entity &&
                    instance.IsList == false;
                // Not sure if we want to keep this or not, but currently objects in Entities can't be attached to the Camera
                bool shouldShowAttachToCamera = instance.GetContainerType() == ContainerType.Screen;

                if (!shouldShowAttachToContainer)
                {
                    this.ExcludeMember("AttachToContainer");
                }


                if (!shouldShowAttachToCamera)
                {
                    ExcludeMember("AttachToCamera");
                }

                if (instance.SourceType == SaveClasses.SourceType.FlatRedBallType && instance.SourceClassType == "Layer")
                {

                }

                // We used to not show the AddToManagers property for objects inside Entities, but as I worked on SteamBirds
                // I found myself needing it.
                //else if (ContainerType == ContainerType.Entity)
                //{
                //    ExcludeMember("AddToManagers");
                //}

                if (instance.SetByDerived)
                {
                    ExcludeMember("AttachToContainer");
                }
                if (instance.InstantiatedByBase)
                {
                    ExcludeMember("SourceType");
                    shouldIncludeSourceClassType = false;
                    shouldIncludeSourceClassGenericType = false;

                    ExcludeMember("InstanceName");
                    ExcludeMember("CallActivity");
                    ExcludeMember("IgnoresPausing");
                    ExcludeMember("HasPublicProperty");
                    ExcludeMember("ExposedInDerived");

                    ExcludeMember("SetByDerived");
                    ExcludeMember("SetByContainer");
                }

                bool shouldIncludeAddToManagers = !instance.InstantiatedByBase && !instance.IsList;
                if (!shouldIncludeAddToManagers)
                {
                    ExcludeMember("AddToManagers");
                }

                UpdateLayerIncludeAndExclude(instance);

                if (containerType != ContainerType.Entity)
                {
                    shouldIncludeIsContainer = false;
                    shouldIncludeSetByContainer = false;
                }

                #region Camera-related properties

                if (instance.SourceType != SaveClasses.SourceType.FlatRedBallType || instance.SourceClassType != "Camera")
                {
                    ExcludeMember("IsNewCamera");
                }

                #endregion

                #region Text-related properties

                if (instance.SourceType != SaveClasses.SourceType.FlatRedBallType || instance.SourceClassType != "Text")
                {
                    ExcludeMember("IsPixelPerfect");
                }

                #endregion

                
                                        // we don't show this on files because Sprites from file will be put on the z buffer according to the
                                        // file.
                shouldShowIsZBuffered = instance.SourceType == SourceType.FlatRedBallType &&
                    (instance.SourceClassType != "Sprite" || instance.SourceClassType != "SpriteFrame");


                #region Remove based off of SourceType

                if (instance.SourceType == SourceType.FlatRedBallType)
                {
                    shouldShowCurrentState = false;
                    shouldIncludeSourceFile = false;
                    shouldIncludeSourceName = false;

                    if (!instance.IsGenericType)
                    {
                        shouldIncludeSourceClassGenericType = false;
                    }
                }
                else if (instance.SourceType == SourceType.File)
                {
                    shouldShowCurrentState = false;
                    shouldIncludeSourceClassType = false;
                    shouldIncludeSourceClassGenericType = false;

                }
                else if (instance.SourceType == SourceType.Entity)
                {
                    shouldIncludeSourceFile = false;
                    shouldIncludeSourceName = false;
                    shouldIncludeSourceClassGenericType = false;



                    shouldShowCurrentState = DetermineIfShouldShowStates(instance);
                }

                #endregion


                if (shouldIncludeSourceClassType)
                {
                    IncludeMember("SourceClassType", typeof(NamedObjectSave), new AvailableClassTypeConverter(instance));
                }
                else
                {
                    ExcludeMember("SourceClassType");
                }

                if (shouldIncludeSourceFile)
                {
                    IncludeMember("SourceFile", typeof(NamedObjectSave), new AvailableFileStringConverter(CurrentElement));
                }
                else
                {
                    ExcludeMember("SourceFile");
                }


                if (shouldIncludeSourceName)
                {
                    IncludeMember("SourceName", typeof(NamedObjectSave), new AvailableNameablesStringConverter(instance));
                }
                else
                {
                    ExcludeMember("SourceName");
                }

                if (shouldIncludeSourceClassGenericType)
                {
                    IncludeMember("SourceClassGenericType", typeof(NamedObjectSave), new AvailableClassGenericTypeConverter());
                }
                else
                {
                    ExcludeMember("SourceClassGenericType");
                }

                if (shouldShowCurrentState)
                {
                    IncludeMember("CurrentState", typeof(NamedObjectSave),
                        new AvailableStates(CurrentNamedObject, CurrentElement, CurrentCustomVariable, CurrentStateSave));
                }
                else
                {
                    ExcludeMember("CurrentState");
                }

                if (!shouldIncludeIncludeInIClickable)
                {
                    ExcludeMember("IncludeInIClickable");
                }
                if (!shouldIncludeIncludeInIVisible)
                {
                    ExcludeMember("IncludeInIVisible");
                }

                if (!shouldShowIsZBuffered)
                {
                    ExcludeMember("IsZBuffered");
                }
                if (!shouldIncludeSetByContainer)
                {
                    ExcludeMember("SetByContainer");
                }
                //else if (this.SourceType == SourceType.SetByParentContainer)
                //{
                //    ExcludeMember("SourceFile");
                //    ExcludeMember("SourceName");
                //    ExcludeMember("SourceClassGenericType");
                //    ExcludeMember("AddToManagers");
                //}
            }
        }

        private void UpdateLayerIncludeAndExclude(NamedObjectSave instance)
        {
            bool shouldIncludeLayerOn = !instance.IsLayer && !instance.IsList;

            if (shouldIncludeLayerOn)
            {
                IncludeMember("LayerOn", typeof(NamedObjectSave), new AvailableLayersTypeConverter(CurrentElement), CategoryAttribute("Layer"));
            }
            else
            {
                ExcludeMember("LayerOn");
            }


            if (instance.IsLayer)
            {
                

                IncludeMember("IndependentOfCamera", containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                if (instance.IndependentOfCamera)
                {

                    IncludeMember(memberToInclude: "Is2D", containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                    if (!instance.Is2D)
                    {
                        ExcludeMember("DestinationRectangle");
                        ExcludeMember("LayerCoordinateType");

                    }
                    else
                    {
                        IncludeMember("DestinationRectangle", containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                        IncludeMember("LayerCoordinateUnit", containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                        IncludeMember("LayerCoordinateType", containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                    }
                }
                else
                {
                    ExcludeMember("Is2D");
                }
            }
            else
            {
                ExcludeMember("IndependentOfCamera");
                ExcludeMember("Is2D");
                ExcludeMember("DestinationRectangle");
                ExcludeMember("LayerCoordinateType");
                ExcludeMember("LayerCoordinateUnit");
            }
        }

        private bool DetermineIfShouldShowStates(NamedObjectSave instance)
        {
            IElement referencedEntitySave = instance.GetReferencedElement();

            bool shouldRemove = referencedEntitySave == null;

            if (referencedEntitySave != null)
            {
                shouldRemove = true;

                IElement element = referencedEntitySave;

                while (element != null)
                {
                    if (element.States.Count != 0)
                    {
                        shouldRemove = false;
                        break;
                    }
                    else
                    {
                        element = ObjectFinder.Self.GetIElement(element.BaseElement);
                    }
                }
            }

            return !shouldRemove;

        }

        #endregion

    }
}
