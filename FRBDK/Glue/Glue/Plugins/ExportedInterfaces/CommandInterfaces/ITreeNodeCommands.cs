using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface ITreeNodeCommands
    {
        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="element">Item to select</param>
        void SelectTreeNode(IElement element);

        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="namedObjectSave">Item to select</param>
        void SelectTreeNode(NamedObjectSave namedObjectSave);


        /// <summary>
        /// Selects the node representing the argument file
        /// </summary>
        /// <param name="referencedFileSave">Item to select</param>
        void SelectTreeNode(ReferencedFileSave referencedFileSave);


        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="customVariable">Item to select</param>
        void SelectTreeNode(CustomVariable customVariable);

        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="stateSave">Item to select</param>
        void SelectTreeNode(StateSave stateSave);

        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="stateSaveCategory">Item to select</param>
        void SelectTreeNode(StateSaveCategory stateSaveCategory);

        /// <summary>
        /// Selects the item in the TreeView
        /// </summary>
        /// <param name="codeFile">Item to select</param>
        void SelectTreeNode(string codeFile);

        /// <summary>
        /// Used to determine if selected treenode is an Entity
        /// </summary>
        /// <returns>True if the currently selected treenode is an entity treenode</returns>
        bool SelectedTreeNodeIsEntity();

        /// <summary>
        /// Used to determine if selected treenode is a Screen
        /// </summary>
        /// <returns>True if the currently selected treenode is a screen treenode</returns>
        bool SelectedTreeNodeIsScreen();

        /// <summary>
        /// Returns the tree nodes EntitySave if it is an EntityTreeNode
        /// </summary>
        /// <returns>EntitySave or Null if not an EntityTreeNode</returns>
        EntitySave GetSelectedEntitySave();

        /// <summary>
        /// Returns the tree nodes ScreenSave if it is a ScreenTreeNode
        /// </summary>
        /// <returns>ScreenSave or Null if not a ScreenTreeNode</returns>
        ScreenSave GetSelectedScreenSave();

        /// <summary>
        /// Sets the value of a property for the selected node
        /// </summary>
        /// <param name="name">Name of property to set</param>
        /// <param name="value">Value of property to set to</param>
        /// <typeparam name="T">Type of value object</typeparam>
        void SetProperty<T>(string name, T value);

        /// <summary>
        /// Sets the value of a property for EntitySave
        /// </summary>
        /// <param name="entitySave">EntitySave to set property for</param>
        /// <param name="name">Name of property to set</param>
        /// <param name="value">Value of property to set to</param>
        /// <typeparam name="T">Type of value object</typeparam>
        void SetProperty<T>(EntitySave entitySave, string name, T value);

        /// <summary>
        /// Sets the value of a property for ScreenSave
        /// </summary>
        /// <param name="screenSave">ScreenSave to set property for</param>
        /// <param name="name">Name of property to set</param>
        /// <param name="value">Value of property to set to</param>
        /// <typeparam name="T">Type of value object</typeparam>
        void SetProperty<T>(ScreenSave screenSave, string name, T value);

        /// <summary>
        /// Gets the value of a property for the selected node
        /// </summary>
        /// <param name="name">Name of property to get</param>
        /// <typeparam name="T">Type of value object</typeparam>
        T GetProperty<T>(string name);

        /// <summary>
        /// Gets the value of a property for the EntitySave
        /// </summary>
        /// <param name="entitySave">EntitySave to get property from</param>
        /// <param name="name">Name of property to get</param>
        /// <typeparam name="T">Type of value object</typeparam>
        T GetProperty<T>(EntitySave entitySave, string name);

        /// <summary>
        /// Gets the value of a property for the ScreenSave
        /// </summary>
        /// <param name="screenSave">ScreenSave to get property from</param>
        /// <param name="name">Name of property to get</param>
        /// <typeparam name="T">Type of value object</typeparam>
        T GetProperty<T>(ScreenSave screenSave, string name);

        /// <summary>
        /// Gets the value of a property for the Element
        /// </summary>
        /// <param name="element">Element to get property from</param>
        /// <param name="name">Name of property to get</param>
        /// <typeparam name="T">Type of value object</typeparam>
        T GetProperty<T>(IElement element, string name);
    }
}
