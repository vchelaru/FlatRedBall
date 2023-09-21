using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Controls;
using System.Reflection;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.Particle;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Text.RegularExpressions;

namespace FlatRedBall.Glue.CodeGeneration
{
    #region Enums

    public enum CodeGenerationType
    {
        Full,
        OnlyContainedObjects,
        Nothing
    }

    #endregion

    public class NamedObjectSaveCodeGenerator : ElementComponentCodeGenerator
    {
        public static Dictionary<string, string> ReusableEntireFileRfses { get; set; }

        #region Fields

        public static void GenerateFieldAndPropertyForNamedObject(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {

            string typeName = GetQualifiedTypeName(namedObjectSave);

            CodeGenerationType codeGenerationType = GetFieldCodeGenerationType(namedObjectSave);

            if (codeGenerationType == CodeGenerationType.OnlyContainedObjects)
            {
                // Since the base defines it, we don't want to define the object here; however,
                // any objects that it contains will not be defined by base, so we do need to loop
                // through contained NamedObjectSaves and define those.  We also want to create variable
                // reset fields.
                CreateVariableResetField(namedObjectSave, typeName, codeBlock);

                foreach (NamedObjectSave childNos in namedObjectSave.ContainedObjects)
                {
                    GenerateFieldAndPropertyForNamedObject(childNos, codeBlock);
                }
            }

            else if(codeGenerationType == CodeGenerationType.Full)
            {
                AddIfConditionalSymbolIfNecesssary(codeBlock, namedObjectSave);

                #region Get the Access Modifier
                string accessModifier = "private";



                if (namedObjectSave.SetByDerived || namedObjectSave.ExposedInDerived)
                {
                    accessModifier = "protected";
                }

                #endregion

                string variableName = namedObjectSave.FieldName;

                // June 26, 2022
                // There's a special
                // case for Lists which
                // need to be instantiated
                // before any other logic (such
                // as adding items to the list) is
                // performed. However, if this is a
                // derived entity, the base will have
                // its initialize called before the derived
                // and that means instances will be added to
                // the list when it's null. To address this, the
                // list will be initialized here:
                if(namedObjectSave.IsList)
                {
                    codeBlock.Line(StringHelper.SpaceStrings(accessModifier, typeName, variableName) + $" = new {typeName}();");
                }
                else
                {
                    codeBlock.Line(StringHelper.SpaceStrings(accessModifier, typeName, variableName) + ";");
                }

                #region If should create public property

                bool shouldCreateProperty = namedObjectSave.HasPublicProperty || namedObjectSave.SetByContainer;

                if (shouldCreateProperty)
                {
                    var prop = codeBlock.Property(StringHelper.SpaceStrings("public", typeName),
                                                  namedObjectSave.InstanceName);
                    prop.Get()
                        .Line("return " + variableName + ";");
                    if (namedObjectSave.SetByContainer)
                    {
                        prop.Set()
                            .Line(variableName + " = value;");
                    }
                    else if (namedObjectSave.SetByDerived)
                    {
                        prop.Set("protected")
                            .Line(variableName + " = value;");
                    }
                    else
                    {
                        // This should still have a private setter to not break
                        // internal code
                        prop.Set("private")
                         .Line(variableName + " = value;");
                    }
                }
                #endregion

                CreateVariableResetField(namedObjectSave, typeName, codeBlock);

                // If this NamedObjectSave has children, then create fields for those too
                foreach (NamedObjectSave childNos in namedObjectSave.ContainedObjects)
                {
                    GenerateFieldAndPropertyForNamedObject(childNos, codeBlock);
                }
                AddEndIfIfNecessary(codeBlock, namedObjectSave);
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            codeBlock.Line("");

            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                var namedObject = element.NamedObjects[i];

                GenerateFieldAndPropertyForNamedObject(namedObject, codeBlock);
            }

            return codeBlock;
        }


        #endregion

        #region Instantiate/Initialize

        public override ICodeBlock GenerateConstructor(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            var constructorSortedNamedObjects = element.NamedObjects
                // never make collision relationships in the constructor
                .Where(item => item.IsCollisionRelationship() == false)
                // entire files first
                .OrderBy(item => item.IsEntireFile == false);

            foreach (var nos in constructorSortedNamedObjects)
            {
                WriteCodeForNamedObjectInitialize(nos, element, codeBlock, null, inConstructor: true);
            }

            return codeBlock;
        }

        public static void WriteCodeForNamedObjectInitialize(NamedObjectSave namedObject, IElement saveObject,
            ICodeBlock codeBlock, string overridingContainerName, bool inConstructor = false)
        {
            var referencedFilesAlreadyUsingFullFile = ReusableEntireFileRfses;

            CodeGenerationType codeGenerationType = GetInitializeCodeGenerationType(namedObject, saveObject);

            #region ///////////////////////EARLY OUT///////////////////////////////

            if (codeGenerationType == CodeGenerationType.Nothing)
            {
                codeBlock.Line($"// Not instantiating for {namedObject} because properties on the object prevent it");
                return;
            }

            #endregion ////////////////////////END EARLY OUT///////////////////////

            AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);

            bool instantiateInConstructor = namedObject.ShouldInstantiateInConstructor();

            bool succeeded = true;

            #region Perform instantiation

            var shouldInstantiate = !namedObject.InstantiatedByBase;

            if (shouldInstantiate)
            {
                // Ensure that the nos is only instantiated where it should be. Since this method
                // can be called for the constructor or Initialize() method, we need to check that
                // we're in the proper place for generating the instantiation code.
                if (inConstructor == instantiateInConstructor)
                {
                    succeeded = GenerateInstantiationOrAssignment(
                        namedObject, saveObject, codeBlock, overridingContainerName, referencedFilesAlreadyUsingFullFile);
                }
                // If the nos is a list, was instantiated in the constructor, and we aren't in the
                // constructor, then we should clear any items out of it before anything new is
                // added in. This isn't necessary for scenes, but it's needed for pooled entities.
                if (namedObject.IsList && instantiateInConstructor && !inConstructor)
                {
                    // If this list is declared in a derived entity but not the base, then
                    // this code can get called before the instance is instantiated. In that
                    // case we don't care about actually clearing it because it's null anyway. 
                    // Therefore, adding null coalescing:
                    codeBlock.Line($"{namedObject.FieldName}?.Clear();");
                }
            }
            else
            {
                codeBlock.Line($"// skipping instantiation of {namedObject} because it has its InstantiatedByBase set to true");
            }
            #endregion

            if (succeeded)
            {

                #region Set the SetEvents for any variable change

                for (int i = 0; i < namedObject.InstructionSaves.Count; i++)
                {

                    CustomVariableInNamedObject customVariable = namedObject.InstructionSaves[i];

                    if (!string.IsNullOrEmpty(customVariable.EventOnSet) &&
                        ExposedVariableManager.IsExposedVariable(customVariable.Member, namedObject)
                        )
                    {
                        codeBlock.Line(namedObject.InstanceName + "." + customVariable.Member + "SetEvent += " +
                                       customVariable.EventOnSet + ";");
                    }
                }

                #endregion

                if (!inConstructor)
                {
                    // Text specific initialization and contained object initialization should only
                    // take place in the regular Initialize() method and never in the constructor.

                    WriteTextSpecificInitialization(namedObject, saveObject, codeBlock, referencedFilesAlreadyUsingFullFile);

                    foreach (NamedObjectSave containedNos in namedObject.ContainedObjects)
                    {
                        WriteCodeForNamedObjectInitialize(containedNos, saveObject, codeBlock, null);
                    }
                }
            }

            AddEndIfIfNecessary(codeBlock, namedObject);
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, SaveClasses.IElement element)
        {

            // Do the named object saves

            var sortedNamedObjects = element.NamedObjects
                // These will be done "late"
                .Where(item => item.IsCollisionRelationship() == false)
                // entire files first
                .OrderBy(item => item.IsEntireFile == false);

            foreach(var nos in sortedNamedObjects)
            {
                WriteCodeForNamedObjectInitialize(nos, element, codeBlock, null);
            }

            return codeBlock;
        }

        // Relationships need to be assigned after all other objects. To do that, we'll explicitly call this from CodeWriter
        public static void GenerateCollisionRelationships(ICodeBlock codeBlock, IElement element)
        {
            var sortedNamedObjects = element.NamedObjects
                 .Where(item => item.IsCollisionRelationship())
                 // entire files first
                 .OrderBy(item => item.IsEntireFile == false);

            foreach (var nos in sortedNamedObjects)
            {
                WriteCodeForNamedObjectInitialize(nos, element, codeBlock, null);
            }
        }

        #endregion

        #region Assign Variables

        /// <summary>
        /// Generates variable assignment, this is used in PostInitialize
        /// </summary>
        /// <param name="namedObject"></param>
        /// <param name="codeBlock"></param>
        public static void GenerateVariableAssignment(NamedObjectSave namedObject, ICodeBlock codeBlock, GlueElement container)
        {

            List<CustomVariableInNamedObject> variables = namedObject.InstructionSaves;
            var ati = namedObject.GetAssetTypeInfo();

            var variableDefinitions = ati?.VariableDefinitions;
            

            if (variableDefinitions != null)
            {
                // 
                variables = variables.OrderBy(item =>
                    {
                        var matching = variableDefinitions.FirstOrDefault(definition => definition.Name == item.Member);

                        if(matching == null)
                        {
                            return -1;
                        }
                        else
                        {
                            return variableDefinitions.IndexOf(matching);
                        }
                    })
                    .ToList();

            }

            foreach (var instructionSave in variables)
            {
                GlueElement nosTypeElement = null;

                if (namedObject.SourceType == SourceType.Entity)
                {
                    nosTypeElement = ObjectFinder.Self.GetElement(namedObject.SourceClassType);
                }

                var shouldGenerateVariableAssignment =
                    ExposedVariableManager.IsExposedVariable(instructionSave.Member, namedObject) ||
                    (nosTypeElement != null && nosTypeElement.GetCustomVariableRecursively(instructionSave.Member) != null) ||
                    // Could be something set by container:
                    (nosTypeElement != null && nosTypeElement.GetNamedObjectRecursively(instructionSave.Member) != null) ||
                    variableDefinitions?.Any(item => item.Name == instructionSave.Member) == true;

                if (shouldGenerateVariableAssignment)
                {
                    CustomVariableCodeGenerator.AppendAssignmentForCustomVariableInInstance(namedObject, codeBlock, instructionSave, container);
                }
            }
        }

        #endregion

        #region Add To Managers

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            NamedObjectSaveCodeGenerator.WriteAddToManagersBottomUpForNamedObjectList(
                element.NamedObjects, codeBlock, element,  CodeWriter.ReusableEntireFileRfses);

            return codeBlock;
        }

        #endregion

        #region Remove from Managers
        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, IElement element)
        {
            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                var namedObject = element.NamedObjects[i];

                bool shouldSkip = GetShouldSkipDestroyOn(namedObject);

                if(!shouldSkip)
                {
                    GenerateRemoveFromManagersForNamedObject(element, namedObject, codeBlock);
                }

            }
        }

        #endregion

        #region Destroy

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, SaveClasses.IElement element)
        {

            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                var nos = element.NamedObjects[i];

                // Lists of Entities which inherit from FRB types
                // should be made one-way before the destroy calls
                // happen.  The reason is that the Entities may be on
                // layers, and if the Layer gets destroyed first, then
                // the list will be cleared (all contained objects on the
                // layer will have their RemoveSelfFromListsBelongingTo called).
                // This means the Entities will never have the opportunity to call
                // Destroy
                if (ShouldGeneratePreDestroyMakeOneWay(nos))
                {
                    codeBlock.Line(nos.InstanceName + ".MakeOneWay();");
                }

            }


            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                // Vic says:  We used to manually call UnloadStaticContent on all Entities.  But this
                // doesn't work well with inheritance.  Instead, we now use the ContentManagers UnloadContent method support
                GetDestroyForNamedObject(element, element.NamedObjects[i], codeBlock);
            }


            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                var nos = element.NamedObjects[i];

                // Lists of Entities which inherit from FRB types
                // should be made one-way before the destroy calls
                // happen.  The reason is that the Entities may be on
                // layers, and if the Layer gets destroyed first, then
                // the list will be cleared (all contained objects on the
                // layer will have their RemoveSelfFromListsBelongingTo called).
                // This means the Entities will never have the opportunity to call
                // Destroy
                if (ShouldGeneratePreDestroyMakeOneWay(nos))
                {
                    codeBlock.Line(nos.InstanceName + ".MakeTwoWay();");
                }

            }



            return codeBlock;
        }

        private bool ShouldGeneratePreDestroyMakeOneWay(NamedObjectSave nos)
        {
            // right now we'll make all lists one-way - do we want to make them two way later?
            return nos.IsFullyDefined && !nos.SetByContainer && !nos.SetByDerived && nos.IsList && !nos.IsDisabled;
        }

        #endregion

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            #region Loop through all NamedObjects

            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                GetActivityForNamedObject(element.NamedObjects[i], codeBlock);
            }

            #endregion

            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            return codeBlock;
        }

        public override ICodeBlock GenerateLoadStaticContent(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            #region Loop through all contained Objects and load static on them if they are Screens or Entities

            List<string> typesAlreadyLoaded = new List<string>();

            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                string qualifiedType = GetQualifiedTypeName(nos);

                if ((nos.SourceType == SourceType.Entity) && !typesAlreadyLoaded.Contains(qualifiedType) && nos.IsFullyDefined)
                {
                    typesAlreadyLoaded.Add(qualifiedType);
                    codeBlock.Line(qualifiedType + ".LoadStaticContent(contentManagerName);");
                }
            }


            #endregion

            return codeBlock;
        }


        private static bool GenerateInstantiationOrAssignment(NamedObjectSave namedObject, IElement saveObject, 
            ICodeBlock codeBlock, string overridingName, Dictionary<string, string> referencedFilesAlreadyUsingFullFile)
        {


            bool succeeded = true;

            #region If SourceType is File
            if (namedObject.SourceType == SourceType.File)
            {
                AssetTypeInfo nosAti = namedObject.GetAssetTypeInfo();

                if (string.IsNullOrEmpty(namedObject.SourceFile))
                {
                    succeeded = false;
                }
                else
                {
                    EntitySave entitySave = null;
                    ReferencedFileSave rfs = GetReferencedFileSaveReferencedByNamedObject(namedObject, saveObject, ref entitySave);

                    #region //////////////////////EARLY OUT!!!!  Exit out if the RFS is null or the name is bad

                    if (rfs == null && string.IsNullOrEmpty(overridingName))
                    {
                        if (!string.IsNullOrEmpty(namedObject.SourceFile))
                        {
                            // July 11, 2011
                            // I used to think
                            // that it was best
                            // to "should loudly"
                            // whenever encountering
                            // a bug, however, breaking
                            // the build for everyone is
                            // not the way to do it.  Intead
                            // I'm going to make this not generate
                            // any code if there is no SourceFile, and
                            // we should have some way in Glue to search
                            // for and find incomplete definitions.
                            //string exceptionString = "This object references the file " +
                            //    namedObject.SourceFile + " which is not part of this object.";

                            //stringBuilder.AppendLine("\t\t\tthrow new InvalidOperationException(\"" + exceptionString + "\");");
                            succeeded = false;
                        }
                        else
                        {
                            succeeded = false;
                        }
                    }



                    if ((string.IsNullOrEmpty(namedObject.SourceName) || namedObject.SourceName == "<NONE>") &&
                        FileManager.GetExtension(namedObject.SourceFile) != "srgx")
                    {
                        succeeded = false;
                    }
                    #endregion

                    if (succeeded)
                    {
                        string objectName = namedObject.FieldName;


                        //InstantiateObjectUsingFile(namedObject, codeBlock, referencedFilesAlreadyUsingFullFile, rfs, saveObject, containerName, overridingName);
                        WriteMethodForClone(namedObject, codeBlock, referencedFilesAlreadyUsingFullFile, rfs, saveObject, overridingName);
                        
                    }
                }
            }

            #endregion

            else if (namedObject.SourceType == SourceType.FlatRedBallType || namedObject.SourceType == SourceType.Gum)
            {
                string objectName = namedObject.FieldName;
                AssetTypeInfo nosAti = namedObject.GetAssetTypeInfo();

                if (nosAti?.ConstructorFunc != null)
                {
                    string line = nosAti.ConstructorFunc(saveObject, namedObject, null);
                    codeBlock.Line(line);
                }
                // We treat Cameras in a special way:
                // Eventually we should move this to an ATI that uses custom code (like in the if-statement above)
                else if (namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Camera)
                {
                    if (namedObject.IsNewCamera)
                    {
                        string contentManagerNameString = "ContentManagerName";
                        codeBlock.Line(objectName + " = new FlatRedBall.Camera(" + contentManagerNameString + ");");
                        codeBlock.Line("FlatRedBall.SpriteManager.Cameras.Add(" + objectName + ");");
                    }
                    else
                    {
                        codeBlock.Line($"{objectName} = FlatRedBall.SpriteManager.Camera;");

                    }
                }
                else if (namedObject.IsContainer)
                {
                    codeBlock.Line(objectName + " = this;");
                }
                else
                {
                    string qualifiedName = GetQualifiedTypeName(namedObject);

                    if(string.IsNullOrEmpty(qualifiedName))
                    {
                        codeBlock.Line($"// skipping code generation for {objectName} because it does not have its type assigned.");
                    }
                    else
                    {
                        var isInstantiatedOnField = namedObject.IsList;
                        if(!isInstantiatedOnField)
                        {
                            codeBlock.Line($"{objectName} = new {qualifiedName}();");
                        }

                        if (namedObject.IsLayer || 
                            namedObject.SourceType == SourceType.FlatRedBallType)
                        {
                            codeBlock.Line($"{objectName}.Name = \"{namedObject.InstanceName}\";");

                            if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode)
                            {
                                var hasCreationSource = nosAti?.IsPositionedObject == true;
                                if(hasCreationSource)
                                {
                                    codeBlock.Line($"{objectName}.CreationSource = \"Glue\";");
                                }
                            }
                        }
                    }

                }


            }

            #region else if SourceType is Entity

            else // SourceType == SourceType.Entity
            {
                string objectName = namedObject.FieldName;

                codeBlock.Line(string.Format("{0} = new {1}(ContentManagerName, false);", objectName,
                                             GetQualifiedTypeName(namedObject)));
                codeBlock.Line($"{objectName}.Name = \"{namedObject.InstanceName}\";");

                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode)
                {
                    codeBlock.Line($"{objectName}.CreationSource = \"Glue\";");
                }
            }

            #endregion


            return succeeded;
        }

        private static ReferencedFileSave GetReferencedFileSaveReferencedByNamedObject(NamedObjectSave namedObject, IElement saveObject, ref EntitySave entitySave)
        {
            ReferencedFileSave rfs = null;
            if (saveObject is EntitySave)
            {
                entitySave = saveObject as EntitySave;
                rfs = entitySave.GetReferencedFileSave(namedObject.SourceFile);
            }
            else
            {
                rfs = (saveObject as ScreenSave).GetReferencedFileSave(namedObject.SourceFile);
            }
            return rfs;
        }

        private static void InstantiateObjectInSwitchStatement(NamedObjectSave namedObject, ICodeBlock codeBlock,
            Dictionary<string, string> referencedFilesAlreadyUsingFullFile, ReferencedFileSave rfs,
            List<StateSave> stateSaves, IElement saveObject, string overridingName)
        {
            string containerName = overridingName;
            if (rfs != null)
            {
                containerName = rfs.GetInstanceName();// FileManager.RemovePath(FileManager.RemoveExtension(namedObject.SourceFile));
            }

            var switchBlock = codeBlock.Switch("LoadingState");

            for (int i = 0; i < stateSaves.Count; i++)
            {
                StateSave stateSave = stateSaves[i];

                WriteMethodForClone(namedObject, switchBlock.Case("VariableState." + stateSave.Name), referencedFilesAlreadyUsingFullFile, rfs, 
                    saveObject, overridingName);

            }

            //InstantiateObjectUsingFile(namedObject, switchBlock.Case("VariableState.Uninitialized:"), referencedFilesAlreadyUsingFullFile, rfs,
            //    saveObject, defaultContainer, overridingName);
            WriteMethodForClone(namedObject, switchBlock.Case("VariableState.Uninitialized:"), referencedFilesAlreadyUsingFullFile, rfs,
                saveObject, overridingName);

        }


        private static void WriteCopyToAbsoluteInInitializeCode(NamedObjectSave namedObject, ICodeBlock codeBlock, 
            GlueElement container,
            Dictionary<string, string> referencedFilesAlreadyUsingFullFile, AssetTypeInfo ati, string objectName, ReferencedFileSave rfs)
        {
            bool canWriteAbsoluteInitialize = GetIfCanAttach(namedObject, ati, container);

            if (canWriteAbsoluteInitialize)
            {
                bool isEntireFile = namedObject.SourceName != null && namedObject.SourceName.StartsWith("Entire File (");
                string copyRelativeToAbsolute;

                if (namedObject.InstanceType == "Scene" ||
                    namedObject.InstanceType == "ShapeCollection"
                    )
                {
                    copyRelativeToAbsolute = ".CopyAbsoluteToRelative(false)";
                }
                else
                {
                    copyRelativeToAbsolute = ".CopyAbsoluteToRelative()";
                }
                string namedObjectToPullFrom = null;


                if (rfs != null)
                {
                    if (!isEntireFile && referencedFilesAlreadyUsingFullFile.ContainsKey(rfs.Name))
                    {
                        namedObjectToPullFrom = referencedFilesAlreadyUsingFullFile[rfs.Name];
                    }
                }

                if (string.IsNullOrEmpty(namedObjectToPullFrom))
                {
                    codeBlock.Line(string.Format("{0}{1};",
                                                 objectName,
                                                 copyRelativeToAbsolute));
                }
            }
        }

        private static bool GetIfCanAttach(NamedObjectSave namedObject, AssetTypeInfo ati, GlueElement container)
        {
            var canWriteAbsoluteInitialize =
                (ati != null && ati.ShouldAttach) ||
                namedObject.SourceType == SourceType.Entity &&
                !string.IsNullOrEmpty(namedObject.SourceClassType) &&
                !namedObject.IsContainer;

            if (canWriteAbsoluteInitialize && namedObject.DefinedByBase)
            {
                var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(container);

                var isContainerFromBase = baseElements
                    .SelectMany(item => item.AllNamedObjects)
                    .Any(item => item.InstanceName == namedObject.InstanceName && item.IsContainer);

                if (isContainerFromBase)
                {
                    canWriteAbsoluteInitialize = false;
                }
            }

            return canWriteAbsoluteInitialize;
        }

        private static void WriteMethodForClone(NamedObjectSave namedObject, ICodeBlock codeBlock,
            Dictionary<string, string> referencedFilesAlreadyUsingFullFile,
            ReferencedFileSave rfs, IElement container, string overridingContainerName)
        {
            string containerName = overridingContainerName;
            if (rfs != null)
            {
                containerName = rfs.GetInstanceName();// FileManager.RemovePath(FileManager.RemoveExtension(namedObject.SourceFile));
            }

            AssetTypeInfo nosAti = namedObject.GetAssetTypeInfo();

            if(nosAti?.GetObjectFromFileFunc != null)
            {
                var code = nosAti.GetObjectFromFileFunc(container, namedObject, rfs, overridingContainerName);

                codeBlock.Line(code);
            }
            else
            {
                int lastParen = namedObject.SourceName.LastIndexOf(" (");
                string nameOfSourceInContainer = namedObject.SourceName.Substring(0, lastParen);
                // This could have a quote in the name.  If so we want to escape it since this will be put in code:
                nameOfSourceInContainer = nameOfSourceInContainer.Replace("\"", "\\\"");

                string cloneString = ".Clone()";

                string namedObjectToPullFrom = null;

                if (rfs != null && referencedFilesAlreadyUsingFullFile.ContainsKey(rfs.Name) && !namedObject.SourceName.StartsWith("Entire File ("))
            {
                namedObjectToPullFrom = referencedFilesAlreadyUsingFullFile[rfs.Name];
            }

                // September 30, 2012
                // Now all files - whether
                // they are part of an Entity
                // or part of a Screen, are static.
                // This means that the rfs.IsSharedStatic
                // check will usually fail.  However, Screens
                // will still add to managers even for static
                // object, so we want to make sure that we don't
                // clone an object already added to managers, so we
                // need the last check to see if the container is a ScreenSave.
                if (nosAti == null || !nosAti.CanBeCloned || namedObjectToPullFrom != null || (rfs != null && rfs.IsSharedStatic == false) || container is ScreenSave)
                {
                    cloneString = "";
                }

                if (nosAti != null && !string.IsNullOrEmpty(nosAti.CustomCloneMethod))
                {
                    cloneString = nosAti.CustomCloneMethod;

                }

                AssetTypeInfo rfsAti = null;
                if (rfs != null)
                {
                    rfsAti = rfs.GetAssetTypeInfo();
                }

                bool usesFullConversion = false;

                if (rfsAti != null && rfsAti.Conversion.Count != 0)
                {
                    var foundConversion = rfsAti.Conversion.FirstOrDefault(item => item.ToType == namedObject.InstanceType);

                    if (foundConversion != null)
                    {
                        cloneString = foundConversion.ConversionPattern.Replace("{NEW}", namedObject.FieldName);
                        cloneString = cloneString.Replace("{THIS}", rfs.GetInstanceName());
                        usesFullConversion = true;
                    }
                }

                if (namedObjectToPullFrom != null)
                {
                    containerName = namedObjectToPullFrom;
                }
                if (!string.IsNullOrEmpty(overridingContainerName))
                {
                    containerName = overridingContainerName;
                }

                string listMemberName = ContentParser.GetMemberNameForList(FileManager.GetExtension(namedObject.SourceFile), namedObject.InstanceType);

                if (!string.IsNullOrEmpty(listMemberName))
                {
                    listMemberName += ".";
                }
                if (nameOfSourceInContainer == "Entire File" && string.IsNullOrEmpty(listMemberName))
                {
                    if (usesFullConversion)
                    {
                        codeBlock.Line(cloneString + ";");
                    }
                    else if (cloneString.Contains("{THIS}"))
                    {
                        string entireString = cloneString.Replace("{THIS}", namedObject.FieldName);
                        entireString = entireString.Replace("{SOURCE_FILE}", containerName);

                        codeBlock.Line(entireString);
                    }
                    else
                    {
                        codeBlock.Line($"{namedObject.FieldName} = {containerName}{cloneString};");
                    }
                }
                else
                {
                    string findByNameLine = "";
                    if (nosAti != null)
                    {
                        findByNameLine = nosAti.FindByNameSyntax;

                        if (string.IsNullOrEmpty(findByNameLine))
                        {
                            string message = "The object " + namedObject.ToString() + " is part of a file.  To be properly generated " +
                                "the AssetTypeInfo (or CSV value) for " + nosAti.ToString() + " must contain a FindByNameSyntax property";

                            throw new Exception(message);
                        }
                    }



                    findByNameLine = findByNameLine.Replace("OBJECTNAME", nameOfSourceInContainer);

                    string possibleDot = ".";

                    if (findByNameLine.StartsWith("["))
                    {
                        possibleDot = "";
                    }

                    codeBlock.Line($"{namedObject.FieldName} = {containerName}{possibleDot}{listMemberName}{findByNameLine}{cloneString};");
                }
            }

        }

        public static void WriteTextSpecificInitialization(ReferencedFileSave rfs, ICodeBlock codeBlock)
        {
            AssetTypeInfo ati = rfs.GetAssetTypeInfo();

            if (ati != null)
            {
                if (ati.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Scene" && !rfs.IsSharedStatic && ObjectFinder.Self.GlueProject.UsesTranslation)
                {
                    WriteTextInitializationLoopForScene(codeBlock, FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name)));
                }
            }
        }

        private static void WriteTextSpecificInitialization(NamedObjectSave namedObject, IElement element, ICodeBlock codeBlock, 
            Dictionary<string, string> referencedFilesAlreadyUsingFullFile)
        {
            if (namedObject.SourceType == SourceType.File)
            {

                // If it's defined by base, the base will take care of this behavior.
                if (!namedObject.DefinedByBase)
                {
                    if (namedObject.ClassType == "Scene")
                    {
                        bool isAlreadyTranslated = false;
                        // It will already be translated if this
                        // NOS comes from a RFS and that RFS is
                        // added to managers.
                        ReferencedFileSave sourceRfs = element.GetReferencedFileSaveRecursively(namedObject.SourceFile);

                        if (sourceRfs != null && sourceRfs.IsSharedStatic == false)
                        {
                            isAlreadyTranslated = true;
                        }
                        if (!isAlreadyTranslated)
                        {
                            string sceneName = namedObject.FieldName;
                            WriteTextInitializationLoopForScene(codeBlock, sceneName);
                        }
                    }
                    else if (namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Text)
                    {
                        // If the NamedObject uses a File that has already been used in another "Entire File" NamedObject, then we want to make sure 
                        // we don't double-translate
                        bool isAlreadyTranslated = false;

                        if (referencedFilesAlreadyUsingFullFile.ContainsKey(namedObject.SourceFile) && !namedObject.SourceName.StartsWith("Entire File ("))
                        {
                            isAlreadyTranslated = true;
                        }

                        if (!isAlreadyTranslated)
                        {
                            ReferencedFileSave sourceRfs = element.GetReferencedFileSaveRecursively(namedObject.SourceFile);

                            if (sourceRfs != null && sourceRfs.IsSharedStatic == false)
                            {
                                isAlreadyTranslated = true;
                            }
                        }


                        if (!isAlreadyTranslated && ProjectManager.GlueProjectSave.UsesTranslation)
                        {
                            codeBlock.Line(string.Format(
                                "{0}.DisplayText = FlatRedBall.Localization.LocalizationManager.Translate({0}.DisplayText);", namedObject.FieldName));
                        }

                        codeBlock.Line(string.Format("{0}.AdjustPositionForPixelPerfectDrawing = true;",
                                                     namedObject.FieldName));
                    }
                }
            }
        }

        private static void WriteTextInitializationLoopForScene(ICodeBlock codeBlock, string sceneName)
        {
            bool useTranslation = ProjectManager.GlueProjectSave.UsesTranslation;

            var forBlock = codeBlock.For(string.Format("int i = 0; i < {0}.Texts.Count; i++", sceneName));

            if (useTranslation)
            {
                forBlock.Line(
                    string.Format(
                        "{0}.Texts[i].DisplayText = FlatRedBall.Localization.LocalizationManager.Translate({0}.Texts[i].DisplayText);", sceneName));
            }
            forBlock.Line(string.Format("{0}.Texts[i].AdjustPositionForPixelPerfectDrawing = true;", sceneName));
        }

        public static void GenerateRemoveFromManagersForNamedObject(IElement element, NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);
            bool handled = false;

            if (namedObject.SourceType == SourceType.Entity)
            {
                handled = true;
                codeBlock.Line( namedObject.InstanceName + ".RemoveFromManagers();");
            }

            if (!handled)
            {
                GetDestroyForNamedObject(element, namedObject, codeBlock, forceRecycle: true);
            }
            AddEndIfIfNecessary(codeBlock, namedObject);
        }

        public static void GetDestroyForNamedObject(IElement element, NamedObjectSave namedObject, ICodeBlock codeBlock, bool forceRecycle = false)
        {
            #region EARLY OUTS

            if (GetShouldSkipDestroyOn(namedObject))
            {
                return;
            }

            #endregion

            bool shouldRecycle = 
                forceRecycle ||
                (element is EntitySave && (element as EntitySave).CreatedByOtherEntities);


            AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);

            #region Update dependencies

            // Update April 3, 2011
            // The generated code used
            // to call UpdateDependencies
            // on any contained NamedObjectSave.
            // We did this because we wanted to make
            // sure that all contained NamedObjectSaves
            // were updated so when the Entity is recycled,
            // the contained NamedObjectSaves are in the proper.
            // position...or at least that was how things used to
            // be done.  Now when AddToManagers is called on an Entity,
            // the Entity is moved back to the origin (and unrotated).  This
            // means that we don't need to worry about this anymore:
            //if ( SaveObject is EntitySave && 
            //    (SaveObject as EntitySave).CreatedByOtherEntities && 
            //    (namedObject.AttachToContainer || namedObject.AttachToCamera))
            //{
            //    // Why do we do this?  The reason is because it's possible (and likely) that the Entity that this code
            //    // is being generated for may be moving.  Any attached objects will "lag behind" until the Draw method is
            //    // called which updates dependencies.  But Entities are usually destroyed before this update happens.  If the
            //    // Entity that is destroyed is re-cycled, then all attached objects will be offset slightly.  This can build up
            //    // over time and cause issues.  This should help eliminate a lot of bugs.
            //    if (namedObject.SourceType == SourceType.Entity || 
            //        (namedObject.AssetTypeInfo != null && namedObject.AssetTypeInfo.ShouldAttach))
            //    {
            //        stringBuilder.AppendLine(tabs + namedObject.InstanceName + ".UpdateDependencies(TimeManager.CurrentTime);");
            //    }
            //}

            #endregion

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType, namedObject);

            #region If object was added to manager, remove this object from managers

            if (ati != null && namedObject.AddToManagers)
            {
                string removalMethod = null;

                if (shouldRecycle && !string.IsNullOrEmpty(ati.RecycledDestroyMethod))
                {
                    removalMethod = ati.RecycledDestroyMethod.Replace("this", namedObject.InstanceName);
                }
                else
                {
                    if (ati.DestroyMethod != null)
                    {

                        removalMethod = ati.DestroyMethod.Replace("this", namedObject.InstanceName);

                    }
                }

                if (!string.IsNullOrEmpty(removalMethod))
                {
                    codeBlock.If(namedObject.InstanceName + " != null")
                             .Line(removalMethod + ";");
                }
            }

            #endregion

            // TODO:  Do we want to handle this case for pooled objects?  I think so.

            #region If the object is an Entity, destroy the object and unload the static content

            if (namedObject.SourceType == SourceType.Entity)
            {
                var ifBlock = codeBlock.If(namedObject.InstanceName + " != null");
                ifBlock.Line(namedObject.InstanceName + ".Destroy();");

                if(!forceRecycle)
                {
                        // We also detach so that if the object is recycled its Parent will be null
                    ifBlock
                             .Line(namedObject.InstanceName + ".Detach();");
                }
                // Vic says:  We used to manually call UnloadStaticContent on all Entities.  But this
                // doesn't work well with inheritance.  Instead, we now use the ContentManagers UnloadContent method support
                // GenerateStaticDestroyForType(tabs, namedObject.ClassType);
            }

            #endregion

            #region Special case Camera

            if (namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Camera && namedObject.IsNewCamera)
            {
                codeBlock.Line("FlatRedBall.SpriteManager.Cameras.Remove(" + namedObject.InstanceName + ");");

            }

            #endregion

            #region Special case PositionedObjectList
            if (namedObject.IsList && namedObject.IsFullyDefined)
            {
                bool shouldSkip = namedObject.SetByContainer;

                if (!shouldSkip)
                {
                    var forBlock = codeBlock.For(string.Format("int i = {0}.Count - 1; i > -1; i--", namedObject.InstanceName));

                    bool isEntity = ObjectFinder.Self.GetEntitySave(namedObject.SourceClassGenericType) != null;


                    if (isEntity)
                    {
                        forBlock.Line(namedObject.InstanceName + "[i].Destroy();");
                    }
                    else
                    {
                        string genericClassType = namedObject.SourceClassGenericType;

                        AssetTypeInfo atiForListElement = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(genericClassType,
                            namedObject);

                        if (atiForListElement != null && atiForListElement.DestroyMethod != null)
                        {

                            string removalMethod = atiForListElement.DestroyMethod.Replace("this", namedObject.InstanceName + "[i]");

                            if (!string.IsNullOrEmpty(removalMethod))
                            {
                                forBlock.Line(removalMethod + ";");
                            }
                        }
                        else
                        {
                            forBlock.Line(string.Format("FlatRedBall.SpriteManager.RemovePositionedObject({0}[i]);",
                                                        namedObject.InstanceName));
                        }
                    }

                    string genericType = namedObject.SourceClassGenericType;

                    if (genericType.Contains("\\"))
                    {
                        genericType = genericType.Substring(genericType.IndexOf("\\") + 1);
                    }

                }

                // Vic says:  We used to manually call UnloadStaticContent on all Entities.  But this
                // doesn't work well with inheritance.  Instead, we now use the ContentManagers UnloadContent method support
                // GenerateStaticDestroyForType(tabs, genericType);

            }
            #endregion
            AddEndIfIfNecessary(codeBlock, namedObject);
        }

        private static bool GetShouldSkipDestroyOn(NamedObjectSave namedObject)
        {
            return namedObject.SetByDerived || namedObject.IsDisabled ||
                            string.IsNullOrEmpty(namedObject.InstanceType) ||
                            namedObject.InstanceType == "<NONE>" ||
                            namedObject.InstantiatedByBase ||
                            IsUncloneableEntireFile(namedObject) ||
                            namedObject.IsContainer;
        }

        private static bool IsUncloneableEntireFile(NamedObjectSave namedObject)
        {
            if (namedObject.IsEntireFile)
            {
                AssetTypeInfo ati = namedObject.GetAssetTypeInfo();

                if (ati != null && ati.CanBeCloned == false)
                {
                    return true;
                }
            }
            return false;
        }


        public static CodeGenerationType GetFieldCodeGenerationType(NamedObjectSave namedObjectSave)
        {
            CodeGenerationType codeGenerationType = CodeGenerationType.Full;

            if (namedObjectSave.IsDisabled)
            {
                codeGenerationType = CodeGenerationType.Nothing;
            }
            else if (string.IsNullOrEmpty(GetQualifiedTypeName(namedObjectSave)))
            {
                codeGenerationType = CodeGenerationType.Nothing;
            }
            else if (namedObjectSave.DefinedByBase)
            {
                codeGenerationType = CodeGenerationType.OnlyContainedObjects;
            }
            return codeGenerationType;
        }

        public static CodeGenerationType GetInitializeCodeGenerationType(NamedObjectSave namedObject, IElement saveObject)
        {
            CodeGenerationType codeGenerationType = CodeGenerationType.Full;


            if (namedObject.SetByDerived
                ||
                (namedObject.SetByContainer && saveObject is EntitySave)
                ||

                namedObject.IsDisabled || !namedObject.IsFullyDefined || !namedObject.Instantiate)
            {

                codeGenerationType = CodeGenerationType.Nothing;

            }

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType, namedObject);

            if (ati != null && ati.IsInstantiatedInAddToManagers)
            {

                // This is an object which has to be instantiated by the engine (like Layer), so it will
                // stay null until we call AddToManagers
                codeGenerationType = CodeGenerationType.Nothing;
            }
            return codeGenerationType;
        }


        public static void AddEndIfIfNecessary(ICodeBlock codeBlock, NamedObjectSave nos)
        {
            if (nos == null)
            {
                throw new ArgumentNullException("nos");
            }
            if (!string.IsNullOrEmpty(nos.ConditionalCompilationSymbols))
            {
                codeBlock.Line("#endif");
            }
        }

        public static void AddIfConditionalSymbolIfNecesssary(ICodeBlock codeBlock, NamedObjectSave nos)
        {
            if(nos == null)
            {
                throw new ArgumentNullException("nos");
            }
            if (!string.IsNullOrEmpty(nos.ConditionalCompilationSymbols))
            {
                codeBlock.Line("#if " + nos.ConditionalCompilationSymbols);
            }
        }

        public static string GetQualifiedTypeName(NamedObjectSave namedObjectSave)
        {
            AssetTypeInfo ati = null;
            if (namedObjectSave.SourceType == SaveClasses.SourceType.Entity &&
                !string.IsNullOrEmpty(namedObjectSave.SourceClassType))
            {

                return ProjectManager.ProjectNamespace + '.' + namedObjectSave.SourceClassType.Replace('\\', '.');
            }
            else if(namedObjectSave.IsList)
            {
                return GetQualifiedClassType(namedObjectSave);
            }
            else if ((ati = namedObjectSave.GetAssetTypeInfo()) != null)
            {
                return
                    ati.QualifiedRuntimeTypeName.PlatformFunc?.Invoke(namedObjectSave) ?? 
                    ati.QualifiedRuntimeTypeName.QualifiedType;             }
            else
            {
                return GetQualifiedClassType(namedObjectSave);
            }
        }


        static string GetQualifiedClassType(NamedObjectSave instance)
        {
            if (instance.SourceType == SourceType.FlatRedBallType && !string.IsNullOrEmpty(instance.InstanceType) &&
                instance.InstanceType.Contains("<T>"))
            {
                string genericType = instance.SourceClassGenericType;

                if (genericType == null)
                {
                    return null;
                }
                else
                {
                    // For now we are going to try to qualify this by using the ATIs, but eventually we may want to change the source class generic type to be fully qualified
                    var ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(genericType, true);
                    if (ati != null)
                    {
                        genericType = ati.QualifiedRuntimeTypeName.QualifiedType;
                    }

                    string instanceType = instance.InstanceType;

                    if (instanceType == "List<T>")
                    {
                        instanceType = "System.Collections.Generic.List<T>";
                    }
                    else if (instanceType?.Contains("PositionedObjectList<T>") == true)
                    {
                        instanceType = "FlatRedBall.Math.PositionedObjectList<T>";
                    }

                    if (genericType.StartsWith("Entities\\") || genericType.StartsWith("Entities/"))
                    {
                        genericType =
                            ProjectManager.ProjectNamespace + '.' + genericType.Replace('\\', '.');
                        return instanceType.Replace("<T>", "<" + genericType + ">");

                    }
                    else
                    {
                        if (genericType.Contains("\\"))
                        {
                            // The namespace is part of it, so let's remove it
                            int lastSlash = genericType.LastIndexOf('\\');
                            genericType = genericType.Substring(lastSlash + 1);
                        }


                        return instanceType.Replace("<T>", "<" + genericType + ">");
                    }
                }
            }
            else
            {
                return instance.InstanceType;
            }

        }

        // todo - this should probably move somewhere else?
        public static string ToCSharpTypeString(Type t)
        {
            if (!t.IsGenericType)
                return t.FullName;
            string genericTypeName = t.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0,
                genericTypeName.IndexOf('`'));
            string genericArgs = string.Join(",",
                t.GetGenericArguments()
                    .Select(ta => ToCSharpTypeString(ta)).ToArray());
            return genericTypeName + "<" + genericArgs + ">";
        }

        private static void CreateVariableResetField(NamedObjectSave namedObjectSave, string typeName, ICodeBlock codeBlock)
        {
            for (int i = 0; i < namedObjectSave.VariablesToReset.Count; i++)
            {
                string variableToReset = namedObjectSave.VariablesToReset[i];

                string typeOfResetVariable = "";
                MemberInfo memberInfo = null;

                try
                {
                    memberInfo = GetMemberInfoForMember(namedObjectSave, typeName, variableToReset);
                }
                catch (InvalidOperationException)
                {
                    // If we got here that means that the object doesn't have a variable matching what was passed in.
                    // That's okay, we can just continue...well, after we tell the user about the problem.
                }

                if (memberInfo == null)
                {
                    GlueGui.ShowMessageBox("Error generating code for " + namedObjectSave.ToString() + ":\nCould not find variable " + variableToReset + " in " + namedObjectSave.SourceClassType);
                }
                else
                {
                    if (memberInfo is PropertyInfo)
                    {
                        var memberInfoPropertyType = ((PropertyInfo)memberInfo).PropertyType;
                        var memberInfoPropertyTypeString = ToCSharpTypeString( memberInfoPropertyType);
                        
                        typeOfResetVariable = TypeManager.GetCommonTypeName(memberInfoPropertyTypeString);
                    }
                    else
                    {
                        typeOfResetVariable = TypeManager.GetCommonTypeName(((FieldInfo)memberInfo).FieldType.ToString());

                    }

                    // 1/2/2011
                    // The following
                    // used to be protected
                    // (instance) variable, but
                    // this greatly bloats the size
                    // of instances.  I'm going to make
                    // these variables static and we'll see
                    // if this causes problems.
                    codeBlock.Line(StringHelper.SpaceStrings("static", typeOfResetVariable, namedObjectSave.InstanceName) +
                                   variableToReset.Replace(".", "") + "Reset;");
                }
            }
        }

        private static MemberInfo GetMemberInfoForMember(NamedObjectSave namedObjectSave, string typeName, string variableToReset)
        {
            Type type = null;

            if (namedObjectSave.SourceType == SourceType.Entity)
            {
                type = typeof(PositionedObject);

            }
            else
            {
                type = TypeManager.GetTypeFromString(typeName);
            }

            if (type == null)
            {
                // this is an unknown type, so we should just return null;
                return null;
            }
            else
            {

                string firstVariable = variableToReset;

                bool shouldRecur = false;

                if (variableToReset.Contains('.'))
                {
                    firstVariable = variableToReset.Substring(0, variableToReset.IndexOf('.'));
                    shouldRecur = true;
                }

                MemberInfo[] memberInfoArray = type.GetMember(firstVariable);

                if (memberInfoArray.Length == 0)
                {
                    throw new InvalidOperationException("Could not find any members with the name " + firstVariable + " in the NamedObject " + namedObjectSave.ToString() + " of type " + type);
                }

                MemberInfo memberInfo = memberInfoArray[0];

                if (shouldRecur)
                {
                    string typeOfMember = null;

                    if (memberInfo is FieldInfo)
                    {
                        typeOfMember = ((FieldInfo)memberInfo).FieldType.Name;
                    }
                    else
                    {
                        typeOfMember = ((PropertyInfo)memberInfo).PropertyType.Name;
                    }
                    return GetMemberInfoForMember(namedObjectSave, typeOfMember, variableToReset.Substring(variableToReset.IndexOf('.') + 1));
                }
                else
                {
                    return memberInfo;
                }
            }
        }

        public static void GetActivityForNamedObject(NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            ///////////////////////////EARLY OUT/////////////////////////////////////////////////
            if (
                (namedObjectSave.SetByContainer && namedObjectSave.GetContainer() is EntitySave)
                ||
                namedObjectSave.IsDisabled || namedObjectSave.CallActivity == false ||
                namedObjectSave.InstantiatedByBase || !namedObjectSave.IsFullyDefined)
            {
                return;
            }
            /////////////////////////END EARLY OUT///////////////////////////////////////////////

            bool setByDerived = namedObjectSave.SetByDerived;

            AddIfConditionalSymbolIfNecesssary(codeBlock, namedObjectSave);

            // why not setByDerived? You may want logic in the base implementation. Let the null check do it
            //if (!setByDerived)
            {
                if (namedObjectSave.Instantiate == false)
                {
                    // This may be null or it may be instantiated later by the user, so we should
                    // handle both cases:
                    codeBlock = codeBlock.If(namedObjectSave.InstanceName + " != null");
                }

                if (namedObjectSave.SourceType == SourceType.Entity)
                {
                    // Entities need activity!
                    codeBlock.Line(namedObjectSave.InstanceName + ".Activity();");
                }
                else if (namedObjectSave.IsList)
                {
                    // Now let's see if the object in the list is an entity
                    string genericType = namedObjectSave.SourceClassGenericType;


                    if (genericType.Contains("Entities\\"))
                    {
                        codeBlock.For("int i = " + namedObjectSave.InstanceName + ".Count - 1; i > -1; i--")
                                    .If("i < " + namedObjectSave.InstanceName + ".Count")
                                        .Line("// We do the extra if-check because activity could destroy any number of entities")
                                        .Line(namedObjectSave.InstanceName + "[i].Activity();");
                    }


                }
                else
                {
                    // Update 2/13/2021
                    // If an object comes
                    // from a file, the file
                    // will call its activity.
                    // If it doesn't come from a 
                    // file, we need to make sure
                    // that it is not SetByDerived.
                    // If it is SetByDerived, then the
                    // derived may be responsible for it,
                    // and if we call activity in the base, 
                    // the object may get double activities called.
                    // This happened with TileMaps where the base GameScreen
                    // set the Map to SetByDerived, then the derived screen set
                    // the Map to be from file. The base screen was calling Activity
                    // and so was the derived on the file.

                    if (namedObjectSave.SourceType != SourceType.File && !namedObjectSave.SetByDerived && namedObjectSave.GetAssetTypeInfo()?.ActivityMethod != null)
                    {
                        // if it's a file, then the file should handle that (I think)
                        var activityMethod = namedObjectSave.GetAssetTypeInfo()?.ActivityMethod;
                        codeBlock.Line(activityMethod.Replace("this", namedObjectSave.FieldName) + ";");
                    }
                }

            }

            // If it's an emitter, call TimedEmit:
            ParticleCodeGenerator.GenerateTimedEmit(codeBlock, namedObjectSave);

            //if (!setByDerived)
            {

                if (namedObjectSave.Instantiate == false)
                {
                    // end the if-statement we started above.
                    codeBlock = codeBlock.End();
                }
            }

            AddEndIfIfNecessary(codeBlock, namedObjectSave);
        }

        public static void GetPostInitializeForNamedObjectList(NamedObjectSave container, List<NamedObjectSave> namedObjectList, ICodeBlock codeBlock, GlueElement element)
        {
            // why try/catch here? It messes up the callstack...
            //try
            //{
                foreach (NamedObjectSave nos in namedObjectList)
                {
                    if (!nos.IsDisabled && nos.IsFullyDefined && nos.Instantiate)
                    {

                        // We should put the conditional compilation symbol before adding to a list:
                        AddIfConditionalSymbolIfNecesssary(codeBlock, nos);


                        // Sept 24, 2012
                        // This used to be
                        // in LateInitialize,
                        // but we moved it here.
                        // See the LateInitialize
                        // method for more information.
                        if (container != null && !nos.InstantiatedByBase && nos.IsContainer == false)
                        {
                            bool shouldSkip = nos.SourceType == SourceType.File &&
                                string.IsNullOrEmpty(nos.SourceFile);
                            if (!shouldSkip)
                            {
                                // pooled entities have this method called multiple times. We want to make sure
                                // that instances aren't added multiple times to this list, so we need to add an 
                                // if-statement if this is pooled.
                                bool isPooled = element is EntitySave && ((EntitySave)element).PooledByFactory;
                                // actually it seems even non-pooled do:
                                //if(isPooled)
                                {
                                    codeBlock = codeBlock.If($"!{container.InstanceName}.Contains({nos.InstanceName})");
                                }


                                codeBlock.Line(container.InstanceName + ".Add(" + nos.InstanceName + ");");


                                //if (isPooled)
                                {
                                    codeBlock = codeBlock.End();
                                }
                            }
                        }


                        EntitySave throwAway = null;
                        ReferencedFileSave rfsReferenced = GetReferencedFileSaveReferencedByNamedObject(nos, element, ref throwAway);


                        bool wrappInIf = nos.SetByDerived || nos.SetByContainer;
                        // This may be a SetByDerived NOS, so it could be null
                        if (wrappInIf)
                        {
                            codeBlock = codeBlock
                                .If(nos.InstanceName + "!= null");
                        }

                        if (nos.IsContainer == false)
                        {
                            WriteAttachTo(nos, codeBlock, ReusableEntireFileRfses, rfsReferenced, element);
                        }

                        GetPostInitializeForNamedObjectList(nos, codeBlock, element);

                        GetPostInitializeForNamedObjectList(nos, nos.ContainedObjects, codeBlock, element);
                        if (wrappInIf)
                        {
                            codeBlock = codeBlock.End();
                        }
                        AddEndIfIfNecessary(codeBlock, nos);
                    }
                }
            //}
            //catch(Exception ex)
            //{


            //    System.Diagnostics.Debugger.Break();
            //    throw ex;
            //}
        }

        public static void WriteAddToManagersBottomUpForNamedObjectList(List<NamedObjectSave> namedObjectList, ICodeBlock codeBlock, 
            IElement element, Dictionary<string, string> reusableEntireFileRfses)
        {
            foreach(var nos in namedObjectList.Where(nos=>
                    nos.SourceType != SourceType.FlatRedBallType || 
                    nos.GetAssetTypeInfo()?.FriendlyName != "Layer"))
            {
                ReferencedFileSave filePullingFrom = null;

                string foundSourceFileValue = null;
                NamedObjectSave entireFileNos = null;

                if (nos.SourceType == SourceType.File && nos.SourceName != null)
                {
                    if (nos.IsEntireFile)
                    {
                        filePullingFrom = element.GetReferencedFileSave(nos.SourceFile);
                    }
                    else if(reusableEntireFileRfses.ContainsKey(nos.SourceFile))
                    {
                        entireFileNos = element.GetNamedObjectRecursively(reusableEntireFileRfses[nos.SourceFile]);
                    }
                }

                

                //mReusableEntireFileRfses

                // We need to add this object if it is not part
                // of an EntireFile object, or if it has a custom
                // Layer.
                // Update September 30, 2012
                // This could also be an object 
                // from a file that is part of a 
                // Screen, therefore it is already
                // added to managers.  If so, we don't
                // want to add it again.
                // Update March 31, 2013
                // If the object is part of a file, but
                // its layer differs from the object that
                // it is a part of, then we need to add it.
                // But we want to remove it from the engine before
                // re-adding so that it isn't part of 2 different if
                // the container is itself on a Layer.
                bool isAlreadyAdded = entireFileNos != null ||
                    (filePullingFrom != null && ReferencedFileSaveCodeGenerator.GetIfShouldAddToManagers(element, filePullingFrom));


                if (!isAlreadyAdded || 
                    (entireFileNos != null && entireFileNos.LayerOn != nos.LayerOn) ||
                    (filePullingFrom != null && !string.IsNullOrEmpty(nos.LayerOn))
                    )
                {
                    if (isAlreadyAdded)
                    {

                        var ati = nos.GetAssetTypeInfo();
                        if (ati != null && !string.IsNullOrEmpty(ati.RecycledDestroyMethod))
                        {
                            string recycleMethod = ati.RecycledDestroyMethod.Replace("this", nos.InstanceName) + ";";
                            codeBlock.Line(recycleMethod);
                        }
                    }
                    NamedObjectSaveCodeGenerator.WriteAddToManagersForNamedObject(element, nos, codeBlock);
                }

                #region Loop through all contained NamedObjects in Lists - and call WriteAddToManagersForNamedObject recursively
                // Loop through all contained NamedObjects
                foreach (NamedObjectSave containedNos in nos.ContainedObjects)
                {
                    WriteAddToManagersForNamedObject(element, containedNos, codeBlock);

                    // 12/14/2010
                    // We used to add
                    // objects which are
                    // part of a list to their
                    // list here, but if that happened,
                    // then variables like Visible wouldn't
                    // work properly.  So this code was moved
                    // into initialize 
                    // stringBuilder.AppendLine("\t\t\t" + namedObject.InstanceName + ".Add(" + containedNos.InstanceName + ");");
                }
                #endregion


            }





        }

        static void GetPostInitializeForNamedObjectList(NamedObjectSave namedObject, ICodeBlock codeBlock, GlueElement container)
        {
            if (!namedObject.IsDisabled && namedObject.Instantiate)
            {
                string namedObjectName = namedObject.InstanceName;

                // May 16, 2011
                // I encountered
                // a bug today - using
                // variable resets on a
                // variable that is set in
                // Glue as opposed to in a file
                // causes weird, unexpected behavior.
                // The reason is variables set in a file
                // are "set" before the file is even loaded, 
                // so when the reset variables are recorded, these
                // values are already set.  However, custom variables
                // (which are just exposed variables on the source object)
                // are set *after* reset variables, making them work differently.
                // However, the user doesn't expect this behavior, so instead we're
                // going to set any exposed variables before we record resetting.
                // Update:
                // This was 
                // moved into 
                // PostInitialize 
                // so that all objects
                // have been instantiated
                // and variables can be set
                // safely.

                AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);

                GenerateVariableAssignment(namedObject, codeBlock, container);

                if (!namedObject.SetByDerived && !namedObject.SetByContainer)
                {
                    AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType, namedObject);

                    if (ati != null && !string.IsNullOrEmpty(ati.PostInitializeCode))
                    {
                        codeBlock.Line(ati.PostInitializeCode.Replace("this", namedObjectName) + ";");
                    }


                    // Eventually I want to move this to a plugin but plugins need to be able to override how
                    // variables are generated.  Then they can add custom code and suppress FRB from doing it.
                    if (ati == AvailableAssetTypes.CommonAtis.Polygon)
                    {
                        string internalPoints = null;

                        var customVariable = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "Points");

                        if (customVariable != null)
                        {
                            List<Vector2> points = customVariable.Value as List<Vector2>;

                            if(points != null)
                            {
                                bool isFirst = true;
                                foreach(var point in points)
                                {
                                    if(!isFirst)
                                    {
                                        internalPoints += ", ";
                                    }
                                    internalPoints +=                                        $"new FlatRedBall.Math.Geometry.Point({point.X.ToString(CultureInfo.InvariantCulture)}, {point.Y.ToString(CultureInfo.InvariantCulture)})";

                                    isFirst = false;
                                }

                            }
                        }

                        if(internalPoints == null)
                        {
                            internalPoints = " new FlatRedBall.Math.Geometry.Point(0, 16), new FlatRedBall.Math.Geometry.Point(16, 0), new FlatRedBall.Math.Geometry.Point(-16, 0),  new FlatRedBall.Math.Geometry.Point(0, 16)";

                        }

                        codeBlock.Line("FlatRedBall.Math.Geometry.Point[] " + namedObject.InstanceName + "Points = new FlatRedBall.Math.Geometry.Point[] {" + internalPoints + " };");
                        codeBlock.Line(namedObject.InstanceName + ".Points = " + namedObject.InstanceName + "Points;");
                    }
                }



                AddEndIfIfNecessary(codeBlock, namedObject);
            }
        }



        public static void AddUsingsForNamedObjects(List<string> usingsToAdd, IElement SaveObject)
        {

            for (int i = 0; i < SaveObject.NamedObjects.Count; i++)
            {
                NamedObjectSave nos = SaveObject.NamedObjects[i];

                if (nos.SourceType == SourceType.Entity)
                {
                    if (!string.IsNullOrEmpty(nos.SourceClassType))
                    {
                        string namespaceString = FileManager.MakeRelative(FileManager.GetDirectory(nos.SourceClassType));

                        namespaceString = namespaceString.Replace("/", ".");

                        if (namespaceString.EndsWith("."))
                        {
                            namespaceString = namespaceString.Substring(0, namespaceString.Length - 1);
                        }

                        if (namespaceString != "Entities" && !usingsToAdd.Contains(namespaceString))
                        {
                            usingsToAdd.Add(ProjectManager.ProjectNamespace + "." + namespaceString);
                        }
                    }
                }
                else if (nos.SourceType == SourceType.FlatRedBallType &&
                    !string.IsNullOrEmpty(nos.SourceClassType))
                {
                    string typeAsString = nos.SourceClassType;

                    AddUsingForFlatRedBallType(usingsToAdd, typeAsString);

                    if (!string.IsNullOrEmpty(nos.SourceClassGenericType))
                    {
                        AddUsingForFlatRedBallType(usingsToAdd, nos.SourceClassGenericType);
                    }
                }
                else if (nos.SourceType == SourceType.File &&
                    !string.IsNullOrEmpty(nos.SourceFile) &&
                    !string.IsNullOrEmpty(nos.SourceName))
                {
                    AddUsingForFlatRedBallType(usingsToAdd, nos.ClassType);
                }
            }

        }

        /// <summary>
        /// Returns whether a given NOS has already been added to managers by its referenced file.  This occurs if the NOS uses an EntireFile.
        /// </summary>
        /// <param name="saveObject">The element containing the NOS.</param>
        /// <param name="namedObject">The NOS in question.</param>
        /// <returns>Whether it's already added by its source file.</returns>
        static bool IsAddedToManagerByFile(IElement saveObject, NamedObjectSave namedObject)
        {
            if (namedObject.SourceType == SourceType.File)
            {
                ReferencedFileSave rfs = null;
                if (saveObject is EntitySave)
                {
                    EntitySave entitySave = saveObject as EntitySave;
                    rfs = entitySave.GetReferencedFileSave(namedObject.SourceFile);
                }
                else
                {
                    rfs = (saveObject as ScreenSave).GetReferencedFileSave(namedObject.SourceFile);
                }

                if (rfs == null)
                {
                    // We can't find a matching RFS - we have no idea if it would be handled or not, but...let's assume no for now
                    return true;
                }

                else if (rfs.IsSharedStatic == false || saveObject is ScreenSave)
                {
                    return true;
                }

                if (ReusableEntireFileRfses.ContainsKey( namedObject.SourceFile) && !namedObject.SourceName.StartsWith("Entire File ("))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsAddedToManagerByContainer(IElement saveObject, NamedObjectSave namedObject)
        {
            var toReturn = false;
            if(namedObject.SourceType == SourceType.FlatRedBallType)
            {
                var container = saveObject.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(namedObject));

                if (container != null && container.AddToManagers &&
                    container.IsDisabled == false)
                {
                    // If this is a shape collection, then it's already handled:
                    toReturn = container.SourceType == SourceType.FlatRedBallType &&
                        (container.SourceClassType == "ShapeCollection" ||
                            container.SourceClassType == "FlatRedBall.Math.Geometry.ShapeCollection");
                }
            }
            return toReturn;
        }

        public static void WriteAddToManagersForNamedObject(IElement element, NamedObjectSave namedObject, 
            ICodeBlock codeBlock, bool isInVariableSetterProperty = false, bool considerRemoveIfInvisible = true)
        {

            bool shouldEarlyOut = 
                namedObject.IsFullyDefined == false
                ||
                (namedObject.SetByDerived || namedObject.IsDisabled || !namedObject.Instantiate || namedObject.IsContainer)
                ||
                (namedObject.SetByContainer && element is EntitySave) // Screens can't be contained, so we don't want to early out on Screens
                ||
                (namedObject.SourceType == SourceType.File &&
                (string.IsNullOrEmpty(namedObject.SourceName) || namedObject.SourceName == "<NONE>"))             
                ;

            if (!shouldEarlyOut)
            {
                AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);

                bool isInsideVisibleProperty = considerRemoveIfInvisible == false;
                AssetTypeInfo ati = namedObject.GetAssetTypeInfo();

                if ((considerRemoveIfInvisible && namedObject.RemoveFromManagersWhenInvisible && IsInvisible(namedObject, element)))
                {
                    if (namedObject.SourceType == SourceType.Entity)
                    {
                        // since we want to have all contained elements in namedObject also call AssignCustomVariables, we'll pass 'true'
                        codeBlock.Line(namedObject.FieldName + ".AssignCustomVariables(true);");
                    }
                    // else, we don't do anything here, but we do want the outer if statement to evaluate to true to prevent the addition from occurring below.
                }
                else
                {


                    // If we're setting this
                    // in a variable setter, that
                    // means that this code is going
                    // to be used to dynamically set the
                    // source of an object.  In that case, 
                    // the object will not have yet been added
                    // to the managers.  Also, files that are assigned
                    // here are assumed to also not have been added to managers
                    // because they should have their LoadedOnlyWhenReferenced property
                    // set to true.
                    bool isAddedToManagerByFile = !isInVariableSetterProperty && IsAddedToManagerByFile(element, namedObject);
                    bool isAddedToManagerByContainer = IsAddedToManagerByContainer(element, namedObject);

                    bool addedRegularly = namedObject.AddToManagers && 
                        !namedObject.InstantiatedByBase && !isAddedToManagerByFile && !isAddedToManagerByContainer;
                    if (addedRegularly)
                    {
                        string layerName = "null";

                        if (!string.IsNullOrEmpty(namedObject.LayerOn))
                        {
                            layerName = GetNamedObjectLayerName(namedObject);
                        }
                        else if (element is EntitySave)
                        {
                            layerName = "LayerProvidedByContainer";
                        }
                        else if (element is ScreenSave)
                        {
                            layerName = "mLayer";
                        }

                        void WriteEntityAddToManagers()
                        {
                            if (isInsideVisibleProperty)
                            {
                                codeBlock.Line(namedObject.FieldName + ".ReAddToManagers(" + layerName + ");");
                            }
                            else
                            {
                                codeBlock.Line(namedObject.FieldName + ".AddToManagers(" + layerName + ");");
                            }
                        }

                        #region There is an ATI - it's a type defined in the ContentTypes.csv file in Glue
                        if (ati != null)
                        {
                            bool isLayered = CodeWriter.IsOnOwnLayer(element)
                                || !string.IsNullOrEmpty(namedObject.LayerOn);

                            if(namedObject.IsManuallyUpdated && !string.IsNullOrEmpty(ati.AddManuallyUpdatedMethod))
                            {
                                var line = ati.AddManuallyUpdatedMethod
                                    .Replace("{THIS}", namedObject.FieldName)
                                    .Replace("{LAYER}", layerName) + ';';

                                codeBlock.Line(line);
                            }
                            else if(ati.AddToManagersFunc != null)
                            {
                                string line = null;
                                if(isLayered)
                                {
                                    line = ati.AddToManagersFunc(element, namedObject, null, layerName);
                                }
                                else
                                {
                                    line = ati.AddToManagersFunc(element, namedObject, null, null);

                                }

                                codeBlock.Line(line);

                            }

                            else if (isLayered && ati.LayeredAddToManagersMethod.Count != 0 && !string.IsNullOrEmpty(ati.LayeredAddToManagersMethod[0]))
                            {
                                string layerAddToManagersMethod = DecideOnLineToAdd(namedObject, ati, true);

                                // This used to be inside the if(element is EntitySave) but
                                // I think we want it even if the ElementSave is a Screen.

                                var pattern = @"\bmLayer\b";
                                Regex rgx = new Regex(pattern);
                                layerAddToManagersMethod = rgx.Replace(layerAddToManagersMethod, layerName);

                                codeBlock.Line(layerAddToManagersMethod.Replace("this", namedObject.FieldName) + ";");


                            }
                            else if(ati.AddToManagersMethod?.Count > 0 && !string.IsNullOrEmpty(ati.AddToManagersMethod[0]))
                            {

                                string addLine = DecideOnLineToAdd(namedObject, ati, false);

                                codeBlock.Line(addLine.Replace("this", namedObject.FieldName) + ";");

                                if (namedObject.IsLayer && element is EntitySave)
                                {
                                    string layerToAddAbove = layerName;

                                    int indexOfThis = element.NamedObjects.IndexOf(namedObject);

                                    for (int i = 0; i < indexOfThis; i++)
                                    {
                                        if (element.NamedObjects[i].IsLayer && !element.NamedObjects[i].IsDisabled)
                                        {
                                            layerToAddAbove = element.NamedObjects[i].InstanceName;
                                        }
                                    }

                                    //If the EntitySave contains a Layer, the Layer should be inserted after whatever Layer the Entity is on.
                                    codeBlock.Line("FlatRedBall.SpriteManager.MoveLayerAboveLayer(" + namedObject.FieldName + ", " + layerToAddAbove + ");");
                                }
                            }
                            else if(namedObject.SourceType == SourceType.Entity)
                            {
                                // it has an ATI but it's still an entity, so do the regular add:
                                WriteEntityAddToManagers();
                            }
                            AddLayerSpecificAddToManagersCode(namedObject, codeBlock);

                            AddTextSpecificAddToManagersCode(namedObject, codeBlock, layerName);
                        }
                        #endregion
                        #region No ATI - is it an Entity?
                        else if (namedObject.SourceType == SourceType.Entity)
                        {
                            WriteEntityAddToManagers();

                        }
                        #endregion


                        #region If this object is ignored in pausing, add it to the InstructionManager's ignored list

                        PauseCodeGenerator.AddToPauseIgnoreIfNecessary(codeBlock, element, namedObject);

                        #endregion
                    }

                    #region Special Case:  Add any aliased ReferencedFileSaves that are on layers to the layer
                    // Special Case:  If a NamedObject is an EntireFile, 
                    // and the source ReferencedFileSave is not shared static,
                    // then that means that the NamedObject is essentially just
                    // used to access the ReferencedFileSave in Glue.  The user may
                    // be using the NamedObject to place the ReferencedfileSave on a
                    // layer, so we should check for that

                    // This bool construction
                    // used to also say:
                    // namedObject.SourceName.Contains("Entire ")
                    // but I think we want this
                    // added to a Layer even if it
                    // an entire file.

                    bool hasAddToManagersCode =
                        ati != null &&
                        ((ati.LayeredAddToManagersMethod.Count != 0 && !string.IsNullOrEmpty(ati.LayeredAddToManagersMethod[0])) ||
                        ati.AddToManagersFunc != null);

                    bool shouldAddToLayer = (!namedObject.AddToManagers || isAddedToManagerByFile) && !string.IsNullOrEmpty(namedObject.LayerOn) &&
                        namedObject.SourceType == SourceType.File &&
                        namedObject.SourceName != null &&
                        //namedObject.SourceName.Contains("Entire ") && 
                        hasAddToManagersCode;

                    if (shouldAddToLayer)
                    {
                        string layerName = GetNamedObjectLayerName(namedObject);

                        if (ati.AddToManagersFunc != null)
                        {
                            codeBlock.Line(ati.AddToManagersFunc(element, namedObject, null, layerName));
                        }
                        else
                        {
                            string layerAddToManagersMethod = ati.LayeredAddToManagersMethod[0];

                            // regex this with "\bmLayer\b"
                            var pattern = @"\bmLayer\b";
                            Regex rgx = new Regex(pattern);
                            layerAddToManagersMethod = rgx.Replace(layerAddToManagersMethod, layerName);
                            //layerAddToManagersMethod = layerAddToManagersMethod.Replace("mLayer", layerName);


                            codeBlock.Line(layerAddToManagersMethod.Replace("this", namedObject.FieldName) + ";");
                        }
                    }
                    #endregion




                }
                

                AddEndIfIfNecessary(codeBlock, namedObject);
            }
        }

        private static string GetNamedObjectLayerName(NamedObjectSave namedObject)
        {
            string layerName;
            if (namedObject.LayerOn == AvailableLayersTypeConverter.UnderEverythingLayerName)
            {
                layerName = AvailableLayersTypeConverter.UnderEverythingLayerCode;
            }
            else if (namedObject.LayerOn == AvailableLayersTypeConverter.TopLayerName)
            {
                layerName = AvailableLayersTypeConverter.TopLayerCode;
            }
            else
            {
                layerName = namedObject.LayerOn;
            }

            return layerName;
        }

        public static void AssignInstanceVariablesOn(IElement element, NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType, namedObject);
            // Update June 24, 2012
            // This should happen before
            // setting variables.  Therefore
            // a user can say "I want to have 
            // state X, but then I want to change
            // variable Y and Z to be different.
            StateCodeGenerator.WriteSetStateOnNamedObject(namedObject, codeBlock);
            #region Set all of the custom variables
            // Set custom variables before performing attachments.  That way if the user sets an object's X when it is
            // an object, or if it sets it in the Entity, the result will be the same.
            // Update November 17, 2012 - do we need this anymore?  I think the initialization code handles this:
            // Update - We state state here, and if we set state, we need to set variables after state.  I think the
            // reason we do this is because states may have events tied to them, and we want those events fired in AddToManagers
            // after the caller has added itself to managers


            GenerateVariableAssignment(namedObject, codeBlock, element as GlueElement);


            #endregion


            codeBlock = ApplyResetVariables(namedObject, codeBlock, ati);
            EntitySave throwaway = null;
            ReferencedFileSave rfs = GetReferencedFileSaveReferencedByNamedObject(namedObject, element, ref throwaway);


        }

        private static bool IsInvisible(NamedObjectSave namedObject, IElement container)
        {
            var instruction = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "Visible");

            var customVariable = container.CustomVariables.FirstOrDefault(item =>
                item.SourceObject == namedObject.InstanceName && item.SourceObjectProperty == "Visible");
            if (instruction != null)
            {
                return instruction.Value != null && instruction.Value is bool && ((bool)instruction.Value) == false;
            }
            // Are we tunneling?
            else if (customVariable != null)
            {
                return customVariable.DefaultValue != null && customVariable.DefaultValue is bool &&
                    ((bool)customVariable.DefaultValue) == false;
            }
            // Maybe we should check for states?
            return false;
        }

        private static string DecideOnLineToAdd(NamedObjectSave namedObject, AssetTypeInfo ati, bool layered)
        {
            int index = 0;
            
            if (ati.AddToManagersMethod.Count > 1 && namedObject.SourceType == SourceType.FlatRedBallType &&
                    (namedObject.SourceClassType == "Sprite" || namedObject.SourceClassType == "SpriteFrame") &&
                    namedObject.IsZBuffered)
            {
                index = 1;
            }

            if (layered)
            {
                return ati.LayeredAddToManagersMethod[index];
            }
            else
            {
                return ati.AddToManagersMethod[index];
            }
        }

        private static void AddLayerSpecificAddToManagersCode(NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            string objectName = namedObject.FieldName;

            if (namedObject.IsLayer &&
                namedObject.IndependentOfCamera)
            {
                if (namedObject.Is2D)
                {
                    codeBlock.Line(objectName + ".UsePixelCoordinates();");

                    if (namedObject.DestinationRectangle != null)
                    {
                        float leftDestination = namedObject.DestinationRectangle.Value.X;
                        float topDestination = namedObject.DestinationRectangle.Value.Y;
                        float rightDestination = (namedObject.DestinationRectangle.Value.X + namedObject.DestinationRectangle.Value.Width);
                        float bottomDestination = bottomDestination = (namedObject.DestinationRectangle.Value.Y + namedObject.DestinationRectangle.Value.Height);

                        if (namedObject.LayerCoordinateUnit == LayerCoordinateUnit.Pixel)
                        {
                            // This could be using a scale value, or it could be on a mobile device where the physical resolution doesn't match the desired camera resolution.

                            string multiple = null;

                            if(GlueState.Self.CurrentGlueProject.DisplaySettings != null)
                            {
                                var displaySettings = GlueState.Self.CurrentGlueProject.DisplaySettings;
                                decimal effectiveWidth = displaySettings.ResolutionWidth;

                                // April 13, 2018
                                // Vic says: I'm not sure why I wrote this code. Consider a situation where the game has an aspect ratio of 2,
                                // and is running at a resolution of 1000x600. In this case it will be letterboxed, but the resolution is still 1000
                                if(displaySettings.FixedAspectRatio && displaySettings.AspectRatioWidth/displaySettings.AspectRatioHeight < displaySettings.ResolutionWidth / (decimal)displaySettings.ResolutionHeight )
                                {
                                    var aspectRatio = displaySettings.AspectRatioWidth / displaySettings.AspectRatioHeight;

                                    effectiveWidth = GlueState.Self.CurrentGlueProject.DisplaySettings.ResolutionHeight * aspectRatio;
                                }

                                multiple = $" * FlatRedBall.Camera.Main.DestinationRectangle.Width / {effectiveWidth}f";

                            }

                            codeBlock.Line(objectName + $".LayerCameraSettings.LeftDestination = FlatRedBall.Camera.Main.DestinationRectangle.Left + {FlatRedBall.Math.MathFunctions.RoundToInt( leftDestination )}{multiple};");
                            codeBlock.Line(objectName + $".LayerCameraSettings.TopDestination = FlatRedBall.Camera.Main.DestinationRectangle.Top + {FlatRedBall.Math.MathFunctions.RoundToInt(topDestination )}{multiple};");

                            codeBlock.Line(objectName + $".LayerCameraSettings.RightDestination = FlatRedBall.Camera.Main.DestinationRectangle.Left + {FlatRedBall.Math.MathFunctions.RoundToInt(rightDestination )}{multiple};");
                            codeBlock.Line(objectName + $".LayerCameraSettings.BottomDestination = FlatRedBall.Camera.Main.DestinationRectangle.Top + {FlatRedBall.Math.MathFunctions.RoundToInt(bottomDestination )}{multiple};");

                            codeBlock.Line("// For layers which are 2D, have specified a destination rectangle, and use Pixel coordinate types, the ortho values match the destination rectangle. This can be changed in custom code.");
                            codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalWidth = " + FlatRedBall.Math.MathFunctions.RoundToInt(namedObject.DestinationRectangle.Value.Width ) + ";");
                            codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalHeight = " + FlatRedBall.Math.MathFunctions.RoundToInt(namedObject.DestinationRectangle.Value.Height ) + ";");
                        }
                        else
                        {
                            codeBlock.Line(objectName +
                                ".LayerCameraSettings.LeftDestination = " +
                                "FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.SpriteManager.Camera.DestinationRectangle.Right * " + (.01 * leftDestination) + ");");

                            codeBlock.Line(objectName +
                                ".LayerCameraSettings.TopDestination = " +
                                "FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.SpriteManager.Camera.DestinationRectangle.Bottom * " + (.01 * topDestination) + ");");


                            codeBlock.Line(objectName +
                                ".LayerCameraSettings.BottomDestination = " +
                                "FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.SpriteManager.Camera.DestinationRectangle.Bottom * " + (.01 * bottomDestination) + ");");

                            codeBlock.Line(objectName +
                                ".LayerCameraSettings.RightDestination = " +
                                "FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.SpriteManager.Camera.DestinationRectangle.Right * " + (.01 * rightDestination) + ");");

                            codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalWidth = " +
                                "FlatRedBall.SpriteManager.Camera.OrthogonalWidth * (float)(" + (.01 * (rightDestination - leftDestination)) + ");");
                            codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalHeight = " +
                                "FlatRedBall.SpriteManager.Camera.OrthogonalHeight * (float)(" + (.01 * (bottomDestination - topDestination)) + ");");
                        }

                    }
                    else if (namedObject.LayerCoordinateType == LayerCoordinateType.MatchCamera)
                    {
                        codeBlock = codeBlock.If("FlatRedBall.SpriteManager.Camera.Orthogonal");

                        codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalWidth = FlatRedBall.SpriteManager.Camera.OrthogonalWidth;");
                        codeBlock.Line(objectName + ".LayerCameraSettings.OrthogonalHeight = FlatRedBall.SpriteManager.Camera.OrthogonalHeight;");

                        codeBlock = codeBlock.End();
                    }
                }
                else
                {
                    codeBlock.Line(objectName + ".LayerCameraSettings = new FlatRedBall.Graphics.LayerCameraSettings();");
                    codeBlock.Line(objectName + ".LayerCameraSettings.Orthogonal = false;");
                }
            }
        }

        private static void AddTextSpecificAddToManagersCode(NamedObjectSave namedObject, ICodeBlock codeBlock, string layerName)
        {
            string objectName = namedObject.FieldName;
            // April 1, 2012
            // Text that is added
            // through TextManager.AddText("Hello")
            // will automatically scale itself to be
            // pixel-perfect for the default Camera, or
            // to the Layer that it's added on.  This behavior
            // is very handy but is a little more difficult to reproduce
            // with generated code.  The reason for that is because generated
            // code adds Text objects to the Camera *after* variables are set on
            // the Text object (if they happen to be tunneled).  Therefore, we need
            // to only set pixel perfect if the user has marked the text as being pixel
            // perfect.
            if (namedObject.SourceType == SourceType.FlatRedBallType && namedObject.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Text && namedObject.IsPixelPerfect)
            {
                codeBlock.If(objectName + ".Font != null")
                    .Line(objectName + ".SetPixelPerfectScale(" + layerName + ");");
            }
        }

        private static ICodeBlock ApplyResetVariables(NamedObjectSave namedObject, ICodeBlock codeBlock, AssetTypeInfo ati)
        {
            // Also reset variables before attachment.
            // May 15, 2011:  This should happen before
            // setting the custom variables.
            // May 16, 2011:  CustomVariables which are 
            // exposed variables are now set before AddToManagers
            // is even called.  Therefore, doing this before doesn't matter
            // anymore.  Moved back down to its old position.
            for (int i = 0; i < namedObject.VariablesToReset.Count; i++)
            {
                string variableToReset = namedObject.VariablesToReset[i];


                string relativeVersion = InstructionManager.GetRelativeForAbsolute(namedObject.VariablesToReset[i]);


                if ((namedObject.SourceType == SourceType.Entity || ati.ShouldAttach) && !string.IsNullOrEmpty(relativeVersion))
                {
                    codeBlock = codeBlock.If(namedObject.InstanceName + ".Parent == null");

                }

                codeBlock.Line(namedObject.InstanceName + "." + namedObject.VariablesToReset[i] + " = " +
                    namedObject.InstanceName + namedObject.VariablesToReset[i].Replace(".", "") + "Reset" + ";");

                if ((namedObject.SourceType == SourceType.Entity || ati.ShouldAttach) && !string.IsNullOrEmpty(relativeVersion))
                {
                    codeBlock = codeBlock.End().Else();

                    codeBlock.Line(namedObject.InstanceName + "." + relativeVersion + " = " +
                        namedObject.InstanceName + namedObject.VariablesToReset[i].Replace(".", "") + "Reset" + ";");
                    codeBlock = codeBlock.End();
                }
            }
            return codeBlock;
        }

        private static void WriteAttachTo(NamedObjectSave namedObject, ICodeBlock codeBlock, 
            Dictionary<string, string> referencedFilesAlreadyUsingFullFile, ReferencedFileSave rfs, GlueElement container)
        {

            string objectName = namedObject.FieldName;
            AssetTypeInfo ati = namedObject.GetAssetTypeInfo();

            bool canAttach = GetIfCanAttach(namedObject, ati, container);


            if (canAttach && 
                ((container is EntitySave && namedObject.AttachToContainer) || namedObject.AttachToCamera))
            {
                    string whatToAttachTo = "this";

                    if (namedObject.AttachToCamera)
                    {
                        whatToAttachTo = "FlatRedBall.Camera.Main";
                    }

                    string attachMethodCall = "AttachTo";
                    bool wrapInIf = true;
                    if (ati != null && !string.IsNullOrEmpty(ati.AttachToNullOnlyMethod))
                    {
                        attachMethodCall = ati.AttachToNullOnlyMethod;
                        wrapInIf = false;
                    }

                    var currentBlock = codeBlock;
                    if (wrapInIf)
                    {
                        currentBlock = currentBlock
                            .If(objectName + ".Parent == null");
                    }


                    // March 26, 2012
                    // We used to attach
                    // objects to their parents
                    // by using absolute positions.
                    // The problem is that this means
                    // that if a script ran before the
                    // attachment happened (when a variable
                    // was set) then the script may run before
                    // attachment happened and sometimes after.
                    // That means users would have to handle both
                    // relative and absoltue cases.  We're going to
                    // make attachments happen first so that attachment
                    // happens right away - then the user can always write
                    // scripts using relative values.
                    WriteCopyToAbsoluteInInitializeCode(namedObject, currentBlock, container, referencedFilesAlreadyUsingFullFile, ati, objectName, rfs);

                    // March 27, 2012
                    // We used to attach
                    // by passing 'true' as
                    // the 2nd argument meaning
                    // that the relative values were
                    // set from absolute.  Now we use
                    // the relative values so that the
                    // script can simply set Relative values
                    // always whether it's before or after attachment
                    // and it'll just work.
                    if (namedObject.AttachToCamera)
                    {
                        string adjustRelativeZLine = null;

                        if (ati != null && !string.IsNullOrEmpty(ati.AdjustRelativeZ))
                        {
                            adjustRelativeZLine = ati.AdjustRelativeZ;
                        }
                        else
                        {
                            adjustRelativeZLine = "this.RelativeZ += value";
                        }

                        adjustRelativeZLine = adjustRelativeZLine.Replace("this", "{0}").Replace("value", "{1}");

                        currentBlock.Line(string.Format(adjustRelativeZLine, objectName, "-40") + ";");
                    }
                    currentBlock.Line(objectName + "." + attachMethodCall + "(" + whatToAttachTo + ", false);");
                    
            }

        }



        public static void AssignResetVariables(ICodeBlock codeBlock, IElement saveObject)
        {
            AssignResetVariables(codeBlock, saveObject.NamedObjects);
        }

        static void AssignResetVariables(ICodeBlock codeBlock, List<NamedObjectSave> namedObjects)
        {
            foreach (NamedObjectSave namedObject in namedObjects)
            {

                if (namedObject.SetByDerived || namedObject.SetByContainer || namedObject.IsDisabled)
                {
                    continue;
                }


                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType, namedObject);

                if (ati != null && ati.IsInstantiatedInAddToManagers)
                {
                    // This is an object which has to be instantiated by the engine (like Layer), so it will
                    // stay null until we call AddToManagers
                    continue;
                }

                for (int i = 0; i < namedObject.VariablesToReset.Count; i++)
                {
                    string relativeVariable = null;

                    if (namedObject.SourceType == SourceType.Entity || (ati != null && ati.ShouldAttach))
                    {
                        relativeVariable = InstructionManager.GetRelativeForAbsolute(namedObject.VariablesToReset[i]);
                    }

                    if (!string.IsNullOrEmpty(relativeVariable))
                    {
                        codeBlock = codeBlock.If(namedObject.InstanceName + ".Parent == null");
                    }
                    codeBlock.Line(namedObject.InstanceName + namedObject.VariablesToReset[i].Replace(".", "") + "Reset" +
                                   " = " +
                                   namedObject.InstanceName + "." + namedObject.VariablesToReset[i] + ";");

                    if (!string.IsNullOrEmpty(relativeVariable))
                    {
                        codeBlock = codeBlock.End().Else();
                        codeBlock.Line(namedObject.InstanceName + namedObject.VariablesToReset[i].Replace(".", "") + "Reset" +
                                   " = " +
                                   namedObject.InstanceName + "." + relativeVariable + ";");
                        codeBlock = codeBlock.End();
                    }
                }

                AssignResetVariables(codeBlock, namedObject.ContainedObjects);
            }
        }

        public static void WriteConvertToManuallyUpdated(ICodeBlock codeBlock, IElement element, Dictionary<string, string> reusableEntireFileRfses)
        {
            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                if ((nos.IsList || nos.AddToManagers) && !nos.IsDisabled && !nos.InstantiatedByBase && nos.IsFullyDefined && nos.IsContainer == false)
                {
                    AddIfConditionalSymbolIfNecesssary(codeBlock, nos);


                    bool shouldHaveIfNullCheck = nos.Instantiate == false || nos.SetByDerived;

                    if (shouldHaveIfNullCheck)
                    {
                        codeBlock = codeBlock.If(nos.InstanceName + " != null");
                    }


                    #region The NOS is an Entity

                    if (nos.SourceType == SourceType.Entity)
                    {
                        codeBlock.Line(nos.InstanceName + ".ConvertToManuallyUpdated();");
                    }

                    #endregion

                    #region The NOS is a PositionedObjectList

                    else if (nos.IsList && !string.IsNullOrEmpty(nos.SourceClassGenericType))
                    {
                        WriteConvertToManuallyUpdatedForListNos(codeBlock, nos);
                    }

                    #endregion



                    else
                    {
                        AssetTypeInfo ati = nos.GetAssetTypeInfo();
                        bool alreadyHandledByEntireFileObject = false;

                        if (nos.SourceType == SourceType.File &&
                            !string.IsNullOrEmpty(nos.SourceFile) &&
                            !string.IsNullOrEmpty(nos.SourceName) &&
                            !nos.SourceName.StartsWith("Entire File ("))
                        {
                            if(reusableEntireFileRfses.ContainsKey(nos.SourceFile))
                            {
                                alreadyHandledByEntireFileObject = true;
                            }
                        }

                        if (!alreadyHandledByEntireFileObject && ati != null && !string.IsNullOrEmpty(ati.MakeManuallyUpdatedMethod))
                        {
                            codeBlock.Line(ati.MakeManuallyUpdatedMethod.Replace("this", nos.InstanceName) + ";");
                        }
                    }

                    if (shouldHaveIfNullCheck)
                    {
                        codeBlock = codeBlock.End();
                    }


                    AddEndIfIfNecessary(codeBlock, nos);

                }
            }

        }

        private static void WriteConvertToManuallyUpdatedForListNos(ICodeBlock codeBlock, NamedObjectSave nos)
        {
            // See if the source type is an Entity
            EntitySave entitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);

            bool add = false;
            ICodeBlock forBlock = new CodeBlockFor(null,
                                                   "int i = 0; i < " + nos.InstanceName + ".Count; i++");


            if (entitySave != null)
            {
                forBlock.Line(nos.InstanceName + "[i].ConvertToManuallyUpdated();");
                add = true;
            }
            else
            {
                // See if this type has a built-in way to make it manually updated
                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(nos.SourceClassGenericType, nos);

                if (ati != null && !string.IsNullOrEmpty(ati.MakeManuallyUpdatedMethod))
                {
                    forBlock.Line(ati.MakeManuallyUpdatedMethod.Replace("this", nos.InstanceName + "[i]") +
                                  ";");
                    add = true;
                }
            }

            if (add)
            {
                codeBlock.InsertBlock(forBlock);
            }
        }



        public static void GetPostCustomActivityForNamedObjectSave(IElement container, NamedObjectSave namedObjectSave, ICodeBlock codeBlock)
        {
            if (!string.IsNullOrEmpty(namedObjectSave.ClassType))
            {
                AssetTypeInfo ati = namedObjectSave.GetAssetTypeInfo();// AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObjectSave.ClassType);

                if (ati != null && !string.IsNullOrEmpty(ati.AfterCustomActivityMethod))
                {
                    bool shouldGenerate = true;

                    if (namedObjectSave.SourceType == SourceType.File && !string.IsNullOrEmpty(namedObjectSave.SourceFile))
                    {
                        ReferencedFileSave rfs = container.GetReferencedFileSaveRecursively(namedObjectSave.SourceFile);

                        if (rfs != null && !rfs.IsSharedStatic)
                        {
                            shouldGenerate = false;
                        }
                    }

                    if (shouldGenerate)
                    {
                        bool wrappInNullCheck = namedObjectSave.Instantiate == false;

                        if (wrappInNullCheck)
                        {
                            codeBlock = codeBlock.If(namedObjectSave.InstanceName + " != null");
                        }
                        codeBlock.Line(ati.AfterCustomActivityMethod.Replace("this", namedObjectSave.InstanceName) + ";");
                        if (wrappInNullCheck)
                        {
                            codeBlock = codeBlock.End();
                        }
                    }
                }
            }
        }

        public static void AddUsingForFlatRedBallType(List<string> usingsToAdd, string typeAsString)
        {
            Type flatRedBallType = TypeManager.GetFlatRedBallType(typeAsString);
            if (flatRedBallType != null)
            {
                string fullName = flatRedBallType.FullName;

                string typeNamespace = FileManager.RemoveExtension(fullName);

                usingsToAdd.Add(typeNamespace);

            }
        }
    }
}
