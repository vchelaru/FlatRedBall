using System;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class TreeNodeCommands : ITreeNodeCommands
    {
        public void SelectTreeNode(TreeNode treeNode)
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { ElementViewWindow.SelectedNode = treeNode; }));
        }

        public void SelectTreeNode(IElement element)
        {
            if (element == null)
            {
                SelectTreeNode((TreeNode) null);
                return;
            }

            SelectTreeNode(GlueState.Self.Find.ElementTreeNode(element));
        }

        public void SelectTreeNode(NamedObjectSave namedObjectSave)
        {
            if (namedObjectSave == null)
            {
                SelectTreeNode((TreeNode) null);
                return;
            }

            SelectTreeNode(GlueState.Self.Find.NamedObjectTreeNode(namedObjectSave));
        }

        public void SelectTreeNode(ReferencedFileSave referencedFileSave)
        {
            if (referencedFileSave == null)
            {
                SelectTreeNode((TreeNode)null);
                return;
            }

            SelectTreeNode(GlueState.Self.Find.ReferencedFileSaveTreeNode(referencedFileSave));
        }


        public void SelectTreeNode(CustomVariable customVariable)
        {
            if (customVariable == null)
            {
                SelectTreeNode((TreeNode) null);
                return;
            }

            SelectTreeNode(GlueState.Self.Find.CustomVariableTreeNode(customVariable));
        }

        public void SelectTreeNode(StateSave stateSave)
        {
            if (stateSave == null)
            {
                SelectTreeNode((TreeNode) null);
                return;
            }

            SelectTreeNode(GlueState.Self.Find.StateTreeNode(stateSave));
        }

        public void SelectTreeNode(StateSaveCategory stateSaveCategory)
        {
            throw new NotImplementedException();
        }

        public void SelectTreeNode(string codeFile)
        {
            throw new NotImplementedException();
        }

        public bool SelectedTreeNodeIsEntity()
        {
            return GlueState.Self.CurrentEntitySave != null;
        }

        public bool SelectedTreeNodeIsScreen()
        {
            return GlueState.Self.CurrentScreenSave != null;
        }

        public EntitySave GetSelectedEntitySave()
        {
            return GlueState.Self.CurrentEntitySave;
        }

        public ScreenSave GetSelectedScreenSave()
        {
            return GlueState.Self.CurrentScreenSave;
        }

        public void SetProperty<T>(string name, T value)
        {
            if(GlueState.Self.CurrentEntitySave != null)
            {
                var currentEntity = GlueState.Self.CurrentEntitySave;

                SetProperty(currentEntity, name, value);
            }else if(GlueState.Self.CurrentScreenSave != null)
            {
                var currentScreen = GlueState.Self.CurrentScreenSave;

                SetProperty(currentScreen, name, value);
            }else
            {
                throw new Exception("Unable to set property for this tree node type.");   
            }
        }

        public void SetProperty<T>(EntitySave entitySave, string name, T value)
        {
            string newValue;

            FileManager.XmlSerialize(value, out newValue);

            entitySave.Properties.SetValue(name, newValue);
        }

        public void SetProperty<T>(ScreenSave screenSave, string name, T value)
        {
            string newValue;

            FileManager.XmlSerialize(value, out newValue);

            screenSave.Properties.SetValue(name, newValue);
        }

        public T GetProperty<T>(string name)
        {
            if (GlueState.Self.CurrentEntitySave != null)
            {
                var currentEntity = GlueState.Self.CurrentEntitySave;

                return GetProperty<T>(currentEntity, name);
            }
            
            if (GlueState.Self.CurrentScreenSave != null)
            {
                var currentScreen = GlueState.Self.CurrentScreenSave;

                return GetProperty<T>(currentScreen, name);
            }

            throw new Exception("Unable to get property for this tree node type.");
        }

        public T GetProperty<T>(EntitySave entitySave, string name)
        {
            var returnValue = (string)entitySave.Properties.GetValue(name);

            return !String.IsNullOrEmpty(returnValue) ? FileManager.XmlDeserializeFromString<T>(returnValue) : default(T);
        }

        public T GetProperty<T>(ScreenSave screenSave, string name)
        {
            var returnValue = (string)screenSave.Properties.GetValue(name);

            return !String.IsNullOrEmpty(returnValue) ? FileManager.XmlDeserializeFromString<T>(returnValue) : default(T);
        }

        public T GetProperty<T>(IElement element, string name)
        {
            if(element is ScreenSave)
            {
                return GetProperty<T>(element as ScreenSave, name);
            }
            
            if(element is EntitySave)
            {
                return GetProperty<T>(element as EntitySave, name);
            }

            return default(T);
        }

        public void RefreshCurrentElementTreeNode()
        {
            var currentElement = GlueState.Self.CurrentElement;
            var treeNode = GlueState.Self.Find.ElementTreeNode(currentElement);

            treeNode.RefreshTreeNodes();
        }
    }
}
