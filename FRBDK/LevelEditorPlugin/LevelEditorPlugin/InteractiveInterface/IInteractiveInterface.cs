using System.ComponentModel;

namespace InteractiveInterface
{
    [Description("InteractiveInterface")]
    public interface IInteractiveInterface
    {
        void SelectNamedObjectSave(string containerName, string namedObjectName);
        void UpdateNamedObjectSave(string containerName, string associatedNamedObjectSave);
    }
}
