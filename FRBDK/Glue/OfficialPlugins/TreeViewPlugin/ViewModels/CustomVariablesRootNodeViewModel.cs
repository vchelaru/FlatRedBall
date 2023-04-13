using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.ViewModels
{
    class CustomVariablesRootNodeViewModel : NodeViewModel
    {
        private GlueElement glueElement;

        public CustomVariablesRootNodeViewModel(NodeViewModel parent, GlueElement glueElement) : base(parent)
        {
            this.glueElement = glueElement;

        }

        public override void RefreshTreeNodes(TreeNodeRefreshType treeNodeRefreshType)
        {
            while (this.Children.Count < glueElement.CustomVariables.Count)
            {
                int indexToAddAt = Children.Count;
                var text = GetDisplayTextForCustomVariable(glueElement.CustomVariables[indexToAddAt]);
                var node = new NodeViewModel(this);

                var variable = glueElement.CustomVariables[indexToAddAt];
                if(variable.DefinedByBase)
                {
                    node.ImageSource = VariableIconDerived;
                }
                else
                {
                    node.ImageSource = VariableIcon;
                }
                node.Tag = variable;
                node.Text = text;
                Children.Add(node);
            }



            while (Children.Count > glueElement.CustomVariables.Count)
            {
                Children.RemoveAt(Children.Count - 1);

            }


            for (int i = 0; i < glueElement.CustomVariables.Count; i++)
            {

                var treeNode = Children[i];

                CustomVariable customVariable = glueElement.CustomVariables[i];

                if (treeNode.Tag != customVariable)
                {
                    treeNode.Tag = customVariable;
                }


                string textToSet = GetDisplayTextForCustomVariable(customVariable);

                if (treeNode.Text != textToSet)
                {
                    treeNode.Text = textToSet;
                }

                // Vic says - no need to support disabled custom variables
                //if (mSaveObject.NamedObjects[i].IsDisabled)
                //{
                //    treeNode.ForeColor = DisabledColor;
                //}

                //Color colorToSet;
                //if (customVariable.SetByDerived)
                //{
                //    colorToSet = ElementViewWindow.SetByDerivedColor;
                //}
                //else if (customVariable.DefinedByBase)
                //{
                //    colorToSet = ElementViewWindow.DefinedByBaseColor;
                //}
                //else if (!string.IsNullOrEmpty(customVariable.SourceObject) && mSaveObject.GetNamedObjectRecursively(customVariable.SourceObject) == null)
                //{
                //    colorToSet = ElementViewWindow.MissingObjectColor;
                //}
                //else
                //{
                //    colorToSet = Color.White;
                //}

                //if (treeNode.ForeColor != colorToSet)
                //{
                //    treeNode.ForeColor = colorToSet;
                //}
            }

        }

        private string GetDisplayTextForCustomVariable(CustomVariable customVariable)
        {
            if (string.IsNullOrEmpty(customVariable.OverridingPropertyType))
            {
                return
                    customVariable.Name + " (" + customVariable.Type + ")";
            }
            else
            {
                return
                    customVariable.Name + " (" + customVariable.Type + " as " + customVariable.OverridingPropertyType + ")";
            }
        }
    }
}
