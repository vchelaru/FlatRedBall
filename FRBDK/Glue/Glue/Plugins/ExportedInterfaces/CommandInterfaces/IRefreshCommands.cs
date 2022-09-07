using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.SaveClasses;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public enum TreeNodeRefreshType
    {
        All,
        NamedObjects,
        CustomVariables,
        StateSaves
        // eventually add more here as needed
    }

    public interface IRefreshCommands
    {
        /// <summary>
        /// Refreshes the selected element TreeNode
        /// </summary>
        void RefreshCurrentElementTreeNode();

        /// <summary>
        /// Refreshes the entire tree node view
        /// </summary>
        void RefreshTreeNodes();

        /// <summary>
        /// Refreshes the tree node for the selected element.
        /// This may add or remove tree nodes depending on whether
        /// the tree node is already created, and if the element's
        /// IsHiddenInTreeView is set to true.
        /// </summary>
        /// <param name="element">GlueElement to update the tree node for</param>
        void RefreshTreeNodeFor(GlueElement element, TreeNodeRefreshType treeNodeRefreshType = TreeNodeRefreshType.All);

        void RefreshUi(StateSaveCategory category);

        /// <summary>
        /// Refreshes the UI for the Global Content tree node
        /// </summary>
        void RefreshGlobalContent();

        /// <summary>
        /// Refreshes all errors.
        /// </summary>
        void RefreshErrors();

        void RefreshErrorsFor(ErrorReporterBase errorReporter);

        Task ClearFixedErrors();


        /// <summary>
        /// Refreshes the propertygrid so that the latest data will be shown.  This should be called whenever data
        /// shown in the property grid has changed because the propertygrid does not automatically reflect the change.
        /// </summary>
        void RefreshPropertyGrid();

        /// <summary>
        /// Refreshes the variables tab.
        /// </summary>
        void RefreshVariables();

        void RefreshSelection();

        void RefreshDirectoryTreeNodes();
    }
}
