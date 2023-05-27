using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.StringConverters;

namespace FlatRedBall.Glue.GuiDisplay
{
    public class AvailableStates : TypeConverterWithNone
    {
        public NamedObjectSave CurrentNamedObject
        {
            get;
            set;
        }

        public IElement CurrentElement
        {
            get;
            set;
        }


        public CustomVariable CurrentCustomVariable
        {
            get;
            set;
        }

        public StateSave CurrentStateSave
        {
            get;
            set;
        }


        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableStates(NamedObjectSave currentNamedObject, IElement currentElement, CustomVariable currentCustomVariable, StateSave currentStateSave) : base()
        {
            IncludeNoneOption = true;
            CurrentNamedObject = currentNamedObject;
            CurrentElement = currentElement;
            CurrentCustomVariable = currentCustomVariable;
            CurrentStateSave = currentStateSave;

        }

        List<string> listOfStates = new List<string>();
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {

            listOfStates.Clear();
            GetListOfStates(listOfStates, context?.PropertyDescriptor.DisplayName);



			StandardValuesCollection svc = new StandardValuesCollection(listOfStates);

			return svc;
        }

        public void GetListOfStates(List<string> listToFill, string selectedItemName)
        {
            if (selectedItemName != null && selectedItemName.Contains(" set in "))
            {
                selectedItemName = selectedItemName.Substring(0, selectedItemName.IndexOf(" set in "));
            }
            IElement currentElement = null;

            NamedObjectSave currentNamedObject = CurrentNamedObject;

            if (currentNamedObject != null)
            {
                FillPossibleStatesFor(listToFill, selectedItemName, currentNamedObject, base.IncludeNoneOption);
            }
            else
            {
                currentElement = CurrentElement;


                CustomVariable customVariable = CurrentCustomVariable;

                if (customVariable == null)
                {
                    StateSave stateSave = CurrentStateSave;

                    string variableLookingFor = selectedItemName;

                    for (int i = 0; i < currentElement.CustomVariables.Count; i++)
                    {

                        if (currentElement.CustomVariables[i].Name == variableLookingFor)
                        {
                            customVariable = currentElement.CustomVariables[i];
                            break;
                        }
                    }
                }

                if (customVariable == null)
                {
                    int m = 3;
                }
                FillPossibleStatesFor(listToFill, currentElement, customVariable, base.IncludeNoneOption);

            }
        }

        private static CustomVariable FillPossibleStatesFor(List<string> listToFill, IElement currentElement, CustomVariable customVariable, bool includeNone)
        {

            IElement sourceElement = null;

            customVariable = customVariable.GetDefiningCustomVariable();

            if (!string.IsNullOrEmpty(customVariable.SourceObject))
            {
                NamedObjectSave namedObjectSave = currentElement.GetNamedObjectRecursively(customVariable.SourceObject);

                sourceElement = ObjectFinder.Self.GetEntitySave(namedObjectSave.SourceClassType);
                if (sourceElement == null)
                {
                    sourceElement = ObjectFinder.Self.GetScreenSave(namedObjectSave.SourceClassType);
                }
            }
            else
            {
                var name = customVariable.GetEntityNameDefiningThisTypeCategory();

                if(!string.IsNullOrEmpty(name) && name != currentElement?.Name)
                {
                    sourceElement = ObjectFinder.Self.GetIElement(name);
                }
                else
                {
                    sourceElement = currentElement;
                }
            }


            IEnumerable<StateSave> whatToLoopThrough = null;

            if (customVariable != null)
            {
                if(sourceElement == null)
                {
                    throw new NullReferenceException("The source element is null here and it shouldn't be");
                }

                string categoryName = customVariable.Type;

                // If the type name has a '.', then it's a fully qualified type. We already know the entity
                // so let's unqualify it:
                if(categoryName.Contains('.'))
                {
                    categoryName = categoryName.Substring(categoryName.LastIndexOf('.') + 1);
                }

                StateSaveCategory category = sourceElement.GetStateCategoryRecursively(categoryName);

                if (category != null)
                {
                    whatToLoopThrough = category.States;
                }
            }

            if(includeNone)
            {
                listToFill.Add("<NONE>");
            }

            if (whatToLoopThrough == null)
            {
                foreach (StateSave state in sourceElement.GetUncategorizedStatesRecursively())
                {
                    listToFill.Add(state.Name);

                }
            }
            else
            {
                // We are looping through a category
                foreach (StateSave stateSave in whatToLoopThrough)
                {
                    listToFill.Add(stateSave.Name);
                }
            }

            var temp = listToFill.OrderBy(item => item).ToArray();
            listToFill.Clear();
            listToFill.AddRange(temp);

            return customVariable;
        }

        public static void FillPossibleStatesFor(List<string> listToFill, string selectedItemName, NamedObjectSave currentNamedObject, bool includeNone = true)
        {


            var element = currentNamedObject.GetReferencedElement();

            while (element != null)
            {
                IEnumerable<StateSave> stateList = element.States;

                // Let's see if this element has a variable by this name
                CustomVariable foundVariable = element.GetCustomVariableRecursively(selectedItemName);

                if (foundVariable != null)
                {
                    FillPossibleStatesFor(listToFill, element, foundVariable, includeNone);
                    break;
                }
                else
                {
                    listToFill.Add("<NONE>");
                    bool useDefaultStateList = selectedItemName == "CurrentVariableState" ||
                        (foundVariable == null && selectedItemName == "CurrentState");

                    if (useDefaultStateList)
                    {
                        foreach (StateSave state in element.States)
                        {
                            listToFill.Add(state.Name);
                        }
                    }
                    else
                    {
                        if (!useDefaultStateList)
                        {
                            string stateCategory = "";
                            if (foundVariable != null)
                            {
                                stateCategory = foundVariable.Type;
                            }
                            else
                            {
                                stateCategory = StateSaveExtensionMethods.GetStateTypeFromCurrentVariableName(selectedItemName);
                            }



                            StateSaveCategory category = element.GetStateCategory(stateCategory);
                            if (category != null)
                            {
                                stateList = category.States;
                            }
                        }

                        foreach (StateSave state in stateList)
                        {
                            listToFill.Add(state.Name);
                        }
                    }
                    element = ObjectFinder.Self.GetElement(element.BaseElement);
                }
            }
            var temp = listToFill.OrderBy(item => item).ToArray();
            listToFill.Clear();
            listToFill.AddRange(temp);
        }
    }
}
