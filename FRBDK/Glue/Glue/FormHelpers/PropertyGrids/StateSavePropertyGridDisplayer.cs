using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using System.ComponentModel;
using FlatRedBall.Glue.Controls.PropertyGridControls;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Content.Instructions;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class StateSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        public GlueElement CurrentElement
        {
            get;
            set;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {

                UpdateIncludedAndExcluded(value as StateSave);

                base.Instance = value;
            }
        }

        public object GetValue(string variableName)
        {
            StateSave stateSave = Instance as StateSave;

            foreach (InstructionSave instructionSave in stateSave.InstructionSaves)
            {
                if (instructionSave.Member == variableName)
                {
                    return instructionSave.Value;
                }
            }

            return null;
        }

        public StateSaveCategory GetContainingCategory(StateSave instance, IElement containingElement)
        {
            foreach (StateSaveCategory category in containingElement.StateCategoryList)
            {
                if (category.States.Contains(instance))
                {
                    return category;
                }
            }
            return null;
        }

        private void UpdateIncludedAndExcluded(StateSave instance)
        {
            ResetToDefault();
            var element = ObjectFinder.Self.GetElementContaining(instance);
            if(element == null)
            {
                return;
            }
            ExcludeMember("InstructionSaves");
            ExcludeMember("NamedObjectPropertyOverrides");


            // This doesn't share variables, so it may own the variable
            StateSaveCategory thisCategory = GetContainingCategory(instance, element);
                        
            for (int i = 0; i < element.CustomVariables.Count; i++)
            {
                CustomVariable customVariable = element.CustomVariables[i];

                var isIncluded = thisCategory == null || thisCategory.ExcludedVariables.Contains(customVariable.Name) == false;

                if(isIncluded)
                {
                    StateSave stateSaveOwningVariable = null;
                    List<StateSave> statesInThisCategory = null;
                    if (thisCategory != null)
                    {
                        statesInThisCategory = thisCategory.States;
                    }
                    else
                    {
                        statesInThisCategory = element.States;
                    }

                    foreach (StateSave stateInThisCategory in statesInThisCategory)
                    {
                        if (stateInThisCategory.AssignsVariable(customVariable))
                        {
                            stateSaveOwningVariable = instance;
                        }
                    }

                    if (stateSaveOwningVariable == null)
                    {
                        stateSaveOwningVariable = GetStateThatVariableBelongsTo(customVariable, element);
                    }
                    Type type = TypeManager.GetTypeFromString(customVariable.Type);

                    TypeConverter typeConverter = customVariable.GetTypeConverter(CurrentElement, instance, null);
                    
                    Attribute[] customAttributes;
                    if (stateSaveOwningVariable == instance ||
                        stateSaveOwningVariable == null || GetContainingCategory(instance, element) == GetContainingCategory(stateSaveOwningVariable, element))
                    {

                        customAttributes = new Attribute[] 
                        { 
                            new CategoryAttribute("State Variable"),
                            new EditorAttribute(typeof(StateValueEditor), typeof(System.Drawing.Design.UITypeEditor))
                        
                        };
                    }
                    else
                    {
                        StateSaveCategory category = GetContainingCategory(stateSaveOwningVariable, element);

                        string categoryName = "Uncategorized";

                        if (category != null)
                        {
                            categoryName = category.Name;
                        }
                        customAttributes = new Attribute[] 
                        { 
                            // Do we want it to be readonly?  I think this may be too restrictive
                            //new ReadOnlyAttribute(true),
                            new CategoryAttribute("Variables set by other states"),
                            new DisplayNameAttribute(customVariable.Name + " set in " + categoryName)
                            //,
                            //new EditorAttribute(typeof(StateValueEditor), typeof(System.Drawing.Design.UITypeEditor))
                        };
                    }

                    Type typeToPass = customVariable.GetRuntimeType();
                    if (typeToPass == null)
                    {
                        typeToPass = typeof(string);
                    }

                    IncludeMember(
                        customVariable.Name,
                        typeToPass,
                        delegate(object sender, MemberChangeArgs args)
                        {
                            object value = args.Value;

                            // May 16, 2012
                            // This crashed if
                            // the type was a Texture2D.
                            // I don't think we ever want
                            // to set the value on a StateSave
                            // to an actual Texture2D - rather it
                            // should be a string.  I don't think it
                            // should ever be any loaded file, in fact,
                            // so we should prob make sure it's not a file
                            // by adding a check.
                            if (CustomVariablePropertyGridDisplayer.GetShouldCustomVariableBeConvertedToType(args, customVariable))
                            {
                                value = PropertyValuePair.ConvertStringToType((string)args.Value, customVariable.GetRuntimeType());
                            }

                            instance.SetValue(args.Member, value);

                        }

                        ,
                        delegate()
                        {
                            return GetValue(customVariable.Name);

                        },
                        typeConverter,
                        customAttributes


                        );
                }
            }
        }

        public static StateSave GetStateThatVariableBelongsTo(CustomVariable variable, GlueElement element)
        {
            // We only loop through categories, not uncategorized States because a variable can't belong to an uncategorized state.
            // See update below for-loop
            foreach (StateSaveCategory category in element.StateCategoryList)
            {
                // This doesn't share variables, so it may own the variable
                foreach (StateSave stateSave in category.States)
                {
                    if (stateSave.AssignsVariable(variable))
                    {
                        return stateSave;
                    }
                }
            }

            // Update November 16, 2011
            // Yes variables can actually
            // belong to the no-category category
            foreach (StateSave stateSave in element.States)
            {
                if (stateSave.AssignsVariable(variable))
                {
                    return stateSave;
                }
            }

            return null;
        }
    }
}
