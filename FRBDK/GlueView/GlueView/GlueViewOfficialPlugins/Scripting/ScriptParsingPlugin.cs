using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using GlueView.Plugin;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using GlueView.Facades;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.IO;
using System.Reflection;
using FlatRedBall;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using System.Diagnostics;
using GlueView.Scripting;
using ICSharpCode.NRefactory.CSharp;
using FlatRedBall.Utilities;

namespace GlueViewOfficialPlugins.Scripting
{
    class ScriptAndContext
    {
        public string Script;
        public CodeContext Context;
    }

    [Export(typeof(GlueViewPlugin))]
    public class ScriptParsingPlugin : GlueViewPlugin
    {
        #region Enums



        #endregion


        #region Fields


        static ScriptParsingPlugin mSelf;

        ExpressionParser mExpressionParser;

        bool mIsActive = false;
        List<FieldInfo> mAllFields = new List<FieldInfo>();
        List<PropertyInfo> mAllProperties = new List<PropertyInfo>();

        ScriptingControl mControl;
        char[] separators = new char[] { '\r' };

        Dictionary<string, string[]> mCachedMethodLines = new Dictionary<string, string[]>();


        StringBuilder mParserLog = new StringBuilder();


        List<ScriptAndContext> mStringsToApply = new List<ScriptAndContext>();

        #endregion


        #region Properties

        public int LastErrorLine
        {
            get;
            private set;
        }

        public override string FriendlyName
        {
            get { return "Script Parser Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        List<Dictionary<string, object>> mCallStackVariables = new List<Dictionary<string, object>>();

        public static ScriptParsingPlugin Self
        {
            get
            {
#if UNIT_TESTS
                if (mSelf == null)
                {
                    mSelf = new ScriptParsingPlugin();
                }
#endif
                return mSelf;
            }
        }

        Dictionary<string, object> TopOfVariableStack
        {
            get
            {
                return mCallStackVariables[mCallStackVariables.Count - 1];
            }
        }
        #endregion




        public override void StartUp()
        {
            mSelf = this;

            mIsActive = true;
            this.BeforeVariableSet += new EventHandler<FlatRedBall.Glue.VariableSetArgs>(OnBeforeVariableSet);
            this.AfterVariableSet += new EventHandler<FlatRedBall.Glue.VariableSetArgs>(HandleAfterVariableSet);
            this.ElementLoaded += new EventHandler(OnElementLoaded);
            this.ResolutionChange += new Action(HandleResolutionChange);
            this.Update += new EventHandler(HandleUpdate);

            StartUpForTests();
            
#if !UNIT_TESTS
            mControl = new ScriptingControl(this);
            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Script Parsing", 80, mControl, this);

            mControl.ButtonClick += new EventHandler(OnDebugButtonClick);
#endif
        }

        void HandleUpdate(object sender, EventArgs args)
        {
            lock (mStringsToApply)
            {
#if !UNIT_TESTS
                if (mControl.Enabled)
#endif
                {
                    foreach (var scriptAndContext in mStringsToApply)
                    {
                        CodeContext codeContext = scriptAndContext.Context;
                        if (codeContext == null)
                        {
                            codeContext = new CodeContext(GlueViewState.Self.CurrentElementRuntime);
                        }

                        ApplyLinesInternal(scriptAndContext.Script.Split(new char[]{'\n'}), 0, mStringsToApply.Count,
                            GlueViewState.Self.CurrentElement,
                            codeContext);
                    }
#if !UNIT_TESTS
                    mControl.FrameBasedUpdate();
#endif
                }
                mStringsToApply.Clear();
            }
        }

        public void StartUpForTests()
        {
            SaveFieldsAndPropertiesForType(typeof(PositionedObject));
            SaveFieldsAndPropertiesForType(typeof(Sprite));
            SaveFieldsAndPropertiesForType(typeof(Text));
            SaveFieldsAndPropertiesForType(typeof(Circle));



            mExpressionParser = new ExpressionParser();
            mExpressionParser.LogStringBuilder = mParserLog;

        }

        void OnDebugButtonClick(object sender, EventArgs e)
        {
            string text = this.mParserLog.ToString();

            string fileName =  FileManager.UserApplicationDataForThisApplication + "ScriptParsingLog.txt";

            FileManager.SaveText(text, fileName);

            Process.Start(fileName);

            // Clear out the log now that the user has seen it
            mParserLog.Clear();
        }

        void OnElementLoaded(object sender, EventArgs e)
        {
            lock (mCachedMethodLines)
            {
                mCachedMethodLines.Clear();
            }

            // After this thing is loaded we want to call the resolution change event
            // since this is how generated code behaves.
            HandleResolutionChange();
        }

        private void SaveFieldsAndPropertiesForType(Type type)
        {
            mAllFields.AddRangeUnique(type.GetFields());
            mAllProperties.AddRangeUnique(type.GetProperties());
        }



        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            mIsActive = false;
            return true;
        }


        void OnBeforeVariableSet(object sender, VariableSetArgs e)
        {

        }

        void HandleResolutionChange()
        {
            HandleResolutionChange(GlueViewState.Self.CurrentElementRuntime);   
        }

        void HandleResolutionChange(ElementRuntime elementRuntime)
        {
            if (elementRuntime != null)
            {
                IElement element = elementRuntime.AssociatedIElement;

                if (element != null)
                {
                    EventResponseSave ers = element.GetEvent("ResolutionOrOrientationChanged");

                    if (ers != null)
                    {
                        ApplyEventResponseSave(elementRuntime, ers);
                    }
                }
            }
        }

        void HandleAfterVariableSet(object sender, VariableSetArgs e)
        {
            try
            {
                if ( mControl != null && mControl.Enabled)
                {
                    ElementRuntime elementRuntime = sender as ElementRuntime;
                    // If the user has just selected the element runtime,then it hasn't been set
                    // as the current element yet, so we can't use the GlueViewState facade
                    // IElement element = GlueViewState.Self.CurrentElement;
                    IElement element = elementRuntime.AssociatedIElement;

                    if (element != null)
                    {


                        string variableName = e.VariableName;

                        EventResponseSave ers = element.GetEvent("After" + variableName + "Set");

                        if (ers != null)
                        {
                            mParserLog.AppendLine("Reacting to after " + e.VariableName + " Set in the file :\n\t\t" + EventResponseSave.GetSharedCodeFullFileName(element, FileManager.GetDirectory(GlueViewState.Self.CurrentGlueProjectFile)));

                            ApplyEventResponseSave(elementRuntime, ers);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                int m = 3;
            }
        }

        private void ApplyEventResponseSave(ElementRuntime elementRuntime, EventResponseSave ers)
        {
            IElement element = elementRuntime.AssociatedIElement;
            string projectDirectory = FileManager.GetDirectory(GlueViewState.Self.CurrentGlueProjectFile);
            string[] lines = GetMethodLines(element, ers, projectDirectory);
            string fileName = EventResponseSave.GetSharedCodeFullFileName(element, projectDirectory);

            CodeContext codeContext = new CodeContext(elementRuntime);
            ApplyLinesInternal(lines, 0, lines.Length, element, codeContext, fileName);
        }

        private string[] GetMethodLines(IElement element, EventResponseSave ers, string projectDirectory)
        {
            string[] toReturn = null;
            lock (mCachedMethodLines)
            {
                if (!mCachedMethodLines.ContainsKey(ers.EventName))
                {
                    ParsedMethod parsedMethod =
                        ers.GetParsedMethodFromAssociatedFile(element, projectDirectory);
                    string[] lines = parsedMethod.MethodContents.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    mCachedMethodLines.Add(ers.EventName, lines);
                }

                toReturn = mCachedMethodLines[ers.EventName];
            }
            return toReturn;
        }

        public void ApplyLines(string script, CodeContext codeContext = null)
        {
            lock (mStringsToApply)
            {
                ScriptAndContext toAdd = new ScriptAndContext();
                toAdd.Script = script;
                toAdd.Context = codeContext;

                mStringsToApply.Add(toAdd);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startingIndex"></param>
        /// <param name="count"></param>
        /// <param name="element"></param>
        /// <param name="codeContext"></param>
        /// <param name="fileName"></param>
        /// <param name="addStackVariables"></param>
        /// <param name="removeStackVariables"></param>
        /// <returns>Whether the application succeeded.</returns>
        public bool ApplyLinesInternal(string[] lines, int startingIndex, int count, IElement element, 
            CodeContext codeContext, string fileName = null, bool addStackVariables = true, bool removeStackVariables = true)
        {
            LastErrorLine = -1;
            bool returnValue = true;
            mCallStackVariables.Add(new Dictionary<string, object>());
            for(int i = startingIndex; i < startingIndex + count && i < lines.Length; i++)
            {
                string line = lines[i];

                try
                {
                    int iBefore = i;
                    ApplyLine(line.Trim(), codeContext, lines, fileName, ref i);
                    if (iBefore != i)
                    {
                        // ApplyLine may increment the lines if in a 
                        // conditional code block.  If so we need to subtract
                        // 1 because our for-loop will automatically add one.
                        i--;
                    }


                }
                catch (StateSettingException e)
                {
                    string errorText = null;

                    if (string.IsNullOrEmpty(fileName))
                    {
                        errorText = "Error setting state for line:  ";
                    }
                    else
                    {
                        errorText = "Error setting state for line in the file " + fileName + ":  ";
                    }
                    AddErrorText(errorText + line);
                }
                catch (Exception)
                {
                    // There was a scripting error
                    // Eventually we want to log this
                    AddErrorText("Unknown line:  " + line);
                    returnValue = false;
                    LastErrorLine = i;
                    break;
                }
            }
            mCallStackVariables.Remove(TopOfVariableStack);
            return returnValue;
        }

        static List<string> variables = new List<string>();
        private void ApplyLine(string line, CodeContext codeContext, string[] allLines, string fileName, ref int index)
        {
            bool succeeded = true;

            mParserLog.AppendLine("--- " + line + " ---");
            

            if (string.IsNullOrEmpty(line))
            {
                // do nothing
                // This may be empty
                // because the split function
                // may have returned a line with
                // only a newline character.  If so
                // that becomes an empty line when trim
                // is called on it.  This could be a line
                // that was trimmed which is now empty.
            }
            else if (line.Trim().StartsWith("//"))
            {
                // comment, carry on
            }
            else if (line.Trim().StartsWith("#region") || line.Trim().StartsWith("#endregion"))
            {
                // we can skip region blocks
            }
            else if (ConditionalCodeBlock.GetBlockTypeStartingAt(allLines, index) != BlockType.None)
            {
                ElementRuntime elementRuntime = codeContext.ContainerInstance as ElementRuntime;

                IElement element = null;

                if (elementRuntime != null)
                {
                    element = elementRuntime.AssociatedIElement;
                }

                succeeded = ApplyConditionalBlocks(allLines, element, codeContext, fileName, ref index);

                if (!succeeded)
                {
                    throw new Exception();
                }
            }
            else
            {

                // here we want to identify how many lines make up the statement since
                // it could be something like
                // this.X = 3
                //       + 4;
                int numberOfLinesInStatement = GetNumberOfLinesInRegularStatement(allLines, index);

                string combined = CombineLines(allLines, index, numberOfLinesInStatement);

                string trimmedCombined = combined.Trim();
                if (trimmedCombined.StartsWith("{") && trimmedCombined.EndsWith("}"))
                {
                    combined = trimmedCombined.Substring(1, trimmedCombined.Length - 2);
                }
                CSharpParser parser = new CSharpParser();
                var statements = parser.ParseStatements(combined);



                // I can be incremented by this function by the number of lines.
                // If this value is incremented, then the calling function is responsible
                // for recognizing that in its loop and acting properly.  The calling function
                // assumes an increment of 1 so we won't do anything if there's only 1 line in this
                // statement.
                if (numberOfLinesInStatement != 1)
                {
                    index += numberOfLinesInStatement;
                }

                foreach (var statement in statements)
                {
                    if (statement is ExpressionStatement)
                    {
                        Expression expression = ((ExpressionStatement)statement).Expression;
                        if (expression is InvocationExpression || expression is UnaryOperatorExpression)
                        {
                            mExpressionParser.EvaluateExpression(expression, codeContext);
                        }
                        else if (expression is AssignmentExpression)
                        {
                            succeeded = ApplyAssignmentLine(expression as AssignmentExpression, codeContext);
                            if (!succeeded)
                            {
                                throw new Exception();
                            }
                        }
                        else if (expression is IdentifierExpression || expression is MemberReferenceExpression)
                        {
                            // This is probably an incomplete line, so let's tolerate it and move on...
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else if (statement is VariableDeclarationStatement)
                    {
                        VariableDeclarationStatement vds = statement as VariableDeclarationStatement;

                        foreach (var child in vds.Children)
                        {
                            if (child is VariableInitializer)
                            {
                                VariableInitializer variableInitializer = child as VariableInitializer;
                                ApplyAssignment(variableInitializer, vds, codeContext);
                                //variableInitializer.
                            }
                        }
                        //ApplyAssignment(codeContext, vds.Variables.First().GetText(), vds.
                        //vds.E
                        //ApplyAssignmentLine(statement.ToString(), codeContext);
                    }
                    //if ( is AssignmentExpression || result is MemberReferenceExpression)
                    //{
                    //    ApplyAssignmentLine(combined, codeContext);
                    //}
                    //else if (result is InvocationExpression)
                    //{
                    //    ApplyMethodCall(combined, codeContext);
                    //}
                    else
                    {
                        AddErrorText("Unknown line(s):  " + combined);
                    }
                }
            }

        }

        public int GetNumberOfLinesInRegularStatement(string[] allLines, int index)
        {
            int lineCount = 0;

            int parensDeep = 0;
            int curlyBracketsDeep = 0;

            // This is not an if statement, not a comment, therefore it must end in a semicolon
            while (index < allLines.Length)
            {
                lineCount++;
                string lineAtIndex = allLines[index].Trim();
                parensDeep += allLines[index].CountOf('(');
                parensDeep -= allLines[index].CountOf(')');

                curlyBracketsDeep += allLines[index].CountOf('{');
                curlyBracketsDeep -= allLines[index].CountOf('}');

                index++;
                if (curlyBracketsDeep == 0 && parensDeep == 0 &&  lineAtIndex.Trim().EndsWith(";"))
                {
                    break;
                }

            }

            return lineCount;
        }

        StringBuilder mCombinedStringBuilder = new StringBuilder();
        private string CombineLines(string[] allLines, int index, int numberOfLinesInStatement)
        {
            mCombinedStringBuilder.Clear();
            bool hasCombined = false;
            for (int i = index; i < index + numberOfLinesInStatement; i++)
            {
                
                if(hasCombined)
                {
                    mCombinedStringBuilder.Append(" ");
                }

                mCombinedStringBuilder.Append(allLines[i].Trim());
                
                hasCombined = true;
            }

            return mCombinedStringBuilder.ToString();
        }

        private bool ApplyConditionalBlocks(string[] allLines, IElement element, CodeContext codeContext, string fileName, ref int index)
        {
            bool succeeded = true;

            BlockType lastBlockType = BlockType.None;

            BlockType nextBlockType = ConditionalCodeBlock.GetBlockTypeStartingAt(allLines, index);

            List<ConditionalCodeBlock> conditionalBlocks = new List<ConditionalCodeBlock>();

            while (nextBlockType.LinksFromPreviousType(lastBlockType))
            {
                ConditionalCodeBlock ccb = ConditionalCodeBlock.GetConditionalBlockFrom(allLines, index);
                conditionalBlocks.Add(ccb);
                lastBlockType = nextBlockType;

                index += ccb.LineCountIncludingConditionLine;
                nextBlockType = ConditionalCodeBlock.GetBlockTypeStartingAt(allLines, index);

            }


            // Only one of these blocks can trigger
            foreach (ConditionalCodeBlock ccb in conditionalBlocks)
            {
                // This code context is for the contents of the condition.
                // For example, the i variable in a for-loop will have scope
                // limited to the application of the for-loop.
                codeContext.AddVariableStack();

                bool shouldExecute = BranchingParser.Self.DetermineIfShouldExecute(
                    this, codeContext, ccb, mExpressionParser, true);


                while (shouldExecute)
                {
                    codeContext.AddVariableStack();

                    int startBlock = ccb.FirstLineOfBlockIndex;
                    int blockLength = ccb.BlockLength;
                    if (ccb.IsBlockWrappedInBrackets)
                    {
                        startBlock++;
                        blockLength -= 2;
                    }
                    succeeded = ApplyLinesInternal(allLines, startBlock, blockLength, element, codeContext, fileName);

                    
                    shouldExecute = false;
                    if (succeeded)
                    {
                        if (ccb.BlockType == BlockType.For)
                        {
                            BranchingParser.Self.IncrementFor(codeContext, ccb, mExpressionParser);

                            shouldExecute = BranchingParser.Self.DetermineIfShouldExecute(
                                this, codeContext, ccb, mExpressionParser, false);
                        }
                        else if (ccb.BlockType == BlockType.While)
                        {
                            shouldExecute = BranchingParser.Self.DetermineIfShouldExecute(
                                this, codeContext, ccb, mExpressionParser, false);
                        }
                    }

                    codeContext.RemoveVariableStack();
                }
                codeContext.RemoveVariableStack();

            }

            return succeeded;
        }


        private bool LineIsIfStatement(string line)
        {
            return line.StartsWith("if") && StringFunctions.GetWordAfter("if", line).StartsWith("(");
        }

        private void ApplyMethodCall(string line, CodeContext codeContext)
        {
            
            mExpressionParser.EvaluateExpression(line, codeContext);
            mParserLog.AppendLine("Called method from line: " + line);

        }

        private bool ApplyAssignmentLine(AssignmentExpression expression, CodeContext codeContext)
        {
            bool succeeded = false;

            string leftOfEquals = expression.Left.GetText();
            string rightOfEquals = expression.Right.GetText();
            
            // Eventually we'll want to get rid of this, but a lot of the code parsing 
            // expects there to be no "this.", so let's get rid of this.:
            if (leftOfEquals.StartsWith("this."))
            {
                leftOfEquals = leftOfEquals.Substring("this.".Length);
            }

            string newVariableDeclarationType = null;
            // I don't know if this if statement can possibly evaluate to true anymore...
            if (leftOfEquals.Contains(' '))
            {
                newVariableDeclarationType = leftOfEquals.Substring(0, leftOfEquals.IndexOf(' '));
                leftOfEquals = leftOfEquals.Substring(leftOfEquals.IndexOf(' ') + 1, leftOfEquals.Length - (leftOfEquals.IndexOf(' ') + 1));
            }

            succeeded = ApplyAssignment(codeContext, leftOfEquals, rightOfEquals, newVariableDeclarationType, expression.Operator);

            return succeeded;
        }

        public bool ApplyAssignment(VariableInitializer variableInitializer, VariableDeclarationStatement vds, CodeContext codeContext)
        {
            return ApplyAssignment(codeContext, variableInitializer.Name, variableInitializer.Initializer.GetText(),
                vds.Type.GetText(), AssignmentOperatorType.Assign);
        }

        public bool ApplyAssignment(CodeContext codeContext, string leftOfEquals, string rightOfEquals, string newVariableDeclarationType,
            AssignmentOperatorType assignmentOperator)
        {
            bool succeeded = false;
            if (newVariableDeclarationType == null && variables.Contains(leftOfEquals))
            {
                // Circular depenency detected!
                AddErrorText("Circular dependency detected on " + leftOfEquals);
            }
            else
            {

                variables.Add(leftOfEquals);

                try
                {
                    AssignValue(newVariableDeclarationType, leftOfEquals, rightOfEquals, codeContext, assignmentOperator);
                    succeeded = true;
                }
                catch (Exception exception)
                {
                    string combined = newVariableDeclarationType + " " + leftOfEquals + " assignment " + rightOfEquals;
                    AddErrorText("Parse error in:  " + combined);
                    mParserLog.AppendLine("Parse error for line " + combined + "\nException:\n" + exception);
                }
                variables.Remove(leftOfEquals);
            }

            return succeeded;
        }

        private void AddErrorText(string text)
        {
#if !UNIT_TESTS
            this.mControl.AddText(text);
#endif
        }

        private static void GetLeftAndRightOfEquals(string line, out string leftOfEquals, out string rightOfEquals)
        {
            int indexOfEquals = line.IndexOf('=');
            int endOfLeft = indexOfEquals;
            if (endOfLeft == -1)
            {
                endOfLeft = line.IndexOf(';');
            }
            leftOfEquals = line.Substring(0, endOfLeft).Trim();
            leftOfEquals = RemoveThisDot(leftOfEquals);

            rightOfEquals = null;

            if (indexOfEquals != -1)
            {
                rightOfEquals = line.Substring(indexOfEquals + 1, line.Length - (indexOfEquals + 1)).Trim();
                rightOfEquals = RemoveThisDot(rightOfEquals);

                if (rightOfEquals.EndsWith(";"))
                {
                    rightOfEquals = rightOfEquals.Substring(0, rightOfEquals.Length - 1).Trim();
                }
            }
        }

        private static string RemoveThisDot(string value)
        {
            if (value.StartsWith("this."))
            {
                value = value.Substring("this.".Length);
            }
            if (value.Contains(" this."))
            {
                value = value.Replace(" this.", "");
            }

            return value;
        }

        private void AssignValue(string newVariableDeclarationType,  string leftOfEquals, string rightOfEquals, CodeContext codeContext,
            AssignmentOperatorType assignOperator)
        {
            ElementRuntime elementRuntime = codeContext.ContainerInstance as ElementRuntime;
            IElement element = null;

            if (elementRuntime != null)
            {
                element = elementRuntime.AssociatedIElement;
            }

            CustomVariable customVariable = CreateCustomVariableFor(newVariableDeclarationType, leftOfEquals, rightOfEquals, elementRuntime, codeContext);

            if (customVariable.Name.CountOf('.') > 1)
            {
                throw new Exception("Script parsing doesn't support setting properties/fields on properties/fields.  In other words, assigning something.X is okay, but not something.Position.x");
            }

            mParserLog.AppendLine("Left side:  " + leftOfEquals + "  RightSide:  " + rightOfEquals + "    Resulting variable:   " + customVariable);

            if (newVariableDeclarationType != null)
            {
                TopOfVariableStack[leftOfEquals] = customVariable.DefaultValue;
                codeContext.VariableStack.Last().Add(leftOfEquals, customVariable.DefaultValue);
            }
            else if (customVariable != null)
            {
                if (codeContext.ContainerInstance != null)
                {
                    ElementRuntime runtime = codeContext.ContainerInstance as ElementRuntime;
                    runtime.SetCustomVariable(customVariable, runtime.AssociatedIElement, customVariable.DefaultValue, true, VariableSettingOptions.LiteralSet );
                }
                else
                {
                    var reference = mExpressionParser.EvaluateExpression(leftOfEquals, codeContext, ExpressionParseType.GetReference);
                    bool wasAssigned = false;

                    wasAssigned = TryAssignIAssignableReference(assignOperator, customVariable, reference);

                    if (!wasAssigned)
                    {
                        throw new Exception("Could not assign the value");
                    }
                }
            }
        }

        private static bool TryAssignIAssignableReference(AssignmentOperatorType assignOperator, CustomVariable customVariable, object reference)
        {
            bool wasAssigned = false;
            if (reference is IAssignableReference)
            {
                if (assignOperator == AssignmentOperatorType.Assign)
                {
                    ((IAssignableReference)reference).CurrentValue = customVariable.DefaultValue;
                    wasAssigned = true;
                }
                else if (assignOperator == AssignmentOperatorType.Add)
                {
                    object currentValue = ((IAssignableReference)reference).CurrentValue;
                    object addedResult = PrimitiveOperationManager.Self.AddObjects(currentValue, customVariable.DefaultValue);
                    ((IAssignableReference)reference).CurrentValue = addedResult;
                    wasAssigned = true;
                }
                else if (assignOperator == AssignmentOperatorType.Subtract)
                {
                    object currentValue = ((IAssignableReference)reference).CurrentValue;
                    object addedResult = PrimitiveOperationManager.Self.SubtractObjects(currentValue, customVariable.DefaultValue);
                    ((IAssignableReference)reference).CurrentValue = addedResult;
                    wasAssigned = true;
                }
                else if (assignOperator == AssignmentOperatorType.Multiply)
                {
                    object currentValue = ((IAssignableReference)reference).CurrentValue;
                    object addedResult = PrimitiveOperationManager.Self.MultiplyObjects(currentValue, customVariable.DefaultValue);
                    ((IAssignableReference)reference).CurrentValue = addedResult;
                    wasAssigned = true;
                }
                else if (assignOperator == AssignmentOperatorType.Divide)
                {
                    object currentValue = ((IAssignableReference)reference).CurrentValue;
                    object addedResult = PrimitiveOperationManager.Self.DivideObjects(currentValue, customVariable.DefaultValue);
                    ((IAssignableReference)reference).CurrentValue = addedResult;
                    wasAssigned = true;
                }
            }
            return wasAssigned;
        }

        public CustomVariable CreateCustomVariableFor(string newVariableDeclarationType, string leftOfEquals, string rightOfEquals, ElementRuntime elementRuntime, CodeContext codeContext)
        {
            IElement element = null;
            CustomVariable toReturn = null;
            if (elementRuntime != null)
            {
                element = elementRuntime.AssociatedIElement;
                toReturn = element.GetCustomVariableRecursively(leftOfEquals);
            }
            // See if there is already a CustomVariable for this:

            if (toReturn != null)
            {
                toReturn = toReturn.Clone();
            }

            if (toReturn == null)
            {
                // If there's no event, we gotta create one
                toReturn = new CustomVariable();
                toReturn.Name = leftOfEquals;

                // If the left side has a period, that means the user is setting a variable on a contained object (because this. will already have been stripped)
                if (leftOfEquals.Contains('.'))
                {
                    int indexOfDot = leftOfEquals.IndexOf('.');
                    toReturn.SourceObject = leftOfEquals.Substring(0, indexOfDot);
                    toReturn.SourceObjectProperty = leftOfEquals.Substring(indexOfDot + 1, leftOfEquals.Length - (indexOfDot + 1));
                }


                if (newVariableDeclarationType == null)
                {
                    Type throwaway = null;
                    toReturn.Type = GetTypeStringForValue(leftOfEquals, elementRuntime, codeContext, out throwaway);
                }
                else
                {
                    toReturn.Type = newVariableDeclarationType;
                }

            }

            object valueToAssign = null;

            if (!string.IsNullOrEmpty(rightOfEquals))
            {
                valueToAssign = mExpressionParser.EvaluateExpression(rightOfEquals, codeContext);
            }

            if (toReturn.Type == null && valueToAssign != null)
            {
                toReturn.Type = valueToAssign.GetType().FullName;
            }

            if (toReturn.Type != null)
            {


                if (toReturn.GetRuntimeType() == typeof(float))
                {
                    if (valueToAssign is double)
                    {
                        valueToAssign = (float)((double)valueToAssign);
                    }
                    else if (valueToAssign is int)
                    {
                        valueToAssign = (float)((int)valueToAssign);
                    }
                }


                if (valueToAssign is float && float.IsNaN( (float)valueToAssign ))
                {
                    int m = 3;
                }

                toReturn.DefaultValue = valueToAssign;
            }

            return toReturn;
        }


        public string GetTypeStringForValue(string leftOfEquals, ElementRuntime elementRuntime, CodeContext codeContext, out Type runtimeType)
        {
            // try using the new method:
            var leftReference = mExpressionParser.EvaluateExpression(leftOfEquals, codeContext, ExpressionParseType.GetReference);

            if (leftReference != null)
            {
                if (leftReference is IAssignableReference)
                {
                    Type type = ((IAssignableReference)leftReference).TypeOfReference;
                    runtimeType = type;
                    string toReturn = null;
                    if (runtimeType != null)
                    {
                        toReturn = runtimeType.FullName;
                    }
                    return toReturn;
                }
                else
                {
                    runtimeType = null;
                    return null;
                }

            }
            else
            {


                string leftSideRaw = leftOfEquals;
                string leftSideContainer = null;
                if (leftOfEquals.Contains('.'))
                {
                    // That means the user is setting a value on a contained object, so let's see what the raw variable is
                    int lastDot = leftOfEquals.LastIndexOf('.');

                    leftSideRaw = leftOfEquals.Substring(lastDot + 1, leftSideRaw.Length - (lastDot + 1));
                    leftSideContainer = leftOfEquals.Substring(0, lastDot);
                }

                if (leftSideContainer == null)
                {
                    if (elementRuntime != null)
                    {

                        if (IsStateVariable(leftSideRaw, elementRuntime))
                        {
                            runtimeType = typeof(string);
                            return "String";
                        }
                    }

                    FieldInfo fieldInfo = mAllFields.GetFieldByName(leftSideRaw);
                    if (fieldInfo != null)
                    {
                        runtimeType = fieldInfo.FieldType;
                        return fieldInfo.FieldType.ToString();
                    }

                    PropertyInfo propertyInfo = mAllProperties.GetPropertyByName(leftSideRaw);
                    if (propertyInfo != null)
                    {
                        runtimeType = propertyInfo.PropertyType;
                        return propertyInfo.PropertyType.ToString();
                    }
                }
                // reverse loop so we get the inner-most variables first
                for (int i = mCallStackVariables.Count - 1; i > -1; i--)
                {
                    Dictionary<string, object> variableDictionary = mCallStackVariables[i];


                    if (variableDictionary.ContainsKey(leftSideRaw))
                    {
                        runtimeType = variableDictionary[leftSideRaw].GetType();
                        return runtimeType.ToString();
                    }

                }

                if (leftSideContainer != null)
                {
                    ElementRuntime containerElementRuntime = elementRuntime.GetContainedElementRuntime(leftSideContainer);

                    if (containerElementRuntime != null)
                    {
                        return GetTypeStringForValue(leftSideRaw, containerElementRuntime, codeContext, out runtimeType);
                    }
                }

                // If we got this far we should try to get the type through reflection
                GetTypeFromReflection(leftOfEquals, out runtimeType, null);

                if (runtimeType != null)
                {
                    return runtimeType.ToString();
                }
                else
                {
                    return null;
                }
            }
        }

        private bool IsStateVariable(string value, ElementRuntime elementRuntime)
        {
            if(value == "CurrentState")
            {
                return true;

            }

            IElement element = elementRuntime.AssociatedIElement;

            if(element != null && value != null && value.StartsWith("Current") && value.EndsWith("State"))
            {
                string possibleCategory = value.Substring("Current".Length);
                possibleCategory = possibleCategory.Substring(0, possibleCategory.Length - "State".Length);

                return element.GetStateCategoryRecursively(possibleCategory) != null;

            }
            return false;
        }

        private void GetTypeFromReflection(string leftSideRaw, out Type runtimeType, Type containerType)
        {
            string leftBeforeDot = leftSideRaw;
            string leftSideAfterDot = leftSideRaw;
            bool hasDot = leftSideRaw.Contains('.');
            if (hasDot)
            {
                leftBeforeDot = leftSideRaw.Substring(0, leftSideRaw.IndexOf('.'));
                leftSideAfterDot = leftSideRaw.Substring(leftSideRaw.IndexOf('.') + 1);
            }

            if(hasDot)
            {
                GetTypeFromReflection(leftBeforeDot, out containerType, containerType);
                GetTypeFromReflection(leftSideAfterDot, out runtimeType, containerType);
            }
            else
            {
                if (containerType == null)
                {
                    runtimeType = TypeManager.GetTypeFromString(leftSideRaw);
                }
                else
                {
                    runtimeType = null;

                    FieldInfo fieldInfo = containerType.GetField(leftSideRaw);
                    if (fieldInfo != null)
                    {
                        runtimeType = fieldInfo.FieldType;
                    }
                    else
                    {
                        PropertyInfo propertyInfo = containerType.GetProperty(leftSideRaw);
                        if (propertyInfo != null)
                        {
                            runtimeType = propertyInfo.PropertyType;
                        }
                    }
                }
            }


        }


    }
}
