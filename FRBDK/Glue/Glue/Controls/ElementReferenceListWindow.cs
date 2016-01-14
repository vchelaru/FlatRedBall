using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.IO;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Controls
{
    public partial class ElementReferenceListWindow : Form
    {
        public ElementReferenceListWindow()
        {
            InitializeComponent();
        }

        public void PopulateWithReferencesTo(ReferencedFileSave rfs)
        {
            List<IElement> elements = ObjectFinder.Self.GetAllElementsReferencingFile(rfs.Name);

            foreach (IElement element in elements)
            {
                var rfsInThisElement = element.GetReferencedFileSave(rfs.Name);
                listBox1.Items.Add(rfsInThisElement);

                foreach(var namedObject in element.AllNamedObjects)
                {
                    if(namedObject.SourceType == SourceType.File &&
                        namedObject.SourceFile == rfsInThisElement.Name)
                    {
                        listBox1.Items.Add(namedObject);
                    }
                }
            }

            // If this is a CSV, then loop through all of the variables and see if any of them use this type
            if (rfs.IsCsvOrTreatedAsCsv)
            {
                string className = rfs.Name;
                CustomClassSave customClass = ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(rfs.Name);
                if (customClass != null)
                {
                    className = customClass.Name;
                }

                foreach (IElement element in ObjectFinder.Self.GlueProject.Screens)
                {
                    foreach (CustomVariable customVariable in element.CustomVariables.Where(customVariable => customVariable.Type == className))
                    {
                        listBox1.Items.Add(customVariable);
                    }
                }
                foreach (IElement element in ObjectFinder.Self.GlueProject.Entities)
                {
                    foreach (CustomVariable customVariable in element.CustomVariables.Where(customVariable => customVariable.Type == className))
                    {
                        listBox1.Items.Add(customVariable);
                    }
                }
            }

        }
        

        public void PopulateWithReferencesToElement(IElement element)
        {
            #region Get all named objects

            List<NamedObjectSave> referencedNamedObjectSaves = null;

            if (element is EntitySave)
            {
                referencedNamedObjectSaves = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity((EntitySave)element);
            }

            // TODO:  Handle inheritance here
            if (referencedNamedObjectSaves != null)
            {
                foreach (NamedObjectSave nos in referencedNamedObjectSaves)
                {
                    listBox1.Items.Add(nos);
                }
            }

            #endregion

            if (element is ScreenSave)
            {
                List<ScreenSave> screens = 
                    ObjectFinder.Self.GetAllScreensThatInheritFrom((ScreenSave)element);

                // See if any Screens link to this as their next Screen in Glue
                foreach (ScreenSave screen in ObjectFinder.Self.GlueProject.Screens)
                {
                    if (screen.NextScreen == element.Name && !screens.Contains(screen))
                    {
                        screens.Add(screen);
                    }
                }

                foreach (ScreenSave screen in screens)
                {
                    listBox1.Items.Add(screen);
                }
            }

            else if (element is EntitySave)
            {
                List<EntitySave> entities =
                    ObjectFinder.Self.GetAllEntitiesThatInheritFrom((EntitySave)element);

                foreach (EntitySave entity in entities)
                {
                    listBox1.Items.Add(entity);
                }
            }

            UpdateTextToReferenceCount();


        }

        private void UpdateTextToReferenceCount()
        {
            if (listBox1.Items.Count == 0)
            {
                this.Text = "No references found";
            }
            else if (listBox1.Items.Count == 1)
            {
                this.Text = "1 reference found";
            }
            else
            {
                this.Text = listBox1.Items.Count + " references found";
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            object highlightedObject = listBox1.SelectedItem;

            TreeNode treeNode = null;

            if (highlightedObject == null)
            {
                // do nothing
            }
            else if (highlightedObject is ScreenSave)
            {
                treeNode = GlueState.Self.Find.ScreenTreeNode((ScreenSave)highlightedObject);
            }
            else if (highlightedObject is EntitySave)
            {
                treeNode = GlueState.Self.Find.EntityTreeNode((EntitySave)highlightedObject);
            }
            else if (highlightedObject is NamedObjectSave)
            {
                treeNode = GlueState.Self.Find.NamedObjectTreeNode((NamedObjectSave)highlightedObject);
            }
            else if (highlightedObject is ReferencedFileSave)
            {
                treeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode((ReferencedFileSave)highlightedObject);
            }
            else if (highlightedObject is CustomVariable)
            {
                treeNode = GlueState.Self.Find.CustomVariableTreeNode((CustomVariable)highlightedObject);
            }
            else if (highlightedObject is StateSave)
            {
                treeNode = GlueState.Self.Find.StateTreeNode((StateSave)highlightedObject);
            }
            else if (highlightedObject is EventResponseSave)
            {
                treeNode = GlueState.Self.Find.EventResponseTreeNode((EventResponseSave)highlightedObject);
            }
            if (treeNode != null)
            {
                ElementViewWindow.SelectedNode = treeNode;
            }
        }

        internal void PopulateWithReferencesTo(NamedObjectSave namedObjectSave, IElement container)
        {
            foreach (var variable in container.CustomVariables.Where(item => item.SourceObject == namedObjectSave.InstanceName))
            {
                listBox1.Items.Add(variable);
            }

            var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(container);

            foreach (var element in derivedElements)
            {
                foreach (var nos in element.NamedObjects.Where(item => item.DefinedByBase && item.InstanceName == namedObjectSave.InstanceName))
                {
                    listBox1.Items.Add(nos);
                }

            }
        }

        internal void PopulateWithReferencesTo(CustomVariable customVariable, IElement container)
        {
            foreach (var state in container.AllStates)
            {
                if (state.InstructionSaves.Any(instruction => instruction.Member == customVariable.Name))
                {
                    listBox1.Items.Add(state);
                }
            }

            var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(container);

            foreach (var element in derivedElements)
            {
                foreach(var variable in element.CustomVariables.Where(item=>item.DefinedByBase && item.Name == customVariable.Name))
                {
                    listBox1.Items.Add(variable);
                }
            }

            foreach (var ers in container.GetEventsOnVariable(customVariable.Name))
            {
                listBox1.Items.Add(ers);
            }
        }
    }
}
