using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.IO;
using FlatRedBall.Glue.TypeConversions;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Instructions;
using System;
using System.Linq;
using System.Collections.Generic;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class CustomVariableCodeGenerator : ElementComponentCodeGenerator
    {

        #region Fields/Properties

        public static ICodeBlock AppendCodeForMember(GlueElement saveObject, ICodeBlock codeBlock, CustomVariable customVariable, bool forceGenerateExposed = false)
        {
            VariableDefinition variableDefinition = null;
            if (!string.IsNullOrEmpty(customVariable.SourceObject))
            {
                var owner = saveObject.GetNamedObjectRecursively(customVariable.SourceObject);
                var nosAti = owner.GetAssetTypeInfo();
                variableDefinition = nosAti?.VariableDefinitions.Find(item => item.Name == customVariable.SourceObjectProperty);
            }

            // Regarding customVariable.IsTunneling -
            // If a variable is tunneling, then we want to generate code for DefinedByBase - this means
            // the use will be able to override virtual properties and give each class a specific implementation
            // Regarding customVariable.CreatesEvent - 
            // If this event creates an event, then we need to create a property for it, even if there is a base property.
            var shouldGenerate = !customVariable.DefinedByBase || customVariable.IsTunneling || customVariable.IsShared || customVariable.CreatesEvent;
            if(variableDefinition != null)
            {
                // June 14, 2022
                // This is tricky - if a variable uses custom code generation it may...
                // * not be a generated variable at all, only used in the editor
                // * be generated, but have custom get/set properties
                // It's hard to know which, so for now we'll just exclude generation completely, and see
                // if this causes problems...
                shouldGenerate = variableDefinition.UsesCustomCodeGeneration == false 
                    // Update December 30, 2022
                    // Actually we can check if it has a custom setter. If it does, then we know that it is going
                    // to be generated with custom sets:
                    || variableDefinition.CustomPropertySetFunc != null;
            }
            if (shouldGenerate)
            {
                #region if Tunneled Variable
                if (customVariable.IsTunneling && IsSourceObjectEnabled(saveObject, customVariable))
                {
                    AppendPropertyForTunneledVariable(saveObject, codeBlock, customVariable, variableDefinition);

                }
                #endregion

                #region else Exposed/Custom variable
                else
                {

                    bool isExposedExistingMember = customVariable.GetIsExposingVariable(saveObject);
                    // Exposed
                    // variables
                    // are variables
                    // which automatically
                    // exist as part of the
                    // Entity.  For example the
                    // property X is part of the
                    // PositionedObject.  Exposed
                    // variables do not need to be
                    // added as fields - they are already
                    // part of the class.
                    // Update March 25, 2012
                    // But what if the variable
                    // has an event?  Then it needs
                    // to have a property that fires
                    // the event.
                    // If it's Visible, it's handled by the IVisible generator
                    var shouldGenerateDueToExposed = (!isExposedExistingMember || customVariable.CreatesEvent) && customVariable.Name != "Visible";
                    shouldGenerateDueToExposed = shouldGenerateDueToExposed || forceGenerateExposed;
                    if (shouldGenerateDueToExposed)
                    {
                        CreateNewVariableMember(codeBlock, customVariable, isExposedExistingMember, saveObject);
                    }
                }
                #endregion


                CreateAccompanyingVelocityVariables(codeBlock, customVariable);

            }

            return codeBlock;
        }

        private static void AppendPropertyForTunneledVariable(IElement saveObject, ICodeBlock codeBlock, CustomVariable customVariable, VariableDefinition variableDefinition)
        {
            NamedObjectSave referencedNos = saveObject.GetNamedObjectRecursively(customVariable.SourceObject);

            if (referencedNos != null)
            {
                NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, referencedNos); 

                string customVariableType = GetMemberTypeFor(customVariable, saveObject);

                if (customVariable.CreatesEvent)
                {
                    EventCodeGenerator.GenerateEventsForVariable(codeBlock, customVariable.Name, customVariableType);
                }

                if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                {
                    customVariableType = customVariable.OverridingPropertyType;
                }

                ICodeBlock prop = WritePropertyHeader(codeBlock, customVariable, customVariableType);

                bool hasGetter = true;

                string propertyToAssign = customVariable.SourceObjectProperty;


                // later we will want to make this data driven
                if (referencedNos.SourceType == SourceType.File &&
                    FileManager.GetExtension(referencedNos.SourceFile) == "scnx" &&
                    referencedNos.SourceName.StartsWith("Entire File") &&
                    customVariable.SourceObjectProperty == "Visible" ||
                    variableDefinition?.HasGetter == false)
                {
                    hasGetter = false;
                }





                bool isVisibleSetterOnList = referencedNos.IsList &&
                    customVariable.SourceObjectProperty == "Visible";

                if (isVisibleSetterOnList)
                {
                    hasGetter = false;
                }
                if (hasGetter)
                {
                    WriteGetterForProperty(customVariable, saveObject, prop);
                }

                WriteSetterForProperty(saveObject, customVariable, prop, isVisibleSetterOnList, variableDefinition);

                NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, referencedNos);

            }
        }

        private static void CreateNewVariableMember(ICodeBlock codeBlock, CustomVariable customVariable, bool isExposing, GlueElement element)
        {
            string variableAssignment = "";

            if (customVariable.DefaultValue != null)
            {
                // July 24, 2023
                // Why not use GetRightSide?
                // That is 
                if (!IsTypeFromCsv(customVariable, element))
                {
                    //variableAssignment =
                    //    CodeParser.ConvertValueToCodeString(customVariable.DefaultValue);

                    //// If this is a file, we don't want to assign it here
                    //if (customVariable.GetIsFile())
                    //{
                    //    variableAssignment = null;
                    //}

                    //if (customVariable.Type == "Color")
                    //{
                    //    variableAssignment = "Color." + variableAssignment.Replace("\"", "");

                    //}
                    //else if (customVariable.Type != "string" && variableAssignment == "\"\"")
                    //{
                    //    variableAssignment = null;
                    //}
                    //else
                    //{
                    //    if (customVariable.DefaultValue != null)
                    //    {
                    //        (bool isState, StateSaveCategory category) =
                    //            customVariable.GetIsVariableStateAndCategory(element as GlueElement);
                    //        if (isState)
                    //        {
                    //            var type = customVariable.Type;
                    //            if (category != null)
                    //            {
                    //                var categoryElement = ObjectFinder.Self.GetElementContaining(category);

                    //                if (categoryElement != null)
                    //                {
                    //                    type = $"{categoryElement.Name.Replace("\\", ".")}.{category.Name}";

                    //                }

                    //            }
                    //            variableAssignment = type + "." + customVariable.DefaultValue;
                    //        }

                    //    }
                    //}
                    var variableAssignmentValue = GetRightSideOfEquals(customVariable, element);

                    if (variableAssignmentValue != null)
                    {
                        variableAssignment = " = " + variableAssignmentValue;
                    }
                }
                else if(!string.IsNullOrEmpty(customVariable.DefaultValue as string) && (string)customVariable.DefaultValue != "<NULL>")
                {
                    // If the variable IsShared (ie static) then we
                    // don't want to assign the value because the CSV
                    // may not yet be loaded.  This may create behavior
                    // the user doesn't expect, but the alternative is either
                    // to load the file before the user wants to (which maybe we
                    // will end up doing) or to get a crash
                    // Update June 2, 2013
                    // If the customVariable 
                    // is not "IsShared" (it's
                    // not static), we don't want
                    // to assign the value where we
                    // create the variable as a field
                    // because this means the value will
                    // attempt to assign before LoadStaticContent.
                    // This can cause a crash, and has in the GlueTestProject.
                    // Update June 2, 2013
                    // Used to set it to null
                    // if it's static, but we should
                    // allow statics to set their values
                    // if they come from global content files.

                    
                    if (
                        ReferencesCsvFromGlobalContent(customVariable) &&
                        ShouldAssignToCsv(customVariable, customVariable.DefaultValue as string))
                    {
                        variableAssignment = " = " + GetAssignmentToCsvItem(customVariable, element, (string)customVariable.DefaultValue);
                    }
                    else
                    {
                        variableAssignment = null;
                    }
                }
            }

            bool needsToBeProperty = (customVariable.SetByDerived && !customVariable.IsShared) || customVariable.CreatesProperty || customVariable.CreatesEvent
                || IsVariableWholeNumberWithVelocity(customVariable);

            if(needsToBeProperty)
            {
                var isState = customVariable.GetIsVariableState();

                if(isState)
                {

                    var elementDefiningCategory = ObjectFinder.Self.GetElementDefiningStateCategory(customVariable.Type);
                    var isDefinedInDifferentElement = elementDefiningCategory != null && elementDefiningCategory != element && ObjectFinder.Self.GetAllBaseElementsRecursively(element).Contains(elementDefiningCategory) == false;
                    if (!isDefinedInDifferentElement)
                    {
                        needsToBeProperty = false;
                    }
                }
            }

            EventCodeGenerator.TryGenerateEventsForVariable(codeBlock, customVariable, element);
            
            var memberType = GetMemberTypeFor(customVariable, element);
            var scopeValue = customVariable.Scope.ToString().ToLowerInvariant();


            if (needsToBeProperty)
            {
                // If the variable
                // creates an event
                // then it needs to have
                // custom code (it can't be
                // an automatic property).
                var isWholeNumberWithVelocity = IsVariableWholeNumberWithVelocity(customVariable);

                if (customVariable.CreatesEvent || isWholeNumberWithVelocity || customVariable.DefaultValue != null )
                {
                    var variableToAssignInProperty = "base." + customVariable.Name;
                    // create a field for this, unless it's defined by base - then the base creates a field for it
                    if (!isExposing && !customVariable.DefinedByBase)
                    {
                        variableToAssignInProperty = "m" + customVariable.Name;

                        // First we make the field that will get set here:
                        var line = 
                            // field is always private:
                            //scopeValue + " " + 
                            "private " + 
                            StringHelper.Modifiers(Static: customVariable.IsShared, Type: memberType, Name: variableToAssignInProperty) + variableAssignment + ";";
                        codeBlock.Line(line);
                    }

                    string propertyHeader;

                    if (isExposing)
                    {
                        propertyHeader = $"{scopeValue} new {memberType} {customVariable.Name}";
                    }
                    else if (customVariable.DefinedByBase)
                    {
                        propertyHeader = $"{scopeValue} override {memberType} {customVariable.Name}";
                    }
                    else if (customVariable.SetByDerived)
                    {
                        propertyHeader = $"{scopeValue} virtual {memberType} {customVariable.Name}";
                    }
                    else
                    {
                        propertyHeader = $"{scopeValue} {memberType} {customVariable.Name}";
                    }

                    if (!string.IsNullOrWhiteSpace(customVariable.Summary))
                    {
                        GenerateVariableSummary(codeBlock, customVariable);
                    }

                    ICodeBlock set = codeBlock.Property(propertyHeader, Static:customVariable.IsShared)
                        .Set();

                    if (EventCodeGenerator.ShouldGenerateEventsForVariable(customVariable, element))
                    {
                        EventCodeGenerator.GenerateEventRaisingCode(set, BeforeOrAfter.Before, customVariable.Name, element);
                    }

                    set.Line(variableToAssignInProperty + " = value;");
                    if (IsVariableWholeNumberWithVelocity(customVariable))
                    {
                        set.Line(customVariable.Name + "ModifiedByVelocity = value;");
                    }
                    if (EventCodeGenerator.ShouldGenerateEventsForVariable(customVariable, element))
                    {
                        EventCodeGenerator.GenerateEventRaisingCode(set, BeforeOrAfter.After, customVariable.Name, element);
                    }

                    ICodeBlock get = set.End().Get();

                    codeBlock = get.Line("return " + variableToAssignInProperty + ";")
                        .End().End(); // end the getter, end the property
                }
                else
                {
                    // Static vars can't be virtual
                    bool isVirtual = !customVariable.IsShared;
                    codeBlock.AutoProperty(customVariable.Name, customVariable.Scope, Virtual: isVirtual, Static: customVariable.IsShared, Type: memberType);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(customVariable.Summary))
                {
                    GenerateVariableSummary(codeBlock, customVariable);
                }
                if (!customVariable.GetIsVariableState())
                {

                    var line = customVariable.Scope.ToString().ToLower() + " " + StringHelper.Modifiers(Static: customVariable.IsShared, 
                        Type: memberType, Name: customVariable.Name) + variableAssignment + ";";
                    codeBlock.Line(line);


                }
                else
                {
                    var definingEntityNameFromType = customVariable.GetEntityNameDefiningThisTypeCategory();

                    var isStateDefinedInOtherEntity = !string.IsNullOrEmpty(definingEntityNameFromType) &&
                        customVariable.GetEntityNameDefiningThisTypeCategory() != element.Name;

                    if(isStateDefinedInOtherEntity)
                    {
                        var line = customVariable.Scope.ToString().ToLowerInvariant() + " " + 
                            StringHelper.Modifiers(Static: customVariable.IsShared, Type: memberType, Name: customVariable.Name) + ";";

                        codeBlock.Line(line);

                    }


                    else if (IsVariableTunnelingToDisabledObject(customVariable, element))
                    {
                        // If it's a varaible
                        // that is exposing a
                        // state variable for a
                        // disabled object, we still
                        // want to generate something:
                        var line = scopeValue.ToLowerInvariant() + " " +
                            StringHelper.Modifiers(Static: customVariable.IsShared, Type: memberType, Name: customVariable.Name)
                            + ";";



                        // No assignment for now.  Do we eventually want this?  The reason
                        // this even exists is to satisfy a variable that may be needed by other
                        // code which would point to a disabled object.
                        //+ variableAssignment 
                        codeBlock.Line(line);
                        
                    }
                }
            }
        }

        private static void GenerateVariableSummary(ICodeBlock codeBlock, CustomVariable customVariable)
        {
            codeBlock.Line("/// <summary>");
            codeBlock.Line($"/// {customVariable.Summary}");
            codeBlock.Line("/// </summary>");
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            var glueElement = element as GlueElement;

            for (int i = 0; i < element.CustomVariables.Count; i++)
            {
                CustomVariable customVariable = element.CustomVariables[i];

                if (CodeWriter.IsVariableHandledByCustomCodeGenerator(customVariable, element) == false)
                {
                    AppendCodeForMember(glueElement, codeBlock, customVariable);
                }
            }
            return codeBlock;
        }

        private static void WriteSetterForProperty(IElement saveObject, CustomVariable customVariable, ICodeBlock prop, 
            bool isVisibleSetterOnList, VariableDefinition variableDefinition)
        {
            var setter = prop.Set();

            if (EventCodeGenerator.ShouldGenerateEventsForVariable(customVariable, saveObject))
            {
                EventCodeGenerator.GenerateEventRaisingCode(setter, BeforeOrAfter.Before, customVariable.Name, saveObject);
            }

            NamedObjectSave nos = saveObject.GetNamedObjectRecursively(customVariable.SourceObject);

            bool addOrRemoveOnValueChange = nos.RemoveFromManagersWhenInvisible && customVariable.SourceObjectProperty == "Visible";

            if (variableDefinition?.CustomPropertySetFunc != null)
            {
                setter.Line(variableDefinition.CustomPropertySetFunc(saveObject, customVariable));
            }


            else if (isVisibleSetterOnList)
            {
                var forLoop = setter.For($"int i = 0; i < {customVariable.SourceObject}.Count; i++");
                forLoop.Line($"{customVariable.SourceObject}[i].Visible = value;");
            }
            else if(customVariable.GetIsSourceFile(saveObject))
            {
                // We need to assign the property here on the NamedObjectSave, as if it's done in code in the AddToManagersBottomUp

                // We need to temporarily make the NOS act as if it's using the argument file as its SourceFile
                var oldSourceType = nos.SourceType;
                var oldSourceFile = nos.SourceFile;
                var oldSourceName = nos.SourceName;
                nos.SourceType = SourceType.File;
                nos.SourceFile = "value";
                nos.SourceName = "Entire File (" + customVariable.Type  + ")";

                
                NamedObjectSaveCodeGenerator.WriteCodeForNamedObjectInitialize(nos, saveObject, setter, "value");//WriteAddToManagersForNamedObject(saveObject, nos, setter);

                bool storeOldValue = nos.RemoveFromManagersWhenInvisible && customVariable.SourceObjectProperty == "Visible";

                bool canBeLayered = false;
                if (nos.GetAssetTypeInfo() != null)
                {
                    canBeLayered = nos.GetAssetTypeInfo().LayeredAddToManagersMethod.Count != 0;
                }


                if (canBeLayered)
                {
                    // This object may be 
                    // added to a Layer.  We
                    // will add it to the NOS's
                    // Layer if there is one.  If 
                    // not, we'll use the object's
                    // Layer
                    string layerName;
                    if (!string.IsNullOrEmpty(nos.LayerOn))
                    {
                        layerName = nos.LayerOn;
                    }
                    else if (saveObject is EntitySave)
                    {
                        layerName = "LayerProvidedByContainer";
                    }
                    else
                    {
                        // I don't remember if Screens
                        // have a Layer that they use by
                        // default.  If so, this has probably
                        // fallen out of style because of Glue.
                        layerName = "null";
                    }

                    // Glue has no way of knowing whether this property will be used or not:
                    setter.Line("#pragma warning disable");
                    setter.Line("");
                    setter.Line("FlatRedBall.Graphics.Layer layerToAddTo = " + layerName + ";");
                    setter.Line("#pragma warning enable");
                    // Wow, so crazy, but the automated
                    // tests revealed that the line following
                    // the #pragma wasn't ever being run on iOS.
                    // I suspect it has to do with line endings after
                    // the #pragma line?  Anyway, putting a new line in
                    // seems to fix the bug.
                    setter.Line("");
                }

                NamedObjectSaveCodeGenerator.WriteAddToManagersForNamedObject(saveObject, nos, setter, true);

                nos.SourceType = oldSourceType;
                nos.SourceFile = oldSourceFile;
                nos.SourceName = oldSourceName;
            }
            else
            {
                bool shouldGenerateRelative = GetIfShouldGenerateRelative(customVariable, saveObject);

                string relativeVersionOfProperty = null;
                if(shouldGenerateRelative)
                {
                    relativeVersionOfProperty = InstructionManager.GetRelativeForAbsolute(customVariable.SourceObjectProperty);

                }
                if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                {
                    setter = setter.If(customVariable.SourceObject + ".Parent == null");
                }

                string value = "value";

                bool hasCustomType = !string.IsNullOrEmpty(customVariable.OverridingPropertyType) &&
                    // If the overriding type is the same as the variable type then no conversion can be performed
                    customVariable.OverridingPropertyType != customVariable.Type;

                if (hasCustomType)
                {
                    value = TypeConverterHelper.Convert(
                        customVariable, GetterOrSetter.Setter, value);
                }

                bool recordChange = nos.RemoveFromManagersWhenInvisible;

                

                string leftSide = customVariable.SourceObject + "." + customVariable.SourceObjectProperty;


                if (addOrRemoveOnValueChange)
                {
                    setter.Line("var oldValue = " + leftSide + ";");
                }

                if(hasCustomType && string.IsNullOrEmpty(value))
                {
                    setter.Line($"throw new System.Exception(\"Could not convert from {customVariable.Type} to {customVariable.OverridingPropertyType}\");");
                }
                else
                {
                    setter.Line(leftSide + " = " + value + ";");
                }

                if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                {
                    setter = setter.End().Else();
                    setter.Line(customVariable.SourceObject + "." + relativeVersionOfProperty + " = " + value + ";");
                    setter = setter.End();
                }
            }

            if (addOrRemoveOnValueChange)
            {
                string leftSide = customVariable.SourceObject + "." + customVariable.SourceObjectProperty;

                var outerIf = setter.If("oldValue != " + leftSide);
                var ifNotValue = outerIf.If("!value");

                // write remove from managers!


                NamedObjectSaveCodeGenerator.GenerateRemoveFromManagersForNamedObject(
                    saveObject,
                    nos,
                    ifNotValue);
                // remove from managers

                var elseIfValue = outerIf.Else();
                NamedObjectSaveCodeGenerator.WriteAddToManagersForNamedObject(
                    saveObject,
                    nos,
                    elseIfValue,
                    false, false);
                //NamedObjectSaveCodeGenerator

            }

            if (EventCodeGenerator.ShouldGenerateEventsForVariable(customVariable, saveObject))
            {

                EventCodeGenerator.GenerateEventRaisingCode(setter, BeforeOrAfter.After, customVariable.Name, saveObject);
            }
        }


        #endregion

        #region Initialize

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            // Before August 23, 2010 Custom Variables used to be set
            // here in Initialize before the AddToManagers method.  This
            // is problematic because:
            // 1.   We probably want these
            //      variables reset whenever
            //      this object is recycled. 
            // 2.   If we set the position of 
            //      the Entity before its children
            //      have been attached, then the attachment
            //      will not work as expected.  Therefore, I've
            //      decided to move custom variable code to AddToManagers.
            // UPDATE:  Actually, we do want variables set here so that they
            // are available in Custom Initialize.
            // UPDATE2:  This was moved to its own method so that inheritance works.
            // UPDATE3:  It turns out there's 2 types of variables.  
            // 1.   Variables that are not set by derived.  These variables should get set
            //      in the base class or else they'll never get set for derived objects.
            // 2.   Variables that are set by derived.  These variables should get set in the
            //      "SetCustomVariables" method so that they get overridden by derived objects.
            // This means that we're going to set variables that are not set by derived here, and the
            // rest will get set in SetCustomVariables.
            // UPDATE4:  This has all moved to PostInitialize, which is called
            //           bottom-up.  This means there is no more split on variables.
            // UPDATE5:  This has been moved out of BaseElementTreeNode int CustomVariableCodeGenerator.
            return codeBlock;
        }

        #endregion

        #region Assignment

        public static string GetRightSideOfEquals(CustomVariable customVariable, GlueElement glueElement, object forcedValue = null, string forcedType = null)
        {
            string rightSide = "";

            CustomVariable variableConsideringDefinedByBase = customVariable?.GetDefiningCustomVariable();

            var alternative = ObjectFinder.Self.GetBaseCustomVariable(customVariable);

            IElement containerOfState = null;

            // This can be null
            // if the user takes
            // an Element that inherits
            // from another and has variables
            // from it, then changes the Element
            // to no longer inherit.

            if(variableConsideringDefinedByBase != null)
            {
                // don't pass glueElement, it may not match variableConsideringDefinedByBase's container
                //containerOfState = GetElementIfCustomVariableIsVariableState(variableConsideringDefinedByBase, glueElement);
                // Update July 24, 2023 - If the variable type is unqualified, then it may be a variable of type state. This is
                // not recommended, but old FRB projects may still have these variables around. Therefore, if containerOfState is null
                // and if the type is unqualified, and if glueElement is not null, let's try by passing the container:
                containerOfState = GetElementIfCustomVariableIsVariableState(variableConsideringDefinedByBase);

                if(containerOfState == null && glueElement != null && customVariable.Type?.Contains(".") == false)
                {
                    containerOfState = GetElementIfCustomVariableIsVariableState(variableConsideringDefinedByBase, glueElement);
                }   
            }

            if (containerOfState == null)
            {
                Type overridingType = null;
                if ((forcedValue ?? variableConsideringDefinedByBase?.DefaultValue) is List<Vector2>)
                {
                    overridingType = typeof(List<FlatRedBall.Math.Geometry.Point>);
                }
                // this makes an assumption. The real way to do this would be to continue to go deeper and deeper until we get the root
                // AssetTypeInfo and see if it's a polygon...
                rightSide =
                    CodeParser.ConvertValueToCodeString(forcedValue ?? customVariable.DefaultValue, overridingType);
                NamedObjectSave namedObject = glueElement.GetNamedObjectRecursively(variableConsideringDefinedByBase?.SourceObject);


                if ((!string.IsNullOrEmpty(forcedType) && CustomVariableExtensionMethods.GetIsFile(forcedType)) || variableConsideringDefinedByBase?.GetIsFile() == true)
                {
                    rightSide = rightSide.Replace("\"", "").Replace("-", "_");

                    if (rightSide == "<NONE>")
                    {
                        rightSide = "null";
                    }
                }
                else if (variableConsideringDefinedByBase?.GetIsCsv() == true)
                {
                    if (ShouldAssignToCsv(variableConsideringDefinedByBase, rightSide))
                    {
                        rightSide = GetAssignmentToCsvItem(customVariable, glueElement, rightSide);
                    }
                    else
                    {
                        rightSide = null;
                    }
                }
                else if(variableConsideringDefinedByBase?.GetIsBaseElementType(out GlueElement baseElement) == true)
                {
                    var effectiveValue = forcedValue ?? customVariable.DefaultValue;
                    if (!string.IsNullOrEmpty( effectiveValue as string) && (effectiveValue as string) != "<NONE>")
                    {
                        var valueAfterLastBackslash = FileManager.RemovePath(effectiveValue as string);
                        rightSide = variableConsideringDefinedByBase.Type + "." + valueAfterLastBackslash;
                    }
                    else
                    {
                        rightSide = "null";
                    }
                }
                else if(variableConsideringDefinedByBase?.GetIsBaseElementType() == true)
                {
                    rightSide = variableConsideringDefinedByBase.Type + "." + rightSide;
                }
                else if(forcedType == "FlatRedBall.Sprite" || forcedType == "Sprite")
                {
                    rightSide = rightSide.Replace("\"", "");
                }
            
                else if (variableConsideringDefinedByBase?.Type == "Color" || forcedType == "Color" || forcedType == "Microsoft.Xna.Framework.Color")
                {
                    rightSide = "Microsoft.Xna.Framework.Color." + rightSide.Replace("\"", "");

                }
                 //This code was setting the variable to "null" but if it's explicitly "", then we should leave it as that because that's what is used
                 //for instructions. We want instructions and variables to work the same way, I think, but I'm leaving this here incase it does cause complications
                 // Update May 23, 2023 - this is important because values of types like `decimal?` can be nullable, and so they should assign null
                else if ( (variableConsideringDefinedByBase?.Type != "string" || (forcedType != null && forcedType != "string")) && rightSide == "\"\"")
                {
                    rightSide = null;
                }
                else
                {

                    //Not sure why this wasn't localizing variables but it caused a problem with 
                    //variables that tunnel in to a Text's DisplayText not being localized
                    //finish here

                    // Special Case:
                    // We don't want to
                    // set Visible on NOS's
                    // which are added/removed
                    // when Visible is set on them:
                    // Update March 7, 2014 
                    // We do want to set it because if we don't,
                    // then the checks inside the Visible property
                    // don't properly detect that they've been changed.
                    // Therefore, we're going to set it on the underlying
                    // object 
                    bool shouldSetUnderlyingValue = namedObject != null && namedObject.RemoveFromManagersWhenInvisible &&
                        variableConsideringDefinedByBase.SourceObjectProperty == "Visible";

                    if (shouldSetUnderlyingValue)
                    {
                        // Don't change the variable to assign
                    }
                    else
                    {
                        rightSide = CodeWriter.MakeLocalizedIfNecessary(namedObject, customVariable?.SourceObjectProperty,
                            (forcedValue ?? customVariable.DefaultValue), rightSide, null);

                        if (namedObject?.SourceType == SourceType.Gum && variableConsideringDefinedByBase.Type?.Contains(".") == true && variableConsideringDefinedByBase.Type.EndsWith("?"))
                        {
                            // this is a state type, so remove the "?" and prefix it:
                            rightSide = variableConsideringDefinedByBase.Type.Substring(0, variableConsideringDefinedByBase.Type.Length - 1) + "." + (forcedValue ?? customVariable.DefaultValue);
                        }

                        rightSide = rightSide?.Replace("+", ".");
                    }
                }
            }
            else
            {
                string valueAsString = (string)(forcedValue ?? customVariable.DefaultValue);

                if (!string.IsNullOrEmpty(valueAsString))
                {
                    // If the custom variable is passed, use that type because it's accurate.
                    string type = customVariable?.Type ?? forcedValue?.GetType().Name;
                    rightSide = StateCodeGenerator.FullyQualifyStateValue(containerOfState, (string)valueAsString, type );
                }
            }


            return rightSide;
        }


        // Note - this code is very similar to StateCodeGenerator.cs's GetRightSideAssignmentValueAsString
        // Unify?
        public static ICodeBlock AppendAssignmentForCustomVariableInElement(ICodeBlock codeBlock, CustomVariable customVariable, IElement saveObject)
        {
            var glueElement = saveObject as GlueElement;

            // Victor Chelaru
            // December 17, 2014
            // We used to not go into
            // this if statement if SetByDerived
            // was true, but actually since variables
            // are set bottom-up, there's really no reason
            // to set it only on the derived, because the base
            // will always get assigned first, then the derived
            // can override it.
            //if (!customVariable.SetByDerived && 
            if (customVariable.DefaultValue != null && // if it's null the user doesn't want to change what is set in the file or in the source object
                !customVariable.IsShared && // no need to handle statics here because they're always defined in class scope
                !IsVariableTunnelingToDisabledObject(customVariable, glueElement)
                )
            {
                string rightSide = GetRightSideOfEquals(customVariable, glueElement);

                if (!string.IsNullOrEmpty(rightSide))
                {
                    string relativeVersionOfProperty = InstructionManager.GetRelativeForAbsolute(customVariable.Name);
                    if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                    {
                        codeBlock = codeBlock.If("Parent == null");
                    }

                    NamedObjectSave namedObject = glueElement.GetNamedObject(customVariable.SourceObject);

                    bool shouldSetUnderlyingValue = namedObject != null && namedObject.RemoveFromManagersWhenInvisible &&
                        customVariable.SourceObjectProperty == "Visible";

                    if (namedObject != null)
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);
                    }

                    if (shouldSetUnderlyingValue)
                    {
                        codeBlock.Line(StringHelper.SpaceStrings(namedObject.InstanceName + "." + customVariable.SourceObjectProperty + " = ", rightSide + ";"));

                    }
                    else
                    {
                        codeBlock.Line(StringHelper.SpaceStrings(customVariable.Name, "=", rightSide + ";"));

                    }


                    if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                    {
                        if (customVariable.Name == "Z")
                        {
                            codeBlock = codeBlock.End().ElseIf("Parent is FlatRedBall.Camera");
                            codeBlock.Line(relativeVersionOfProperty + " = " + rightSide + " - 40.0f;");
                        }
                        codeBlock = codeBlock.End().Else();
                        codeBlock.Line(relativeVersionOfProperty + " = " + rightSide + ";");
                        codeBlock = codeBlock.End();
                    }


                    if (namedObject != null)
                    {
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, namedObject);
                    }




                }
            }

            return codeBlock;
        }

        #endregion

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, SaveClasses.IElement elementAsIElement)
        {
            var element = elementAsIElement as GlueElement;
            HashSet<GlueElement> elementsToLoadStaticContent = new HashSet<GlueElement>();
            for (int i = 0; i < element.CustomVariables.Count; i++)
            {
                CustomVariable customVariable = element.CustomVariables[i];

                // If a CustomVariable references a CSV and if that CustomVariable is shared static,
                // we can't assign it until the CSV is loaded.  Therefore, we will do it in LoadStaticContent
                if (customVariable.IsShared && 
                    (!customVariable.DefinedByBase || customVariable.IsTunneling || customVariable.CreatesEvent) &&
                    ShouldAssignToCsv(customVariable, customVariable.DefaultValue as string, element) &&
                    !ReferencesCsvFromGlobalContent(customVariable))
                {
                    string variableAssignment = " = " + GetAssignmentToCsvItem(customVariable, element, (string)customVariable.DefaultValue);

                    codeBlock.Line(customVariable.Name + variableAssignment + ";");
                    
                }

                // If a custom variable references a state in another element, that state in the other element
                // may require files to be loaded. For example, the state in the other element may reference movement
                // variables in a platformer, which would require the CSV to be loaded.
                // If this is the case, call LoadStaticContent on the container. Sure, it means that we may load unnecessarily, but
                // it would be a lot of logic to make it more efficient. Plus, all-global-content games are probably becoming more and more common.
                var getCategoryResult = ObjectFinder.Self.GetStateSaveCategory(customVariable, element as GlueElement);
                GlueElement ownerOfCategory = null;
                if(getCategoryResult.IsState && getCategoryResult.Category != null)
                {
                    ownerOfCategory = ObjectFinder.Self.GetElementContaining(getCategoryResult.Category);
                }

                if(ownerOfCategory != null && ownerOfCategory != null 
                    // No inheritance relationship to prevent recursion
                    && !ObjectFinder.Self.GetIfInherits(ownerOfCategory, element) 
                    && !ObjectFinder.Self.GetIfInherits(element, ownerOfCategory)
                    && element != ownerOfCategory)
                {
                    elementsToLoadStaticContent.Add(ownerOfCategory);
                    
                }

            }

            if(elementsToLoadStaticContent.Count > 0)
            {
                codeBlock.Line("// Generating LoadStaticContent calls because this element references states from the following elements, and those states may reference files internally.");
            }
            foreach(var elementToLoad in elementsToLoadStaticContent)
            {
                var fullName = CodeWriter.GetGlueElementNamespace(elementToLoad) + "." + elementToLoad.GetStrippedName();
                codeBlock.Line($"{fullName}.LoadStaticContent(contentManagerName);");
            }
            return codeBlock;
        }

        static bool ReferencesCsvFromGlobalContent(CustomVariable customVariable)
        {
            if (customVariable.GetIsCsv() == false)
            {
                return false;
            }
            else
            {

                return customVariable.Type.StartsWith("GlobalContent/");
            }
        }

        static bool IsSourceObjectEnabled(IElement saveObject, CustomVariable variable)
        {
            NamedObjectSave referencedNos = saveObject.GetNamedObjectRecursively(variable.SourceObject);

            return referencedNos != null && referencedNos.IsDisabled == false;
        }
            
            


        private static void CreateAccompanyingVelocityVariables(ICodeBlock codeBlock, CustomVariable customVariable)
        {
            if (customVariable.HasAccompanyingVelocityProperty)
            {
                string variableAssignment = " = 0";

                string type = customVariable.Type;
                if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                {
                    type = customVariable.OverridingPropertyType;
                }

                // Create the variable to store velocity
                codeBlock.Line(StringHelper.Modifiers(Public: true, Static: customVariable.IsShared, Type: type, Name: customVariable.Name + "Velocity") + variableAssignment + ";");

                if (customVariable.Type == "int")
                {
                    codeBlock.Line(StringHelper.Modifiers(Public: true, Static: customVariable.IsShared, Type: "float", Name: customVariable.Name + "ModifiedByVelocity") + variableAssignment + ";");
                }
                else if (customVariable.Type == "long")
                {
                    codeBlock.Line(StringHelper.Modifiers(Public: true, Static: customVariable.IsShared, Type: "double", Name: customVariable.Name + "ModifiedByVelocity") + variableAssignment + ";");
                }
            }
        }

        private static ICodeBlock WritePropertyHeader(ICodeBlock codeBlock, CustomVariable customVariable, string customVariableType)
        {
            ICodeBlock prop;

            bool needsToCloseIf = false;

            if (customVariableType == "Microsoft.Xna.Framework.Color")
            {
                codeBlock.Line("#if XNA3 || SILVERLIGHT");
                prop = codeBlock.Property(customVariable.Name, Public: true,
                                         Override: customVariable.DefinedByBase,
                                         Virtual: (customVariable.SetByDerived && !customVariable.IsShared),
                                         Type: "Microsoft.Xna.Framework.Graphics.Color");
                prop.PreCodeLines.RemoveAt(1); // get rid of its opening bracket
                prop.PostCodeLines.Clear();
                prop.End();
                codeBlock.Line("#else");
                needsToCloseIf = true;
            }


            prop = codeBlock.Property(customVariable.Name, Public: true,
                                     Override: customVariable.DefinedByBase,
                                     Virtual: (customVariable.SetByDerived && !customVariable.IsShared),
                                     Type: customVariableType);
            if (needsToCloseIf)
            {
                prop.PreCodeLines.Insert(1, new CodeLine("#endif"));
            }
            return prop;
        }

        private static void WriteGetterForProperty(CustomVariable customVariable, IElement element, ICodeBlock prop)
        {
            var getter = prop.Get();

            if (customVariable.GetIsSourceFile(element))
            {
                getter.Line("return " + customVariable.SourceObject +";");
            }
            else
            {
                bool shouldGenerateRelative = GetIfShouldGenerateRelative(customVariable, element);

                string relativeVersionOfProperty = null;
                if (shouldGenerateRelative)
                {
                    relativeVersionOfProperty = InstructionManager.GetRelativeForAbsolute(customVariable.SourceObjectProperty);
                }

                if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                {
                    getter = getter.If(customVariable.SourceObject + ".Parent == null");
                }

                string valueToReturn = customVariable.SourceObject + "." + customVariable.SourceObjectProperty;
                if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType) &&
                    // If overriding to the same type, no conversion can be performed
                    customVariable.OverridingPropertyType != customVariable.Type)
                {
                    valueToReturn = TypeConverterHelper.Convert(
                        customVariable, GetterOrSetter.Getter, valueToReturn);
                }
                if (!string.IsNullOrEmpty(valueToReturn))
                {
                    getter.Line("return " + valueToReturn + ";");
                }
                else
                {
                    getter.Line($"throw new System.Exception(\"Glue can't convert from {customVariable.Type} to {customVariable.OverridingPropertyType}\");");
                }



                if (!string.IsNullOrEmpty(relativeVersionOfProperty))
                {
                    getter = getter.End().Else();

                    valueToReturn = customVariable.SourceObject + "." + relativeVersionOfProperty;
                    if (!string.IsNullOrEmpty(customVariable.OverridingPropertyType))
                    {
                        valueToReturn = TypeConverterHelper.Convert(
                            customVariable, GetterOrSetter.Getter, valueToReturn);
                    }


                    getter.Line("return " + valueToReturn + ";");
                    getter.End();
                }
            }
        }

        private static bool GetIfShouldGenerateRelative(CustomVariable customVariable, IElement element)
        {
            NamedObjectSave referencedNos = null;
            if (!string.IsNullOrEmpty(customVariable.SourceObject))
            {
                referencedNos = element.AllNamedObjects.FirstOrDefault(item => item.InstanceName == customVariable.SourceObject);
            }

            return (referencedNos == null && element is EntitySave) ||
                (referencedNos?.GetAssetTypeInfo()?.IsPositionedObject == true);
        }

        private static bool IsVariableWholeNumberWithVelocity(CustomVariable customVariable)
        {
            return customVariable.HasAccompanyingVelocityProperty && (customVariable.Type == "int" || customVariable.Type == "long");
        }


        
        public static string GetMemberTypeFor(CustomVariable customVariable, IElement element)
        {
            customVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable, element as GlueElement);
            NamedObjectSave referencedNos = element.GetNamedObjectRecursively(customVariable.SourceObject);

            string customVariableType;
            bool isTypeFromCsv = false;
            if (IsTypeFromCsv(customVariable, element as GlueElement))
            {
                // This is a type defined in a CSV
                ReferencedFileSave rfsForCsv = ObjectFinder.Self.GetAllReferencedFiles().FirstOrDefault(item =>
                    item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == customVariable.Type);


                if (rfsForCsv != null)
                {
                    customVariableType = rfsForCsv.GetTypeForCsvFile();
                }
                else
                {
                    string unqualifiedType = null;
                    unqualifiedType = FileManager.RemovePath(FileManager.RemoveExtension(customVariable.Type));
                    if (unqualifiedType.EndsWith("File"))
                    {
                        unqualifiedType = unqualifiedType.Substring(0, unqualifiedType.Length - "File".Length);
                    }
                    customVariableType = ProjectManager.ProjectNamespace + ".DataTypes." + unqualifiedType;

                }

                isTypeFromCsv = true;
            }
            else
            {
                customVariableType = customVariable.Type;
            }



            if (customVariable.GetIsVariableState(element as GlueElement))
            {
                // handle old tunneled variables:  tunneled state variables used to set their type
                // as "string" instead of the variable state type.
                // We now set the type to be the variable type (the enum type) but we want existing projects to continue to work
                if (customVariableType == "string")
                {
                    customVariableType = "VariableState";
                }
                if (customVariable.IsTunneling)
                {
                    IElement namedObjectElement = ObjectFinder.Self.GetElement(referencedNos.SourceClassType);
                    if (namedObjectElement != null)
                    {
                        customVariableType = namedObjectElement.Name.Replace("\\", ".") + "." + customVariableType;
                    }
                }
                else
                {
                    var isQualified = customVariableType.Contains('.');
                    if(isQualified == false)
                    {
                        customVariableType = element.Name.Replace("\\", ".") + "." + customVariableType;
                    }
                }
            }
            // CSVs may happen to be named the same as base types - like "Resources",
            // so we only want to use the TypeManager if we didn't first find a CSV.
            // Update June 12, 2018:
            // Someone can name a category "Type", in which case the TypeManager would find something
            // Therefore, we don't want to check that until we first give variable states a chance.
            // This if statement used to be above the custom GetIsVariableState call, but moved it down
            // to give variable states priority
            if (!isTypeFromCsv)
            {

                if(customVariableType.StartsWith("List<"))
                {
                    customVariableType = "System.Collections.Generic." + customVariableType;
                }
                else
                {
                    Type type = null;

                    if (!string.IsNullOrEmpty(customVariableType))
                    {
                        type = TypeManager.GetTypeFromString(customVariableType);
                    }

                    if (type != null)
                    {
                        // If it's a common type we don't want to make things confusing
                        // by using "System.Single", but we do want to fully-qualify things
                        // like FlatRedBall types to make sure we don't get any kind of naming
                        // conflicts
                        // If a value is defined inside a class (like the Borders enum in SpriteFrame)
                        // then its type will come out with a +.  We need to replace that with a dot.
                        customVariableType = TypeManager.GetCommonTypeName(type.FullName).Replace("+", ".");
                    }
                }
            }

            return customVariableType;
        }

        public static IElement GetElementIfCustomVariableIsVariableState(CustomVariable customVariable, GlueElement saveObject = null)
        {

            saveObject = saveObject ?? ObjectFinder.Self.GetElementContaining(customVariable);
            if (customVariable.GetIsVariableState(saveObject) && string.IsNullOrEmpty(customVariable.SourceObject))
            {
                return saveObject;
            }
            else
            {
                customVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);
                NamedObjectSave sourceNamedObjectSave = saveObject?.GetNamedObjectRecursively(customVariable.SourceObject);

                if (sourceNamedObjectSave != null)
                {
                    EntitySave sourceEntitySave = ObjectFinder.Self.GetEntitySave(sourceNamedObjectSave.SourceClassType);

                    if (sourceEntitySave != null &&
                        ((sourceEntitySave.States.Count != 0 && customVariable.SourceObjectProperty == "CurrentState") ||
                        sourceEntitySave.StateCategoryList.ContainsCategoryName(customVariable.Type))
                        )
                    {
                        return sourceEntitySave;
                    }
                    else if (sourceEntitySave == null)
                    {
                        ScreenSave sourceScreenSave = ObjectFinder.Self.GetScreenSave(sourceNamedObjectSave.SourceClassType);

                        if (sourceScreenSave != null && sourceScreenSave.States.Count != 0 && customVariable.SourceObjectProperty == "CurrentState")
                        {
                            return sourceScreenSave;
                        }

                    }
                }
                return null;
            }
        }



        private static bool IsVariableTunnelingToDisabledObject(CustomVariable customVariable, IElement saveObject)
        {
            if (string.IsNullOrEmpty(customVariable.SourceObject))
            {
                return false;
            }
            else
            {
                NamedObjectSave nos = saveObject.GetNamedObjectRecursively(customVariable.SourceObject);

                return nos != null && nos.IsDisabled;
            }
        }

        public static bool ShouldAssignToCsv(CustomVariable customVariable, string variableToAssign, GlueElement element = null)
        {
            return 
                !string.IsNullOrEmpty(variableToAssign) && 
                variableToAssign != "<NULL>" && 
                variableToAssign != "\"\"" && 
                IsTypeFromCsv(customVariable, element) && 
                !customVariable.GetIsListCsv();
        }

        public static string GetAssignmentToCsvItem(CustomVariable customVariable, IElement saveObject, string variableToAssign)
        {

            string preferredFile = null;

            // strip off the ""'s so we can modify the value, then bring them back
            if (variableToAssign.StartsWith("\"") && variableToAssign.EndsWith("\""))
            {
                variableToAssign = variableToAssign.Substring(1, variableToAssign.Length - 2);
            }

            if (variableToAssign.Contains(" in "))
            {
                string originalValue = variableToAssign;

                int index = variableToAssign.LastIndexOf(" in ");

                variableToAssign = variableToAssign.Substring(0, index);

                preferredFile = originalValue.Substring(index + " in ".Length);
            }

            if (variableToAssign == "<NULL>")
            {
                variableToAssign = "null";
            }
            else if (customVariable.Type.StartsWith("GlobalContent/"))
            {
                variableToAssign = "GlobalContent." + FileManager.RemovePath(FileManager.RemoveExtension(customVariable.Type)) + "[\"" + variableToAssign + "\"]";
            }
            else
            {
                IEnumerable<ReferencedFileSave> files = ObjectFinder.Self.GetAllReferencedFiles().Where(item =>
                    item.IsCsvOrTreatedAsCsv && (item.GetTypeForCsvFile() == customVariable.Type || item.Name == customVariable.Type));

                // Which one contains this value?
                ReferencedFileSave rfs = null;

                if (!string.IsNullOrEmpty(preferredFile))
                {
                    rfs = files.FirstOrDefault(item => item.Name.EndsWith(preferredFile));
                }
                else
                {
                    rfs = files.FirstOrDefault();
                }

                if (rfs != null)
                {
                    // Victor Chelaru
                    // August 17, 2013
                    // We used to assume
                    // the class name prefixed
                    // to the rfs.GetInstanceName
                    // but this may be referenced from
                    // outside of the element that defined
                    // the CSV, and in that case we need to
                    // fully-qualify the property.  It's okay
                    // if we qualify for references within the class:
                    var container = rfs.GetContainer();

                    if (container == null)
                    {
                        string prefix = "GlobalContent." + rfs.GetInstanceName();
                        variableToAssign =  prefix + "[\"" + variableToAssign + "\"]";
                    
                    }
                    else
                    {
                        // Don't remove the path, we want the prefix:
                        //string prefix = FileManager.RemovePath(saveObject.Name);
                        string prefix = saveObject.Name.Replace('\\', '.');
                        variableToAssign = prefix + "." + rfs.GetInstanceName() + "[\"" + variableToAssign + "\"]";
                    }
                }
            }
            return variableToAssign;
        }

        public static ICodeBlock AppendAssignmentForCustomVariableInInstance(NamedObjectSave namedObject, ICodeBlock codeBlock, 
            InstructionSave instructionSave, GlueElement nosOwner)
        {
            // We don't support assigning lists yet, and lists are generic, so we're going to test for generic assignments
            // Eventually I may need to make this a little more accurate.
            // Update August 22, 2022
            // Now we do support lists!
            //if (instructionSave.Value != null && instructionSave.Value.GetType().IsGenericType == false)
            if (instructionSave.Value == null)
            {
                return codeBlock;
            }

            bool usesStandardCodeGen = true;

            var ati = namedObject.GetAssetTypeInfo();
            var foundVariableDefinition = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == instructionSave.Member);
            if(foundVariableDefinition != null && foundVariableDefinition.UsesCustomCodeGeneration)
            {
                usesStandardCodeGen = false;
            }

            if(foundVariableDefinition?.CustomGenerationFunc != null)
            {
                if(nosOwner != null)
                {
                    var line = foundVariableDefinition.CustomGenerationFunc(nosOwner, namedObject, null, instructionSave.Member);
                    if(!string.IsNullOrWhiteSpace(line))
                    {
                        codeBlock.Line(line);
                    }
                }
            }
            else if (usesStandardCodeGen)
            {
                CustomVariable customVariable = null;
                EntitySave entitySave = null;
                if (namedObject.SourceType == SourceType.Entity && !string.IsNullOrEmpty(namedObject.SourceClassType))
                {
                    entitySave = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassType);
                    if (entitySave != null)
                    {
                        customVariable = entitySave.GetCustomVariable(instructionSave.Member);
                    }
                }


                IElement rootElementForVariable = entitySave;
                string rootVariable = instructionSave.Member;
                var handledByTunneledCustomCodeGeneration = false;
                while (customVariable != null && customVariable.IsTunneling)
                {
                    NamedObjectSave referencedNamedObject = rootElementForVariable.GetNamedObjectRecursively(customVariable.SourceObject);
                    if (referencedNamedObject != null && referencedNamedObject.IsFullyDefined && referencedNamedObject.SourceType == SourceType.Entity)
                    {
                        rootElementForVariable = ObjectFinder.Self.GetElement(referencedNamedObject.SourceClassType);
                        rootVariable = customVariable.SourceObjectProperty;

                        customVariable = rootElementForVariable.GetCustomVariable(customVariable.SourceObjectProperty);
                    }
                    else
                    {
                        var nosAti = referencedNamedObject?.GetAssetTypeInfo();
                        var nosAtiVariableDefinition = nosAti.VariableDefinitions?.FirstOrDefault(item => item.Name == customVariable.SourceObjectProperty);

                        if (nosAtiVariableDefinition?.CustomGenerationFunc != null)
                        {
                            handledByTunneledCustomCodeGeneration = true;
                            var line = nosAtiVariableDefinition.CustomGenerationFunc(nosOwner, namedObject, null, customVariable.Name);
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                codeBlock.Line(line);
                            }
                        }

                        break;
                    }
                }

                // We do a check up top to see if we should skip generation, but that's based on null values.
                // This check requires a little more context.
                bool shouldSkipGeneration = customVariable?.GetIsVariableState(entitySave) == true &&
                    (instructionSave.Value as string) == "<NONE>";

                if(!shouldSkipGeneration && !handledByTunneledCustomCodeGeneration)
                {
                    AppendCustomVariableInInstanceStandard(namedObject, codeBlock, instructionSave, ati, entitySave, customVariable, rootVariable);

                }
            }
            else
            {
                PluginManager.WriteInstanceVariableAssignment(namedObject, codeBlock, instructionSave);
            }

            return codeBlock;
        }

        private static void AppendCustomVariableInInstanceStandard(NamedObjectSave namedObject, ICodeBlock codeBlock, 
            InstructionSave instructionSave, AssetTypeInfo ati, IElement entitySave, CustomVariable customVariable, string rootVariable)
        {
            // old code - this is replaced by GetRightSideOfEquals, but leaving this here in case anyone reports codegen errors
            //object objectToParse = instructionSave.Value;
            //#region Determine the right-side value to assign
            //string value = CodeParser.ConvertValueToCodeString(objectToParse);
            //if (CustomVariableExtensionMethods.GetIsFile(instructionSave.Type))
            //{
            //    value = value.Replace("\"", "").Replace("-", "_");
            //    if (value == "<NONE>")
            //    {
            //        value = "null";
            //    }
            //}
            //else if (ShouldAssignToCsv(customVariable, value))
            //{
            //    value = GetAssignmentToCsvItem(customVariable, entitySave, value);
            //}
            //else if (instructionSave.Type == "Color" || instructionSave.Type == "Microsoft.Xna.Framework.Color")
            //{
            //    value = "Microsoft.Xna.Framework.Color." + value.Replace("\"", "");
            //}
            //else if (instructionSave.Type == "FlatRedBall.Sprite" || instructionSave.Type == "Sprite")
            //{
            //    value = value.Replace("\"", "");
            //}
            //else if ((customVariable != null && customVariable.GetIsVariableState()) || (customVariable == null && rootVariable == "CurrentState"))
            //{
            //    //string type = "VariableState";
            //    //if (customVariable != null && customVariable.Type.ToLower() != "string")
            //    //{
            //    //    type = customVariable.Type;
            //    //}
            //    value = StateCodeGenerator.GetRightSideAssignmentValueAsString(entitySave, instructionSave);
            //}
            //else
            //{
            //    value = CodeWriter.MakeLocalizedIfNecessary(namedObject, instructionSave.Member, objectToParse, value, null);
            //}
            //if (namedObject.DoesMemberNeedToBeSetByContainer(instructionSave.Member))
            //{
            //    value = value.Replace("\"", "");
            //}
            //#endregion





            var rightSide = GetRightSideOfEquals(customVariable, entitySave as GlueElement, instructionSave.Value, instructionSave.Type);

            //if(value != rightSide)
            //{
            //    int m = 3;
            //}

            string leftSideMember = instructionSave.Member;

            bool makeRelative = !string.IsNullOrEmpty(InstructionManager.GetRelativeForAbsolute(leftSideMember));

            if (makeRelative)
            {
                if (ati != null)
                {
                    // If it can't attach, then don't try to set relative values
                    makeRelative &= ati.ShouldAttach;
                }
            }

            var objectName = namedObject.InstanceName;

            if (makeRelative)
            {
                codeBlock = codeBlock.If(objectName + ".Parent == null");
            }

            bool needsEndif = false;

            if (namedObject.DefinedByBase)
            {
                objectName = "base." + objectName;
            }

            if(rightSide != null)
            {
                // If it's null, then this will be an invalid line of code
                codeBlock.Line(objectName + "." + instructionSave.Member + " = " + rightSide + ";");
            }


            if (needsEndif)
            {
                codeBlock.Line("#endif");
            }

            if (makeRelative)
            {
                codeBlock = codeBlock.End().Else();

                // Special Case!
                // If this NOS is
                // attached to a Camera
                // then we have to subtract
                // 40 from the position.
                if (namedObject.AttachToCamera && leftSideMember == "Z")
                {
                    rightSide = rightSide + " - 40.0f";
                }

                codeBlock.Line(objectName + "." + InstructionManager.GetRelativeForAbsolute(leftSideMember) + " = " + rightSide + ";");
                codeBlock.End();
            }
        }

        public static void WriteVelocityForCustomVariables(List<CustomVariable> customVariableList, ICodeBlock codeBlock)
        {
            foreach (CustomVariable customVariable in customVariableList)
            {
                if (!customVariable.DefinedByBase && customVariable.HasAccompanyingVelocityProperty)
                {
                    string velocityName = customVariable.Name + "Velocity";
                    codeBlock = codeBlock.If(velocityName + "!= 0");

                    if (customVariable.Type == "int" ||
                        customVariable.Type == "long")
                    {
                        string function;
                        if(customVariable.Type == "int")
                        {
                            function = "FlatRedBall.Math.MathFunctions.RoundToInt";
                        }
                        else
                        {
                            function = "FlatRedBall.Math.MathFunctions.RoundToLong";
                        }
                        codeBlock.Line(customVariable.Name + "ModifiedByVelocity += " + velocityName +
                         " * FlatRedBall.TimeManager.SecondDifference;");

                        codeBlock.Line(customVariable.Type + " converted = " + function + "(" + customVariable.Name + "ModifiedByVelocity" + ");");
                        codeBlock = codeBlock.If("converted != " + customVariable.Name);
                        codeBlock.Line("var temp = " + customVariable.Name + "ModifiedByVelocity;");
                        codeBlock.Line(customVariable.Name + " = converted;");
                        codeBlock.Line(customVariable.Name + "ModifiedByVelocity = temp;");

                        codeBlock = codeBlock.End();
                    }
                    else
                    {
                        codeBlock.Line(customVariable.Name + " += " + velocityName +
                            " * FlatRedBall.TimeManager.SecondDifference;");
                    }
                    codeBlock = codeBlock.End();
                    
                }
            }
        }

        internal static bool IsTypeFromCsv(CustomVariable customVariable, GlueElement glueElement = null)
        {
            if(customVariable != null && customVariable.Type != null &&
                customVariable.GetIsVariableState(glueElement) == false &&
                customVariable.GetIsBaseElementType() == false &&
                customVariable.Type.Contains(".") &&
                customVariable.GetRuntimeType() == null)
            {
                var isCsv = true;
                // If it's from CSV, there will be no asset type info for the CSV:
                if(customVariable.IsTunneling)
                {
                    var containingElement = ObjectFinder.Self.GetElementContaining(customVariable);
                    var nos = containingElement?.GetNamedObject(customVariable.SourceObject);

                    if(nos != null)
                    {
                        if(nos.SourceType != SourceType.Entity)
                        {
                            isCsv = false;
                        }
                    }                    
                }

                return isCsv;
            }
            return false;
        }
    }
}
