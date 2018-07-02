using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;

using FlatRedBall.Glue.Elements;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using System.Reflection;
using Microsoft.Xna.Framework;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Localization;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Glue.RuntimeObjects;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Parsing;
//using GlueView.Reflection;
using FlatRedBall.IO.Csv;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Instructions;
using FlatRedBall.Math.Splines;

namespace FlatRedBall.Glue
{
    #region VariableSetArgs class
    public class VariableSetArgs : EventArgs
    {
        public string VariableName;
        public object Value;

        public override string ToString()
        {
            return VariableName + " = " + Value;
        }
    }
    #endregion

    #region VariableGetType enum

    public enum VariableGetType
    {
        DefinedInIElement,
        AsExistsAtRuntime
    }

    #endregion

    public enum VariableSettingOptions
    {
        LiteralSet,
        TreatAbsoluteAsRelativeIfAttached
    }

    public class CreationOptions
    {
        public EventHandler<VariableSetArgs> OnBeforeVariableSet { get; set; }
        public EventHandler<VariableSetArgs> OnAfterVariableSet { get; set; }
        public Layer LayerProvidedByContainer { get; set; }

    }


    public partial class ElementRuntime : PositionedObject, IVisible, IClickable
    {
        #region Fields

        ReferencedFileRuntimeList mReferencedFileRuntimeList = new ReferencedFileRuntimeList();

        Dictionary<string, Scene> mEntireScenes;
        Dictionary<string, ShapeCollection> mEntireShapeCollections;
        Dictionary<string, NodeNetwork> mEntireNodeNetworks;
        Dictionary<string, EmitterList> mEntireEmitterLists;
        Dictionary<string, SplineList> mEntireSplineLists;

        PositionedObjectList<ElementRuntime> mContainedElements;
        PositionedObjectList<ElementRuntime> mElementsInList;
        IElement mAssociatedIElement;
        NamedObjectSave mAssociatedNamedObjectSave;

        /// <summary>
        /// The object that the ElementRuntime wraps. This can be a primitive type like a Sprite or a Text object.
        /// </summary>
        object mDirectObjectReference;
        String mCurrentStateName;
        

        Layer mLayer;

        List<PropertyValuePair> mCustomVariables;

        string ContentManagerName;

        #endregion

        #region Properties

        internal CreationOptions CreationOptions { get; private set;}


        public bool IgnoresParentVisibility
        {
            get;
            set;
        }

        IVisible IVisible.Parent
        {
            get
            {
                return mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return Visible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public Layer Layer
        {
            get { return mLayer; }
        }

        public ReferencedFileRuntimeList ReferencedFileRuntimeList
        {
            get
            {
                return mReferencedFileRuntimeList;
            }
        }

        public List<PropertyValuePair> CustomVariables
        {
            get
            {
                return mCustomVariables;
            }
        }

        public IElement AssociatedIElement
        {
            get { return mAssociatedIElement; }
        }

        public NamedObjectSave AssociatedNamedObjectSave
        {
            get { return mAssociatedNamedObjectSave; }
        }

        public static string ContentDirectory
        {
            get
            {
                //GlueProjectSave glueProjectSave = ObjectFinder.GlueProject;
                if (!string.IsNullOrEmpty(GluxManager.AlternativeContentDirectory))
                {
                    return GluxManager.AlternativeContentDirectory;
                }
                else
                {

                    return GluxManager.ContentDirectory;
                }
            }
        }


        public PositionedObjectList<ElementRuntime> ContainedElements
        {
            get { return mContainedElements; }
        }

        public PositionedObjectList<ElementRuntime> ElementsInList
        {
            get { return mElementsInList; }
        }

        public Dictionary<string, Scene> EntireScenes
        {
            get { return mEntireScenes; }
        }

        public Dictionary<string, ShapeCollection> EntireShapeCollections
        {
            get { return mEntireShapeCollections; }
        }

        public Dictionary<string, EmitterList> EntireEmitterLists
        {
            get
            {
                return mEntireEmitterLists;
            }
        }

        public Dictionary<string, NodeNetwork> EntireNodeNetworks
        {
            get { return mEntireNodeNetworks; }
        }

        public Dictionary<string, SplineList> EntireSplineLists
        {
            get
            {
                return mEntireSplineLists;
            }
        }

        public object DirectObjectReference
        {
            get { return mDirectObjectReference; }
            internal set
            {
                mDirectObjectReference = value;
            }
        }

        bool mVisible = true;

        public bool Visible
        {
            set
            {
                mVisible = value;

                // yay for IVisible!  No need to set visible anything that is IVisible

                //foreach (Scene scene in ReferencedFileRuntimeList.LoadedScenes)
                //{
                //    scene.Visible = value;
                //}

                //foreach (ShapeCollection sh in ReferencedFileRuntimeList.LoadedShapeCollections)
                //{
                //    sh.Visible = value;
                //}

                foreach (NodeNetwork nn in ReferencedFileRuntimeList.LoadedNodeNetworks)
                {
                    nn.Visible = value;
                }

                if (mAssociatedNamedObjectSave != null)
                {
                    if (mAssociatedNamedObjectSave.IncludeInIVisible)
                    {
                        if (mDirectObjectReference != null)
                        {
                            Type t = mDirectObjectReference.GetType();
                            t.GetProperty("Visible").SetValue(mDirectObjectReference, value, null);
                        }
                    }
                }

                //for (int i = 0; i < mContainedElements.Count; i++)
                //{
                //    ElementRuntime er = mContainedElements[i];

                //    er.Visible = value;
                //}

                //for (int i = 0; i < mElementsInList.Count; i++)
                //{
                //    ElementRuntime er = mElementsInList[i];

                //    er.Visible = value;
                //}

                
            }
            get
            {
                return mVisible;
            }
        }

        public string FieldName
        {
            get
            {
                return mAssociatedNamedObjectSave != null ? mAssociatedNamedObjectSave.FieldName : String.Empty;
            }
        }

        public string ContainerName
        {
            get
            {
                if (mAssociatedNamedObjectSave != null)
                {
                    var container = mAssociatedNamedObjectSave.GetContainer();
                    if (container != null) return container.Name;
                }

                return String.Empty;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event which gets raised after each variable is applied on the runtime object.  This can be handled
        /// to apply after variable set events.
        /// </summary>
        public EventHandler<VariableSetArgs> BeforeVariableApply;
        
        /// <summary>
        /// Event which gets raised after each variable is applied on the runtime object.  This can be handled
        /// to apply after variable set events.
        /// </summary>
        public EventHandler<VariableSetArgs> AfterVariableApply;

        #endregion

        #region Methods

        //public ElementRuntime(IElement elementSave, Layer layerToPutOn, NamedObjectSave namedObjectSave) : this(elementSave, layerToPutOn, namedObjectSave, null, null)
        //{



        //}

        public ElementRuntime(IElement elementSave, Layer layerProvidedByContainer, NamedObjectSave namedObjectSave, EventHandler<VariableSetArgs> onBeforeVariableSet, EventHandler<VariableSetArgs> onAfterVariableSet)
        {
            CreationOptions = new CreationOptions();
            CreationOptions.OnBeforeVariableSet = onBeforeVariableSet;
            CreationOptions.OnAfterVariableSet = onAfterVariableSet;
            CreationOptions.LayerProvidedByContainer = layerProvidedByContainer;

            this.mLayer = CreationOptions.LayerProvidedByContainer;

            if (CreationOptions.OnBeforeVariableSet != null)
            {
                this.BeforeVariableApply += CreationOptions.OnBeforeVariableSet;
            }
            if (CreationOptions.OnAfterVariableSet != null)
            {
                this.AfterVariableApply += CreationOptions.OnAfterVariableSet;
            }

            ContentManagerName = GluxManager.ContentManagerName;

            mAssociatedIElement = elementSave;
            mAssociatedNamedObjectSave = namedObjectSave;

            InstantiateLists();

            if (elementSave != null)
            {
                this.Name = elementSave.Name;
                try
                {
                    LoadReferencedFiles(elementSave);
                }
                catch(Exception e)
                {
                    throw new Exception("Error loading referenced files for " + elementSave + ":\n\n" + e.ToString());
                }
                LoadNamedObjects(elementSave);
                // October 6, 2011
                // Victor Chelaru
                // In the earlier days
                // of GlueView, we would
                // set variables defind at
                // in the container before we
                // set instance variables.  Therefore
                // the code would call LoadCustomVariables
                // before SetInstanceVariablesOnNamedObjects.
                // Now Glue forces both instance and custom variables
                // to be the same so order doesn't matter; however, initial
                // states are treated as variables and those should be set after
                // instance variables are set.  Therefore, I'm going to switch the
                // order that the following two methods are called.
                //LoadCustomVariables(elementSave);
                //SetInstanceVariablesOnNamedObjects();
                SetInstanceVariablesOnNamedObjects();

                CreateCustomVariableContainers(elementSave);
                SetCustomVariables(elementSave);
            }

        }

        private void InstantiateLists()
        {
            mElementsInList = new PositionedObjectList<ElementRuntime>();
            mContainedElements = new PositionedObjectList<ElementRuntime>();

            mEntireScenes = new Dictionary<string, Scene>();
            mEntireShapeCollections = new Dictionary<string, ShapeCollection>();
            mEntireNodeNetworks = new Dictionary<string, NodeNetwork>();
            mEntireEmitterLists = new Dictionary<string, EmitterList>();
            mEntireSplineLists = new Dictionary<string, SplineList>();
            mCustomVariables = new List<PropertyValuePair>();
        }

        #region Referenced File Functions

        private void LoadReferencedFiles(IElement elementSave)
        {
            foreach (ReferencedFileSave r in elementSave.ReferencedFiles)
            {
                mReferencedFileRuntimeList.LoadReferencedFileSave(r, mAssociatedIElement);
            }

            if (elementSave.BaseElement != null && elementSave.BaseElement != "")
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(elementSave.BaseElement);
                if (baseElement != null)
                {
                    LoadReferencedFiles(baseElement);
                }
            }
        }


        public object LoadReferencedFileSave(ReferencedFileSave r)
        {
            return mReferencedFileRuntimeList.LoadReferencedFileSave(r, mAssociatedIElement);
        }

        public object LoadReferencedFileSave(ReferencedFileSave r, bool isBeingAccessed, IElement container)
        {
            return mReferencedFileRuntimeList.LoadReferencedFileSave(r, isBeingAccessed, container);
        }

        #endregion


        #region NamedObject Functions

        private void LoadNamedObjects(IElement elementSave)
        {
            if (elementSave == null)
            {
                throw new ArgumentNullException("Argument elementSave is null", "elementSave");
            }

            elementSave.UpdateCustomProperties();

            var layers = elementSave.AllNamedObjects.Where(item=>item.IsLayer);
            var entireFiles = elementSave.AllNamedObjects.Where(item => item.IsEntireFile);
            var everythingElse = elementSave.AllNamedObjects.Where(item => !item.IsLayer && !item.IsEntireFile);

            var ordered = layers.Concat(entireFiles).Concat(everythingElse);

            PositionedObjectList<ElementRuntime> listToPopulate = mContainedElements;
            PositionedObject parentElementRuntime = this;

            CreateNamedObjectElementRuntime(elementSave, CreationOptions.LayerProvidedByContainer, ordered.ToList(), listToPopulate, parentElementRuntime);

            LoadEmbeddedNamedObjects(elementSave, CreationOptions.LayerProvidedByContainer);

            if (elementSave.InheritsFromElement())
            {
                var elementSaveBaseElement = ObjectFinder.Self.GetIElement(elementSave.BaseElement);
                if (elementSaveBaseElement != null)
                {
                    LoadNamedObjects(elementSaveBaseElement);
                }
            }
        }

        private void LoadEmbeddedNamedObjects(IElement element, Layer layerProvidedByContainer)
        {
            foreach (NamedObjectSave nos in element.NamedObjects.Where(item=>!item.IsLayer && item.IsEntireFile))
            {
                CreateNamedObjectElementRuntime(element, layerProvidedByContainer,
                    nos.ContainedObjects, this.mElementsInList, this);
            }
        }


        public static bool ShouldElementRuntimeBeCreatedForNos(NamedObjectSave n, IElement container)
        {
            bool returnValue = true;



            // We used to create elements that were DefinedByBase, but now
            // we don't want to do that because GlueView will recursively loop
            // through all base objects and create objects for everything defined
            // in base.  If we don't check for DefinedByBase, we get duplicate objects
            // created.
            // Update June 1, 2013
            // If it's DefinedByBase, we
            // should only create it if it's
            // SetByDerived
            if (n.SetByDerived || n.IsDisabled)
            {
                returnValue = false;
            }
            if (n.DefinedByBase)
            {
                // We need to find the defining NOS, and see if it's SetByDerived is set to true
                var found = n.GetDefiningNamedObjectSave(container);
                if (found != null)
                {
                    returnValue = found.SetByDerived;
                }
            }

            return returnValue;
        }

        private static void DetachAndMoveParentToOrigin(PositionedObject asPositionedObject, PositionedObject parent, ref Vector3 oldParentPosition, ref Matrix oldParentRotation)
        {
            if (parent != null)
            {
                asPositionedObject.Detach();

                oldParentPosition = parent.Position;
                oldParentRotation = parent.RotationMatrix;

                parent.Position = new Vector3();

                if (parent is Camera)
                {
                    parent.Z = 40;
                }
                parent.RotationMatrix = Matrix.Identity;
            }
        }

        #region EntityObject Functions

        ElementRuntime LoadEntityObject(NamedObjectSave n, Layer layerToPutOn, PositionedObjectList<ElementRuntime> listToPopulate)
        {
            IElement entityElement = ObjectFinder.Self.GetEntitySave(n.SourceClassType);

            ElementRuntime newElement = new ElementRuntime(entityElement, layerToPutOn, n, CreationOptions.OnBeforeVariableSet, CreationOptions.OnAfterVariableSet);

            newElement.Name = n.InstanceName;

            listToPopulate.Add(newElement);
            SpriteManager.AddPositionedObject(newElement);


            return newElement;
        }

        #endregion



        #endregion


        #region Custom Variable Functions

        private void SetCustomVariables(IElement elementSave)
        {
            foreach (CustomVariable cv in elementSave.CustomVariables)
            {
                SetCustomVariable(cv, this.mAssociatedIElement, cv.DefaultValue, true);
            }
        }

        private void CreateCustomVariableContainers(IElement elementSave)
        {
            foreach (CustomVariable cv in elementSave.CustomVariables)
            {
                CreateCustomVariableContainer(cv);
            }
            if (!string.IsNullOrEmpty(elementSave.BaseElement))
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(elementSave.BaseElement);
                if (baseElement != null)
                {
                    CreateCustomVariableContainers(baseElement);
                }
            }
        }

        /// <summary>
        /// This method populates the mCustomVariables list with default valued variables.  These can later get set in SetCustomVariable, but they need to exist so that 
        /// plugins like the script parser know whether something is a custom variable or not.
        /// </summary>
        /// <param name="cv">The variable to evaluate and add if it is a custom variable.</param>
        private void CreateCustomVariableContainer(CustomVariable cv)
        {
            bool isCustomVariable = true;

            if(!string.IsNullOrEmpty(cv.SourceObject))
            {
                isCustomVariable = false;
            }

            if (isCustomVariable)
            {

                Type positionedObjectType = typeof(PositionedObject);

                PropertyInfo property = positionedObjectType.GetProperty(cv.Name);
                FieldInfo field = positionedObjectType.GetField(cv.Name);
                if (property != null || field != null)
                {
                    isCustomVariable = false;
                }
            }

            if (isCustomVariable)
            {
                EntitySave baseEntitySave = ObjectFinder.Self.GetEntitySave(this.AssociatedIElement.BaseElement);

                if (baseEntitySave != null && baseEntitySave.GetCustomVariable(cv.Name) != null)
                {
                    isCustomVariable = false;
                }
            }

            if (isCustomVariable)
            {
                object defaultValue = null;
                if (cv.GetIsFile())
                {
                    defaultValue = null;
                }
                    
                else if (cv.GetIsCsv())
                {
                    defaultValue = null;
                }
                else if (cv.GetIsVariableState())
                {
                    defaultValue = null;
                }
                else
                {
                    try
                    {
                        defaultValue = TypeManager.GetDefaultForTypeAsType(cv.Type);
                    }
                    catch
                    {
                        throw new Exception("Could not get the default value for variable " + cv);
                    }
                }

                mCustomVariables.Add(new PropertyValuePair(cv.Name, defaultValue));

            }
        }

        public void SetCustomVariable(CustomVariable cv)
        {
            SetCustomVariable(cv, this.mAssociatedIElement, cv.DefaultValue, true);
        }

        // made public for testing
        public void SetCustomVariable(CustomVariable cv, IElement container, object valueToSetTo, bool attachAndUnattach, 
            VariableSettingOptions settingOptions = VariableSettingOptions.TreatAbsoluteAsRelativeIfAttached)
        {
            //////////////////////////////////Early out/////////////////////////////////////
            if (valueToSetTo == null ||
                // May 28, 2012 - I hesitate to write this at first because I thought - what if the user wants to set something like
                // a texture to null.  GView should understand this. However, it seems like if we want to do that we should instead have
                // a specific value for it like <NONE> or <NULL>.
                (valueToSetTo is string && string.IsNullOrEmpty((string)valueToSetTo) && cv.Type != "String" && cv.Type != "string")
                
                
                )
            {
                // I need to 
                // fix this -
                // maybe this method
                // should never be called
                // when this is null?  Or maybe
                // this method should tolerate a
                // null value and do nothing?  Need
                // to see what calls this.

                // Update - I think we should do nothing 
                return;
            }
            //////////////////////////////////End early out/////////////////////////////////////

            
            
            
            // This converts the value if the user is overriding the type (like converting a string to an int for a score Text object)
            valueToSetTo = ConvertIfOverriding(cv, valueToSetTo);
            object untranslated = valueToSetTo;
            
            

            valueToSetTo = CastAndTranslateValueToSet(cv, valueToSetTo);

            ElementRuntime sourceElement;
            string variableName;
            GetSourceElementAndVariableName(cv, out sourceElement, out variableName);

            if (sourceElement != null)
            {
                object objectToSetOn;
                PositionedObject parent;
                GetObjectToSetOnAndParent(sourceElement, out objectToSetOn, out parent);
                if (objectToSetOn is PositionedObject && ((PositionedObject)objectToSetOn).Parent != null && settingOptions == VariableSettingOptions.TreatAbsoluteAsRelativeIfAttached)
                {
                    variableName = ConvertAbsoluteToRelative(variableName);

                }

                PropertyInfo property;
                FieldInfo field;
                bool hasFieldOrProperty;
                GetHasFieldOrProperty(variableName, objectToSetOn, out property, out field, out hasFieldOrProperty);

                if (hasFieldOrProperty)
                {
                    parent = SetFieldOrPropertyCustomVariable(cv, valueToSetTo, attachAndUnattach, container, sourceElement, objectToSetOn, parent, property, field);
                }
                else if (cv.SourceObject != null && cv.SourceObjectProperty != null) // It's tunneled!
                {
                    SetTunneledVariable(valueToSetTo, attachAndUnattach, untranslated, variableName, objectToSetOn, container,
                        container.GetNamedObjectRecursively(cv.SourceObject));
                }
                else if ( cv.DefaultValue is StateSave || 
                    (cv.GetIsVariableState(this.AssociatedIElement) && valueToSetTo is string))
                {
                    SetStateFromCustomVariable(cv, container, valueToSetTo);
                }
                else
                {
                    SetCustomVariableWithNoSourceObject(cv, valueToSetTo);
                }
            }
            else
            {
                SetCustomVariableWithNoSourceObject(cv, valueToSetTo);
            }
        }

        public void SetStateFromCustomVariable(CustomVariable cv, IElement container, object valueToSetTo)
        {
            // We need to see if the state exists, and not just call SetState
            // with the name of the state.  The reason is because if we call SetState
            // and the state is null, it will set all variables which will in turn set
            // the state for the state CustomVariable which again will be null and will 
            // infinitely repeat.
            StateSave stateToSet = GetStateSaveFromCustomVariableValue(cv, valueToSetTo);
            if (stateToSet != null)
            {
                if (valueToSetTo is string)
                {
                    mCurrentStateName = valueToSetTo as string;
                }
                else
                {
                    mCurrentStateName = stateToSet.Name;
                }
                const bool refreshPositions = false;
                SetState(stateToSet, refreshPositions, container);

                SetCustomVariableValue(cv.Name, stateToSet);
            }
        }

        public StateSave GetStateSaveFromCustomVariableValue(CustomVariable cv, object valueToSetTo)
        {
            StateSave stateToSet;
            if (cv.DefaultValue is StateSave)
            {
                stateToSet = cv.DefaultValue as StateSave;
            }
            else
            {
                stateToSet = GetStateRecursively(valueToSetTo as string, cv.Type);
            }
            return stateToSet;
        }

        private static void GetHasFieldOrProperty(string variableName, object objectToSetOn, out PropertyInfo property, out FieldInfo field, out bool hasFieldOrProperty)
        {
            Type elementType = objectToSetOn.GetType();

            property = null;
            field = null;



            property = elementType.GetProperty(variableName);
            field = elementType.GetField(variableName);

            hasFieldOrProperty = (property != null) || (field != null);
        }

        private void SetTunneledVariable(object valueToSetTo, bool attachAndUnattach, object untranslated, string variableName,
            object objectToSetOn, IElement container, NamedObjectSave nos)
        {
            if (objectToSetOn is ElementRuntime)
            {
                ElementRuntime asElementRuntime = objectToSetOn as ElementRuntime;
                CustomVariable containedVariable = asElementRuntime.GetCustomVariable(variableName);

                bool wasFound = false;

                if (containedVariable != null) //VIC!!!! This case needs to be handled. The contained variable might be null if I'm tunneling to an Entity which doesn't expose said variable.  
                {
                    // This method will internally translate the value.  This could cause double-translation
                    // so we want to just pass the raw here:
                    asElementRuntime.SetCustomVariable(containedVariable, container, untranslated, attachAndUnattach);
                    wasFound = true;
                }

                if (!wasFound && valueToSetTo is string)
                {
                    // don't want to translate states
                    StateSave stateSave = asElementRuntime.GetStateRecursively((string)untranslated);

                    if (stateSave != null)
                    {
                        wasFound = true;

                        asElementRuntime.SetState(stateSave, false, container);
                    }

                }
                else if (!wasFound && valueToSetTo is StateSave)
                {
                    StateSave stateSave = valueToSetTo as StateSave;

                    if (stateSave != null)
                    {
                        wasFound = true;

                        asElementRuntime.SetState(stateSave, false, container);
                    }
                }
            }
            else if (variableName == "SourceFile")
            {
                 // We're setting the source, which means it's setting the file new:
                if (ShouldElementRuntimeBeCreatedForNos(nos, container))
                {
                    Layer layerToPutOn = GetLayerForNos(this.mLayer, nos);

                    // Setting the source file should make the NOS pretend like it's loading from a particular
                    // file, so let's set the NOS variables temporarily.
                    // I'm not 100% certain this is the best approach, but so much code is integrated into the NOS
                    // and I don't want to duplicate that logic, or tear out the variables because it would make the
                    // methods have really long parameter lists (which they already do)
                    string className = nos.SourceClassType;

                    var oldSourceType = nos.SourceType;
                    var oldSourceFile = nos.SourceFile;
                    var oldSourceName = nos.SourceName;

                    string foundName = null;
                    // We need to convert the variable name to the source RFS
                    foreach (var rfs in container.ReferencedFiles)
                    {
                        if (rfs.GetInstanceName() == (string)valueToSetTo)
                        {
                            foundName = rfs.Name;
                        }
                    }

                    if (!string.IsNullOrEmpty(foundName))
                    {
                        nos.SourceType = SourceType.File;
                        nos.SourceFile = foundName;
                        nos.SourceName = "Entire File (" + className + ")";

                        LoadFileObject(nos, container, layerToPutOn,
                            this.mContainedElements);

                        nos.SourceType = oldSourceType;
                        nos.SourceFile = oldSourceFile;
                        nos.SourceName = oldSourceName;
                    }

                    //LoadFileObject(nos, container, layerToPutOn, 
                    //    mContainedElements, false);

                }
            }
        }

        private void SetCustomVariableWithNoSourceObject(CustomVariable cv, object valueToSetTo)
        {
            // This is just a simple custom variable.  We still need to handle this and raise events
            VariableSetArgs vse = new VariableSetArgs();
            vse.Value = valueToSetTo;
            vse.VariableName = cv.Name;

            if (BeforeVariableApply != null)
            {
                BeforeVariableApply(this, vse);
            }

            SetCustomVariableValue(cv.Name, valueToSetTo);

            if (AfterVariableApply != null)
            {
                AfterVariableApply(this, vse);
            }
        }

        private PositionedObject SetFieldOrPropertyCustomVariable(CustomVariable cv, object valueToSetTo, bool attachAndUnattach, IElement container, ElementRuntime sourceElement, object objectToSetOn, PositionedObject parent, PropertyInfo property, FieldInfo field)
        {
            if (objectToSetOn != sourceElement && sourceElement.mAssociatedNamedObjectSave.SourceType == SourceType.FlatRedBallType &&
                objectToSetOn is PositionedObject && attachAndUnattach)
            {
                parent = sourceElement;
            }

            // Not sure if this is the best place to put this, but let's replace "\\n" with "\n"
            if (valueToSetTo is string && ((string)valueToSetTo).Contains("\\n"))
            {
                valueToSetTo = ((string)valueToSetTo).Replace("\\n", "\n");
            }

            VariableSetArgs vse = new VariableSetArgs();
            vse.Value = valueToSetTo;
            vse.VariableName = cv.Name;

            if (BeforeVariableApply != null)
            {
                BeforeVariableApply(this, vse);
            }

            if (property != null)
            {
                //try
                //{
                    if (cv.GetIsFile())
                    {
                        object fileRuntime = null;

                        if (valueToSetTo is string)
                        {
                            ReferencedFileSave rfs = GetReferencedFileFromName(valueToSetTo);

                            if (rfs == null)
                            {
                                fileRuntime = null;
                            }
                            else
                            {
                                fileRuntime = LoadReferencedFileSave(rfs, true, container);
                            }
                        }
                        else
                        {
                            fileRuntime = valueToSetTo;
                        }

                        property.SetValue(objectToSetOn, fileRuntime, null);
                    }
                    else
                    {
                        object convertedValue = valueToSetTo;
                        if (property.PropertyType == typeof(Microsoft.Xna.Framework.Color) && valueToSetTo is string)
                        {
                            convertedValue = PropertyValuePair.ConvertStringToType((string)valueToSetTo, property.PropertyType);
                        }
                        if(property.PropertyType == typeof(IList<FlatRedBall.Math.Geometry.Point>))
                        {
                            // We may be storing vectors, so if so we need to convert
                            if (valueToSetTo != null && valueToSetTo is List<Vector2>)
                            {
                                List<FlatRedBall.Math.Geometry.Point> converted = new List<FlatRedBall.Math.Geometry.Point>();
                                foreach(var item in valueToSetTo as List<Vector2>)
                                {
                                    converted.Add(new Math.Geometry.Point(item.X, item.Y));
                                }
                                convertedValue = converted;
                            }
                        }
                        bool shouldSet = true;

                        if (convertedValue is string &&
                            (string.IsNullOrEmpty((string)convertedValue)) &&
                            (
                            property.PropertyType == typeof(float) ||
                            property.PropertyType == typeof(bool) ||
                            property.PropertyType == typeof(long) ||
                            property.PropertyType == typeof(double) ||
                            property.PropertyType == typeof(int)))
                        {
                            shouldSet = false;
                        }
                        if (shouldSet)
                        {
                            // It's possible that GlueView
                            // can set a bad value like float.NaN
                            // on an X which ultimately makes the engine
                            // crash hard!  If an exception occurs on a property
                            // set, then we need to catch it and undo the set, then
                            // throw an exception so that whoever set it can deal with
                            // the problem.
                            object oldValue = null;
                            if(property.CanRead)
                            {
                                oldValue = property.GetValue(objectToSetOn, null);
                            }
                            try
                            {
                                bool wasCustomSet = false;

                                if (objectToSetOn is PositionedObject)
                                {

                                }
                                else
                                {

                                }
                                property.SetValue(objectToSetOn, convertedValue, null);
                            }
                            catch (Exception e)
                            {
                                if (property.CanRead)
                                {
                                    // We failed, so let's set the value back (if we can)
                                    try
                                    {
                                        property.SetValue(objectToSetOn, oldValue, null);
                                    }
                                    catch
                                    {
                                        // do nothing
                                    }
                                }
                                //throw new Exception("Error setting " + property.Name + " on " + objectToSetOn + ":\n" + e.ToString());
                            }
                        }
                    }
                //}
                //catch
                //{
                //    // do nothing for now
                //}
            }
            else if (field != null)
            {
                field.SetValue(objectToSetOn, valueToSetTo);
            }

            if (objectToSetOn is PositionedObject)
            {
                ((PositionedObject)objectToSetOn).ForceUpdateDependencies();
            }

            // If we changed the position of this, we want to make sure to update any 
            // children before we continue to eliminate errors from repositioning
            foreach (PositionedObject childObject in this.Children)
            {
                childObject.ForceUpdateDependenciesDeep();
            }
            foreach (ElementRuntime er in this.ContainedElements)
            {
                er.ForceUpdateDependenciesDeep();
            }

            if (AfterVariableApply != null)
            {
                AfterVariableApply(this, vse);
            }

            return parent;
        }

        private ReferencedFileSave GetReferencedFileFromName(object valueToSetTo)
        {
            ReferencedFileSave rfs = null;
            if (this.mAssociatedNamedObjectSave != null)
            {
                // We want to use this.AssociatedIElement so that we get
                // RFS's that belong to "this".  We used to use the container
                // but that's not the scope of custom variables.
                IElement element = this.AssociatedIElement;
                if (element != null)
                {
                    rfs = element.GetReferencedFileSaveRecursively((string)valueToSetTo);


                    if (rfs == null)
                    {
                        rfs = element.GetReferencedFileSaveByInstanceNameRecursively(((string)valueToSetTo).Replace("-", "_"));
                    }
                }
            }
            else
            {
                rfs = mAssociatedIElement.GetReferencedFileSaveRecursively((string)valueToSetTo);

                if (rfs == null)
                {
                    rfs = mAssociatedIElement.GetReferencedFileSaveByInstanceNameRecursively(((string)valueToSetTo).Replace("-", "_"));
                }
            }
            return rfs;
        }

        private string ConvertAbsoluteToRelative(string absoluteName)
        {
            // Why don't we just use the InstructionManager here?
            string relative = InstructionManager.GetRelativeForAbsolute(absoluteName);

            if (!string.IsNullOrEmpty(relative))
            {
                return relative;
            }

            return absoluteName;

        }

        private void GetObjectToSetOnAndParent(ElementRuntime sourceElement, out object objectToSetOn, out PositionedObject parent)
        {
            objectToSetOn = null;

            parent = this;

            #region Get the objectToSetOn

            if (sourceElement.mDirectObjectReference == null)
            {
                objectToSetOn = sourceElement;
            }
            else
            {
                objectToSetOn = sourceElement.mDirectObjectReference;
            }

            if (objectToSetOn == this)
            {
                parent = this.Parent;
            }


            #endregion
        }

        // Made public for testing
        public void GetSourceElementAndVariableName(CustomVariable cv, out ElementRuntime sourceElement, out string variableName)
        {
            sourceElement = null;
            variableName = null;
            if (cv.DefinedByBase)
            {
                IElement baseElement = ObjectFinder.Self.GetIElement(mAssociatedIElement.BaseElement);

                while (baseElement != null)
                {
                    CustomVariable variableInBase = baseElement.GetCustomVariableRecursively(cv.Name);

                    if (!string.IsNullOrEmpty(variableInBase.SourceObject) && !string.IsNullOrEmpty(variableInBase.SourceObjectProperty))
                    {
                        string name = variableInBase.SourceObject;

                        sourceElement = GetElementFromName(name);
                        variableName = variableInBase.SourceObjectProperty;
                        break;
                    }
                    else if (variableInBase.SetByDerived == true && !variableInBase.DefinedByBase &&
                        string.IsNullOrEmpty(variableInBase.SourceObject))
                    {

                        sourceElement = this;
                        variableName = cv.Name;
                        break;
                    }
                    else
                    {
                        baseElement = ObjectFinder.Self.GetIElement(baseElement.BaseElement);
                    }
                }

                // If the variable is set by derived, and is the CurrentState for a state that's defined
                // in the base, then we'll never get a SourceObject or SourceObjectProperty, but it does need
                // to modify "this".
                if (sourceElement == null)
                {
                    sourceElement = this;
                    variableName = cv.Name;
                }
            }
            else if (cv.SourceObject == null || cv.SourceObjectProperty == null)
            {
                sourceElement = this;
                variableName = cv.Name;
            }
            else
            {
                string name = cv.SourceObject;

                sourceElement = GetElementFromName(name);
                variableName = cv.SourceObjectProperty;
            }
        }

        public static object ConvertIfOverriding(CustomVariable cv, object valueToSetTo)
        {

            if (!string.IsNullOrEmpty(cv.OverridingPropertyType))
            {
                switch (cv.Type)
                {
                    case "string":
                        switch (cv.OverridingPropertyType)
                        {
                            case "int":
                            case "long":
                                {
                                    // In .NET 4 we can do this:
                                    //return valueToSetTo.ToString("n0");
                                    // but for .NET 3 we gotta:
                                    string s = valueToSetTo.ToString();
                                    int asInt = 0;


                                    
                                    asInt = (int)System.Convert.ChangeType(valueToSetTo, typeof(int)) ;

                                    if (cv.TypeConverter == "Comma Separating")
                                    {
                                        for (int p = s.Length - 3; p > 0; p -= 3)
                                        {
                                            s = s.Insert(p, System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator);
                                        }
                                    }
                                    else if (cv.TypeConverter == "Minutes:Seconds")
                                    {
                                        s = string.Format("{0}:{1}", (asInt / 60).ToString("D1"), (asInt % 60).ToString("D2"));
                                    }

                                    return s;
                                    //break;
                                }
                            case "float":
                                {
                                    string s = valueToSetTo.ToString();
                                    float asFloat = (float)valueToSetTo;

                                    if (cv.TypeConverter == "Comma Separating")
                                    {
                                        s = string.Format("{0:n}", asFloat);
                                        //s = asFloat.ToString("n0");
                                    }

                                    else if (cv.TypeConverter == "Minutes:Seconds.Hundredths")
                                    {


                                        s = string.Format(
                                            "{0}:{1}{2}",
                                            ((int)asFloat / 60).ToString("D1"),
                                            ((int)asFloat % 60).ToString("D2"),
                                            ((float)asFloat - (int)asFloat).ToString(".00"));
                                    }
                                    return s;
                                }


                                
                        }

                        break;

                }
                return valueToSetTo;
            }
            else
            {
                return valueToSetTo;
            }
        }

        private object CastAndTranslateValueToSet(CustomVariable cv, object valueToSetTo)
        {
            if (cv.Type == "Color")
            {
                if (valueToSetTo == null || string.IsNullOrEmpty(valueToSetTo as string))
                {

                }
                Type colorType = typeof(Color);
                PropertyInfo propertyInfo = colorType.GetProperty((string)valueToSetTo, BindingFlags.Public | BindingFlags.Static);

                Color colorToSet = (Color)(propertyInfo.GetValue(null, null));
                return colorToSet;
            }
            else if (cv.Type == "string" && valueToSetTo is string)
            {
                if (cv.GetIsFile())
                {
                    // do noting
                }
                else if (cv != null && cv.GetIsAnimationChain())
                {
                    // do nothing
                }
                else if (LocalizationManager.HasDatabase)
                {
                    valueToSetTo = LocalizationManager.Translate((string)valueToSetTo);
                }
                //else if (namedObject.SourceType == SourceType.File)
                //{
                //    if (instructionSave.Member != "CurrentChain")
                //    {
                //        value = "LocalizationManager.Translate(" + value + ")";
                //    }
                //}
                //else if (namedObject.SourceType == SourceType.Entity)
                //{
                //    EntitySave entitySave = ObjectFinder.GetEntitySave(namedObject.SourceClassType);

                //    if (entitySave != null)
                //    {
                //        CustomVariable variableInEntity = entitySave.GetCustomVariable(instructionSave.Member);

                //        if (variableInEntity == null || variableInEntity.IsAnimationChain == false)
                //        {
                //            value = "LocalizationManager.Translate(" + value + ")";
                //        }
                //    }
                //}





                return valueToSetTo;
            }
            else
            {
                return valueToSetTo;
            }
        }

        private ElementRuntime GetElementFromName(string name)
        {
            ElementRuntime sourceElement;
            sourceElement = mContainedElements.FindByName(name);

            if (sourceElement == null)
            {
                sourceElement = mElementsInList.FindByName(name);
            }

            return sourceElement;
        }

        #endregion

        public void Activity()
        {
            mReferencedFileRuntimeList.Activity();


            if (this.DirectObjectReference != null )
            {
                if(this.DirectObjectReference is FlatRedBall.Graphics.Particle.Emitter)
                {
                    ((FlatRedBall.Graphics.Particle.Emitter)this.DirectObjectReference).TimedEmit();
                }
                else if(this.DirectObjectReference is FlatRedBall.Graphics.Particle.EmitterList)
                {
                    ((FlatRedBall.Graphics.Particle.EmitterList)this.DirectObjectReference).TimedEmit();
                }
            }


            if (this.AssociatedIElement is ScreenSave)
            {
                foreach (var emitterList in this.mReferencedFileRuntimeList.LoadedEmitterLists)
                {
                    emitterList.TimedEmit();
                }
            }

            foreach (var splineList in this.EntireSplineLists.Values)
            {
                foreach (var spline in splineList)
                {
                    spline.UpdateShapes();
                }
            }

            if(this.DirectObjectReference is Spline)
            {
                var asSpline = DirectObjectReference as Spline;

                if(asSpline.Visible)
                {
                    asSpline.UpdateShapes();
                }
            }

            if (this.DirectObjectReference is SplineList)
            {
                var asSplineList = DirectObjectReference as SplineList;

                foreach (var spline in asSplineList)
                {
                    if (spline.Visible)
                    {
                        spline.UpdateShapes();
                    }
                }
            }

            for (int i = 0; i < mContainedElements.Count; i++)
            {
                ElementRuntime elementRuntime = mContainedElements[i];
                elementRuntime.Activity();
            }

            for (int i = 0; i < mElementsInList.Count; i++)
            {
                ElementRuntime elementRuntime = mElementsInList[i];
                elementRuntime.Activity();
            }
            //PositionedObjectList<ElementRuntime> mElementsInList;

        }

        
        public StateSave GetStateRecursively(string stateName, string category = null)
        {
            StateSave foundState = GetStateInIElement(stateName, mAssociatedIElement, category);

            return foundState;
        }

        private StateSave GetStateInIElement(string stateName, IElement element, string category = null)
        {
            if (element != null)
            {
                if (category == "VariableState")
                {
                    category = null;
                }
                return element.GetStateRecursively(stateName, category);
            }
            else
            {
                return null;
            }
        }

        public void SetState(string stateName)
        {
            SetState(stateName, true);
        }

        public void SetState(string stateName, bool refreshPositionedObjectValues)
        {

            if (mAssociatedIElement == null)
            {
                return;
            }

            StateSave stateToSet = GetStateRecursively(stateName);
            mCurrentStateName = stateName;

            SetState(stateToSet, refreshPositionedObjectValues, mAssociatedIElement);
        }

        public void SetState(StateSave stateToSet, IElement container)
        {
            // Do we need to refresh positioned values?  I don't think so, because it causes bugs and
            // the fact that we set relative values now means we don't need to refresh the povalues anymore...
            //SetState(stateToSet, true, container);
            SetState(stateToSet, false, container);
        }

        public void SetState(StateSave stateToSet, bool refreshPositionedObjectValues, IElement container)
        {
            // 5/18/2011:  I found a bug
            // that occurred when one state
            // set another state.  We would get
            // the creeping/accumulating offset happening
            // I trakced it down to it being a state containing
            // another state.  Making sure that deeper state settings
            // don't refresh positions seems to fix it.  
            if (refreshPositionedObjectValues)
            {
                RefreshNamedObjectPositionedObjectValues();
            }
            if (stateToSet != null)
            {


                foreach (InstructionSave instructionSave in stateToSet.InstructionSaves)
                {
                    CustomVariable customVariable = GetCustomVariable(instructionSave.Member);
                    if (customVariable != null)
                    {
                        SetCustomVariable(customVariable, container, instructionSave.Value, refreshPositionedObjectValues);
                    }

                }
            }
            else
            {
                SetCustomVariables(mAssociatedIElement);
            }
        }


        private void RefreshContainedElements()
        {
            foreach (ElementRuntime er in ContainedElements)
            {
                er.RefreshContainedElements();
            }

            foreach (ElementRuntime er in ElementsInList)
            {
                er.RefreshContainedElements();
            }

            if (mAssociatedIElement != null)
            {
                // When variables change, the GLUX is reloaded.  Since it's reloated
                // the AssociatedIElement is no longer valid - even though the two may
                // be similar, they're two different references, and could have different
                // values.  So we refresh that here.
                mAssociatedIElement = ObjectFinder.Self.GetIElement(mAssociatedIElement.Name);

                if (mAssociatedIElement != null)
                {
                    foreach (ElementRuntime er in ContainedElements)
                    {
                        if (er.mAssociatedNamedObjectSave != null)
                        {
                            er.mAssociatedNamedObjectSave = mAssociatedIElement.GetNamedObjectRecursively(er.mAssociatedNamedObjectSave.InstanceName);
                        }
                    }

                    foreach (ElementRuntime er in ElementsInList)
                    {
                        if (er.mAssociatedNamedObjectSave != null)
                        {
                            er.mAssociatedNamedObjectSave = mAssociatedIElement.GetNamedObjectRecursively(er.mAssociatedNamedObjectSave.InstanceName);
                        }

                    }
                }
            }




        }

        private void RefreshNamedObject(NamedObjectSave n)
        {
            ElementRuntime foundElement = ContainedElements.FindByName(n.InstanceName);
            if (foundElement != null)
            {
                // this may be a PositionedObjectList or some other type that doesn't
                // get a ElementRuntime created for it.
                foundElement.mAssociatedNamedObjectSave = n;
            }

            foreach (NamedObjectSave containedNos in n.ContainedObjects)
            {
                RefreshNamedObject(containedNos);
            }
        }

        public void RefreshVariables()
        {
            RefreshContainedElements();

            SetCustomVariables(mAssociatedIElement);
            SetInstanceVariablesOnNamedObjects();

            string stateToSet = "";
            if (mCurrentStateName != null)
            {
                stateToSet = mCurrentStateName;
            }

            SetState(stateToSet);

        }

        public CustomVariable GetCustomVariable(string variableName, VariableGetType variableGetType = VariableGetType.DefinedInIElement)
        {
            if (variableGetType == VariableGetType.AsExistsAtRuntime)
            {
                PropertyValuePair? foundPvp = null;

                foreach (var variable in mCustomVariables)
                {
                    if (variable.Property == variableName)
                    {
                        foundPvp = variable;
                        break;
                    }
                }

                if (foundPvp.HasValue)
                {
                    CustomVariable customVariable = mAssociatedIElement.GetCustomVariableRecursively(variableName);

                    if (customVariable != null)
                    {
                        CustomVariable toReturn = customVariable.Clone();
                        toReturn.DefaultValue = foundPvp.Value.Value;
                        return toReturn;
                    }
                }
                return null;
            }
            else
            {
                // mAssociatedIElement is null if an object is created for a missing object (like a Sprite)
                if (mAssociatedIElement != null)
                {
                    CustomVariable customVariable = mAssociatedIElement.GetCustomVariableRecursively(variableName);

                    return customVariable;
                }
            }
            return null;
        }

        public object GetReferencedFileSaveRuntime(string unqualifiedName)
        {
            foreach (var kvp in mReferencedFileRuntimeList.LoadedRfses)
            {
                string key = kvp.Key;

                string unqualifiedCandidate = FileManager.RemovePath(FileManager.RemoveExtension(key));
                if (unqualifiedCandidate.ToLower() == unqualifiedName.ToLower())
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        public ElementRuntime GetContainedElementRuntime(string name)
        {
            for (int i = 0; i < mContainedElements.Count; i++)
            {
                if (mContainedElements[i].Name == name)
                {
                    return mContainedElements[i];
                }
            }
            for (int i = 0; i < mElementsInList.Count; i++)
            {
                if (mElementsInList[i].Name == name)
                {
                    return mElementsInList[i];
                }
            }
            return null;
        }

        public ElementRuntime GetContainedElementRuntime(NamedObjectSave namedObjectSave)
        {
            for (int i = 0; i < mContainedElements.Count; i++)
            {
                if (mContainedElements[i].mAssociatedNamedObjectSave == namedObjectSave)
                {
                    return mContainedElements[i];
                }
            }

            for (int i = 0; i < mElementsInList.Count; i++)
            {
                if (mElementsInList[i].mAssociatedNamedObjectSave == namedObjectSave)
                {
                    return mElementsInList[i];
                }
            }

            return null;

        }

        public void Destroy()
        {
            if (this.DirectObjectReference == SpriteManager.Camera)
            {
                SpriteManager.Camera.Detach();

            }

            mReferencedFileRuntimeList.Destroy();
            
            for (int i = mContainedElements.Count - 1; i > -1; i--)
            {
                ElementRuntime e = mContainedElements[i];

                e.Destroy();
            }

            for (int i = mElementsInList.Count - 1; i > -1; i--)
            {
                ElementRuntime e = mElementsInList[i];

                e.Destroy();
            }

            if (mDirectObjectReference != null)
            {
                Type directReferenceType = mDirectObjectReference.GetType();

                if (directReferenceType == typeof(AxisAlignedRectangle))
                {
                    ShapeManager.Remove((AxisAlignedRectangle)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Circle))
                {
                    ShapeManager.Remove((Circle)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Layer))
                {
                    SpriteManager.RemoveLayer((Layer)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Line))
                {
                    ShapeManager.Remove((Line)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Polygon))
                {
                    ShapeManager.Remove((Polygon)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Sphere))
                {
                    ShapeManager.Remove((Sphere)mDirectObjectReference);
                }
                else if(directReferenceType == typeof(SplineList))
                {
                    var splineList = ((SplineList)mDirectObjectReference);
                    splineList.RemoveFromManagers();
                }
                else if (directReferenceType == typeof(Spline))
                {
                    var spline = ((Spline)mDirectObjectReference);
                    spline.Visible = false;
                }
                else if (directReferenceType == typeof(Sprite))
                {
                    SpriteManager.RemoveSprite((Sprite)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(SpriteFrame))
                {
                    SpriteManager.RemoveSpriteFrame((SpriteFrame)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Text))
                {
                    TextManager.RemoveText((Text)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(ShapeCollection))
                {
                    ((ShapeCollection)mDirectObjectReference).RemoveFromManagers();
                }
                else if (directReferenceType == typeof(Scene))
                {
                    ((Scene)mDirectObjectReference).RemoveFromManagers();
                }
                else if (directReferenceType == typeof(Emitter))
                {
                    SpriteManager.RemoveEmitter((Emitter)mDirectObjectReference);
                }
                else if (directReferenceType == typeof(Camera))
                {
                    Camera camera = (Camera)mDirectObjectReference;
                    if (camera == SpriteManager.Camera)
                    {
                        // do nothing
                    }
                    else
                    {
                        SpriteManager.Cameras.Remove((Camera)mDirectObjectReference);
                    }
                }
                else if (directReferenceType == typeof(SpriteGrid))
                {
                    SpriteGrid spriteGrid = (SpriteGrid)mDirectObjectReference;
                    spriteGrid.Destroy();
                }
                else
                {
                    throw new NotImplementedException("GlueView is not properly able to remove object of type " + mDirectObjectReference.GetType());
                }

            }

            SpriteManager.RemovePositionedObject(this);

            FlatRedBallServices.Unload(ContentManagerName);

            mContainedElements.Clear();
        }

        private void RefreshNamedObjectPositionedObjectValues()
        {
            for (int i = 0; i < ContainedElements.Count; i++)
            {
                RefreshFromFileValues(ContainedElements[i]);
            }
            for (int i = 0; i < this.mElementsInList.Count; i++)
            {
                RefreshFromFileValues(mElementsInList[i]);
            }
        }

        void RefreshFromFileValues(ElementRuntime elementRuntime)
        {
            if (elementRuntime.mAssociatedNamedObjectSave != null &&
                elementRuntime.mAssociatedNamedObjectSave.SourceType == SourceType.File &&
                !string.IsNullOrEmpty(elementRuntime.mAssociatedNamedObjectSave.SourceFile) &&
                !string.IsNullOrEmpty(elementRuntime.mAssociatedNamedObjectSave.SourceName))
            {
                Scene scene = null;
                string fileNameToFind =
                    FileManager.Standardize(ContentDirectory + elementRuntime.mAssociatedNamedObjectSave.SourceFile);
                foreach (Scene loadedScene in mReferencedFileRuntimeList.LoadedScenes)
                {
                    if (loadedScene.Name == fileNameToFind)
                    {
                        scene = loadedScene;
                        break;
                    }
                }

                if (scene != null)
                {

                    INameable nameable = scene.FindByName(elementRuntime.mAssociatedNamedObjectSave.SourceNameWithoutParenthesis);

                    // It's valid if this is null.  This may happen if the NamedObjectSave references
                    // the entire file.
                    if (nameable != null)
                    {
                        Type typeOfObject = nameable.GetType();

                        bool wasFound = false;
                        BindingFlags bindingFlags = BindingFlags.Default;

                        if (typeOfObject.GetProperty("X") != null)
                        {
                            wasFound = true;
                            bindingFlags = BindingFlags.GetProperty;
                        }

                        //if(typeOfObject.GetField("X") != null)
                        //{
                        //    wasFound = true;
                        //    bindingFlags = BindingFlags.GetField;
                        //}


                        if (wasFound)
                        {
                            Binder binder = null;
                            object[] args = null;

                            // I think this can't be attached to a camera, but we should throw an exception if so


                            PositionedObject asPositionedObject = elementRuntime.mDirectObjectReference as PositionedObject;

                            if(asPositionedObject == null)
                            {
                                throw new Exception("The directObjectReference should be a PositionedObject, but it's not");
                            }

                            if (asPositionedObject.Parent != null)
                            {

                                asPositionedObject.RelativeX = (float)typeOfObject.InvokeMember("X",
                                       bindingFlags, binder, nameable, args);

                                asPositionedObject.RelativeY = (float)typeOfObject.InvokeMember("Y",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RelativeZ = (float)typeOfObject.InvokeMember("Z",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RelativeRotationX = (float)typeOfObject.InvokeMember("RotationX",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RelativeRotationY = (float)typeOfObject.InvokeMember("RotationY",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RelativeRotationZ = (float)typeOfObject.InvokeMember("RotationZ",
                                    bindingFlags, binder, nameable, args);
                                asPositionedObject.ForceUpdateDependencies();
                            }
                            else
                            {

                                asPositionedObject.X = (float)typeOfObject.InvokeMember("X",
                                       bindingFlags, binder, nameable, args);

                                asPositionedObject.Y = (float)typeOfObject.InvokeMember("Y",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.Z = (float)typeOfObject.InvokeMember("Z",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RotationX = (float)typeOfObject.InvokeMember("RotationX",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RotationY = (float)typeOfObject.InvokeMember("RotationY",
                                    bindingFlags, binder, nameable, args);

                                asPositionedObject.RotationZ = (float)typeOfObject.InvokeMember("RotationZ",
                                    bindingFlags, binder, nameable, args);
                            }
                        }
                    }
                }
            }

            elementRuntime.RefreshNamedObjectPositionedObjectValues();
        }


		public bool IsMouseOver(Cursor cursor, Layer layer)
		{
            if(layer == mLayer)
            {
			    var mouseOver = mDirectObjectReference as IMouseOver;

			    if (mouseOver != null)
			    {
				    if (mouseOver.IsMouseOver(cursor, layer))
				    {
					    return true;
				    }
			    }

			    foreach (ElementRuntime element in mContainedElements)
			    {
				    if (element.IsMouseOver(cursor, layer))
				    {
					    return true;
				    }
			    }

			    foreach (ElementRuntime element in mElementsInList)
			    {
				    if (element.IsMouseOver(cursor, layer))
				    {
					    return true;
				    }
			    }
            }
			return false;
		}

        public void SetCustomVariableValue(string name, object value)
        {
            for(int i = 0; i < mCustomVariables.Count; i++)
            {
                PropertyValuePair pvp = mCustomVariables[i];

                if (pvp.Property == name)
                {
                    pvp.Value = value;

                    mCustomVariables[i] = pvp;
                    break;
                }
            }
        }

        public bool TryGetCurrentCustomVariableValue(string name, out object value)
        {
            value = null;
            foreach (PropertyValuePair pvp in mCustomVariables)
            {
                if (pvp.Property == name)
                {
                    value = pvp.Value;
                    return true;
                }
            }
            return false;
        }

        #endregion


        public bool HasCursorOver(Cursor cursor)
        {
            return IsMouseOver(cursor, mLayer);
        }
    }
}
