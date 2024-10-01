using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Elements;



namespace FlatRedBall.Glue.SaveClasses
{

    public static class IElementHelper
    {
        



        public static bool ContainsRecursively(this GlueElement element, ReferencedFileSave whatToLookFor)
        {
            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                if (rfs == whatToLookFor)
                {
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(element.BaseElement))
            {
                GlueElement baseElement = ObjectFinder.Self.GetElement(element.BaseElement);
                if (baseElement != null)
                {
                    return baseElement.ContainsRecursively(whatToLookFor);
                }
            }
            return false;
        }

        public static void SortStatesToCustomVariables(this GlueElement element)
        {
            foreach (StateSave stateSave in element.AllStates)
            {
                stateSave.SortInstructionSaves(element.CustomVariables);
            }
        }

        public static void CleanUnusedVariablesFromStates(this GlueElement element)
        {
            foreach (StateSave state in element.AllStates)
            {
                for (int i = state.InstructionSaves.Count - 1; i > -1; i--)
                {
                    // Make sure this variable is good:
                    if (!element.ContainsCustomVariable(state.InstructionSaves[i].Member))
                    {
                        state.InstructionSaves.RemoveAt(i);
                    }
                }
            }
        }

        public static void RemoveState(this GlueElement elementToRemoveFrom, StateSave stateSave)
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
