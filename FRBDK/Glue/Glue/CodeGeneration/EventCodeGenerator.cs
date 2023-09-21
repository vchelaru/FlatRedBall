using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Performance;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class EventCodeGenerator : ElementComponentCodeGenerator
    {

        #region ElementComponentCodeGenerator methods

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                bool isTunneling = ers.GetIsTunneling();
                bool isExposing = ers.GetIsExposing();

                bool shouldCreateMember =
                    isExposing || isTunneling;

                if (!shouldCreateMember)
                {
                    shouldCreateMember = !string.IsNullOrEmpty(ers.DelegateType);
                }

                if (shouldCreateMember)
                {
                    string delegateType = ers.GetEffectiveDelegateType(element);

                    codeBlock.Line("public event " + delegateType + " " + ers.EventName + ";");

                }
            }

            return codeBlock;
        }


        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                // Turns out this needs to be in post initialize
                // otherwise derived types will try to += events on
                // objects created by their base before the objects have
                // been instantiated.
                // GenerateInitializeForEvent(codeBlock, element, ers);
                
            }

            return codeBlock;
        }

        public static void GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                GenerateInitializeForEvent(codeBlock, element, ers);
            }

            if (element.Events.FirstOrDefault(item => item.EventName == "InitializeEvent") != null)
            {
                codeBlock.Line("this.InitializeEvent(this, null);");
            }

        }

        private static void GenerateInitializeForEvent(ICodeBlock codeBlock, IElement element, EventResponseSave ers)
        {
            //We always want this to happen, even if it's 
            // emtpy
            //if (!string.IsNullOrEmpty(ers.Contents))
            //{
            bool hasIfStatementForNos;
            string leftSide;
            GetEventGenerationInfo(element, ers, out hasIfStatementForNos, out leftSide);

            if (!string.IsNullOrEmpty(leftSide))
            {
                if (hasIfStatementForNos)
                {
                    NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, element.GetNamedObject(ers.SourceObject));
                }
                var rightSide = "On" + ers.EventName;

                var isPooled = element is EntitySave entitySave && entitySave.PooledByFactory;
                if (isPooled)
                {
                    codeBlock.Line("// This entity is pooled and has events. This is a bad mix! We have to do extra checks to prevent duplicate assignments which is slow. This may defeat the purpose of pooling");
                    codeBlock.If($"{leftSide}?.GetInvocationList().Any(item => item.Method.Name == nameof({rightSide})) != true")
                        .Line(leftSide + " += " + rightSide + ";");
                }
                else
                {
                    codeBlock.Line(leftSide + " += " + rightSide + ";");

                }
                if (!string.IsNullOrEmpty(ers.SourceObject) && !string.IsNullOrEmpty(ers.SourceObjectEvent))
                {
                    var innerLeft = ers.SourceObject + "." + ers.SourceObjectEvent;
                    var innerRight = "On" + ers.EventName + "Tunnel";
                    if (isPooled)
                    {
                        codeBlock.Line("// This entity is pooled and has events. This is a bad mix! We have to do extra checks to prevent duplicate assignments which is slow. This may defeat the purpose of pooling");
                        codeBlock.If($"{innerLeft}?.GetInvocationList().Any(item => item.Method.Name == nameof({innerRight})) != true")
                                .Line(innerLeft + " += " + innerRight + ";");

                    }
                    else
                    {
                        codeBlock.Line(innerLeft + " += " + innerRight + ";");
                    }
                    if (hasIfStatementForNos)
                    {
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, element.GetNamedObject(ers.SourceObject));
                    }
                }
            }

        }

        public override void GenerateRemoveFromManagers(ICodeBlock codeBlock, IElement element)
        {
            base.GenerateRemoveFromManagers(codeBlock, element);
        }

        private static void GetEventGenerationInfo(IElement element, EventResponseSave ers, out bool hasIfStatementForNos, out string leftSide)
        {
            hasIfStatementForNos = false;
            leftSide = null;
            if (!string.IsNullOrEmpty(ers.SourceVariable))
            {
                // This is tied to a variable, so the name comes from the variable event rather than the
                // event name itself
                string eventName = ers.BeforeOrAfter.ToString() + ers.SourceVariable + "Set";

                leftSide = "this." + eventName;
            }

            else if (string.IsNullOrEmpty(ers.SourceObject) || ers.SourceObject == "<NONE>")
            {

                EventSave eventSave = ers.GetEventSave();
                if (eventSave == null || string.IsNullOrEmpty(eventSave.ExternalEvent))
                {
                    leftSide = "this." + ers.EventName;
                }
                else
                {
                    leftSide = eventSave.ExternalEvent;
                }
            }
            else if (!string.IsNullOrEmpty(ers.SourceObjectEvent))
            {
                // Only append this if the source NOS is fully-defined.  If not, we don't want to generate compile errors.
                NamedObjectSave sourceNos = element.GetNamedObject(ers.SourceObject);

                if (sourceNos != null && sourceNos.IsFullyDefined)
                {
                    leftSide = ers.SourceObject + "." + ers.SourceObjectEvent;
                    hasIfStatementForNos = true;
                }
            }
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                if ((ers.GetEventSave() != null && !string.IsNullOrEmpty(ers.GetEventSave().ConditionCode)))
                {
                    codeBlock = codeBlock.If(ers.GetEventSave().ConditionCode + " && " + ers.EventName + " != null");
                    codeBlock.Line(ers.EventName + "();");
                    codeBlock = codeBlock.End();
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                EventSave eventSave = ers.GetEventSave();
                
                // External events don't belong to this object - they will probably
                // belong to static/singleton objects, and we don't want to accumulate
                // events.  Thank goodness for gen code taking care of this for us.
                if (eventSave != null && !string.IsNullOrEmpty(eventSave.ExternalEvent))
                {
                    codeBlock.Line(eventSave.ExternalEvent + " -= On" + ers.EventName + ";");
                }
                else
                {
                    bool hasIfStatementForNos;
                    string leftSide;
                    GetEventGenerationInfo(element, ers, out hasIfStatementForNos, out leftSide);

                    //if(leftSide != null)
                    //{
                    //    codeBlock.Line(leftSide + " -= On" + ers.EventName + ";");
                    //}
                    if (!string.IsNullOrEmpty(leftSide))
                    {
                        if (hasIfStatementForNos)
                        {
                            NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, element.GetNamedObject(ers.SourceObject));
                        }

                        if (!string.IsNullOrEmpty(ers.SourceObject) && !string.IsNullOrEmpty(ers.SourceObjectEvent))
                        {
                            codeBlock.Line(ers.SourceObject + "." + ers.SourceObjectEvent + " -= On" + ers.EventName + "Tunnel;");
                            if (hasIfStatementForNos)
                            {
                                NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, element.GetNamedObject(ers.SourceObject));
                            }
                        }
                        else if(IsDefinedByBase(ers, element as GlueElement))
                        {
                            var rightSide = "On" + ers.EventName;
                            codeBlock.Line($"{leftSide} -= On{ers.EventName};");

                        }
                        else
                        {
                            // only null it out if it doesn't have a source object. Otherwise, we can't assign null on an event that isn't owned by this class
                            codeBlock.Line(leftSide + " = null;");
                        }
                    }

                }


            }
            return codeBlock;
        }

        private bool IsDefinedByBase(EventResponseSave ers, GlueElement element)
        {
            var baseElement = ObjectFinder.Self.GetBaseElement(element);

            return baseElement?.Events.Any(item => item.EventName == ers.EventName) == true;
        }


        #endregion

        internal static void GenerateEventsForVariable(ICodeBlock codeBlock, string variableName, string variableType, bool isEventNew = false)
        {
            string prefix = "public ";
            if (isEventNew)
            {
                prefix += "new ";
            }
            codeBlock.Line(prefix + $"event Action<{variableType}> Before" + variableName + "Set;");
            codeBlock.Line(prefix + "event System.EventHandler After" + variableName + "Set;");

        }

        internal static void GenerateEventRaisingCode(ICodeBlock codeBlock, BeforeOrAfter beforeOrAfter, string variableName, IElement saveObject)
        {
            PerformancePluginCodeGenerator.CodeBlock = codeBlock;
            PerformancePluginCodeGenerator.SaveObject = saveObject;

            string beforeOrAfterAsString = "Before";
            string parameters = "value";
            if (beforeOrAfter == BeforeOrAfter.After)
            {
                beforeOrAfterAsString = "After";
                parameters = "this, null";
            }
            PerformancePluginCodeGenerator.GenerateStart(beforeOrAfterAsString + " set " + variableName);

            codeBlock.If(beforeOrAfterAsString + variableName + "Set != null")
                .Line(beforeOrAfterAsString + variableName + $"Set({parameters});");

            PerformancePluginCodeGenerator.GenerateEnd();
        }

        public static void AddStubsForCustomEvents(GlueElement element)
        {
            // EARLY OUT///////
            if (element.Events.Count == 0)
            {
                return;
            }
            ////////EARLY OUT/////////
            
            var file = element.Events[0].GetCustomEventFullFileName();

            if (File.Exists(file))
            {
                ParsedClass parsedClass = GetParsedClassFrom(file);

                if (parsedClass == null)
                {
                    // this file is empty
                    foreach (EventResponseSave ers in element.Events)
                    {
                        try
                        {
                            InjectTextForEventAndSaveCustomFile(element, ers, "");
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Failed to generate custom code stubs for event " + ers.EventName + "\n\n" + e);

                        }
                    }
                }
                else
                {
                    foreach (EventResponseSave ers in element.Events)
                    {
                        ParsedMethod parsedMethod = parsedClass.GetMethod("On" + ers.EventName);

                        if (parsedMethod == null)
                        {
                            // This method doesn't exist.  This will cause an error
                            // because generated code expects this to always exist. Let's
                            // make an empty stub

                            InjectTextForEventAndSaveCustomFile(element, ers, "");
                        }
                    }
                }
            }
            else
            {
                // The file doesn't exist, we gotta generate it
            }
        }

        static ParsedClass GetParsedClassFrom(string fileName)
        {
            ParsedFile parsedFile = new ParsedFile(fileName, false, false);

            if (parsedFile.Namespaces.Count != 0)
            {
                ParsedNamespace parsedNamespace = parsedFile.Namespaces[0];

                if (parsedNamespace.Classes.Count != 0)
                {
                    return parsedNamespace.Classes[0];
                }
            }
            return null;
        }


        public static void GenerateEventGeneratedFile(IElement element)
        {
            //string fileName = EventManager.GetEventFileNameForElement(element);
            //string fullCustomFileName = ProjectManager.ProjectBase.Directory + fileName;

            var project = ProjectManager.ProjectBase;

            ////////////////// Early Out //////////////////////
            if(project == null)
            {
                return;
            }
            ///////////////// End Early Out////////////////////

            string projectDirectory = project.Directory;


            string fullGeneratedFileName = projectDirectory + EventManager.GetGeneratedEventFileNameForElement(element);
            FilePath generatedFilePath = fullGeneratedFileName;
            ////////////////EARLY OUT///////////////
            if (element.Events.Count == 0)
            {
                // The file may exist.  If it does, we want to make sure it's empty:
                if (File.Exists(fullGeneratedFileName))
                {
                    FileWatchManager.IgnoreNextChangeOnFile(fullGeneratedFileName);
                    FileManager.SaveText("", fullGeneratedFileName);
                }


                return;
            }
            ///////////////END EARLY OUT///////////

            if (!File.Exists(fullGeneratedFileName))
            {

                CodeWriter.AddEventGeneratedCodeFileForElement(element);
            }
            else
            {
                // Make sure the file is part of the project
                GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(ProjectManager.ProjectBase, generatedFilePath, false, false);
            }

            ICodeBlock codeBlock = GenerateEventGeneratedCodeFile(element as GlueElement);

            // Let's try this a few times:
            int numberOfFailures = 0;
            bool succeeded = false;
            FileWatchManager.IgnoreNextChangeOnFile(fullGeneratedFileName);

            while (numberOfFailures < 3)
            {
                try
                {
                    FileManager.SaveText(codeBlock.ToString(), fullGeneratedFileName);
                    succeeded = true;
                    break;
                }
                catch
                {
                    numberOfFailures++;
                    System.Threading.Thread.Sleep(30);
                }
            }

            if (!succeeded)
            {
                GlueGui.ShowMessageBox("Could not save " + fullGeneratedFileName);
            }
        }

        private static ICodeBlock GenerateEventGeneratedCodeFile(GlueElement element)
        {
            ICodeBlock codeBlock = new CodeDocument();
            var currentBlock = codeBlock;

            string elementNamespace = ProjectManager.ProjectNamespace;


            elementNamespace += "." + element.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, element.Name.Length - (element.ClassName.Length + 1));

            AddUsingStatementsToBlock(currentBlock);

            currentBlock = currentBlock
                .Namespace(elementNamespace);

            currentBlock = currentBlock
                .Class("public partial", element.ClassName, "");

            foreach (EventResponseSave ers in element.Events)
            {
                currentBlock = FillWithGeneratedEventCode(currentBlock, ers, element);
            }
            return codeBlock;
        }

        public static ICodeBlock FillWithGeneratedEventCode(ICodeBlock currentBlock, EventResponseSave ers, GlueElement element)
        {
            EventSave eventSave = ers.GetEventSave();

            string args = ers.GetArgsForMethod(element);

            if (!string.IsNullOrEmpty(ers.SourceObject) && !string.IsNullOrEmpty(ers.SourceObjectEvent))
            {
                currentBlock = currentBlock
                    .Function("void", "On" + ers.EventName + "Tunnel", args);

                string reducedArgs = StripTypesFromArguments(args);

                currentBlock.If("this." + ers.EventName + " != null")
                    .Line(ers.EventName + "(" + reducedArgs + ");")
                    .End();

                foreach(var generator in CodeWriter.CodeGenerators)
                {
                    generator.GenerateEvent(currentBlock, element as GlueElement, ers);
                }
                currentBlock = currentBlock.End();
            }


            return currentBlock;
        }

        public static ICodeBlock FillWithCustomEventCode(ICodeBlock currentBlock, EventResponseSave ers, string contents, GlueElement element)
        {
            string args = ers.GetArgsForMethod(element);

            // We used to not 
            // generate the empty
            // shell of an event if
            // the contents were empty.
            // However now we want to for
            // two reasons:
            // 1:  A user may want to add an
            // event in Glue, but then mdoify
            // the event in Visual Studio.  The
            // user shouldn't be forced into adding
            // some content in Glue first to wdit the
            // event in Visual Studio.
            // 2:  A designer may decide to remove the 
            // contents of a method.  If this happens then
            // code that the designer doesn't work with shouldn't
            // break (IE, if the code calls the OnXXXX method).
            //if (!string.IsNullOrEmpty(contents))
            {

                // Need to modify the event CSV to include the arguments for this event

                currentBlock = currentBlock
                    .Function("void", "On" + ers.EventName, args);

                currentBlock.TabCharacter = "";

                int tabCount = currentBlock.TabCount;
                currentBlock.TabCount = 0;

                currentBlock
                        .Line(contents);

                currentBlock = currentBlock.End();
            }
            return currentBlock;
        }

        public static void AddUsingStatementsToBlock(ICodeBlock currentBlock)
        {
            List<string> usings = new List<string>();
            usings.Add("System");
            usings.Add("FlatRedBall");
            usings.Add("FlatRedBall.Input");
            usings.Add("FlatRedBall.Instructions");
            usings.Add("Microsoft.Xna.Framework.Graphics");
            usings.Add("System.Collections.Specialized");
            usings.Add("FlatRedBall.Audio");
            usings.Add("FlatRedBall.Screens");
            usings.Add("FlatRedBall.Instructions");

            foreach (EntitySave entitySave in ObjectFinder.Self.GlueProject.Entities)
            {
                var namespaceString = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(entitySave);
                usings.Add(namespaceString);
            }

            foreach (ScreenSave screenSave in ObjectFinder.Self.GlueProject.Screens)
            {
                var namespaceString = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(screenSave);

                usings.Add(namespaceString);
            }

            StringFunctions.RemoveDuplicates(usings);

            foreach (string s in usings)
            {
                currentBlock.Line("using " + s + ";");
            }
        }

        private static string StripTypesFromArguments(string argumentsWithTypes)
        {
            /////////////////EARLY OUT//////////////////////////
            if (string.IsNullOrEmpty(argumentsWithTypes))
            {
                return argumentsWithTypes;
            }
            ///////////////END EARLY OUT///////////////////////

            int index = 0;

            string reducedArguments = argumentsWithTypes;

            while (true)
            {

                int indexOfSpace = reducedArguments.IndexOf(' ');

                if (indexOfSpace == -1)
                {
                    break;
                }
                int countToRemove = indexOfSpace - index;

                reducedArguments = reducedArguments.Remove(index, indexOfSpace - index).Trim() ;

                int indexOfComma = reducedArguments.IndexOf(',', index);

                if (indexOfComma == -1)
                {
                    break;
                }

                // Why do we remove the comma?
                // I think we want to remove the spaces after the comma instead
                //reducedArguments = reducedArguments.Remove(indexOfComma, 1);
                int afterComma = indexOfComma + 1;
                while (afterComma < reducedArguments.Length && reducedArguments[afterComma] == ' ')
                {
                    reducedArguments = reducedArguments.Remove(afterComma, 1);
                }

                index = afterComma;
            }

            return reducedArguments;
        }


        #region Code editing methods

        /// <summary>
        /// Injects the insideOfMethod into an event for the argument 
        /// </summary>
        /// <param name="currentElement">The IElement containing the EventResponseSave.</param>
        /// <param name="eventResponseSave">The EventResponseSave which should have its contents set or replaced.</param>
        /// <param name="insideOfMethod">The inside of the methods to assign.</param>
        /// <returns>The full file name which contains the method contents.</returns>
        public static string InjectTextForEventAndSaveCustomFile(GlueElement currentElement, EventResponseSave eventResponseSave, string insideOfMethod)
        {
            // In case the user passes null we don't want to have null reference exceptions:
            if (insideOfMethod == null)
            {
                insideOfMethod = "";
            }

            ParsedMethod parsedMethod =
                eventResponseSave.GetParsedMethodFromAssociatedFile();


            var fullFileName = eventResponseSave.GetCustomEventFullFileName();

            bool forceRegenerate = false;

            string fileContents = null;

            if (File.Exists(fullFileName))
            {
                fileContents = FileManager.FromFileText(fullFileName);
                forceRegenerate = fileContents.Contains("public partial class") == false && fileContents.Contains("{") == false;

                if (forceRegenerate)
                {
                    GlueGui.ShowMessageBox("Forcing a regneration of " + fullFileName + " because it appears to be empty.");
                }
            }

            CreateEmptyCodeIfNecessary(currentElement, fullFileName, forceRegenerate);

            fileContents = FileManager.FromFileText(fullFileName);

            int indexToAddAt = 0;

            bool hasBracketNamespace = true;

            if (parsedMethod != null)
            {
                int startIndex;
                int endIndex;
                GetStartAndEndIndexForMethod(parsedMethod, fileContents, out startIndex, out endIndex);
                // We want to include the \r\n at the end, so add 2
                endIndex += 2;
                string whatToRemove = fileContents.Substring(startIndex, endIndex - startIndex);

                fileContents = fileContents.Replace(whatToRemove, null);

                indexToAddAt = startIndex;
                // remove the method to re-add it
            }
            else
            {
                indexToAddAt = EventManager.GetLastLocationInClass(fileContents, startOfLine:true, out hasBracketNamespace);
            }

            var tabCount = hasBracketNamespace ? 2 : 1;

            ICodeBlock codeBlock = new CodeDocument(tabCount);
            codeBlock.TabCharacter = "    ";

            insideOfMethod = "" + insideOfMethod.Replace("\r\n", "\r\n            ");
            codeBlock = EventCodeGenerator.FillWithCustomEventCode(codeBlock, eventResponseSave, insideOfMethod, currentElement);

            string methodContents = codeBlock.ToString();


            fileContents = fileContents.Insert(indexToAddAt, codeBlock.ToString());

            eventResponseSave.Contents = null;
            try
            {
                FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile(fullFileName);
                FileManager.SaveText(fileContents, fullFileName);
            }
            catch (Exception e)
            {
                PluginManager.ReceiveError("Could not save file to " + fullFileName);
            }
            return fullFileName;
        }


        public static void CreateEmptyCodeIfNecessary(GlueElement currentElement, string fullFileName, bool forceRegenerateContents)
        {
            bool doesFileExist = File.Exists(fullFileName);

            if (!doesFileExist)
            {
                PluginManager.ReceiveOutput("Forcing a regeneration of " + fullFileName + " because Glue can't find it anywhere.");

                // There is no shared code file for this event, so we need to make one
                ProjectManager.CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile(fullFileName, true);
            }

            if (!doesFileExist || forceRegenerateContents)
            {
                string namespaceString = GlueCommands.Self.GenerateCodeCommands.GetNamespaceForElement(currentElement);


                ICodeBlock templateCodeBlock = new CodeDocument();

                EventCodeGenerator.AddUsingStatementsToBlock(templateCodeBlock);

                ICodeBlock namespaceCB = templateCodeBlock.Namespace(namespaceString);

                ICodeBlock classCB = namespaceCB.Class("public partial", currentElement.ClassName, null);

                classCB
                    ._()
                    .End()
                .End();

                string templateToSave = templateCodeBlock.ToString();
                FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile(fullFileName);
                FileManager.SaveText(templateToSave, fullFileName);
            }

            // Add if it isn't part of the project
            const bool saveFile = false; // don't save it - we just want to make sure it's part of the project
            ProjectManager.CodeProjectHelper.CreateAndAddPartialGeneratedCodeFile(fullFileName, saveFile);

        }


        /// <summary>
        /// Returns the start and end index in the argument fileContents of the entire method including the header of the method
        /// and the opening/closing brackets.
        /// </summary>
        /// <param name="parsedMethod">The parsed method.</param>
        /// <param name="fileContents">The contents of the entire file.</param>
        /// <param name="startIndex">The found startIndex.</param>
        /// <param name="endIndex">The found endIndex which includes the closing bracket.</param>
        public static void GetStartAndEndIndexForMethod(ParsedMethod parsedMethod, string fileContents, out int startIndex, out int endIndex)
        {
            string name = parsedMethod.Name;


            GetStartAndEndIndexForMethod(fileContents, name, out startIndex, out endIndex);
        }

        public static void GetStartAndEndIndexForMethod(string fileContents, string name, out int startIndex, out int endIndex)
        {
            startIndex = fileContents.IndexOf("void " + name);
            int endlineIndex = fileContents.LastIndexOf('\r', startIndex);
            int amountToAdd = 2;
            if (endlineIndex == -1)
            {
                endlineIndex = fileContents.LastIndexOf('\n', startIndex);
                amountToAdd = 1;
            }

            startIndex = endlineIndex + amountToAdd; ; // to get after the \r\n or \n

            endIndex = StringFunctions.GetClosingCharacter(fileContents, fileContents.IndexOf('{', startIndex) + 1, '{', '}');
            // to include the last } we'll add 1 to endIndex
            endIndex++;
        }

        #endregion


        internal static void GenerateAddToManagersBottomUp(ICodeBlock codeBlock, IElement element)
        {
            foreach (EventResponseSave ers in element.Events)
            {
                EventSave eventSave = ers.GetEventSave();


                // Right now I'm hardcoding this event, but eventually we may want to pull this value from the data
                // if it's something we want to support on other events
                bool raiseInAddToManagers = eventSave != null && eventSave.EventName == "ResolutionOrOrientationChanged";

                if (raiseInAddToManagers)
                {
                    codeBlock.Line("OnResolutionOrOrientationChanged(this, null);");

                    codeBlock = codeBlock.End();

                    


                }
            }
        }

        public static bool ShouldGenerateEventsForVariable(CustomVariable customVariable, IElement container)
        {

            // Victor Chelaru
            // August 17, 2013
            // I debated whether
            // static variables should
            // create events or not.  They
            // technically could, but that means
            // that the custom code events would have
            // to be modified when switching between static
            // and instance.  This could have some impacts on
            // GlueView code parsing, and it currently seems to
            // be something that is either rare or something which
            // has some pretty simple workarounds.  Therefore, I'm going
            // to say that only instance variables can create events.
            bool shouldGenerate = customVariable.CreatesEvent && !customVariable.IsShared;

            if(shouldGenerate && customVariable.DefinedByBase)
            {
                var baseContainers = ObjectFinder.Self.GetAllBaseElementsRecursively(container as GlueElement);

                foreach(var baseContainer in baseContainers)
                {
                    if(baseContainer.CustomVariables.Any(item=>item.DefinedByBase == false && item.Name == customVariable.Name && item.CreatesEvent))
                    {
                        shouldGenerate = false;
                        break;
                    }
                }
            }

            return shouldGenerate;
        }

        internal static void TryGenerateEventsForVariable(ICodeBlock codeBlock, CustomVariable customVariable, IElement container)
        {
            var shouldGenerate = ShouldGenerateEventsForVariable(customVariable, container);
            // Currently we don't support events on static variables
            if (shouldGenerate)
            {
                var variableType = customVariable.Type;
                if(customVariable.GetIsCsv())
                {
                    var referencedFile = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(variableType);

                    if(referencedFile != null)
                    {
                        variableType = referencedFile.GetTypeForCsvFile();
                    }
                }
                EventCodeGenerator.GenerateEventsForVariable(codeBlock, customVariable.Name, variableType);
            }
        }
    }
}