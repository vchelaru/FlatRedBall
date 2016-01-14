using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Instructions.Reflection;
using System.Reflection;
using FlatRedBall;
using FlatRedBall.Glue;
using System.Text.RegularExpressions;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using CodeTranslator.Parsers;
using FlatRedBall.Utilities;
using GlueView.SaveClasses;
using FlatRedBall.Content.Instructions;
using CSharpParser = ICSharpCode.NRefactory.CSharp.CSharpParser;
using SyntaxTree = ICSharpCode.NRefactory.CSharp.SyntaxTree;
using GlueView.Scripting;
using FlatRedBall.Glue.Parsing;
using ICSharpCode.NRefactory.CSharp;
using GlueViewOfficialPlugins.States;
using FlatRedBall.Instructions;

namespace GlueViewOfficialPlugins.Scripting
{
    #region Enums

    public enum ExpressionOperators
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public enum SpecialValues
    {
        Null
    }

    public enum ExpressionParseType
    {
        Evaluate,
        GetReference
    }


    #endregion


    public class TypeReference
    {
        public Type Type;
    }




    public class ExpressionParser
    {


        StringBuilder mLogStringBuilder;

        Dictionary<Type, List<FieldInfo>> mFieldsByType = new Dictionary<Type, List<FieldInfo>>();
        Dictionary<Type, List<PropertyInfo>> mPropertiesByType = new Dictionary<Type, List<PropertyInfo>>();
        //List<FieldInfo> mAllFields;
        //List<PropertyInfo> mAllProperties;

        MethodCallParser mMethodCallParser;

        PropertyInfo[] mIScalableObjectProperties;
        
        public StringBuilder LogStringBuilder
        {
            get
            {
                return mLogStringBuilder;
            }
            set
            {
                mLogStringBuilder = value;
                this.mMethodCallParser.LogStringBuilder = value;
            }
        }

        public ExpressionParser()
        {

            FillFieldsAndPropertiesForType(typeof(PositionedObject));
            FillFieldsAndPropertiesForType(typeof(Sprite));

            Initialize();
        }

        private void FillFieldsAndPropertiesForType(Type type)
        {
            mFieldsByType.Add(type, new List<FieldInfo>());
            mFieldsByType[type].AddRange(type.GetFields());


            mPropertiesByType.Add(type, new List<PropertyInfo>());
            mPropertiesByType[type].AddRange(type.GetProperties());
        }


        void Initialize()
        {
            mMethodCallParser = new MethodCallParser(this);
        }


        CSharpParser parser = new CSharpParser();
        public object EvaluateExpression(string expression, CodeContext codeContext, ExpressionParseType parseType = ExpressionParseType.Evaluate)
        {
//            

            var result = parser.ParseExpression(expression);
            if (result.ToString() == "Null")
            {
                IEnumerable<Statement> statements = parser.ParseStatements(expression);

                Statement firstStatement = statements.First();

                return EvaluateStatement(firstStatement, codeContext, parseType);
            }
            else
            {
                return EvaluateExpression(result, codeContext, parseType);
            }
        }

        internal object EvaluateStatement(Statement statement, CodeContext codeContext, ExpressionParseType parseType)
        {
            if (statement is VariableDeclarationStatement)
            {
                VariableDeclarationStatement vds = statement as VariableDeclarationStatement;

                var astType = vds.Type;

                Type type = TypeManager.GetTypeFromString(astType.GetText());

                var variables = vds.Variables;

                string addedVariable = null;

                foreach (var variable in variables)
                {
                    // right now we only support one declaration per line, will need to
                    // expand this later
                    addedVariable = variable.Name;
                    codeContext.VariableStack.Last().Add(addedVariable, null);
                    break;
                }

                if (parseType == ExpressionParseType.Evaluate)
                {
                    return null; // hasn't been assigned yet
                }
                else
                {
                    StackVariableReference svr = new StackVariableReference();
                    int index;
                    codeContext.GetVariableInformation(addedVariable, out index);
                    svr.StackIndex = index;
                    svr.VariableName = addedVariable;
                    svr.TypeOfReference = type;
                    return svr;
                }
            }
            else
            {
                return null;
            }
        }

        internal object EvaluateExpression(Expression expression, CodeContext codeContext, ExpressionParseType parseType = ExpressionParseType.Evaluate)
        {
            if (codeContext.VariableStack.Count == 0)
            {
                throw new Exception("codeContext must have at least one entry in scoped variables");
            }
            if (expression is ICSharpCode.NRefactory.CSharp.PrimitiveExpression)
            {
                return (expression as ICSharpCode.NRefactory.CSharp.PrimitiveExpression).Value;
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression)
            {
                return EvaluateBinaryOperatorExpression(expression, codeContext);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.ParenthesizedExpression)
            {
                return EvaluateExpression(((ICSharpCode.NRefactory.CSharp.ParenthesizedExpression)expression).Expression, codeContext);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.InvocationExpression)
            {
                return EvaluateInvocationExpression(expression as ICSharpCode.NRefactory.CSharp.InvocationExpression, codeContext);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
            {
                return EvaluateMemberReferenceExpression(expression as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression, codeContext, parseType);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.ObjectCreateExpression)
            {
                return EvaluateObjectCreateExpression(expression as ICSharpCode.NRefactory.CSharp.ObjectCreateExpression, codeContext);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.ThisReferenceExpression)
            {
                return codeContext.ContainerInstance;
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.TypeReferenceExpression)
            {
                return TypeManager.GetTypeFromString(expression.GetText());
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
            {
                return GetObjectFromContainerAndMemberName(codeContext.ContainerInstance, expression.GetText(), codeContext, parseType);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.IndexerExpression)
            {
                return EvaluateIndexerExpression(expression as ICSharpCode.NRefactory.CSharp.IndexerExpression, codeContext);
            }
            else if (expression is LambdaExpression)
            {
                return EvaluateLambdaExpression(expression as LambdaExpression, codeContext);
            }
            else if (expression is UnaryOperatorExpression)
            {
                return EvaluateUnaryOperatorExpression(expression as UnaryOperatorExpression, codeContext);
            }
            else if (expression is CastExpression)
            {
                return EvaluateCastExpression(expression as CastExpression, codeContext);
            }
            else
            {

                return null;
            }
        }

        private object EvaluateCastExpression(CastExpression castExpression, CodeContext codeContext)
        {
            Type whatToCastTo = TypeManager.GetTypeFromString(castExpression.Type.ToString());
            object whatToCast = EvaluateExpression(castExpression.Expression, codeContext);

            // do we do anything here?
            return whatToCast;
        }

        private object EvaluateUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, CodeContext context)
        {
            ExpressionParseType referenceOrValue;
            var operatorType = unaryOperatorExpression.Operator;
            if(operatorType == UnaryOperatorType.PostDecrement ||
                operatorType == UnaryOperatorType.PostIncrement ||
                operatorType == UnaryOperatorType.Decrement ||
                operatorType == UnaryOperatorType.Increment)
            {
                referenceOrValue = ExpressionParseType.GetReference;
            }
            else
            {
                referenceOrValue = ExpressionParseType.Evaluate;
            }
            // We need to get referene so that the 
            object value = EvaluateExpression(unaryOperatorExpression.Expression, context, referenceOrValue);
            if (value == null)
            {
                return value;
            }
            else
            {

                return PrimitiveOperationManager.Self.ApplyUnaryOperation(unaryOperatorExpression.Operator, value, referenceOrValue);
            }

        }

        private object EvaluateLambdaExpression(LambdaExpression lambdaExpression, CodeContext codeContext)
        {
            ExecuteScriptInstruction esi = new ExecuteScriptInstruction(codeContext, lambdaExpression.Body.GetText());
            return esi;
        }

        private object EvaluateIndexerExpression(ICSharpCode.NRefactory.CSharp.IndexerExpression indexerExpression, CodeContext codeContext)
        {
            object evaluatedTarget = EvaluateExpression(indexerExpression.Target, codeContext);

            if (evaluatedTarget is RuntimeCsvRepresentation)
            {
                List<object> evaluatedArguments = new List<object>();
                foreach (var argument in indexerExpression.Arguments)
                {
                    evaluatedArguments.Add(EvaluateExpression(argument, codeContext));
                }
                string requiredKey = evaluatedArguments[0] as string;


                
                RuntimeCsvRepresentation rcr = evaluatedTarget as RuntimeCsvRepresentation;

                return GetCsvEntryByRequiredKey(requiredKey, rcr);

            }
            return null;
        }

        private static object GetCsvEntryByRequiredKey(string requiredKey, RuntimeCsvRepresentation rcr)
        {
            rcr.RemoveHeaderWhitespaceAndDetermineIfRequired();

            int requiredIndex = rcr.GetRequiredIndex();

            int startingRow;
            int count;
            GetStartingAndCount(rcr, requiredIndex, requiredKey, out startingRow, out count);
            CsvEntry csvEntry = null;

            if (startingRow != -1)
            {
                csvEntry = new CsvEntry();

                csvEntry.RuntimeCsvRepresentation = rcr;
                csvEntry.Count = count;
                csvEntry.StartIndex = startingRow;
            }

            return csvEntry;
        }

        private static void GetStartingAndCount(RuntimeCsvRepresentation rcr, int requiredIndex, string requiredName, out int startingRow, out int count)
        {
            startingRow = -1;
            count = 0;

            for (int i = 0; i < rcr.Records.Count; i++)
            {
                if (startingRow == -1)
                {
                    if (rcr.Records[i][requiredIndex] == requiredName)
                    {
                        startingRow = i;
                        count = 1;
                    }
                }
                else if (startingRow != -1)
                {
                    if (string.IsNullOrEmpty(rcr.Records[i][requiredIndex]))
                    {
                        count++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private object EvaluateObjectCreateExpression(ICSharpCode.NRefactory.CSharp.ObjectCreateExpression objectCreateExpression, CodeContext codeContext)
        {

            string typeAsString = objectCreateExpression.Type.GetText();

            Type type = TypeManager.GetTypeFromString(typeAsString);

            if (type != null)
            {
                List<object> arguments = new List<object>();
                foreach (var argumentExpression in objectCreateExpression.Arguments)
                {
                    arguments.Add(EvaluateExpression(argumentExpression, codeContext));
                }

                return Activator.CreateInstance(type, arguments.ToArray());
            }
            else
            {
                return null;
            }
        }

        private object EvaluateMemberReferenceExpression(ICSharpCode.NRefactory.CSharp.MemberReferenceExpression memberReferenceExpression, CodeContext codeContext, ExpressionParseType parseType)
        {
            object container = EvaluateExpression(memberReferenceExpression.Target, codeContext, ExpressionParseType.GetReference);
            if (container == null)
            {
                // Couldn't get a member reference, so it could be a variable on an Element, so let's try a non-reference get
                container = EvaluateExpression(memberReferenceExpression.Target, codeContext, ExpressionParseType.Evaluate);
            }
            string memberName = memberReferenceExpression.MemberName;

            object foundValue = GetObjectFromContainerAndMemberName(container, memberName, codeContext, parseType);

            return foundValue;
        }

        private static object GetObjectFromContainerAndMemberName(object container, string memberName, CodeContext codeContext, ExpressionParseType parseType)
        {
            object foundValue = null;
            bool wasFound = false;

            if (parseType == ExpressionParseType.Evaluate)
            {
                if (container is IAssignableReference)
                {
                    container = ((IAssignableReference)container).CurrentValue;
                }

                GetObjectFromContainerAndNameEvaluate(container, memberName, codeContext, ref foundValue, ref wasFound);
            }
            else
            {
                GetObjectFromContainerAndNameReference(container, memberName, codeContext, ref foundValue, ref wasFound);

            }
            return foundValue;
        }

        private static void GetObjectFromContainerAndNameReference(object container, string memberName, CodeContext codeContext, 
            ref object foundValue, ref bool wasFound)
        {
            object originalContainer = container;

            if (container == null)
            {
                int stackDepth = -1;
                codeContext.GetVariableInformation(memberName, out stackDepth);

                if (stackDepth != -1)
                {
                    StackVariableReference reference = new StackVariableReference();
                    reference.StackIndex = stackDepth;
                    reference.VariableName = memberName;
                    reference.CodeContext = codeContext;

                    foundValue = reference;
                    wasFound = foundValue != null;
                }
            }
            else
            {
                
                Type typeToGetFrom = null;

                if (container is IAssignableReference)
                {
                    container = ((IAssignableReference)container).CurrentValue;
                }

                if (container is Type)
                {
                    typeToGetFrom = container as Type;
                }
                else if (container != null)
                {
                    typeToGetFrom = container.GetType();
                }

                AssignableReference assignableReference = null;

                var fieldInfo = typeToGetFrom.GetField(memberName);
                if (fieldInfo != null)
                {
                    assignableReference = new AssignableReference();
                    assignableReference.FieldInfo = fieldInfo;
                    assignableReference.Owner = container;

                    if (originalContainer is IAssignableReference)
                    {
                        assignableReference.Parent = originalContainer as IAssignableReference;
                    }
                }
                else
                {
                    var propertyInfo = typeToGetFrom.GetProperty(memberName);
                    if (propertyInfo != null)
                    {
                        assignableReference = new AssignableReference();
                        assignableReference.PropertyInfo = propertyInfo;
                        assignableReference.Owner = container;

                        if (originalContainer is IAssignableReference)
                        {
                            assignableReference.Parent = originalContainer as IAssignableReference;
                        }
                    }
                }

                foundValue = assignableReference;
                wasFound = foundValue != null;
            }
        }

        private static void GetObjectFromContainerAndNameEvaluate(object container, string memberName, CodeContext codeContext, ref object foundValue, ref bool wasFound)
        {

            if (container is CsvEntry)
            {
                foundValue = (container as CsvEntry).GetValue(memberName);

            }

            if (codeContext.VariableStack.Count == 0)
            {
                throw new Exception("codeContext doesn't have any entries.  It needs to have at least one");
            }

            int index = -1;
            codeContext.GetVariableInformation(memberName, out index);

            if (index != -1)
            {
                foundValue = codeContext.VariableStack[index][memberName];
                wasFound = true;
            }

            if (wasFound == false && foundValue == null && container != null)
            {
                object instance = container;
                Type type = container.GetType();
                if (container is Type)
                {
                    instance = null;
                    type = container as Type;
                }

                // First let's do reflection
                if (container is ElementRuntime && (container as ElementRuntime).DirectObjectReference != null)
                {
                    ElementRuntime containerElementRuntime = container as ElementRuntime;

                    if (LateBinder.GetInstance(containerElementRuntime.DirectObjectReference.GetType()).TryGetValue(containerElementRuntime.DirectObjectReference, memberName, out foundValue))
                    {
                        // do nothing.
                        wasFound = true;
                    }
                }
                else
                {


                    if (LateBinder.GetInstance(type).TryGetValue(instance, memberName, out foundValue))
                    {
                        // do nothing.
                        wasFound = true;
                    }
                }

                if (foundValue == null && container is ElementRuntime)
                {
                    ElementRuntime containerElementRuntime = container as ElementRuntime;

                    IElement containerElement = (container as ElementRuntime).AssociatedIElement;


                    foundValue = TryToGetStateCategoryFromElement(memberName, containerElement);

                    if (foundValue == null)
                    {
                        foundValue = containerElementRuntime.GetContainedElementRuntime(memberName);
                    }

                    if (foundValue == null)
                    {
                        foundValue = containerElementRuntime.GetReferencedFileSaveRuntime(memberName);
                    }

                    if (foundValue == null && containerElement != null)
                    {
                        // Some values like X or Y are stored inside the element runtime
                        // (because it actually stores those values locally).  However, if
                        // a value doesn't have an underlying value, 
                        CustomVariable variable = containerElementRuntime.GetCustomVariable(memberName, VariableGetType.AsExistsAtRuntime);
                        //CustomVariable variable = containerElement.GetCustomVariableRecursively(memberName);
                        if (variable != null)
                        {
                            if (variable.GetIsCsv())
                            {
                                string rfsToLookFor = FileManager.RemoveExtension(variable.Type);
                                foundValue = containerElementRuntime.GetReferencedFileSaveRuntime(rfsToLookFor);
                                // if it's null, maybe it's a global file
                                if (foundValue == null)
                                {
                                    ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(variable.Type);
                                    if (rfs != null)
                                    {
                                        foundValue = GluxManager.GlobalContentFilesRuntime.LoadReferencedFileSave(rfs, true, containerElement);
                                    }
                                }

                                if (foundValue != null)
                                {
                                    foundValue = GetCsvEntryByRequiredKey(variable.DefaultValue as string, foundValue as RuntimeCsvRepresentation);
                                    // We have a RFS, so let's get values out of it
                                }
                            }
                            else
                            {
                                foundValue = variable.DefaultValue;
                                wasFound = true;
                            }
                        }
                    }
                    wasFound = foundValue != null;
                }
                else if (container is StateSaveCategory)
                {
                    foundValue = (container as StateSaveCategory).States.FirstOrDefault(state => state.Name == memberName);
                    wasFound = foundValue != null;
                }
                else if (container is IElement)
                {
                    foundValue = TryToGetStateCategoryFromElement(memberName, container as IElement);
                }

            }
            if (wasFound == false && foundValue == null)
            {
                foundValue = ObjectFinder.Self.GetElementUnqualified(memberName);
                wasFound = foundValue != null;
            }
            if (wasFound == false && foundValue == null)
            {
                foundValue = TypeManager.GetTypeFromString(memberName);

                wasFound = foundValue != null;


            }
        }

        private static object TryToGetStateCategoryFromElement(string memberName, IElement element)
        {
            object foundObject = null;
            StateSaveCategory existingCategory = element.GetStateCategoryRecursively(memberName);
            bool isUncategorizedCategory = memberName == "VariableState";
            if (!isUncategorizedCategory)
            {
                if (existingCategory != null)
                {
                    if (existingCategory.SharesVariablesWithOtherCategories == false)
                    {
                        foundObject = existingCategory;
                    }
                    else
                    {
                        isUncategorizedCategory = true;
                    }
                }
            }
            if (isUncategorizedCategory)
            {
                StateSaveCategory stateSaveCategory = new StateSaveCategory();

                foreach (StateSave stateSave in element.States)
                {
                    stateSaveCategory.States.Add(stateSave);
                }
                foundObject = stateSaveCategory;
            }
            return foundObject;
        }

        private object EvaluateInvocationExpression(ICSharpCode.NRefactory.CSharp.InvocationExpression invocationExpression, CodeContext codeContext)
        {
            List<object> argumentValues = new List<object>();
            foreach (var unevaluated in invocationExpression.Arguments)
            {
                argumentValues.Add(EvaluateExpression(unevaluated, codeContext));
            }

            return EvaluateInvocationExpression(invocationExpression.Target, argumentValues, codeContext);
        }

        private object EvaluateInvocationExpression(Expression expression, List<object> argumentValues, CodeContext codeContext)
        {
            string invocation = expression.ToString();


            object lastObject = codeContext.ContainerInstance;
            if (invocation == "System.Math.Max" ||
                invocation == "Math.Max")
            {
                return PrimitiveOperationManager.Self.MaxObjects(argumentValues[0], argumentValues[1]);
            }
            else if (invocation == "GetFile" && argumentValues.Count == 1)
            {
                ElementRuntime elementRuntime = null;
                // We're going to have to assume the user means to call GetFile from the current element
                if (codeContext.ContainerInstance != null)
                {
                    if (codeContext.ContainerInstance is ElementRuntime)
                    {
                        elementRuntime = codeContext.ContainerInstance as ElementRuntime;
                    }
                }

                if (elementRuntime != null)
                {
                    return elementRuntime.GetReferencedFileSaveRuntime(argumentValues[0] as string);
                }
                else
                {
                    return null;
                }
            }
            else if (invocation == "System.Math.Min" ||
                invocation == "Math.Min")
            {
                return PrimitiveOperationManager.Self.MinObjects(argumentValues[0], argumentValues[1]);
            }
            else if (invocation == "InterpolateBetween" || invocation == "this.InterpolateBetween")
            {
                MethodCallParser.InterpolateBetween(lastObject as ElementRuntime, argumentValues[0], argumentValues[1], argumentValues[2]);
                return null;
            }
            else
            {
                object caller = GetCaller(expression, codeContext);

                MethodInfo methodInfo;

                object toReturn = null;

                Type[] argumentTypes = new Type[argumentValues.Count];

                for (int i = 0; i < argumentValues.Count; i++)
                {
                    if (argumentValues[i] != null)
                    {
                        argumentTypes[i] = argumentValues[i].GetType();
                    }
                }

                if (TryHandleSpecialCase(expression, argumentValues, argumentTypes, codeContext, invocation, caller, out toReturn))
                {
                    // do nothing, toReturn is set
                }
                else
                {
                    GetMethodInfo(expression, argumentValues, argumentTypes, codeContext, invocation, caller, out methodInfo);

                    if (methodInfo != null)
                    {
                        toReturn = methodInfo.Invoke(caller, argumentValues.ToArray());
                    }
                }
                return toReturn;
            }
            //else
            //{
            //    throw new NotImplementedException();
            //}
        }

        private object GetCaller(ICSharpCode.NRefactory.CSharp.Expression expression, CodeContext codeContext)
        {
            object caller;
            if (expression is MemberReferenceExpression)
            {
                MemberReferenceExpression mre = expression as MemberReferenceExpression;


                caller = EvaluateExpression(mre.Target, codeContext);

                if (caller == null)
                {
                    Type type = TypeManager.GetTypeFromString(mre.Target.GetText());
                    if (type != null)
                    {
                        TypeReference typeReference = new TypeReference();
                        typeReference.Type = type;

                        caller = typeReference;
                    }
                        
                }


                if (caller == null)
                {
                    caller = codeContext.ContainerInstance;
                }


                return caller;


            }
            else if (expression is InvocationExpression)
            {
                return codeContext.ContainerInstance;
            }
            else if (expression is IdentifierExpression)
            {
                return codeContext.ContainerInstance;
            }
            else
            {
                return null;

            }
        }

        private bool TryHandleSpecialCase(Expression expression, List<object> argumentValues, Type[] argumentTypes, CodeContext codeContext, string invocation, object caller, out object result)
        {
            result = null;
            bool handled = false;
            if (expression is MemberReferenceExpression)
            {
                MemberReferenceExpression mre = expression as MemberReferenceExpression;

                if(mre.MemberName == "Call" &&
                        argumentValues.Count == 1 && argumentValues[0] is ExecuteScriptInstruction)
                {
                    if (caller != null && caller is PositionedObject)
                    {
                        // call the ExecuteScriptInstruction itself - it has a After method so it can handle whatever.
                        result = argumentValues[0];
                        handled = true;
                    }
                }

                if (!handled)
                {
                    if (caller != null && caller is ElementRuntime)
                    {
                        handled = TryHandleElementRuntimeSpecialCases(argumentValues, argumentTypes, caller as ElementRuntime, ref result, mre);
                    }
                }
                if (!handled && caller is IElement)
                {
                    if (mre.MemberName == "GetFile")
                    {
                        IElement element = caller as IElement;

                        //ReferencedFileSave rfs = element.GetReferencedFileSaveByInstanceName(argumentValues[0] as string);

                        IElement containerInstance = null;
                        if (codeContext.ContainerInstance != null)
                        {
                            if (codeContext.ContainerInstance is ElementRuntime)
                            {
                                containerInstance = (codeContext.ContainerInstance as ElementRuntime).AssociatedIElement;
                            }
                        }
                        if (element == containerInstance)
                        {
                            result = (codeContext.ContainerInstance as ElementRuntime).GetReferencedFileSaveRuntime(argumentValues[0] as string);
                            handled = true;
                        }
                    }
                }
                if (!handled && caller.GetType().Name == "SetHolder`1")
                {
                    MethodInfo methodInfo;

                    // let's invoke this regularly, but add it to the container
                    GetMethodInfo(expression, argumentValues, argumentTypes, codeContext, invocation, caller, out methodInfo);
                    if (methodInfo != null)
                    {
                        result = methodInfo.Invoke(caller, argumentValues.ToArray());

                        // no need to do anything?
                        //if (result is Instruction)
                        //{
                        //    ((result as Instruction).Target as IInstructable).Instructions.Add(result as Instruction);
                        //}
                        handled = true;
                    }
                }

            }

            return handled;
        }

        private static bool TryHandleElementRuntimeSpecialCases(List<object> argumentValues, Type[] argumentTypes, ElementRuntime caller, ref object result, MemberReferenceExpression mre)
        {
            bool handled = false;
            
            if (argumentValues.Count == 5 && mre.MemberName == "InterpolateToState")
            {
                // using advanced interpolation
                StateViewPlugin.Self.ApplyInterpolateToState(
                    argumentValues[0],
                    argumentValues[1],
                    (float)argumentValues[2],
                    (FlatRedBall.Glue.StateInterpolation.InterpolationType)argumentValues[3],
                    (FlatRedBall.Glue.StateInterpolation.Easing)argumentValues[4]);
                result = null;
                handled = true;
            }

            if (!handled && caller.DirectObjectReference != null)
            {
                Type directObjectType = caller.DirectObjectReference.GetType();

                MethodInfo methodInfo = directObjectType.GetMethod(mre.MemberName, argumentTypes);
                if (methodInfo != null)
                {

                    result = methodInfo.Invoke(caller, argumentValues.ToArray());
                    handled = true;
                }
            }

            if (!handled && argumentValues.Count == 1 && mre.MemberName == "Set" && argumentTypes[0] == typeof(string))
            {
                result = caller.Set(argumentValues[0] as string);
                handled = true;
            }
            return handled;
        }
        

        private void GetMethodInfo(ICSharpCode.NRefactory.CSharp.Expression expression, List<object> argumentValues, Type[] types, CodeContext codeContext, string invocation, object container, out MethodInfo methodInfo)
        {
            methodInfo = null;



            if (expression is ICSharpCode.NRefactory.CSharp.IdentifierExpression)
            {

                methodInfo = container.GetType().GetMethod(invocation, types);
            }
            else if (expression is ICSharpCode.NRefactory.CSharp.MemberReferenceExpression)
            {
                ICSharpCode.NRefactory.CSharp.MemberReferenceExpression mre = expression as ICSharpCode.NRefactory.CSharp.MemberReferenceExpression;

                bool doTypesHaveNull = false;
                foreach (var item in types)
                {
                    if (item == null)
                    {
                        doTypesHaveNull = true;
                    }
                }

                Type typeToCallGetMethodOn;

                if (container is Type)
                {
                    typeToCallGetMethodOn = (container as Type);
                }
                else if (container is TypeReference)
                {
                    typeToCallGetMethodOn = (container as TypeReference).Type;
                }
                else
                {
                    typeToCallGetMethodOn = container.GetType();
                }
                if (doTypesHaveNull)
                {
                    // Let's hope there's no ambiguity or else we're in trouble...
                    methodInfo = typeToCallGetMethodOn.GetMethod(mre.MemberName);
                }
                else
                {
                    bool shouldTryAgain = false; ;
                    try
                    {
                        methodInfo = typeToCallGetMethodOn.GetMethod(mre.MemberName, types);
                    }
                    catch
                    {
                        shouldTryAgain = true;
                    }
                    if(shouldTryAgain || methodInfo == null)
                    {
                        // The method doesn't exist, but it could be because
                        // the parser evaluated the types as one type (like int)
                        // but they are really of another type (like float).
                        var candidates = typeToCallGetMethodOn.GetMethods();
                        foreach (var candidate in candidates)
                        {

                            if (candidate.Name == mre.MemberName &&
                                    candidate.GetParameters().Length == types.Length)
                            {
                                methodInfo = candidate;
                                break;
                            }
                        }
                    }
                }
            }
            else if (expression is InvocationExpression)
            {
                InvocationExpression ie = expression as InvocationExpression;

                if (container is Type)
                {
                    methodInfo = (container as Type).GetMethod(ie.Target.GetText(), types);
                }
                else
                {
                    methodInfo = container.GetType().GetMethod(ie.Target.GetText(), types);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private object EvaluateBinaryOperatorExpression(ICSharpCode.NRefactory.CSharp.Expression result, CodeContext codeContext)
        {
            ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression binaryExpression = result as
                ICSharpCode.NRefactory.CSharp.BinaryOperatorExpression;

            object left = EvaluateExpression(binaryExpression.Left, codeContext);
            object right = EvaluateExpression(binaryExpression.Right, codeContext);

            switch (binaryExpression.Operator)
            {

                case BinaryOperatorType.Add:
                    return PrimitiveOperationManager.Self.AddObjects(left, right);
                case BinaryOperatorType.ConditionalAnd:
                    return (bool)left && (bool)right;
                case BinaryOperatorType.ConditionalOr:
                    return (bool)left || (bool)right;
                case BinaryOperatorType.Divide:
                    return PrimitiveOperationManager.Self.DivideObjects(left, right);
                case BinaryOperatorType.Equality:
                    return left == right;
                case BinaryOperatorType.GreaterThan:
                    return PrimitiveOperationManager.Self.GreaterThan(left, right);
                case BinaryOperatorType.GreaterThanOrEqual:
                    return left == right ||
                        PrimitiveOperationManager.Self.GreaterThan(left, right);
                case BinaryOperatorType.InEquality:
                    return left != right;
                case BinaryOperatorType.LessThan:
                    return PrimitiveOperationManager.Self.LessThan(left, right);
                case BinaryOperatorType.LessThanOrEqual:
                    return left == right ||
                        PrimitiveOperationManager.Self.LessThan(left, right);
                case BinaryOperatorType.Multiply:
                    return PrimitiveOperationManager.Self.MultiplyObjects(left, right);
                case BinaryOperatorType.Subtract:
                    return PrimitiveOperationManager.Self.SubtractObjects(left, right);
                default:
                    return null;
            }
        }

        private bool TryGetLocalVariableValue(string name, List<Dictionary<string, object>> localVariableStack, out object value)
        {
            value = null;
            if (localVariableStack != null)
            {
                foreach (var dictionary in localVariableStack)
                {
                    foreach (KeyValuePair<string, object> kvp in dictionary)
                    {
                        if (kvp.Key == name)
                        {
                            value = kvp.Value;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

    }


}
