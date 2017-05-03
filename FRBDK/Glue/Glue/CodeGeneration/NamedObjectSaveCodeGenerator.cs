using System;
using System.Collections.Generic;
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

namespace FlatRedBall.Glue.CodeGeneration
{
    public enum CodeGenerationType
    {
        Full,
        OnlyContainedObjects,
        Nothing
    }




    public class NamedObjectSaveCodeGenerator : ElementComponentCodeGenerator
    {
        static List<string[]> mReusableEntireFileRfses;

        public static List<string[]> ReusableEntireFileRfses
        {
            get { return mReusableEntireFileRfses; }
            set { mReusableEntireFileRfses = value; }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            #region Generate NamedObject Fields

            codeBlock.Line("");

            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                var namedObject = element.NamedObjects[i];

                GenerateFieldAndPropertyForNamedObject(namedObject, codeBlock);
            }

            #endregion

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, SaveClasses.IElement element)
        {

            // Do the named object saves

            // We're going to do all "Entire File" NOS's first so that they aren't null before 
            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                NamedObjectSave nos = element.NamedObjects[i];

                if (nos.IsEntireFile)
                {
                    WriteCodeForNamedObjectInitialize(nos, element, codeBlock, null);
                }
            }

            // Now do non-entire files:
            for (int i = 0; i < element.NamedObjects.Count; i++)
            {
                NamedObjectSave nos = element.NamedObjects[i];

                if (!nos.IsEntireFile)
                {
                    WriteCodeForNamedObjectInitialize(nos, element, codeBlock, null);

                }
            }



            return codeBlock;
        }

        public override ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            // Now add anything that is part of a list to the list it belongs in
            // Update June 1, 2012
            // This used to be in GenerateInitialize
            // but was moved to GenerateInitializeLate
            // because we want addition to lists to be done
            // after all events for list modification have been
            // assigned - and that's done in GenerateInitialize by
            // the event code generator.
            // Update Sept 24, 2012
            // This used to be here, but we want to move it into PostInitialize
            // because the object may be added to a list that is initialized in the
            // base.Initialize call 
            //for (int i = 0; i < element.NamedObjects.Count; i++)
            //{
            //    NamedObjectSave nos = element.NamedObjects[i];

            //    foreach (NamedObjectSave containedNos in nos.ContainedObjects)
            //    {
            //        if (!containedNos.InstantiatedByBase && !containedNos.IsDisabled && containedNos.Instantiate)
            //        {
            //            bool shouldSkip = containedNos.SourceType == SourceType.File &&
            //                string.IsNullOrEmpty(containedNos.SourceFile);
            //            if (!shouldSkip)
            //            {
            //                codeBlock.Line(nos.InstanceName + ".Add(" + containedNos.InstanceName + ");");
            //            }
            //        }
            //    }
            //}

            return codeBlock;
        }


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            NamedObjectSaveCodeGenerator.WriteAddToManagersBottomUpForNamedObjectList(
                element.NamedObjects, codeBlock, element,  CodeWriter.ReusableEntireFileRfses);

            return codeBlock;
        }

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

        public static void WriteCodeForNamedObjectInitialize(NamedObjectSave namedObject, IElement saveObject,
            ICodeBlock codeBlock, string overridingName)
        {
            List<string[]> referencedFilesAlreadyUsingFullFile = ReusableEntireFileRfses;

            CodeGenerationType codeGenerationType = GetInitializeCodeGenerationType(namedObject, saveObject);

            #region ///////////////////////EARLY OUT///////////////////////////////

            if (codeGenerationType == CodeGenerationType.Nothing)
            {
                return;
            }

            #endregion ////////////////////////END EARLY OUT///////////////////////

            AddIfConditionalSymbolIfNecesssary(codeBlock, namedObject);


            bool succeeded = true;

            #region Perform instantiation

            if (!namedObject.InstantiatedByBase)
            {
                succeeded = GenerateInstantiationOrAssignment(
                    namedObject, saveObject, codeBlock, overridingName, referencedFilesAlreadyUsingFullFile);
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


                WriteTextSpecificInitialization(namedObject, saveObject, codeBlock, referencedFilesAlreadyUsingFullFile);

                foreach (NamedObjectSave containedNos in namedObject.ContainedObjects)
                {
                    WriteCodeForNamedObjectInitialize(containedNos, saveObject, codeBlock, null);
                }

            }

            AddEndIfIfNecessary(codeBlock, namedObject);
        }


        private static bool GenerateInstantiationOrAssignment(NamedObjectSave namedObject, IElement saveObject, 
            ICodeBlock codeBlock, string overridingName, List<string[]> referencedFilesAlreadyUsingFullFile)
        {
            AssetTypeInfo nosAti = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);

            string objectName = namedObject.FieldName;

            bool succeeded = true;

            #region If SourceType is File
            if (namedObject.SourceType == SourceType.File)
            {
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
                        string containerName = overridingName;
                        if (rfs != null)
                        {
                            containerName = rfs.GetInstanceName();// FileManager.RemovePath(FileManager.RemoveExtension(namedObject.SourceFile));
                        }

                        List<StateSave> statesUsingThisNamedObject = saveObject.GetAllStatesReferencingObject(objectName);

                        if (statesUsingThisNamedObject.Count != 0)
                        {
                            InstantiateObjectInSwitchStatement(namedObject, codeBlock, referencedFilesAlreadyUsingFullFile,
                                nosAti, objectName, rfs, statesUsingThisNamedObject, saveObject, containerName, null);
                        }
                        else
                        {
                            InstantiateObjectUsingFile(namedObject, codeBlock, referencedFilesAlreadyUsingFullFile, nosAti, objectName, rfs, saveObject, containerName, overridingName);
                        }
                    }
                }
            }

            #endregion

            else if (namedObject.SourceType == SourceType.FlatRedBallType)
            {
                // We treat Cameras in a special way:
                if (namedObject.ClassType == "Camera")
                {
                    if (namedObject.IsNewCamera)
                    {
                        string contentManagerNameString = "ContentManagerName";
                        codeBlock.Line(objectName + " = new FlatRedBall.Camera(" + contentManagerNameString + ");");
                        codeBlock.Line("FlatRedBall.SpriteManager.Cameras.Add(" + objectName + ");");
                    }
                    else
                    {
                        codeBlock.Line(objectName + " = FlatRedBall.SpriteManager.Camera;");

                    }
                }
                else if (namedObject.IsContainer)
                {
                    codeBlock.Line(objectName + " = this;");
                }
                else
                {
                    string qualifiedName = namedObject.GetQualifiedClassType();

                    if (namedObject.GetAssetTypeInfo() != null)
                    {
                        qualifiedName = namedObject.GetAssetTypeInfo().QualifiedRuntimeTypeName.QualifiedType;
                    }


                    codeBlock.Line(string.Format("{0} = new {1}();", objectName, qualifiedName));

                    if (namedObject.IsLayer || 
                        namedObject.SourceType == SourceType.FlatRedBallType)
                    {
                        codeBlock.Line(string.Format("{0}.Name = \"{1}\";", objectName, objectName));
                    }
                }


            }



            #region else if SourceType is Entity

            else // SourceType == SourceType.Entity
            {
                codeBlock.Line(string.Format("{0} = new {1}(ContentManagerName, false);", objectName,
                                             GetQualifiedTypeName(namedObject)));
                codeBlock.Line(string.Format("{0}.Name = \"{1}\";", objectName, objectName));
                // If it's an Entity List that references a List that can be created by Entities, then the Screen should register this list with the factory

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
            List<string[]> referencedFilesAlreadyUsingFullFile, AssetTypeInfo ati, string objectName, ReferencedFileSave rfs,
            List<StateSave> stateSaves, IElement saveObject, string defaultContainer, string overridingName)
        {
            var switchBlock = codeBlock.Switch("LoadingState");

            for (int i = 0; i < stateSaves.Count; i++)
            {
                StateSave stateSave = stateSaves[i];

                string name = "";

                // I don't think we're going to use these anymore
                //NamedObjectPropertyOverride objectOverride = stateSave.GetNamedObjectOverride(objectName);
                //name = FileManager.RemovePath(FileManager.RemoveExtension(objectOverride.SourceFile));

                InstantiateObjectUsingFile(namedObject, switchBlock.Case("VariableState." + stateSave.Name), referencedFilesAlreadyUsingFullFile, ati, objectName, rfs,
                    saveObject,
                    name, overridingName);
            }

            InstantiateObjectUsingFile(namedObject, switchBlock.Case("VariableState.Uninitialized:"), referencedFilesAlreadyUsingFullFile, ati, objectName, rfs,
                saveObject, defaultContainer, overridingName);
        }


        private static void InstantiateObjectUsingFile(NamedObjectSave namedObject, ICodeBlock codeBlock,
            List<string[]> referencedFilesAlreadyUsingFullFile, AssetTypeInfo nosAti, string objectName,
            ReferencedFileSave rfs, IElement container, string containerName, string overridingName)
        {
            #region If the user hasn't picked an object inside the file, it's either a SpriteRig or we should return ""

            if (string.IsNullOrEmpty(namedObject.SourceName) || namedObject.SourceName == "<NONE>")
            {
                if (FileManager.GetExtension(namedObject.SourceFile) == "srgx")
                {
                    codeBlock.Line(objectName + " = " + containerName + ";");
                }
            }

            #endregion

            #region else, the user has picked a file

            else
            {
                containerName = WriteMethodForClone(namedObject, codeBlock, referencedFilesAlreadyUsingFullFile, nosAti, objectName, rfs, container, containerName, overridingName);

            }

            #endregion
        }

        private static void WriteCopyToAbsoluteInInitializeCode(NamedObjectSave namedObject, ICodeBlock codeBlock, List<string[]> referencedFilesAlreadyUsingFullFile, AssetTypeInfo ati, string objectName, ReferencedFileSave rfs)
        {
            if ((ati != null && ati.ShouldAttach) ||
                namedObject.SourceType == SourceType.Entity &&
                !string.IsNullOrEmpty(namedObject.SourceClassType)
                )
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
                    foreach (string[] stringPair in referencedFilesAlreadyUsingFullFile)
                    {
                        if (stringPair[0] == rfs.Name && !isEntireFile)
                        {
                            namedObjectToPullFrom = stringPair[1];
                            break;
                        }
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

        private static string WriteMethodForClone(NamedObjectSave namedObject, ICodeBlock codeBlock,
            List<string[]> referencedFilesAlreadyUsingFullFile, AssetTypeInfo nosAti, string objectName,
            ReferencedFileSave rfs, IElement container, string containerName, string overridingName)
        {
            int lastParen = namedObject.SourceName.LastIndexOf(" (");
            string nameOfSourceInContainer = namedObject.SourceName.Substring(0, lastParen);
            // This could have a quote in the name.  If so we want to escape it since this will be put in code:
            nameOfSourceInContainer = nameOfSourceInContainer.Replace("\"", "\\\"");

            string cloneString = ".Clone()";

            string namedObjectToPullFrom = null;
            foreach (string[] stringPair in referencedFilesAlreadyUsingFullFile)
            {
                if (rfs != null && stringPair[0] == rfs.Name && !namedObject.SourceName.StartsWith("Entire File ("))
                {
                    namedObjectToPullFrom = stringPair[1];
                    break;
                }
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
                    cloneString = foundConversion.ConversionPattern.Replace("{NEW}", objectName);
                    cloneString = cloneString.Replace("{THIS}", rfs.GetInstanceName());
                    usesFullConversion = true;
                }
            }

            if (namedObjectToPullFrom != null)
            {
                containerName = namedObjectToPullFrom;
            }
            if (!string.IsNullOrEmpty(overridingName))
            {
                containerName = overridingName;
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
                    string entireString = cloneString.Replace("{THIS}", objectName);
                    entireString = entireString.Replace("{SOURCE_FILE}", containerName);

                    codeBlock.Line(entireString);
                }
                else
                {
                    codeBlock.Line(string.Format("{0} = {1}{2};",
                                                 objectName,
                                                 containerName,
                                                 cloneString));
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

                //if (namedObject.AddToManagers)
                {
                    // Not sure why we don't clone on a non add to manager.  This post right here suggests we should
                    // and I tend to believe Scott so...I'm following his advice:
                    // http://www.flatredball.com/frb/forum/viewtopic.php?f=26&t=4741
                    codeBlock.Line(string.Format("{0} = {1}{2}{3}{4}{5};",
                                                 objectName,
                                                 containerName,
                                                 possibleDot,
                                                 listMemberName,
                                                 findByNameLine,
                                                 cloneString));
                }
                //else
                //{

                //    stringBuilder.AppendLine(
                //        string.Format("\t\t\t{0} = {1}{2}{3}{4};",
                //            objectName,
                //            containerName,
                //            possibleDot,
                //            listMemberName,
                //            findByNameLine));
                //}
            }
            return containerName;
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

        private static void WriteTextSpecificInitialization(NamedObjectSave namedObject, IElement element, ICodeBlock codeBlock, List<string[]> referencedFilesAlreadyUsingFullFile)
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
                    else if (namedObject.ClassType == "Text")
                    {
                        // If the NamedObject uses a File that has already been used in another "Entire File" NamedObject, then we want to make sure 
                        // we don't double-translate
                        bool isAlreadyTranslated = false;

                        foreach (string[] stringPair in referencedFilesAlreadyUsingFullFile)
                        {
                            if (stringPair[0] == namedObject.SourceFile && !namedObject.SourceName.StartsWith("Entire File ("))
                            {
                                isAlreadyTranslated = true;
                                break;
                            }
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

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);

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

            if (namedObject.SourceType == SourceType.FlatRedBallType && namedObject.SourceClassType == "Camera" && namedObject.IsNewCamera)
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

                        AssetTypeInfo atiForListElement = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(genericClassType);

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
                codeBlock.Line(StringHelper.SpaceStrings(accessModifier, typeName, variableName) + ";");

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

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);

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
            if (namedObjectSave.SourceType == SaveClasses.SourceType.Entity &&
                !string.IsNullOrEmpty(namedObjectSave.SourceClassType))
            {

                return ProjectManager.ProjectNamespace + '.' + namedObjectSave.SourceClassType.Replace('\\', '.');
            }
            else if (namedObjectSave.GetAssetTypeInfo() != null)
            {
                return namedObjectSave.GetAssetTypeInfo().QualifiedRuntimeTypeName.QualifiedType;
            }
            else
            {
                return namedObjectSave.GetQualifiedClassType();
            }
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
                        typeOfResetVariable = TypeManager.ConvertToCommonType(((PropertyInfo)memberInfo).PropertyType.ToString());
                    }
                    else
                    {
                        typeOfResetVariable = TypeManager.ConvertToCommonType(((FieldInfo)memberInfo).FieldType.ToString());

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

            if (!setByDerived)
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
                else if (namedObjectSave.SourceType == SourceType.FlatRedBallType &&
                    namedObjectSave.ClassType != null &&
                    namedObjectSave.ClassType.Contains("PositionedObjectList<"))
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

            }

            // If it's an emitter, call TimedEmit:
            ParticleCodeGenerator.GenerateTimedEmit(codeBlock, namedObjectSave);

            if (!setByDerived)
            {

                if (namedObjectSave.Instantiate == false)
                {
                    // end the if-statement we started above.
                    codeBlock = codeBlock.End();
                }
            }

            AddEndIfIfNecessary(codeBlock, namedObjectSave);
        }

        public static void GetPostInitializeForNamedObjectList(NamedObjectSave container, List<NamedObjectSave> namedObjectList, ICodeBlock codeBlock, IElement element)
        {

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
                            codeBlock.Line(container.InstanceName + ".Add(" + nos.InstanceName + ");");
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
                        WriteAttachTo(nos, codeBlock, ReusableEntireFileRfses, rfsReferenced);
                    }

                    GetPostInitializeForNamedObjectList(nos, codeBlock);

                    GetPostInitializeForNamedObjectList(nos, nos.ContainedObjects, codeBlock, element);
                    if (wrappInIf)
                    {
                        codeBlock = codeBlock.End();
                    }
                    AddEndIfIfNecessary(codeBlock, nos);
                }
            }
        }

        public static void WriteAddToManagersBottomUpForNamedObjectList(List<NamedObjectSave> namedObjectList, ICodeBlock codeBlock, IElement element, List<string[]> reusableEntireFileRfses)
        {
            foreach(var nos in namedObjectList.Where(nos=>nos.SourceType != SourceType.FlatRedBallType || nos.SourceClassType != "Layer"))
            {
                ReferencedFileSave filePullingFrom = null;

                string[] foundPair = null;

                if (nos.SourceType == SourceType.File && nos.SourceName != null)
                {
                    if (nos.IsEntireFile)
                    {
                        filePullingFrom = element.GetReferencedFileSave(nos.SourceFile);
                    }
                    else
                    {
                        foreach (string[] stringPair in reusableEntireFileRfses)
                        {
                            if (stringPair[0] == nos.SourceFile)
                            {
                                foundPair = stringPair;
                                break;
                            }
                        }
                    }
                }

                NamedObjectSave entireFileNos = null;

                if (foundPair != null)
                {
                    entireFileNos = element.GetNamedObjectRecursively(foundPair[1]);
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

        static void GetPostInitializeForNamedObjectList(NamedObjectSave namedObject, ICodeBlock codeBlock)
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

                GenerateVariableAssignment(namedObject, codeBlock);

                if (!namedObject.SetByDerived && !namedObject.SetByContainer)
                {
                    AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);

                    if (ati != null && !string.IsNullOrEmpty(ati.PostInitializeCode))
                    {
                        codeBlock.Line(ati.PostInitializeCode.Replace("this", namedObjectName) + ";");
                    }


                    // Eventually I want to move this to a plugin but plugins need to be able to override how
                    // variables are generated.  Then they can add custom code and suppress FRB from doing it.
                    if (ati != null && ati.QualifiedRuntimeTypeName.QualifiedType == typeof(Polygon).FullName &&
                        namedObject.SourceType == SourceType.FlatRedBallType && namedObject.SourceClassType == "Polygon")
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
                                    internalPoints += "new FlatRedBall.Math.Geometry.Point(" + point.X + ", " + point.Y + ")";

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



        public static void GenerateVariableAssignment(NamedObjectSave namedObject, ICodeBlock codeBlock)
        {

            IEnumerable<CustomVariableInNamedObject> enumerable = namedObject.InstructionSaves;
            var ati = namedObject.GetAssetTypeInfo();

            var variableDefinitions = ati?.VariableDefinitions;
            

            if (variableDefinitions != null)
            {
                enumerable = enumerable.OrderBy(item =>
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
                    });

            }

            foreach (var instructionSave in enumerable)
            {
                IElement element = null;

                if (namedObject.SourceType == SourceType.Entity)
                {
                    element = ObjectFinder.Self.GetIElement(namedObject.SourceClassType);
                }


                if (ExposedVariableManager.IsExposedVariable(instructionSave.Member, namedObject) ||
                    (element != null && element.GetCustomVariableRecursively(instructionSave.Member) != null) ||
                    // Could be something set by container:
                    (element != null && element.GetNamedObjectRecursively(instructionSave.Member) != null)
                    )
                {
                    CustomVariableCodeGenerator.AppendAssignmentForCustomVariableInInstance(namedObject, codeBlock, instructionSave);
                }
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

                foreach (string[] stringPair in ReusableEntireFileRfses)
                {
                    if (stringPair[0] == namedObject.SourceFile && !namedObject.SourceName.StartsWith("Entire File ("))
                    {
                        return true;
                    }
                }
            }
            return false;
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
                string objectName = namedObject.FieldName;
                AssetTypeInfo ati = namedObject.GetAssetTypeInfo();

                if ((considerRemoveIfInvisible && namedObject.RemoveFromManagersWhenInvisible && IsInvisible(namedObject, element)))
                {
                    if (namedObject.SourceType == SourceType.Entity)
                    {
                        // since we want to have all contained elements in namedObject also call AssignCustomVariables, we'll pass 'true'
                        codeBlock.Line(objectName + ".AssignCustomVariables(true);");
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
                    bool addedRegularly = namedObject.AddToManagers && !namedObject.InstantiatedByBase && !isAddedToManagerByFile;
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

                        #region There is an ATI - it's a type defined in the ContentTypes.csv file in Glue
                        if (ati != null)
                        {
                            if ((BaseElementTreeNode.IsOnOwnLayer(element)
                                || !string.IsNullOrEmpty(namedObject.LayerOn))
                                && ati.LayeredAddToManagersMethod.Count != 0 && !string.IsNullOrEmpty(ati.LayeredAddToManagersMethod[0]))
                            {
                                string layerAddToManagersMethod = DecideOnLineToAdd(namedObject, ati, true);

                                // This used to be inside the if(element is EntitySave) but
                                // I think we want it even if the ElementSave is a Screen.


                                layerAddToManagersMethod = layerAddToManagersMethod.Replace("mLayer", layerName);

                                codeBlock.Line(layerAddToManagersMethod.Replace("this", objectName) + ";");


                            }
                            else
                            {


                                if (ati.AddToManagersMethod.Count != 0 && !string.IsNullOrEmpty(ati.AddToManagersMethod[0]))
                                {
                                    string addLine = DecideOnLineToAdd(namedObject, ati, false);

                                    codeBlock.Line(addLine.Replace("this", objectName) + ";");

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
                                        codeBlock.Line("FlatRedBall.SpriteManager.MoveLayerAboveLayer(" + objectName + ", " + layerToAddAbove + ");");
                                    }
                                }
                            }

                            AddLayerSpecificAddToManagersCode(namedObject, codeBlock, objectName);

                            AddTextSpecificAddToManagersCode(namedObject, codeBlock, objectName, layerName);
                        }
                        #endregion
                        #region No ATI - is it an Entity?
                        else if (namedObject.SourceType == SourceType.Entity)
                        {
                            if (isInsideVisibleProperty)
                            {
                                codeBlock.Line(objectName + ".ReAddToManagers(" + layerName + ");");
                            }
                            else
                            {
                                codeBlock.Line(objectName + ".AddToManagers(" + layerName + ");");
                            }
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
                    bool shouldAddToLayer = (!namedObject.AddToManagers || isAddedToManagerByFile) && !string.IsNullOrEmpty(namedObject.LayerOn) &&
                        namedObject.SourceType == SourceType.File &&
                        namedObject.SourceName != null &&
                        //namedObject.SourceName.Contains("Entire ") && 
                        ati != null &&
                        ati.LayeredAddToManagersMethod.Count != 0 &&
                        !string.IsNullOrEmpty(ati.LayeredAddToManagersMethod[0]);

                    if (shouldAddToLayer)
                    {
                        string layerAddToManagersMethod = ati.LayeredAddToManagersMethod[0];

                        string layerName = GetNamedObjectLayerName(namedObject);

                        layerAddToManagersMethod = layerAddToManagersMethod.Replace("mLayer", layerName);


                        codeBlock.Line(layerAddToManagersMethod.Replace("this", objectName) + ";");
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

        public static void AssignInstanceVaraiblesOn(IElement element, NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);
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


            GenerateVariableAssignment(namedObject, codeBlock);


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

        private static void AddLayerSpecificAddToManagersCode(NamedObjectSave namedObject, ICodeBlock codeBlock, string objectName)
        {
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
                            codeBlock.Line(objectName + ".LayerCameraSettings.LeftDestination = " + FlatRedBall.Math.MathFunctions.RoundToInt( leftDestination ) + ";");
                            codeBlock.Line(objectName + ".LayerCameraSettings.TopDestination = " + FlatRedBall.Math.MathFunctions.RoundToInt(topDestination ) + ";");

                            codeBlock.Line(objectName + ".LayerCameraSettings.RightDestination = " + FlatRedBall.Math.MathFunctions.RoundToInt(rightDestination ) + ";");
                            codeBlock.Line(objectName + ".LayerCameraSettings.BottomDestination = " + FlatRedBall.Math.MathFunctions.RoundToInt(bottomDestination ) + ";");

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

        private static void AddTextSpecificAddToManagersCode(NamedObjectSave namedObject, ICodeBlock codeBlock, string objectName, string layerName)
        {
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
            if (namedObject.SourceType == SourceType.FlatRedBallType && namedObject.SourceClassType == "Text" && namedObject.IsPixelPerfect)
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

        private static void WriteAttachTo(NamedObjectSave namedObject, ICodeBlock codeBlock, List<string[]> referencedFilesAlreadyUsingFullFile, ReferencedFileSave rfs)
        {

            string objectName = namedObject.FieldName;
            AssetTypeInfo ati = namedObject.GetAssetTypeInfo();

            if ((namedObject.GetContainerType() == ContainerType.Entity && namedObject.AttachToContainer) || namedObject.AttachToCamera)
            {
                bool shouldAttach = true;



                if (ati != null)
                {
                    shouldAttach = ati.ShouldAttach;
                }
                if (namedObject.IsList)
                {
                    shouldAttach = false;
                }

                if (shouldAttach)
                {
                    string whatToAttachTo = "this";

                    if (namedObject.AttachToCamera)
                    {
                        whatToAttachTo = "FlatRedBall.Camera.Main";
                    }


                    // Files (like .scnx) can
                    // contain objects which can
                    // be attached to other objects
                    // in the file.  In some cases this
                    // hierarchy is important.  Therefore,
                    // the generated code will 
                    string attachMethodCall = "AttachTo";
                    bool wrapInIf = true;
                    if (ati != null && !string.IsNullOrEmpty(ati.AttachToNullOnlyMethod))
                    {
                        attachMethodCall = ati.AttachToNullOnlyMethod;
                        wrapInIf = false;
                    }

                    string tabs = "\t\t\t";

                    if (namedObject.ClassType == "SpriteRig")
                    {
                        codeBlock.Line(objectName + ".Root.AttachTo(" + whatToAttachTo + ", true);");

                    }
                    else
                    {
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
                        WriteCopyToAbsoluteInInitializeCode(namedObject, currentBlock, referencedFilesAlreadyUsingFullFile, ati, objectName, rfs);

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


                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(namedObject.InstanceType);

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

        public static void WriteConvertToManuallyUpdated(ICodeBlock codeBlock, IElement element, List<string[]> reusableEntireFileRfses)
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

                            foreach (string[] stringPair in reusableEntireFileRfses)
                            {
                                if (stringPair[0] == nos.SourceFile)
                                {
                                    alreadyHandledByEntireFileObject = true;
                                    break;
                                }
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
                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(nos.SourceClassGenericType);

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
