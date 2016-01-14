using System;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using InteractiveInterface;

namespace OfficialPlugins.GlueView
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
            // Do we even want to use this anymore?
            // It seems to cause bugs, and we've moved
            // to wcf...
            //_glueCommands.TreeNodeCommands.SelectTreeNode(nos);
        }

        public void UpdateNamedObjectSave(string containerName, string associatedNamedObjectSave)
        {
            var nos = FileManager.XmlDeserializeFromString<NamedObjectSave>(associatedNamedObjectSave);
            IgnoreNextRefresh = true;
            _glueCommands.UpdateCommands.Update(containerName, nos);
        }
    }
}
