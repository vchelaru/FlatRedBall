using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;

namespace FlatRedBall.Glue
{
    public static class CsvCodeGenerator
    {
        public static void RegenerateAllCsvs()
        {
            foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
            {
                foreach (ReferencedFileSave rfs in entitySave.ReferencedFiles)
                {
                    if (rfs.IsCsvOrTreatedAsCsv)
                    {
                        CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                    }
                }
            }

            foreach (ScreenSave screensave in ProjectManager.GlueProjectSave.Screens)
            {
                foreach (ReferencedFileSave rfs in screensave.ReferencedFiles)
                {
                    if (rfs.IsCsvOrTreatedAsCsv)
                    {
                        CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                    }
                }
            }

            foreach (ReferencedFileSave rfs in ProjectManager.GlueProjectSave.GlobalFiles)
            {
                if (rfs.IsCsvOrTreatedAsCsv && !rfs.IsDatabaseForLocalizing)
                {
                    try
                    {
                        CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Error generating the file\n\n" + rfs.Name + "\n\nError details:\n\n" +
                            e.ToString());
                    }
                }
            }
        }

        public static async Task GenerateAndSaveDataClassAsync(ReferencedFileSave rfs, AvailableDelimiters delimiter)
        {
            await TaskManager.Self.AddAsync(() => GenerateAndSaveDataClass(rfs, delimiter), "GenerateAndSaveDataClassAsync " + rfs);
        }

        public static void GenerateAndSaveDataClass(ReferencedFileSave rfs, AvailableDelimiters delimiter)
        {
            TaskManager.Self.WarnIfNotInTask();
            string fileName = rfs.Name;
            fileName = GlueCommands.Self.GetAbsoluteFileName(rfs);

            /////////////// Early Out /////////////////////////
            if (!System.IO.File.Exists(fileName))
            {
                GlueCommands.Self.PrintError("Could not find the CSV file " + fileName + 
                    " when trying to generate a data file");
            }
            //////////////End Early Out//////////////////////

            #region Save off the old delimiter and switch to using the new one - we need the old one so we can switch back after this function finishes

            char oldDelimiter = CsvFileManager.Delimiter;
            CsvFileManager.Delimiter = delimiter.ToChar();
            #endregion

            if (!string.IsNullOrEmpty(rfs.UniformRowType))
            {
                // This simply
                // checks to make
                // sure the CSV is
                // set up right - it
                // doesn't actually generate
                // any code because the CSV will
                // deserialize to an array of primitives.
                // Update December 6 2022 Now handled by error
                // window.
                //CheckUniformTypeValidity(rfs, fileName, oldDelimiter);
            }
            else
            {
                RuntimeCsvRepresentation rcr;
                bool succeeded;
                DeserializeToRcr(delimiter, fileName, out rcr, out succeeded);
                    
                if(succeeded)
                {
                    CsvFileManager.Delimiter = oldDelimiter;
                    

                    string whyIsCsvWrong = GetDuplicateMessageIfDuplicatesFound(rcr, rfs.CreatesDictionary, fileName);

                    if (!string.IsNullOrEmpty(whyIsCsvWrong))
                    {
                        // No need - handled by error view model
                        //GlueGui.ShowMessageBox(whyIsCsvWrong);
                        succeeded = false;
                    }
                    else
                    {
                        string className;
                        List<TypedMemberBase> members;
                        Dictionary<string, string> untypedMembers;

                        CustomClassSave customClass = GetCustomClassForCsv(rfs.Name);

                        if (customClass == null || customClass.GenerateCode)
                        {
                            fileName = GetClassInfoFromCsvs(rfs, fileName, rcr, out className, out members, out untypedMembers);


                            succeeded = GenerateClassFromMembers(rfs, succeeded, className, members, untypedMembers);
                        }
                    }
                }
            }
        }

        public static void GetDictionaryTypes(ReferencedFileSave referencedFileSave, out string keyType, out string valueType)
        {
            valueType = referencedFileSave.GetTypeForCsvFile();

            // To know the value type, we got to pop this file open and find the first required type
            keyType = null;

            char oldDelimiter = CsvFileManager.Delimiter;

            switch (referencedFileSave.CsvDelimiter)
            {
                case AvailableDelimiters.Comma:
                    CsvFileManager.Delimiter = ',';
                    break;
                case AvailableDelimiters.Tab:
                    CsvFileManager.Delimiter = '\t';
                    break;
                case AvailableDelimiters.Pipe:
                    CsvFileManager.Delimiter = '|';
                    break;
            }

            string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);

            // If the file doesn't exist this will generate bad code.  But this isn't
            // considered a silent failure because Glue will raise flags about missing
            // files earlier (like when it first starts up).  We don't want to crash the
            // entire application in this case.
            if (System.IO.File.Exists(absoluteFileName))
            {
                RuntimeCsvRepresentation rcr = CsvFileManager.CsvDeserializeToRuntime(absoluteFileName);

                // See if any of the headers are required
                foreach (CsvHeader header in rcr.Headers)
                {
                    int indexOfOpeningParen = header.Name.IndexOf("(");

                    if (indexOfOpeningParen != -1)
                    {
                        if (header.Name.IndexOf("required", indexOfOpeningParen) != -1)
                        {
                            keyType = CsvHeader.GetClassNameFromHeader(header.Name);
                            break;
                        }
                    }
                }
            }

            CsvFileManager.Delimiter = oldDelimiter;
        }

        internal static List<string> GetMemberNamesFrom(ReferencedFileSave rfs)
        {
            List<string> toReturn = new List<string>();
                
                        
            string fileName;
            fileName = GlueCommands.Self.GetAbsoluteFileName(rfs);

            RuntimeCsvRepresentation rcr = CsvFileManager.CsvDeserializeToRuntime(
                fileName);

                            
            for (int i = 0; i < rcr.Headers.Length; i++)
            {
                string memberName = rcr.Headers[i].Name;

                if (memberName.Trim().StartsWith("//"))
                {
                    continue;
                }

                memberName = StringFunctions.RemoveWhitespace(memberName); 

                if (memberName.Contains("("))
                {
                    memberName = memberName.Substring(0, memberName.IndexOfAny(new char[] { '(' }));
                }

                toReturn.Add(memberName);
            }


            return toReturn;
        }

        private static bool GenerateClassFromMembers(ReferencedFileSave rfs, bool succeeded, string className, List<TypedMemberBase> members, Dictionary<string, string> untypedMembers)
        {
            ICodeBlock codeBlock = new CodeDocument();

            // If the CSV
            // is going to
            // be deserialized
            // to a dictionary,
            // then we should create
            // const members for all of
            // the keys in the dictionary
            // to make accessing the dictionary
            // type-safe.
            if (rfs != null)
            {
                succeeded = CreateConstsForCsvEntries(rfs, members, untypedMembers, codeBlock);
            }

            FilePath absoluteFileName = null;

            if (succeeded)
            {

                ICodeBlock codeContent = CodeWriter.CreateClass(ProjectManager.ProjectNamespace + ".DataTypes", className, true, true, members, false, new List<string>(), untypedMembers, codeBlock);


                if (rfs != null)
                {
                     absoluteFileName = Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectDirectory + GetFullDataFileNameFor(rfs);
                }
                else
                {
                    absoluteFileName = Plugins.ExportedImplementations.GlueState.Self.CurrentGlueProjectDirectory + "DataTypes/" + className + ".Generated.cs";
                }

                CodeWriter.SaveFileContents(codeContent.ToString(), absoluteFileName.FullPath, true);

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(absoluteFileName, save:false);
            }

            // 4/27/2021 - this is noisy. Do users really care?
            //if (succeeded)
            //{
            //    string message;
            //    if (rfs != null)
            //    {
            //        message = "Generating class " + className + " from CSV " + rfs.Name + " to " + absoluteFileName;
            //    }
            //    else
            //    {
            //        message = "Generating class " + className + " from Custom Class";
            //    }
                

            //    Plugins.PluginManager.ReceiveOutput(message);
            //}
            return succeeded;
        }

        private static bool CreateConstsForCsvEntries(ReferencedFileSave initialRfs, List<TypedMemberBase> members, Dictionary<string, string> untypedMembers, ICodeBlock codeBlock)
        {
            bool succeeded = true;

            bool addToOrderedLists = true;


            CustomClassSave customClass = GetCustomClassForCsv(initialRfs.Name);

            bool usesCustomClass = customClass != null;
            List<ReferencedFileSave> rfsesForClass = new List<ReferencedFileSave>();

            if (usesCustomClass)
            {
                foreach (string name in customClass.CsvFilesUsingThis)
                {
                    ReferencedFileSave foundRfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(name);

                    // A dupe was added one during a Glue crash, so let's protect against that:
                    if (foundRfs != null && rfsesForClass.Contains(foundRfs) == false)
                    {
                        rfsesForClass.Add(foundRfs);

                    }
                }
            }
            else
            {
                rfsesForClass.Add(initialRfs);
            }


            Dictionary<ReferencedFileSave, RuntimeCsvRepresentation> representations = new Dictionary<ReferencedFileSave, RuntimeCsvRepresentation>();


            List<string> allKeys = new List<string>();
            foreach (ReferencedFileSave rfs in rfsesForClass)
            {
                if (rfs.CreatesDictionary)
                {
                    string fileName = rfs.Name;
                    fileName = GlueCommands.Self.GetAbsoluteFileName(fileName, true);

                    var rcr = CsvFileManager.CsvDeserializeToRuntime(fileName);

                    representations.Add(rfs, rcr);
                    rcr.RemoveHeaderWhitespaceAndDetermineIfRequired();
                    int requiredIndex = rcr.GetRequiredIndex();
                    if (requiredIndex == -1)
                    {
                        succeeded = false;
                        GlueGui.ShowMessageBox("The file " + rfs.Name + " is marked as a dictionary but has no column marked as required");
                    }
                    else
                    {
                        var requiredValues = RequiredColumnValues(rcr);
                        allKeys.AddRange(requiredValues);

                    }
                }
            }

            if(allKeys.Any())
            {
                var distinct = allKeys.Distinct();

                var firstRcr = representations.First().Value;
                int requiredIndex = firstRcr.GetRequiredIndex();

                string type = GetRequiredKeyType(firstRcr, members, untypedMembers, requiredIndex);


                FillCodeBlockWithKeys(codeBlock, type, firstRcr, allKeys.Distinct().ToArray());
                // the first rcr defines the ordered keys. Others won't add themselves to this list
                FillOrderedListWithKeys(codeBlock, type, firstRcr);
            }

            return succeeded;
        }

        private static void FillOrderedListWithKeys(ICodeBlock codeBlock, string type, RuntimeCsvRepresentation rcr)
        {
            int numberAdded = 0;
            foreach (string value in RequiredColumnValues(rcr))
            {
                if (numberAdded == 0)
                {
                    codeBlock.Line("public static System.Collections.Generic.List<" + type + "> OrderedList = new System.Collections.Generic.List<" + type + ">");
                    codeBlock.Line("{");
                    codeBlock.TabCount++;

                }
                string name = GetConstNameForValue(value);
                
                if (numberAdded != 0)
                {
                    // This is a little more efficient than doing a count first, then adding
                    codeBlock.Line("," + name);
                }
                else
                {
                    codeBlock.Line(name);
                }
                numberAdded++;
            }

            if (numberAdded != 0)
            {
                codeBlock.TabCount--;
                codeBlock.Line("};");
            }
        }

        public static string GetFullDataFileNameFor(ReferencedFileSave rfs)
        {
            string toReturn = "DataTypes/" + rfs.GetUnqualifiedTypeForCsv() + ".Generated.cs";
            return toReturn;
        }

        private static string GetClassInfoFromCsvs(ReferencedFileSave rfs, string fileName, RuntimeCsvRepresentation rcr, out string className, out List<TypedMemberBase> members, out Dictionary<string, string> untypedMembers)
        {
            className = rfs.GetUnqualifiedTypeForCsv();

            CustomClassSave customClass = GetCustomClassForCsv(rfs.Name);

            fileName = GetClassInfo(fileName, rcr, customClass, out members, out untypedMembers);
            return fileName;
        }

        private static string GetClassInfo(string fileName, RuntimeCsvRepresentation rcr, CustomClassSave customClass, out List<TypedMemberBase> members, out Dictionary<string, string> untypedMembers)
        {
            bool usesCustomClass = customClass != null;
            List<RuntimeCsvRepresentation> rcrsForClass = new List<RuntimeCsvRepresentation>();

            if (usesCustomClass)
            {
                foreach (string name in customClass.CsvFilesUsingThis)
                {
                    var filePath = GlueCommands.Self.GetAbsoluteFilePath(name);
                    ReferencedFileSave foundRfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath);

                    if (foundRfs == null)
                    {
                        int m = 3;
                    }
                    else
                    {
                        fileName = GlueCommands.Self.GetAbsoluteFileName(foundRfs);

                        RuntimeCsvRepresentation runtimeToAdd = null;
                        try
                        {
                            runtimeToAdd = CsvFileManager.CsvDeserializeToRuntime(fileName);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error trying to parse CSV:\n" + e.ToString());
                        }

                        if (runtimeToAdd != null)
                        {
                            rcrsForClass.Add(runtimeToAdd);
                        }
                    }
                }
            }
            else if(rcr != null)
            {
                rcrsForClass.Add(rcr);
            }




            GetClassInfoFromCsv(rcrsForClass, customClass, out members, out untypedMembers);
            return fileName;
        }

        public static void DeserializeToRcr(AvailableDelimiters delimiter, FilePath filePath, out RuntimeCsvRepresentation rcr, out bool succeeded)
        {
            DeserializeToRcr(delimiter, filePath.FullPath, out rcr, out succeeded);
        }

        public static void DeserializeToRcr(AvailableDelimiters delimiter, string fileName, out RuntimeCsvRepresentation rcr, out bool succeeded)
        {
            rcr = null;

            succeeded = true;

            try
            {
                // Let's load this bad boy and get info about the class we need to make
                rcr = CsvFileManager.CsvDeserializeToRuntime(fileName);
            }
            catch (Exception e)
            {
                GlueGui.ShowMessageBox("Error parsing CSV\n\n" + fileName + "\nAttempted to use " + delimiter + " as the delimiter.  If this is not the delimiter of the file, try changing the delimiter in Glue after the file is added");

                FlatRedBall.Glue.Plugins.PluginManager.ReceiveError(e.ToString());

                succeeded = false;
            }
        }

        private static void FillCodeBlockWithKeys(ICodeBlock codeBlock, string keyType, RuntimeCsvRepresentation rcr, ICollection<string> requiredColumnValues)
        {

            foreach(string value in requiredColumnValues)
            {
                string rightSideOfEquals = value;

                if (keyType == "System.String")
                {
                    rightSideOfEquals = "\"" + rightSideOfEquals + "\"";
                }
                string leftSideOfEquals = GetConstNameForValue(value);

                codeBlock.Line("public const " + keyType + " " + leftSideOfEquals + " = " + rightSideOfEquals + ";");
            }
        }

        public static string GetConstNameForValue(string value)
        {

            string leftSideOfEquals = value;

            if (char.IsDigit(leftSideOfEquals[0]))
            {
                // Need to prepend something
                // because variable names can't
                // start with a number
                leftSideOfEquals = "_" + leftSideOfEquals;
            }
            leftSideOfEquals = leftSideOfEquals.Replace(' ', '_');
            foreach (var character in NameVerifier.InvalidCharacters)
            {
                if (leftSideOfEquals.Contains(character))
                {
                    leftSideOfEquals = leftSideOfEquals.Replace(character, '_');
                }
            }

            return leftSideOfEquals;
        }

        static IEnumerable<string> RequiredColumnValues(RuntimeCsvRepresentation rcr)
        {
            int requiredHeader = rcr.GetRequiredIndex();
            foreach (string[] record in rcr.Records)
            {
                string value = record[requiredHeader];

                if (!string.IsNullOrEmpty(value) && !value.StartsWith("//"))
                {
                    yield return value;
                }
            }
        }

        private static string GetRequiredKeyType(RuntimeCsvRepresentation rcr, List<TypedMemberBase> members, Dictionary<string, string> untypedMembers, int requiredHeader)
        {
            // At this point the headers have their proper names (like XOffset) and don't include their type, so we
            // can use the simple .Name property
            string requiredMember = rcr.Headers[requiredHeader].Name;

            string type = null;

            foreach (TypedMemberBase tmb in members)
            {
                if (tmb.MemberName == requiredMember)
                {
                    type = tmb.MemberType.FullName;
                    break;
                }
            }

            if (string.IsNullOrEmpty(type))
            {
                foreach (KeyValuePair<string, string> kvp in untypedMembers)
                {
                    if (kvp.Key == requiredMember)
                    {
                        type = kvp.Value;
                        break;
                    }
                }
            }
            return type;
        }

        private static void GetClassInfoFromCsv(List<RuntimeCsvRepresentation> rcrsForClass, CustomClassSave customClass,
            out List<TypedMemberBase> members, out Dictionary<string, string> untypedMembers)
        {
            members = new List<TypedMemberBase>();
            untypedMembers = new Dictionary<string, string>();

            //List<RuntimeCsvRepresentation> rcrsForClass = new List<RuntimeCsvRepresentation>();

            List<string> membersAlreadyAdded = new List<string>();
            //rcrsForClass.Add(rcr);

            foreach (RuntimeCsvRepresentation rcr in rcrsForClass)
            {
                GetMembersForRcr(members, untypedMembers, membersAlreadyAdded, rcr);
            }

            if (customClass != null)
            {
                foreach (var item in customClass.RequiredProperties)
                {
                    string memberName = item.Member;
                    string type = item.Type;

                    TryAddMember(members, untypedMembers, membersAlreadyAdded, memberName, null, type);
                }

            }

        }

        private static void GetMembersForRcr(List<TypedMemberBase> members, 
            Dictionary<string, string> untypedMembers, List<string> membersAlreadyAdded, RuntimeCsvRepresentation rcr)
        {
            for (int i = 0; i < rcr.Headers.Length; i++)
            {
                CsvHeader header = rcr.Headers[i];

                string memberName = header.Name;
                if (string.IsNullOrWhiteSpace(memberName) == false && memberName.Trim().StartsWith("//") == false)
                {



                    Type type;
                    string classType;
                    
                    GetTypeFromHeader(ref header, ref memberName, out type, out classType);
                    TryAddMember(members, untypedMembers, membersAlreadyAdded, memberName, type, classType);
                }

            }
        }

        private static void TryAddMember(List<TypedMemberBase> members, Dictionary<string, string> untypedMembers, List<string> membersAlreadyAdded, string memberName, Type type, string classType)
        {
            string toAdd = memberName;
            if (!string.IsNullOrEmpty(memberName) && toAdd.Contains("="))
            {
                toAdd = toAdd.Substring(0, toAdd.IndexOf("=")).Trim();
            }

            if (!string.IsNullOrEmpty(toAdd) && !membersAlreadyAdded.Contains(toAdd))
            {

                membersAlreadyAdded.Add(toAdd);



                if (type == null)
                {
                    untypedMembers.Add(memberName, classType);

                }
                else
                {
                    TypedMemberBase typedMemberBase = TypedMemberBase.GetTypedMemberUnequatable(
                        memberName,
                        type);

                    members.Add(typedMemberBase);

                }
            }
        }

        private static CustomClassSave GetCustomClassForCsv(string csvName)
        {
            foreach (CustomClassSave ccs in ObjectFinder.Self.GlueProject.CustomClasses)
            {
                if (ccs.CsvFilesUsingThis.Contains(csvName))
                {
                    return ccs;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header">We ref this simply for performance reasons.</param>
        /// <param name="memberName"></param>
        /// <param name="type"></param>
        /// <param name="classType"></param>
        private static void GetTypeFromHeader(ref CsvHeader header, ref string memberName, out Type type, out string classType)
        {
            classType = "";

            if (memberName.Contains("("))
            {

                // The user is defining the type for this property
                classType = CsvHeader.GetClassNameFromHeader(header.Name);

                bool shouldBeNewed = false;

                if (classType.StartsWith("List<"))
                {
                    classType = "System.Collections.Generic." + classType;
                    shouldBeNewed = true;
                }

                if (string.IsNullOrEmpty(classType))
                {
                    // We can get here if the class is (required)
                    type = typeof(string);
                }
                else
                {
                    if (classType.Contains("<"))
                    {
                        ParsedType parsedType = new ParsedType(classType);
                        type = TypeManager.GetTypeFromParsedType(parsedType);
                    }
                    else
                    {
                        type = TypeManager.GetTypeFromString(classType);
                    }
                }

                memberName = StringFunctions.RemoveWhitespace(memberName);

                memberName = memberName.Substring(0, memberName.IndexOfAny(new char[] { '(' }));

                if (shouldBeNewed)
                {
                    // The user probably wants these new'ed:
                    memberName += " = new " + classType + "()";
                }
            }
            else
            {
                memberName = StringFunctions.RemoveWhitespace(memberName);
                type = typeof(string);
            }
        }

        public static string GetDuplicateMessageIfDuplicatesFound(RuntimeCsvRepresentation rcr, bool createsDictionary, string fileName)
        {
            string duplicateHeader = null;
            string duplicateRequiredField = null;
            duplicateHeader = rcr.GetFirstDuplicateHeader;

            // Check for duplicates ignoring the type

            if (createsDictionary)
            {
                duplicateRequiredField = rcr.FirstDuplicateRequiredField;
            }

            if (!string.IsNullOrEmpty(duplicateHeader))
            {
                return "The CSV file the following duplicate header.\n\n" +
                    duplicateHeader;
            }
            else if (createsDictionary && !string.IsNullOrEmpty(duplicateRequiredField))
            {
                return "The CSV file has a duplicate required field:" + duplicateRequiredField;
            }

            // WE also have to check for duplicates with different types but the same name
            List<string> namesFound = new List<string>();
            foreach (var header in rcr.Headers)
            {
                string name = CsvHeader.GetNameWithoutParentheses(header.OriginalText);
                if (!string.IsNullOrEmpty(name))
                {
                    if (namesFound.Contains(name))
                    {
                        return "The CSV file has a duplicate header: " + name;
                    }
                    else
                    {
                        namesFound.Add(name);
                    }
                }
            }


            return null;
        }

        public static string GetEntireGenericTypeForCsvFile(ReferencedFileSave referencedFileSave)
        {
            string genericType = referencedFileSave.GetTypeForCsvFile();

            if (referencedFileSave.CreatesDictionary)
            {
                string keyType;
                string valueType;

                GetDictionaryTypes(referencedFileSave, out keyType, out valueType);
                return "System.Collections.Generic.Dictionary<" + keyType + ", " + valueType + ">";
            }
            else
            {
                return "System.Collections.Generic.List<" + genericType + ">";

            }

        }


        internal static void GenerateAllCustomClasses(GlueProjectSave glueProject)
        {
            foreach (var customClass in glueProject.CustomClasses)
            {
                GenerateCustomClass(customClass);


            }
        }

        private static void GenerateCustomClass(CustomClassSave customClass)
        {
            if (customClass.GenerateCode)
            {
                string fileName = "";


                ICodeBlock codeBlock = new CodeDocument();
                List<TypedMemberBase> members;
                Dictionary<string, string> untypedMembers;

                ReferencedFileSave rfs = null;
                if (customClass.CsvFilesUsingThis.Count != 0)
                {
                    // let's find the first RCR that actually exists on disk, in case some RCRs were removed:
                    foreach(var csvName in customClass.CsvFilesUsingThis)
                    {
                        rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(csvName);
                        if(rfs != null)
                        {
                            break;
                        }
                    }
                }

                if (rfs != null)
                {
                    // let's just use the existing code flow, even though it's less efficient:
                    GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                }
                else
                {
                    fileName = GetClassInfo(fileName, null, customClass, out members, out untypedMembers);

                    bool succeeded = GenerateClassFromMembers(rfs, true, customClass.Name, members, untypedMembers);
                }
            }
            //return fileName;
        }
    }
}
