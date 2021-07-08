using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.FormHelpers;

namespace PluginTestbed.GlobalContentManagerPlugins
{
    public partial class PluginForm : Form
    {
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        List<IElement> mElements = new List<IElement>();
        public PluginForm()
        {
            InitializeComponent();
        }



        public void RefreshElements()
        {
            for (int i = 0; i < ProjectManager.GlueProjectSave.Entities.Count; i++)
            {
                var element = ProjectManager.GlueProjectSave.Entities[i];
                AddElement(element);
            }

            for (int i = 0; i < ProjectManager.GlueProjectSave.Screens.Count; i++)
            {
                var element = ProjectManager.GlueProjectSave.Screens[i];
                AddElement(element);
            }
            

            ElementDataGrid.CurrentCellDirtyStateChanged += new EventHandler(ElementDataGrid_CurrentCellDirtyStateChanged);
        }

        private void AddElement(IElement element)
        {
            ElementDataGrid.Rows.Add();

            int index = ElementDataGrid.Rows.Count - 1;

            DataGridViewCell cell = ElementDataGrid.Rows[index].Cells[0];
            cell.Value = element.Name;
            cell.ReadOnly = true;

            cell = ElementDataGrid.Rows[index].Cells[1];
            cell.Value = element.UseGlobalContent;
            cell.ValueType = typeof(bool);

            cell = ElementDataGrid.Rows[index].Cells[2];
            cell.Value = IsElementFullyInGlobalContent(element);
            cell.ValueType = typeof(bool);


            mElements.Add(element);
        }


        void ElementDataGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            ElementDataGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            UpdateValueAtRowColumn(ElementDataGrid.CurrentCell.RowIndex, ElementDataGrid.CurrentCell.ColumnIndex);

        }

        private void UpdateValueAtRowColumn(int rowIndex, int columnIndex)
        {
            var element = mElements[rowIndex];

            switch (columnIndex)
            {
                case 1:
                    element.UseGlobalContent = (bool)ElementDataGrid.Rows[rowIndex].Cells[columnIndex].Value;
                    break;
                case 2:
                    bool shouldBeInGlobal = (bool)ElementDataGrid.Rows[rowIndex].Cells[columnIndex].Value;

                    if (shouldBeInGlobal)
                    {
                        foreach (ReferencedFileSave rfs in element.ReferencedFiles)
                        {
                            bool alreadyExists = false;
                            foreach (ReferencedFileSave existingRfs in ProjectManager.GlueProjectSave.GlobalFiles)
                            {
                                if (existingRfs.Name.ToLower() == rfs.Name.ToLower())
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            if (!alreadyExists)
                            {
                                GlueCommands.GluxCommands.AddReferencedFileToGlobalContent(rfs.Name, true);
                            }
                        }
                    }
                    else
                    {
                        foreach (ReferencedFileSave rfs in element.ReferencedFiles)
                        {
                            for (int i = 0; i < ProjectManager.GlueProjectSave.GlobalFiles.Count; i++)
                            {
                                ReferencedFileSave existingRfs = ProjectManager.GlueProjectSave.GlobalFiles[i];

                                if (existingRfs.Name.ToLower() == rfs.Name.ToLower())
                                {
                                    ProjectManager.GlueProjectSave.GlobalFiles.RemoveAt(i);
                                    break;
                                }
                            }
                        }

                        ElementViewWindow.UpdateGlobalContentTreeNodes(false);
                    }
                    break;
            }
            GlueCommands.GenerateCodeCommands.GenerateElementCode(element as GlueElement);
            GlueCommands.GenerateCodeCommands.GenerateGlobalContentCode();
            GlueCommands.GluxCommands.SaveGlux();

        }

        public void ClearElements()
        {

        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        bool IsElementFullyInGlobalContent(IElement element)
        {
            ////////////////////////EARLY OUT///////////////////////////////
            if (element.ReferencedFiles.Count == 0)
            {
                return false;
            }
            //////////////////////END EARLY OUT/////////////////////////////

            foreach (ReferencedFileSave rfs in element.ReferencedFiles)
            {
                bool alreadyExists = false;
                foreach (ReferencedFileSave existingRfs in ProjectManager.GlueProjectSave.GlobalFiles)
                {
                    if (existingRfs.Name.ToLower() == rfs.Name.ToLower())
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
