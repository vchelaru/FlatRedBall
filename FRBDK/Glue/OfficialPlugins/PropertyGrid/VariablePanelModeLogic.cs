using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.VariableDisplay
{
    public enum VariablePanelMode
    {
        NamedObject,
        Element,
        ReferencedFile,
        Empty
    }

    public static class VariablePanelModeLogic
    {
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
