using System;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using InteractiveInterface;

namespace PluginTestbed.LevelEditor
{
    [Serializable]
    public class InteractiveInterface : MarshalByRefObject, IInteractiveInterface
    {
        private readonly IGlueCommands _glueCommands;
        private readonly IGlueState _glueState;

        public bool IgnoreNextRefresh { get; set; }

        public InteractiveInterface(IGlueCommands glueCommands, IGlueState glueState)
        {
            _glueCommands = glueCommands;
            _glueState = glueState;
        }

        public void SelectNamedObjectSave(string containerName, string namedObjectName)
        {
            var nos = _glueState.GetNamedObjectSave(containerName, namedObjectName);
            _glueCommands.TreeNodeCommands.SelectTreeNode(nos);
        }

        public void UpdateNamedObjectSave(string containerName, string associatedNamedObjectSave)
        {
            var nos = FileManager.XmlDeserializeFromString<NamedObjectSave>(associatedNamedObjectSave);
            IgnoreNextRefresh = true;
            _glueCommands.UpdateCommands.Update(containerName, nos);
        }
    }
}
