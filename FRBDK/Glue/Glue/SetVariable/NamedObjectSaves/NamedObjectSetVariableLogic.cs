using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using Glue.IO;
using Microsoft.Xna.Framework.Graphics;
using GlueFormsCore.Managers;
using GlueFormsCore.SetVariable.NamedObjectSaves;
using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace FlatRedBall.Glue.SetVariable
{
    public class NamedObjectSetVariableLogic
    {
        Dictionary<string, Action<NamedObjectSave, object>> PropertiesToMethods = new Dictionary<string, Action<NamedObjectSave, object>>();

        public NamedObjectSetVariableLogic()
        {
            PropertiesToMethods = new Dictionary<string, Action<NamedObjectSave, object>>();

            PropertiesToMethods.Add("TextureAddressMode", ReactToTextureAddressMode);
        }

        /// <summary>
        /// Performs logic in response to a changed named object property or custom variable (instruction), such as propagating a rename
        /// or updating inherited objects when changing SetByDerived.
        /// </summary>
        /// <param name="changedMember">The name of the variable that has changed.</param>
        /// <param name="oldValue">The old value for the changed property.</param>
        /// <param name="parentPropertyName">The parent property containing this property, only relevant for embedded properties like X in a Rectangle. This will usually be null.</param>
        public async Task ReactToNamedObjectChangedValue(string changedMember, object oldValue, string parentPropertyName = null, NamedObjectSave namedObjectSave = null)
        {
            GlueElement element;
            if(namedObjectSave == null)
            {
                namedObjectSave = GlueState.Self.CurrentNamedObjectSave;
                element = GlueState.Self.CurrentElement;
            }
            else
            {
                element = ObjectFinder.Self.GetElementContaining(namedObjectSave);
            }

            // See discussion in NamedObjectVariableShowingLogic on why we don't do variable setting async
            //TaskManager.Self.WarnIfNotInTask();

            if (PropertiesToMethods.ContainsKey(changedMember))
            {
                PropertiesToMethods[changedMember](namedObjectSave, oldValue);
            }

            #region SourceType changed
            else if (changedMember == nameof(NamedObjectSave.SourceType))
            {
                bool didErrorOccur = false;

                if (didErrorOccur)
                {
                    namedObjectSave.SourceType = (SourceType)oldValue;
                }
                else
                {

                    if (namedObjectSave.SourceType == SourceType.Entity)
                    {
                        namedObjectSave.AddToManagers = true;
                    }
                    else if (namedObjectSave.SourceType == SourceType.File &&
                        namedObjectSave.GetContainerType() == ContainerType.Screen)
                    {
                        namedObjectSave.AddToManagers = false;
                    }
                }
            }
            #endregion

            #region SourceClassType changed

            else if (changedMember == nameof(NamedObjectSave.SourceClassType))
            {
                ReactToChangedSourceClassType(namedObjectSave, oldValue);
            }

            #endregion

            #region SourceFile changed
            else if (changedMember == nameof(NamedObjectSave.SourceFile))
            {
                if (namedObjectSave.SourceFile != (string)oldValue)
                {

                    // See if the current SourceName is valid or not
                    List<string> availableSourceNames =
                        AvailableNameablesStringConverter.GetAvailableNamedObjectSourceNames(namedObjectSave);


                    bool isSourceNameValid = availableSourceNames.Contains(namedObjectSave.SourceName);

                    if (!isSourceNameValid)
                    {
                        namedObjectSave.SourceName = "<NONE>";
                    }
                }
            }

            #endregion

            #region SourceName

            else if (changedMember == nameof(NamedObjectSave.SourceName))
            {
                // This needs to happen before we update custom properties
                ReactToChangedNosSourceName(namedObjectSave, oldValue as string);


                namedObjectSave.UpdateCustomProperties();
            }

            #endregion

            #region InstanceName changed

            else if (changedMember == nameof(NamedObjectSave.InstanceName))
            {
                await ReactToNamedObjectChangedInstanceName(namedObjectSave, oldValue as string);
            }

            #endregion

            #region SetByDerived Changed

            else if (changedMember ==nameof(NamedObjectSave.SetByDerived))
            {
                await SetByDerivedSetLogic.ReactToChangedSetByDerived(namedObjectSave, element);
            }

            #endregion

            #region ExposedInDerived Changed

            else if (changedMember == nameof(NamedObjectSave.ExposedInDerived))
            {
                if (namedObjectSave.SetByDerived && namedObjectSave.ExposedInDerived)
                {
                    // See comment in ExposedByDerived block on why this occurs
                    MessageBox.Show("You have set ExposedInDerived to true, but SetByDerived is also true.  Both cannot be true at the same time " +
                        "so Glue will set SetByDerived to false.");
                    namedObjectSave.SetByDerived = false;
                }


                SetExposedByDerivedRecursively(namedObjectSave, oldValue);

                InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);
            }


            #endregion

            #region SourceClassGenericType

            else if (changedMember == nameof(NamedObjectSave.SourceClassGenericType))
            {
                ReactToSourceClassGenericType(namedObjectSave, oldValue, element);
            }

            #endregion

            #region IsDisabled

            else if (changedMember == nameof(NamedObjectSave.IsDisabled))
            {
                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
            }

            #endregion

            #region SetByContainer Changed
            else if (changedMember == nameof(NamedObjectSave.SetByContainer))
            {

                if (namedObjectSave.SourceType == SourceType.Entity &&
                    !string.IsNullOrEmpty(namedObjectSave.SourceClassType))
                {
                    if (ProjectManager.CheckForCircularObjectReferences(ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType)) == ProjectManager.CheckResult.Failed)
                        namedObjectSave.SetByContainer = !namedObjectSave.SetByContainer;
                }

                var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(
                    GlueState.Self.CurrentElement.Name);

                foreach (IElement derived in derivedElements)
                {
                    foreach (NamedObjectSave nos in derived.NamedObjects)
                    {
                        if (nos.InstanceName == namedObjectSave.InstanceName)
                        {
                            nos.SetByContainer = namedObjectSave.SetByContainer;
                        }
                    }
                }

                if (GlueState.Self.CurrentEntitySave != null)
                {
                    List<NamedObjectSave> entityNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(GlueState.Self.CurrentEntitySave.Name);

                    foreach (NamedObjectSave nos in entityNamedObjects)
                    {
                        nos.UpdateCustomProperties();
                    }
                }
            }

            #endregion

            #region AddToManagers Changed

            else if (changedMember == nameof(NamedObjectSave.AddToManagers))
            {
                if (namedObjectSave.AddToManagers &&
                    namedObjectSave.GetContainerType() == ContainerType.Screen && namedObjectSave.SourceType == SourceType.File)
                {
                    ScreenSave screenSave = namedObjectSave.GetContainer() as ScreenSave;

                    ReferencedFileSave rfs = screenSave.GetReferencedFileSave(namedObjectSave.SourceFile);

                    if (rfs != null && !rfs.IsSharedStatic)
                    {

                        System.Windows.Forms.MessageBox.Show("This object comes from a file.  Files which are part of Screens " +
                            "are automatically added to the engine managers.  " +
                            "Adding this object would result in double-membership in the engine which may cause unexpected results.  " +
                            "\n\nGlue will now set this value back to false.");
                        namedObjectSave.AddToManagers = false;
                    }





                }
            }

            #endregion


            #region LayerOn

            else if (changedMember == nameof(NamedObjectSave.LayerOn))
            {
                if (namedObjectSave.IsList)
                {
                    DialogResult result = DialogResult.No;
                    if (string.IsNullOrEmpty(namedObjectSave.LayerOn))
                    {
                        result = MessageBox.Show("Do you want to remove every object in the List " + namedObjectSave.InstanceName +
                            " from its Layer?",
                            "Remove all from Layer?",
                            MessageBoxButtons.YesNo);
                    }
                    else
                    {
                        result = MessageBox.Show("Do you want to add every object contained in the List " + namedObjectSave.InstanceName +
                            " to the Layer " + namedObjectSave.LayerOn + "?",
                            "Add all to Layer?",
                            MessageBoxButtons.YesNo);
                    }

                    if (result == DialogResult.Yes)
                    {
                        namedObjectSave.SetLayerRecursively(namedObjectSave.LayerOn);
                    }
                }
            }

            #endregion

            #region IsContainer

            else if (changedMember == nameof(NamedObjectSave.IsContainer))
            {
                HandleChangedIsContainer(namedObjectSave, element);
            
            }

            #endregion


            #region AttachToCamera

            else if (changedMember == nameof(NamedObjectSave.AttachToCamera))
            {
                if (namedObjectSave.IsList)
                {
                    DialogResult result = DialogResult.No;

                    if (namedObjectSave.AttachToCamera)
                    {
                        result = MessageBox.Show("Do you want to attach every object contained in the list " + namedObjectSave.InstanceName +
                            " to the Camera?", "Attach all to Camera?",
                            MessageBoxButtons.YesNo);
                    }
                    else
                    {
                        result = MessageBox.Show("Do you want to detach every object contained in the list " + namedObjectSave.InstanceName +
                            " from the Camera?", "Detach all from the Camera?",
                            MessageBoxButtons.YesNo);
                    }

                    if (result == DialogResult.Yes)
                    {
                        namedObjectSave.SetAttachToCameraRecursively(namedObjectSave.AttachToCamera);
                    }
                }

            }


            #endregion

            #region DestinationRectangle.Y (for Layers)
            else if (parentPropertyName == nameof(NamedObjectSave.DestinationRectangle) && changedMember == "Y")
            {
                // If the Y is odd, we should warn the user that it should be even
                // or else text will draw incorrectly
                if (namedObjectSave.DestinationRectangle.HasValue && namedObjectSave.DestinationRectangle.Value.Y % 2 == 1)
                {
                    MessageBox.Show("Setting an odd value to the DestinationRectangle's Y may cause text to render improperly.  An " +
                        "even value is recommended");

                }

            }

            #endregion

            #region RemoveFromManagersWhenInvisible

            else if (changedMember == nameof(NamedObjectSave.RemoveFromManagersWhenInvisible))
            {
                // is this an Entity instance?
                if (namedObjectSave.SourceType == SourceType.Entity && namedObjectSave.RemoveFromManagersWhenInvisible)
                {
                    var entitySave = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);

                    if (entitySave != null)
                    {
                        // Is this CreatedByOtherEntities?
                        if (!entitySave.CreatedByOtherEntities)
                        {
                            MessageBox.Show("The Entity " + entitySave + " should have its CreatedByOtherEntities set to true to enable " +
                                "visibility-based removal to work properly");
                        }
                    }
                }
            }
            #endregion

            #region Custom Variable

            else if (namedObjectSave?.GetCustomVariable(changedMember) != null)
            {
                // See if this variable is tunneled into in this element.
                // If so, set that value too.
                CustomVariableInNamedObject cvino = namedObjectSave.GetCustomVariable(changedMember);
                object value = cvino.Value;

                if(element != null)
                {
                    foreach (CustomVariable customVariable in element.CustomVariables)
                    {
                        if (customVariable.SourceObject == namedObjectSave.InstanceName &&
                            customVariable.SourceObjectProperty == changedMember)
                        {
                            // The custom variable may have a different type:
                            if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                            {
                                // it does, so convert
                                Type overridingType = TypeManager.GetTypeFromString(customVariable.OverridingPropertyType);

                                customVariable.DefaultValue = System.Convert.ChangeType(value, overridingType);
                            }
                            else
                            {
                                customVariable.DefaultValue = value;
                            }
                            break;
                        }
                    }
                }
            }

            #endregion

            // If we changed BitmapFont and if the NOS is marked as PixelPerfect
            // and if it's a Text object, then we should set the Scale, Spacing, and
            // NewLineDistance according to the set BitmapFont
            // We don't do an else because there could be a CustomVariable by the name
            // of BitmapFont as well, and we dont' want to eliminate that.
            if (changedMember == "Font" && namedObjectSave.SourceType == SourceType.FlatRedBallType &&
                namedObjectSave.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Text && namedObjectSave.IsPixelPerfect)
            {
                ReactToFontSet(namedObjectSave, oldValue);
            }

            PropertyGridHelper.UpdateNamedObjectDisplay();

            PluginManager.ReactToNamedObjectChangedValue(changedMember, oldValue, namedObjectSave);
        }



        private void ReactToSourceClassGenericType(NamedObjectSave namedObjectSave, object oldValue, GlueElement element)
        {
            namedObjectSave.UpdateCustomProperties();

            if (ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassGenericType) != null)
            {
                namedObjectSave.AddToManagers = true;
            }

            var baseNos = namedObjectSave;
            
            if (baseNos.SetByDerived)
            {
                List<IElement> derivedElements = new List<IElement>();
                if (element is EntitySave)
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllEntitiesThatInheritFrom(element as EntitySave));
                }
                else
                {
                    derivedElements.AddRange(ObjectFinder.Self.GetAllScreensThatInheritFrom(element as ScreenSave));
                }

                foreach (IElement derivedElement in derivedElements)
                {
                    NamedObjectSave derivedNos = derivedElement.GetNamedObjectRecursively(baseNos.InstanceName);

                    if (derivedNos != baseNos)
                    {
                        UpdateDerivedNosFromBase(baseNos, derivedNos);
                    }
                }
            }

            InheritanceManager.UpdateAllDerivedElementFromBaseValues(true, element);
        }


        private void HandleChangedIsContainer(NamedObjectSave nos, GlueElement element)
        {
            if (nos.IsContainer)
            {
                var existingContainer = element.AllNamedObjects.FirstOrDefault(
                    item => item.IsContainer && item != nos);

                bool succeeded = true;

                if (existingContainer != null)
                {
                    succeeded = false;

                    MessageBox.Show("The object " + existingContainer + " is already marked as IsContainer. " +
                        "Two objects in the same element cannot be marked as Iscontainer");

                }

                if (succeeded)
                {
                    string nosType = nos.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType;

                    if(string.IsNullOrEmpty(nosType))
                    {
                        nosType = nos.SourceClassType;
                    }

                    bool doesBaseMatch = !string.IsNullOrEmpty(element.BaseElement) &&
                        element.BaseElement == nosType;

                    if (!doesBaseMatch)
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();

                        string containerType = element.BaseElement;
                        if (string.IsNullOrEmpty(containerType))
                        {
                            containerType = "<NONE>";
                        }


                        mbmb.MessageText = "The object is of type " + nosType + " but the container is of type " + containerType + "\n\n" +
                            "What would you like to do?";

                        mbmb.AddButton("Set the container's type to " + nosType, DialogResult.Yes);
                        mbmb.AddButton("Nothing - game may not compile until this has been fixed", DialogResult.No);
                        mbmb.AddButton("Set 'IsContainer' back to false", DialogResult.Cancel);

                        var dialogResult = mbmb.ShowDialog();
                        if (dialogResult == DialogResult.Yes)
                        {
                            element.BaseObject = nosType;
                        }
                        else if (dialogResult == DialogResult.Cancel)
                        {
                            succeeded = false;
                        }
                    }
                }

                if(!succeeded)
                {
                    nos.IsContainer = false;
                }

            }

            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
        }

        private void ReactToChangedNosSourceName(NamedObjectSave namedObjectSave, string oldValue)
        {
            var container = GlueState.Self.CurrentElement;

            if (!string.IsNullOrEmpty(container.BaseElement) && !string.IsNullOrEmpty(namedObjectSave.InstanceType))
            {
                var baseElement = ObjectFinder.Self.GetElement(container.BaseElement);

                NamedObjectSave namedObjectInBase = baseElement.GetNamedObjectRecursively(namedObjectSave.InstanceName);

                if (namedObjectInBase == null)
                {
                    // This is not a valid setup - what do we do here?

                }
                else
                {
                    // We'll rely on the instance type, which is the "old" informal way...
                    var doDiffer = namedObjectInBase.InstanceType != namedObjectSave.InstanceType &&
                        // and the new AssetTypeInfo which will resolve issues like unqualified vs qualified types
                        namedObjectInBase.GetAssetTypeInfo() != null &&
                        namedObjectInBase.GetAssetTypeInfo() != namedObjectSave.GetAssetTypeInfo();
                    if (doDiffer)
                    {
                        if (string.IsNullOrEmpty(namedObjectInBase.InstanceType))
                        {
                            string message = "This object has type of " + namedObjectSave.InstanceType +
                                " but the base object in " + baseElement.ToString() + " is untyped.  What would you like to do?";

                            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                            mbmb.MessageText = message;

                            mbmb.AddButton("Change " + namedObjectInBase.InstanceName + " to " +
                                namedObjectSave.InstanceType + " in " + baseElement.ToString(), DialogResult.Yes);

                            mbmb.AddButton("Do nothing (your project will likely not compile so you will need to fix this manually)", DialogResult.No);

                            DialogResult result = mbmb.ShowDialog();

                            if (result == DialogResult.Yes)
                            {
                                switch (namedObjectInBase.SourceType)
                                {
                                    case SourceType.File:
                                        // The base needs to be a FlatRedBallType
                                        namedObjectInBase.SourceType = SourceType.FlatRedBallType;
                                        namedObjectInBase.SourceClassType = namedObjectSave.InstanceType;


                                        break;
                                    case SourceType.FlatRedBallType:
                                        namedObjectInBase.SourceType = SourceType.FlatRedBallType;
                                        namedObjectInBase.SourceClassType = namedObjectSave.SourceClassType;
                                        break;
                                    case SourceType.Entity:
                                        namedObjectInBase.SourceType = SourceType.Entity;
                                        namedObjectInBase.SourceClassType = namedObjectSave.SourceClassType;
                                        break;
                                }
                                namedObjectInBase.UpdateCustomProperties();

                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(baseElement);
                            }
                        }
                        else
                        {
                            string message = "This object is of type " + namedObjectSave.InstanceType + " but the base " +
                                "object is of type " + namedObjectInBase.InstanceType + "";

                            MessageBox.Show(message);

                            namedObjectSave.SourceName = oldValue;

                        }
                    }
                }

            }
        }

        private void ReactToChangedSourceClassType(NamedObjectSave namedObjectSave, object oldValue)
        {
            if (namedObjectSave.SourceClassType == "<NONE>" ||
                namedObjectSave.SourceClassType == null)
            {
                namedObjectSave.SourceClassType = (string)oldValue;
                namedObjectSave.UpdateCustomProperties();

            }
            else
            {



                if (namedObjectSave.SourceType == SourceType.Entity)
                {
                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);

                    // If the EntitySave is null then that probably means that the Entity has been renamed and this hasn't
                    // been pushed to the project yet.
                    if (entitySave != null && ProjectManager.CheckForCircularObjectReferences(entitySave) == ProjectManager.CheckResult.Failed)
                    {
                        namedObjectSave.SourceClassType = (string)oldValue;
                    }
                    else
                    {
                        namedObjectSave.UpdateCustomProperties();
                    }
                }
                else if (namedObjectSave.SourceType == SaveClasses.SourceType.FlatRedBallType)
                {
                    namedObjectSave.UpdateCustomProperties();

                }
            }

            if (namedObjectSave.SourceClassType != (string)oldValue)
            {
                ShowPopupsForMissingVariablesInNewType( namedObjectSave, oldValue);

                if (namedObjectSave.GetAssetTypeInfo()?.FriendlyName == "Layer")
                {
                    namedObjectSave.AddToManagers = true;
                }

                if(namedObjectSave.SetByDerived || namedObjectSave.ExposedInDerived)
                {
                    var nosContainer = ObjectFinder.Self.GetElementContaining(namedObjectSave);
                    var derivedList = ObjectFinder.Self.GetAllDerivedElementsRecursive(nosContainer);

                    foreach(var derived in derivedList)
                    {
                        var derivedInstance = derived.GetNamedObject(namedObjectSave.InstanceName);

                        if(derivedInstance?.DefinedByBase == true)
                        {
                            derivedInstance.SourceClassType = namedObjectSave.SourceClassType;
                            // Don't run any code on the derived. In the future the object may not even be listed explicitly but may be implied
                        }
                    }
                }

            }
            // else, we should probably do something here to revert to defaults.  What a pain

        }

        private void ReactToFontSet(NamedObjectSave namedObjectSave, object oldValue)
        {
            string value = namedObjectSave.GetCustomVariable("Font").Value as string;

            if (!string.IsNullOrEmpty(value))
            {
                GlueElement element = GlueState.Self.CurrentElement;

                ReferencedFileSave referencedFileSave = element.GetReferencedFileSaveByInstanceNameRecursively(value);


                if (referencedFileSave != null)
                {
                    var file = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);

                    string contents = FileManager.FromFileText(file);

                    int size =
                        StringFunctions.GetIntAfter(
                        "size=", contents);

                    float lineHeightInPixels =
                        StringFunctions.GetIntAfter(
                        "lineHeight=", contents);

                    lineHeightInPixels /= 2.0f;

                    namedObjectSave.SetVariable("Scale", (float)lineHeightInPixels);
                    namedObjectSave.SetVariable("Spacing", (float)lineHeightInPixels);
                    namedObjectSave.SetVariable("NewLineDistance", (float)(lineHeightInPixels * 1.5f));

                }
            }
        }

        public Task ReactToNamedObjectChangedInstanceName(NamedObjectSave namedObjectSave, string oldValue)
        {
            ///////////////////Early Out//////////////////////////////
            if (namedObjectSave == null)
            {
                throw new ArgumentNullException(nameof(namedObjectSave));
            }

            string whyItIsntValid;

            NameVerifier.IsNamedObjectNameValid(namedObjectSave.InstanceName, out whyItIsntValid);

            if (!string.IsNullOrEmpty(whyItIsntValid))
            {
                GlueGui.ShowMessageBox(whyItIsntValid);
                namedObjectSave.InstanceName = oldValue;
                return Task.CompletedTask;
            }
            ///////////////End Early Out//////////////////////////////

            var currentElement = ObjectFinder.Self.GetElementContaining(namedObjectSave);

            string baseObject = currentElement?.BaseObject;
            // See if the entity has a base and if the base contains this name
            if (!string.IsNullOrEmpty(baseObject))
            {
                var baseNamedObjectContainer = ObjectFinder.Self.GetNamedObjectContainer(baseObject);

                var baseNamedObject = baseNamedObjectContainer?.GetNamedObjectRecursively(namedObjectSave.InstanceName);

                if (baseNamedObject != null)
                {
                    if (baseNamedObject.SetByDerived)
                    {
                        // There is a base element that has an object with the same
                        // name as the derived element.  The derived doesn't have a same-named
                        // object already, and the base is SetByDerived, so let's setup the derived
                        // to use the base and notify the user.
                        namedObjectSave.DefinedByBase = true;
                        GlueCommands.Self.DialogCommands.ShowMessageBox("This object has the same name as\n\n" + baseNamedObject.ToString() + "\n\nIt is SetByDerived, " +
                            "so Glue will use this as the base object for the object " + namedObjectSave.InstanceName);
                    }
                    else
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("A base object already has an object with this name");
                        namedObjectSave.InstanceName = oldValue;
                    }
                }

            }

            if (currentElement != null)
            {
                // See if any named objects in this instance use this for tunneling

                foreach (var customVariable in currentElement.CustomVariables)
                {
                    if (!string.IsNullOrEmpty(customVariable.SourceObject) && customVariable.SourceObject == oldValue)
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("Changing the variable " + customVariable.Name + " so it uses " + namedObjectSave.InstanceName + " instead of " + (string)oldValue);

                        customVariable.SourceObject = namedObjectSave.InstanceName;
                    }
                }

                // See if any events tunnel to this NOS
                foreach (EventResponseSave eventResponseSave in currentElement.Events)
                {
                    if (!string.IsNullOrEmpty(eventResponseSave.SourceObject) && eventResponseSave.SourceObject == oldValue)
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("Changing the Event " + eventResponseSave.EventName + " so it uses " + namedObjectSave.InstanceName + " instead of " + oldValue);

                        eventResponseSave.SourceObject = namedObjectSave.InstanceName;
                    }

                }
            }

            // If this is a layer, see if any other NOS's use this as their Layer
            if (namedObjectSave.IsLayer)
            {
                List<NamedObjectSave> namedObjectList = GlueState.Self.CurrentElement.NamedObjects;

                string oldLayerName = (string)oldValue;

                foreach (NamedObjectSave nos in namedObjectList)
                {
                    nos.ReplaceLayerRecursively(oldLayerName, namedObjectSave.InstanceName);
                }
            }

            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);

            var changedDerived = false;
            if(namedObjectSave.ExposedInDerived || namedObjectSave.SetByDerived)
            {
                var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(currentElement);

                foreach(var derivedElement in derivedElements)
                {
                    var nosInDerived = derivedElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == oldValue && item.DefinedByBase);

                    if (nosInDerived != null)
                    {
                        // new name:
                        nosInDerived.InstanceName = namedObjectSave.InstanceName;
                        changedDerived = true;
                        GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(derivedElement);
                    }
                }
            }
            if(changedDerived)
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
            }

            return Task.CompletedTask;
        }

        private void ShowPopupsForMissingVariablesInNewType(NamedObjectSave namedObjectSave, object oldValue)
        {
            if (namedObjectSave.SourceType == SourceType.Entity)
            {
                string oldType = (string)oldValue;
                string message = namedObjectSave.GetMessageWhySwitchMightCauseProblems(oldType);

                if (message != null)
                {
                    message += "\nDo you want to change to " + namedObjectSave.SourceClassType + " anyway?";

                    DialogResult result = MessageBox.Show(message, "Change anyway?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.No)
                    {
                        namedObjectSave.SourceClassType = (string)oldValue;
                        namedObjectSave.UpdateCustomProperties();

                    }
                }
            }
        }


        private void SetExposedByDerivedRecursively(NamedObjectSave namedObjectSave, object oldValue)
        {
            foreach (NamedObjectSave containedNamedObject in namedObjectSave.ContainedObjects)
            {
                containedNamedObject.ExposedInDerived = namedObjectSave.ExposedInDerived;

                SetExposedByDerivedRecursively(containedNamedObject, oldValue);
            }
        }

        private void UpdateDerivedNosFromBase(NamedObjectSave baseNos, NamedObjectSave derivedNos)
        {
            if (baseNos.IsList && !string.IsNullOrEmpty(baseNos.SourceClassGenericType))
            {
                derivedNos.SourceType = baseNos.SourceType;
                derivedNos.SourceClassType = baseNos.SourceClassType;
                derivedNos.SourceClassGenericType = baseNos.SourceClassGenericType;
                derivedNos.UpdateCustomProperties();

            }

        }


        private void ReactToTextureAddressMode(NamedObjectSave namedObjectSave, object oldValue)
        {
            bool isSprite = namedObjectSave.SourceType == SourceType.FlatRedBallType && namedObjectSave.SourceClassType == "Sprite";
            if(isSprite)
            {
                var addressModeVariable = namedObjectSave.GetCustomVariable("TextureAddressMode");

                if (addressModeVariable != null && addressModeVariable.Value != null && 
                    ((TextureAddressMode)(addressModeVariable.Value) == TextureAddressMode.Wrap || (TextureAddressMode)(addressModeVariable.Value) == TextureAddressMode.Mirror))
                {
                    // How big is the texture?
                    var textureVariable = namedObjectSave.GetCustomVariable("Texture");

                    if (textureVariable != null && textureVariable.Value != null)
                    {
                        string value = textureVariable.Value as string;

                        var rfs = namedObjectSave.GetContainer().GetReferencedFileSaveByInstanceName(value);

                        if (rfs != null)
                        {

                            var width = ImageHeader.GetDimensions(
                                    GlueCommands.Self.GetAbsoluteFileName(rfs)).Width;
                            var height = ImageHeader.GetDimensions(
                                    GlueCommands.Self.GetAbsoluteFileName(rfs)).Height;

                            string whatIsWrong = null;

                            if (FlatRedBall.Math.MathFunctions.IsPowerOfTwo(width) == false)
                            {
                                whatIsWrong = "This Sprite's texture (" + textureVariable.Value + ") has a width of " + 
                                    width + " but it should be a power of two to use " + addressModeVariable.Value + " TextureAddressMode";
                            }


                            if (FlatRedBall.Math.MathFunctions.IsPowerOfTwo(height) == false)
                            {
                                whatIsWrong = "This Sprite's texture (" + textureVariable.Value + ") has a height of " +
                                    height + " but it should be a power of two to use " + addressModeVariable.Value + " TextureAddressMode";
                            }

                            if(!string.IsNullOrEmpty(whatIsWrong))
                            {
                                whatIsWrong += "\nWhat would you like to do?";

                                var mbmb = new MultiButtonMessageBox();
                                mbmb.MessageText = whatIsWrong;
                                mbmb.AddButton("Undo the change", DialogResult.Cancel);
                                mbmb.AddButton("Keep the change (May cause runtime crashes)", DialogResult.Yes);

                                var result = mbmb.ShowDialog();

                                if(result == DialogResult.Cancel)
                                {
                                    addressModeVariable.Value = oldValue;
                                }

                            }
                        }
                    }
                }
            }
        }


    }
}
