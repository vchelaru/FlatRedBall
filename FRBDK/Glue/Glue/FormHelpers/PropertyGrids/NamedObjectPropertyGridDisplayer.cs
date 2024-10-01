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
using FlatRedBall.Glue.Plugins.ExportedImplementations;

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

        static Plugins.ExportedInterfaces.GlueStateSnapshot mGlueStateSnapshot;

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

        public GlueElement CurrentElement => mGlueStateSnapshot.CurrentElement; 

        public NamedObjectSave CurrentNamedObject => mGlueStateSnapshot.CurrentNamedObjectSave;

        public CustomVariable CurrentCustomVariable => mGlueStateSnapshot.CurrentCustomVariable;

        public StateSave CurrentStateSave => mGlueStateSnapshot.CurrentStateSave;

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
            mGlueStateSnapshot = new Plugins.ExportedInterfaces.GlueStateSnapshot();
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

            var instruction = nos.GetCustomVariable("TextureScale");
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
            if (instance == null || GlueState.Self.CurrentGlueProject == null)
            {
                return;
            }
            ///////////////////End Early Out///////////////////////
            ResetToDefault();

            ExcludeAndIncludeGlueVariables(instance);
            
        }

        private void ExcludeAndIncludeGlueVariables(NamedObjectSave instance)
        {
            var glueVersion = GlueState.Self.CurrentGlueProject.FileVersion;

            bool shouldIncludeSourceClassType = true;
            bool shouldIncludeSourceFile = true;
            bool shouldIncludeSourceName = true;
            bool shouldIncludeSourceClassGenericType = true;
            bool shouldShowCurrentState = true;
            bool shouldIncludeIncludeInIClickable = true;
            bool shouldIncludeIncludeInICollidable = true;
            bool shouldIncludeIsContainer = true;
            bool shouldShowIsZBuffered = false;
            bool shouldIncludeSetByContainer = true;
            bool shouldShowGenerateTimedEmit = false;
            bool shouldIncludeIsManuallyUpdated = false;

            if(glueVersion < (int)GlueProjectSave.GluxVersions.ListsHaveAssociateWithFactoryBool)
            {
                ExcludeMember(nameof(NamedObjectSave.AssociateWithFactory));
            }

            ExcludeMember(nameof(NamedObjectSave.InstructionSaves));
            ExcludeMember(nameof(NamedObjectSave.Properties));
            ExcludeMember(nameof(NamedObjectSave.IsNodeHidden));

            ForcedReadOnlyProperties.Add(nameof(NamedObjectSave.DefinedByBase));
            ForcedReadOnlyProperties.Add(nameof(NamedObjectSave.InstantiatedByBase));


            var assetTypeInfo = instance.GetAssetTypeInfo();

            shouldIncludeIsManuallyUpdated = !string.IsNullOrEmpty(assetTypeInfo?.AddManuallyUpdatedMethod);

            if (DisplayMode == DisplayModes.VariablesOnly)
            {
                ExcludeAllMembers();
            }
            else
            {

                // The property window is now "debug enough" that we should show this.
                //if (DisplayMode != DisplayModes.Debug)
                //{
                //    ExcludeMember(nameof(NamedObjectSave.InstantiatedByBase));
                //}

                var containerType = instance.GetContainerType();

                shouldIncludeIncludeInIClickable = containerType == ContainerType.Entity;
                if(assetTypeInfo != null && assetTypeInfo.HasCursorIsOn == false)
                {
                    shouldIncludeIncludeInIClickable = false;
                }

                // shapes don't implement ICollidable, but they have collision:
                bool isShape = assetTypeInfo?.QualifiedRuntimeTypeName.QualifiedType?.StartsWith("FlatRedBall.Math.Geometry") ?? false;
                if (assetTypeInfo != null && assetTypeInfo.ImplementsICollidable == false && !isShape)
                {
                    shouldIncludeIncludeInICollidable = false;
                }



                bool shouldShowAttachToContainer = containerType == ContainerType.Entity &&
                    instance.IsList == false;
                // Not sure if we want to keep this or not, but currently objects in Entities can't be attached to the Camera
                bool shouldShowAttachToCamera = instance.GetContainerType() == ContainerType.Screen;

                if (!shouldShowAttachToContainer)
                {
                    this.ExcludeMember(nameof(NamedObjectSave.AttachToContainer));
                }


                if (!shouldShowAttachToCamera)
                {
                    ExcludeMember(nameof(NamedObjectSave.AttachToCamera));
                }

                if (instance.SourceType == SaveClasses.SourceType.FlatRedBallType && instance.GetAssetTypeInfo()?.FriendlyName == "Layer")
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
                    ExcludeMember(nameof(NamedObjectSave.AttachToContainer));
                }
                if (instance.InstantiatedByBase)
                {
                    ExcludeMember(nameof(NamedObjectSave.SourceType));
                    shouldIncludeSourceClassType = false;
                    shouldIncludeSourceClassGenericType = false;

                    ExcludeMember(nameof(NamedObjectSave.InstanceName));
                    ExcludeMember(nameof(NamedObjectSave.CallActivity));
                    ExcludeMember(nameof(NamedObjectSave.IgnoresPausing));
                    ExcludeMember(nameof(NamedObjectSave.HasPublicProperty));
                    ExcludeMember(nameof(NamedObjectSave.ExposedInDerived));

                    ExcludeMember(nameof(NamedObjectSave.SetByDerived));
                    ExcludeMember(nameof(NamedObjectSave.SetByContainer));
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

                if (instance.GetAssetTypeInfo() != AvailableAssetTypes.CommonAtis.Camera)
                {
                    ExcludeMember(nameof(NamedObjectSave.IsNewCamera));
                }

                #endregion

                #region Text-related properties

                if (instance.SourceType != SaveClasses.SourceType.FlatRedBallType || instance.GetAssetTypeInfo() != AvailableAssetTypes.CommonAtis.Text)
                {
                    ExcludeMember(nameof(NamedObjectSave.IsPixelPerfect));
                }

                #endregion

                
                                        // we don't show this on files because Sprites from file will be put on the z buffer according to the
                                        // file.
                shouldShowIsZBuffered = instance.SourceType == SourceType.FlatRedBallType &&
                    (instance.SourceClassType != "Sprite" || instance.SourceClassType != "SpriteFrame");

                shouldShowGenerateTimedEmit = 
                    (instance.SourceType == SourceType.FlatRedBallType && instance.SourceClassType == "Emitter") ||
                    (instance.SourceType == SourceType.File && instance.ClassType == "Emitter");

                #region Remove based off of SourceType

                if (instance.SourceType == SourceType.FlatRedBallType || instance.SourceType == SourceType.Gum)
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
                    IncludeMember(nameof(NamedObjectSave.SourceClassType), typeof(NamedObjectSave), new AvailableClassTypeConverter(instance));
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.SourceClassType));
                }

                if (shouldIncludeSourceFile)
                {
                    IncludeMember(nameof(NamedObjectSave.SourceFile), typeof(NamedObjectSave), new AvailableFileStringConverter(CurrentElement));
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.SourceFile));
                }

                if(shouldShowGenerateTimedEmit)
                {
                    IncludeMember(nameof(instance.GenerateTimedEmit), typeof(NamedObjectSave));
                }
                else
                {
                    ExcludeMember(nameof(instance.GenerateTimedEmit));
                }

                if (shouldIncludeSourceName)
                {
                    IncludeMember(nameof(NamedObjectSave.SourceName), typeof(NamedObjectSave), new AvailableNameablesStringConverter(instance, null));
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.SourceName));
                }

                if (shouldIncludeSourceClassGenericType)
                {
                    IncludeMember(nameof(NamedObjectSave.SourceClassGenericType), typeof(NamedObjectSave), new AvailableClassGenericTypeConverter());
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.SourceClassGenericType));
                }

                if (shouldShowCurrentState)
                {
                    IncludeMember(nameof(NamedObjectSave.CurrentState), typeof(NamedObjectSave),
                        new AvailableStates(CurrentNamedObject, CurrentElement, CurrentCustomVariable, CurrentStateSave));
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.CurrentState));
                }

                if (!shouldIncludeIncludeInIClickable)
                {
                    ExcludeMember(nameof(NamedObjectSave.IncludeInIClickable));
                }
                if(!shouldIncludeIncludeInICollidable)
                {
                    ExcludeMember(nameof(NamedObjectSave.IncludeInICollidable));
                }

                if (!shouldShowIsZBuffered)
                {
                    ExcludeMember(nameof(NamedObjectSave.IsZBuffered));
                }
                if (!shouldIncludeSetByContainer)
                {
                    ExcludeMember(nameof(NamedObjectSave.SetByContainer));
                }

                if(shouldIncludeIsManuallyUpdated == false)
                {
                    ExcludeMember(nameof(NamedObjectSave.IsManuallyUpdated));
                }
            }
        }

        private void UpdateLayerIncludeAndExclude(NamedObjectSave instance)
        {
            bool shouldIncludeLayerOn = !instance.IsLayer && !instance.IsList;

            if (shouldIncludeLayerOn)
            {
                IncludeMember(nameof(NamedObjectSave.LayerOn), typeof(NamedObjectSave), new AvailableLayersTypeConverter(CurrentElement), 
                    CategoryAttribute("Layer"));
            }
            else
            {
                ExcludeMember(nameof(NamedObjectSave.LayerOn));
            }


            if (instance.IsLayer)
            {
                IncludeMember(nameof(NamedObjectSave.IndependentOfCamera), containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                if (instance.IndependentOfCamera)
                {

                    IncludeMember(memberToInclude: nameof(NamedObjectSave.Is2D), containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                    if (!instance.Is2D)
                    {
                        ExcludeMember(nameof(NamedObjectSave.DestinationRectangle));
                        ExcludeMember(nameof(NamedObjectSave.LayerCoordinateType));

                    }
                    else
                    {
                        IncludeMember(nameof(NamedObjectSave.DestinationRectangle), containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                        IncludeMember(nameof(NamedObjectSave.LayerCoordinateUnit), containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                        IncludeMember(nameof(NamedObjectSave.LayerCoordinateType), containingType: typeof(NamedObjectSave), attributes: CategoryAttribute("Layer"));
                    }
                }
                else
                {
                    ExcludeMember(nameof(NamedObjectSave.Is2D));
                }
            }
            else
            {
                ExcludeMember(nameof(NamedObjectSave.IndependentOfCamera));
                ExcludeMember(nameof(NamedObjectSave.Is2D));
                ExcludeMember(nameof(NamedObjectSave.DestinationRectangle));
                ExcludeMember(nameof(NamedObjectSave.LayerCoordinateType));
                ExcludeMember(nameof(NamedObjectSave.LayerCoordinateUnit));
            }
        }

        private bool DetermineIfShouldShowStates(NamedObjectSave instance)
        {
            GlueElement referencedEntitySave = instance.GetReferencedElement();

            bool shouldRemove = referencedEntitySave == null;

            if (referencedEntitySave != null)
            {
                shouldRemove = true;

                GlueElement element = referencedEntitySave;

                while (element != null)
                {
                    if (element.States.Count != 0)
                    {
                        shouldRemove = false;
                        break;
                    }
                    else
                    {
                        element = ObjectFinder.Self.GetElement(element.BaseElement);
                    }
                }
            }

            return !shouldRemove;

        }

        #endregion

    }
}
