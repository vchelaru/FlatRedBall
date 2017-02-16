using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.CodeGeneration;
using Alsing.Windows.Forms;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Parsing;
using System.Reflection;

namespace FlatRedBall.Glue.Controls
{
    class AutoCompleteManager
    {
        Alsing.Windows.Forms.SyntaxBoxControl syntaxBoxControl1;

        public void Initialize(SyntaxBoxControl control)
        {
            this.syntaxBoxControl1 = control;
        }



        private static void AddAutoCompleteForElement(List<string> toReturn, IElement element)
        {

            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                foreach (NamedObjectSave innerNos in nos.ContainedObjects)
                {
                    toReturn.Add(innerNos.InstanceName);
                }
                toReturn.Add(nos.InstanceName);
            }

            foreach (CustomVariable customVariable in element.CustomVariables)
            {
                toReturn.Add(customVariable.Name);
            }

            if (element is ScreenSave)
            {
                toReturn.Add("PauseThisScreen()");
                toReturn.Add("UnpauseThisScreen()");
                toReturn.Add("IsPaused");
                toReturn.Add("MoveToScreen(typeof(");
            }
            else if (element is EntitySave)
            {
                toReturn.Add("Destroy()");
            }

            toReturn.AddRange(
                ExposedVariableManager.GetExposableMembersFor(element, false).Select(item=>item.Member));

            if (element.AllStates.GetCount() > 0)
            {
                toReturn.Add("CurrentState");

                foreach (StateSaveCategory category in element.StateCategoryList)
                {
                    if (!category.SharesVariablesWithOtherCategories)
                    {
                        toReturn.Add("Current" + category.Name + "State");
                    }
                }

                toReturn.Add("InterpolateToState(");
            }
        }

        private void AddStaticAutoCompleteFor(List<string> toFill, IElement element)
        {
            toFill.Add("VariableState");
            toFill.Add("GetStaticMember(");
        }

        private static void AddAutoCompleteFor(List<string> toFill, NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.Entity)
            {
                EntitySave entitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassType);

                if (entitySave != null)
                {
                    AddAutoCompleteForElement(toFill, entitySave);
                }
            }
            else if (nos.SourceType == SourceType.FlatRedBallType)
            {
                toFill.AddRange(ExposedVariableManager.GetExposableMembersFor(nos).Select(item=>item.Member));

                if (nos.IsList)
                {
                    toFill.Add("Count");
                }
            }

        }

        private static void AddAutoCompleteFor(List<string> toFill, CustomVariable customVariable)
        {
            if (CustomVariableCodeGenerator.IsTypeFromCsv(customVariable))
            {
                string fileName = ProjectManager.MakeAbsolute(customVariable.Type, true);

                ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(fileName);

                if (rfs?.IsCsvOrTreatedAsCsv == true)
                {
                    toFill.AddRange(CsvCodeGenerator.GetMemberNamesFrom(rfs));
                }
            }
        }

        public List<string> GetAutoCompleteValues()
        {

            string textBeforePeriod = "";
            Alsing.SourceCode.Row row = syntaxBoxControl1.Caret.CurrentRow;

            int indexIntoRow = syntaxBoxControl1.Caret.Position.X;
                
            List<string> toReturn = new List<string>();

            try
            {


                if (EditorLogic.CurrentElement != null)
                {
                    if (syntaxBoxControl1.Caret.PreviousWord != null)
                    {
                        string wordBeforeDot = syntaxBoxControl1.Caret.PreviousWord.Text;
                        string wordBefore2 = syntaxBoxControl1.Caret.GetWordText(1);
                        string wordBefore3 = syntaxBoxControl1.Caret.GetWordText(2);
                        string wordBefore4 = syntaxBoxControl1.Caret.GetWordText(3);

                        Type type;

                        if (wordBefore2 == "this")
                        {
                            IElement element = EditorLogic.CurrentElement;

                            AddAutoCompleteForElement(toReturn, element);
                        }
                        else if (EditorLogic.CurrentElement.GetNamedObjectRecursively(wordBefore2) != null)
                        {
                            NamedObjectSave nos = EditorLogic.CurrentElement.GetNamedObjectRecursively(wordBeforeDot);

                            AddAutoCompleteFor(toReturn, nos);
                        }
                        else if (EditorLogic.CurrentElement.GetCustomVariableRecursively(wordBefore2) != null)
                        {
                            CustomVariable customVariable = EditorLogic.CurrentElement.GetCustomVariableRecursively(wordBefore2);
                            AddAutoCompleteFor(toReturn, customVariable);
                        }
                        else if (ObjectFinder.Self.GetElementUnqualified(wordBefore2) != null)
                        {
                            AddStaticAutoCompleteFor(toReturn, ObjectFinder.Self.GetElementUnqualified(wordBeforeDot));
                        }
                        else if (GetIfVariableState(wordBefore2, wordBefore3, wordBefore4))
                        {
                            IElement element = null;

                            if (string.IsNullOrWhiteSpace(wordBefore3))
                            {
                                element = EditorLogic.CurrentElement;
                            }
                            else
                            {
                                element = ObjectFinder.Self.GetElementUnqualified(wordBefore4);
                            }

                            var availableStates = StateCodeGenerator.GetAllStatesForCategory(element, wordBefore2);

                            if (element.GetStateCategory(wordBefore2) == null)
                            {
                                availableStates = StateCodeGenerator.GetSharedVariableStates(element);
                            }

                            foreach (StateSave stateSave in availableStates)
                            {
                                toReturn.Add(stateSave.Name);
                            }
                        }
                        else if ((type = TypeManager.GetFlatRedBallType(wordBefore2)) != null)
                        {
                            AddStaticAutoCompleteFor(toReturn, type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                int m = 3;
            }
            StringFunctions.RemoveDuplicates(toReturn);
            toReturn.Sort();
            return toReturn;
        }

        private static bool GetIfVariableState(string wordBefore2, string wordBefore3, string wordBefore4)
        {
            IElement element;

            if (string.IsNullOrWhiteSpace(wordBefore3))
            {
                element = EditorLogic.CurrentElement;
            }
            else
            {
                element = ObjectFinder.Self.GetElementUnqualified(wordBefore4);
            }
            if (element != null)
            {
                var availableStates = StateCodeGenerator.GetAllStatesForCategory(element, wordBefore2);
                bool isCategory = wordBefore2 == "VariableState" || availableStates.Count != 0;


                return (isCategory && wordBefore3 == "." && ObjectFinder.Self.GetElementUnqualified(wordBefore4) != null)
                                        ||
                                        (isCategory && string.IsNullOrWhiteSpace(wordBefore3));
            }
            else
            {
                return false;
            }
        }

        private void AddStaticAutoCompleteFor(List<string> toReturn, Type type)
        {
            MethodInfo[] methods = type.GetMethods();

            foreach (var method in methods)
            {
                if(method.IsSpecialName &&( method.Name.StartsWith("set_") || method.Name.StartsWith("get_")))
                {
                    continue;
                }

                toReturn.Add(method.Name + "(");
            }
        }



    }
}
