using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.IO;
using System.IO;



namespace FlatRedBall.Glue.SaveClasses
{

    public static class IElementHelper
    {
        



        // ContainsRecursively moved to GlueCommon.SaveClasses.IElementHelperMethods (#2276) - it only
        // needed the IObjectFinderCore seam over ObjectFinder.Self, not ObjectFinder.Self itself.

        public static void SortStatesToCustomVariables(this IElement element)
        {
            foreach (StateSave stateSave in element.AllStates)
            {
                stateSave.SortInstructionSaves(element.CustomVariables);
            }
        }

        public static void CleanUnusedVariablesFromStates(this IElement element)
        {
            foreach (StateSave state in element.AllStates)
            {
                for (int i = state.InstructionSaves.Count - 1; i > -1; i--)
                {
                    // Make sure this variable is good:
                    if (!element.ContainsCustomVariableRecursively(state.InstructionSaves[i].Member))
                    {
                        state.InstructionSaves.RemoveAt(i);
                    }
                }
            }
        }

        public static void RemoveState(this IElement elementToRemoveFrom, StateSave stateSave)
        {
            if (elementToRemoveFrom.States.Contains(stateSave))
            {
                elementToRemoveFrom.States.Remove(stateSave);
            }
            else
            {
                foreach (StateSaveCategory category in elementToRemoveFrom.StateCategoryList)
                {
                    if (category.States.Contains(stateSave))
                    {
                        category.States.Remove(stateSave);
                        return;
                    }
                }
            }
        }

    }
}
