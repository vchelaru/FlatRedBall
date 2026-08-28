using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.VariableDisplay
{
    public enum VariablePanelMode
    {
        NamedObject,
        MultipleNamedObjects,
        Element,
        ReferencedFile,
        Empty
    }

    public static class VariablePanelModeLogic
    {
        /// <summary>
        /// Overload considering the full multi-selection. Falls back to the single-object
        /// DetermineMode when 0 or 1 objects are selected, so existing single-select behavior
        /// (including its tests) is untouched.
        /// </summary>
        public static VariablePanelMode DetermineMode(
            IReadOnlyList<NamedObjectSave> currentNamedObjectSaves,
            GlueElement currentElement,
            StateSave currentStateSave,
            StateSaveCategory currentStateSaveCategory,
            ITreeNode selectedTreeNode,
            ReferencedFileSave currentReferencedFileSave)
        {
            if (currentNamedObjectSaves != null && currentNamedObjectSaves.Count > 1)
            {
                // Lists never have properties to show, and a list has none of its own to multi-edit
                // either, so if any selected object is a list, fall back to Empty:
                return currentNamedObjectSaves.Any(item => item.IsList)
                    ? VariablePanelMode.Empty
                    : VariablePanelMode.MultipleNamedObjects;
            }

            return DetermineMode(currentNamedObjectSaves?.FirstOrDefault(), currentElement, currentStateSave,
                currentStateSaveCategory, selectedTreeNode, currentReferencedFileSave);
        }

        public static VariablePanelMode DetermineMode(
            NamedObjectSave currentNamedObjectSave,
            GlueElement currentElement,
            StateSave currentStateSave,
            StateSaveCategory currentStateSaveCategory,
            ITreeNode selectedTreeNode,
            ReferencedFileSave currentReferencedFileSave)
        {
            if (currentNamedObjectSave != null)
            {
                // Lists never have properties to show:
                return currentNamedObjectSave.IsList ? VariablePanelMode.Empty : VariablePanelMode.NamedObject;
            }
            else if (currentStateSave != null || currentStateSaveCategory != null)
            {
                // For now we don't handle showing states, so we show the empty state so the user
                // doesn't think they are editing states
                return VariablePanelMode.Empty;
            }
            else if (currentElement != null &&
                (selectedTreeNode?.IsRootCustomVariablesNode() == true
                // It's annoying to have to select the Variables node - the user should be able to
                // see variables just by selecting the entity/screen itself.
                || selectedTreeNode?.IsElementNode() == true))
            {
                return VariablePanelMode.Element;
            }
            else if (currentReferencedFileSave != null)
            {
                return VariablePanelMode.ReferencedFile;
            }
            else
            {
                // Nothing selected, or something without properties (e.g. a folder):
                return VariablePanelMode.Empty;
            }
        }
    }
}
