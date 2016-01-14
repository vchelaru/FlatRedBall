using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.SetVariable
{
    public class StateSaveCategorySetVariableLogic
    {
        public void ReactToStateSaveCategoryChangedValue(StateSaveCategory stateSaveCategory, string changedMember, object oldValue, IElement element, ref bool updateTreeView)
        {
            if(changedMember == "SharesVariablesWithOtherCategories")
            {

                string oldType;
                string newType;
                if ((bool)oldValue == true)
                {
                    oldType = "VariableState";
                    newType = stateSaveCategory.Name;
                }
                else
                {
                    oldType = stateSaveCategory.Name;
                    newType = "VariableState";
                }

                string whyIsntAllowed = GetWhySharesVariableChangeIsntAllowed(stateSaveCategory, element);

                if (!string.IsNullOrEmpty(whyIsntAllowed))
                {
                    MessageBox.Show(whyIsntAllowed);
                    stateSaveCategory.SharesVariablesWithOtherCategories = false;
                }
                else
                {

                    // See if any variables are using this.  If so, let's change them
                    // We need to see if any variables are already tunneling in to this
                    // new type.  If so, then notify the user that the variable will be removed.
                    // Otherwise, notify the user that the variable type is changing.
                    foreach (CustomVariable customVariable in element.CustomVariables)
                    {
                        if (customVariable.Type == oldType && !string.IsNullOrEmpty(customVariable.DefaultValue as string) &&
                            stateSaveCategory.GetState(customVariable.DefaultValue as string) != null)
                        {
                            // We need to do something here...
                            bool shouldChange = false;

                            #if !UNIT_TESTS
                            var dialogResult = System.Windows.Forms.MessageBox.Show("Change variable " + customVariable.Name + " to be of type " + newType + " ?  Selecting 'No' will introduce compile errors.",
                                "Change variable type?", System.Windows.Forms.MessageBoxButtons.YesNo);
                            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                            {
                                shouldChange = true;
                            }
                            #else
                            shouldChange = true;
                            #endif
                            if (shouldChange)
                            {
                                customVariable.Type = newType;
                            }
                        }
                    }
                }
            }
        }

        private string GetWhySharesVariableChangeIsntAllowed(StateSaveCategory category, IElement element)
        {
            string toReturn = null;
            if (category.SharesVariablesWithOtherCategories)
            {
                // Shares, which means we need to go through all uncategorized and all other categories that share to see if there's any matches
                foreach (var state in category.States)
                {
                    if (element.States.Exists(item => item.Name == state.Name))
                    {
                        toReturn = "There is already an uncategorized state named " + state.Name + " which would conflict with the state in " + category.Name;
                        break;
                    }

                    foreach (var otherCategory in element.StateCategoryList.Where(item=>item.SharesVariablesWithOtherCategories && item != category))
                    {
                        if (otherCategory.States.Exists(item => item.Name == state.Name))
                        {
                            toReturn = "There is already a state in the category " + otherCategory.Name + " with the name " + state.Name;
                            break;
                        }
                    }
                }
            }
            return toReturn;
        }
    }
}
