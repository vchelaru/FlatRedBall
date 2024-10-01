using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface ITreeNodeCommands
    {
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
        T GetProperty<T>(GlueElement element, string name);

        void HandleTreeNodeDoubleClicked(ITreeNode treeNode);
    }
}
