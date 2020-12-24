using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IRefreshCommands
    {
        /// <summary>
        /// Refreshes everything for the selected TreeNode
        /// </summary>
        void RefreshUiForSelectedElement();

        /// <summary>
        /// Refreshes everything for the selected element
        /// </summary>
        /// <param name="element">IElement to update UI for</param>
        void RefreshUi(IElement element);

        void RefreshUi(StateSaveCategory category);

        /// <summary>
        /// Refreshes the UI for the Global Content tree node
        /// </summary>
        void RefreshGlobalContent();

        /// <summary>
        /// Refreshes all errors.
        /// </summary>
        void RefreshErrors();


        /// <summary>
        /// Refreshes the propertygrid so that the latest data will be shown.  This should be called whenever data
        /// shown in the property grid has changed because the propertygrid does not automatically reflect the change.
        /// </summary>
        void RefreshPropertyGrid();

        void RefreshSelection();

        void RefreshDirectoryTreeNodes();
    }
}
