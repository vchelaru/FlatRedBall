using System.ComponentModel;

namespace InteractiveInterface
{
    [Description("InteractiveInterface")]
    public interface IInteractiveInterface
    {
        /// <summary>
        /// Allows selection of Glue object from GlueView
        /// </summary>
        /// <param name="containerName">Container of ElementRuntime</param>
        /// <param name="namedObjectName">FieldName of ElementRuntime</param>
        /// Example:
        /// InteractiveConnection.Callback.SelectNamedObjectSave(item.ContainerName, item.FieldName);
        void SelectNamedObjectSave(string containerName, string namedObjectName);


        //void UpdateNamedObjectSave(string containerName, string associatedNamedObjectSave);
    }
}
