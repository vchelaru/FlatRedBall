using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.Controls
{
    public partial class NewObjectTypeSelectionControl : UserControl
    {
        #region Fields

        SearchBarHelper mSearchBarHelper;

        #endregion

        #region Events

        public event EventHandler AfterSelect;

        public event EventHandler AfterStrongSelect;
        #endregion

        #region Properties

        public IEnumerable<EntitySave> AvailableEntities
        {
            get
            {
                if (string.IsNullOrEmpty(this.SearchTextBox.Text))
                {
                    return ObjectFinder.Self.GlueProject.Entities;
                }
                else
                {
                    return ObjectFinder.Self.GlueProject.Entities.Where(
                        item => 
                            {
                                string name = item.Name.ToLower();
                                // All entities start with "Entities\\" so we want to remove
                                // that from the name so we don't get confusing matches
                                name = name.Substring("Entities\\".Length);
                                return name.Contains(SearchTextBox.Text.ToLower());
                            }
                            );

                }
            }
        }

        public SourceType SourceType
        {
            get
            {
                if(FlatRedBallTypeRadioButton.Checked)
                {
                    return SaveClasses.SourceType.FlatRedBallType;
                }
                else if (this.FromFileRadioButton.Checked)
                {
                    return SaveClasses.SourceType.File;
                }
                else
                {
                    return SaveClasses.SourceType.Entity;
                }
            }
            set
            {
                switch(value)
                {
                    case SourceType.FlatRedBallType:
                        FlatRedBallTypeRadioButton.Checked = true;
                        break;
                    case SourceType.File:
                        FromFileRadioButton.Checked = true;
                        break;
                    case SourceType.Entity:
                        this.EntityRadioButton.Checked = true;
                        break;
                    default:
                        throw new NotImplementedException($"Selection type {value} not supported.");
                }
            }
        }

        public string SourceFile
        {
            get
            {
                if (SourceType == SaveClasses.SourceType.File)
                {
                    if (FilesTreeView.SelectedNode != null)
                    {
                        return FilesTreeView.SelectedNode.Text;
                    }
                }
                return "";
            }
            set
            {
                FilesTreeView.SelectedNode = FilesTreeView.Nodes.FirstOrDefault(item => item.Text == value);
            }

        }

        public string SourceClassType
        {
            get
            {
                if (SourceType == SaveClasses.SourceType.FlatRedBallType)
                {
                    if (FlatRedBallTypesTreeView.SelectedNode != null)
                    {
                        return FlatRedBallTypesTreeView.SelectedNode.Text;
                    }
                }
                else if (SourceType == SaveClasses.SourceType.File)
                {
                    return "";
                }
                else
                {
                    if (EntitiesTreeView.SelectedNode != null)
                    {
                        return (EntitiesTreeView.SelectedNode.Tag as EntitySave).Name;
                    }
                }

                return "";
            }
        }

        public string SourceClassGenericType
        {
            get
            {
                if (IsGenericType())
                {
                    return this.GenericTypeComboBox.SelectedItem as string;
                }
                else
                {
                    return null;
                }

            }
        }

        public string SourceName
        {
            get
            {
                return this.SourceNameComboBox.Text;
            }
        }

        #endregion


        public NewObjectTypeSelectionControl()
        {

            InitializeComponent();
            PopulateAllTreeViews();
            RefreshComboBoxVisibility();

            this.FlatRedBallTypeRadioButton.Checked = true;

            SearchBarHelper.Initialize(SearchTextBox, "Type here to filter options...");
        }

        private void EntitiesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (AfterSelect != null)
            {
                AfterSelect(this, e);
            }
        }

        private void FlatRedBallTypesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // See if this thing uses generic types

            RefreshComboBoxVisibility();
            PopulateGenericTypes();
            if (AfterSelect != null)
            {
                AfterSelect(this, e);
            }
        }

        private void RefreshComboBoxVisibility()
        {
            bool usesGenericTypes = IsGenericType();

            GenericTypeComboBox.Visible = usesGenericTypes;
            ListTypeLabel.Visible = usesGenericTypes;

            SourceNameLabel.Visible = SourceType == SaveClasses.SourceType.File;
            SourceNameComboBox.Visible = SourceType == SaveClasses.SourceType.File;
        }

        private bool IsGenericType()
        {
            bool usesGenericTypes = this.SourceType == SaveClasses.SourceType.FlatRedBallType &&
                SourceClassType == "PositionedObjectList<T>";
            return usesGenericTypes;
        }

        private void PopulateGenericTypes()
        {
            if (ObjectFinder.Self.GlueProject != null)
            {
                this.GenericTypeComboBox.Items.Clear();

                AvailableClassGenericTypeConverter converter = new AvailableClassGenericTypeConverter();

                foreach (var value in converter.GetAvailableValues(false))
                {
                    this.GenericTypeComboBox.Items.Add(value);

                }
            }
        }

        private void FilesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.SourceNameComboBox.Items.Clear();

            //AvailableNameablesStringConverter availabl
            List<string> availableObjects = new List<string>();

            AvailableNameablesStringConverter.FillListWithAvailableObjects(FilesTreeView.SelectedNode.Text, availableObjects);

            SourceNameComboBox.Items.AddRange(availableObjects.ToArray());

            if (AfterSelect != null)
            {
                AfterSelect(this, e);
            }
        }

        void PopulateAllTreeViews()
        {
            PopulateFlatRedBallAndCustomTypes();

            PopulateEntities();

            PopulateFiles();
        }

        private void PopulateFiles()
        {


            if (ObjectFinder.Self.GlueProject != null && GlueState.Self.CurrentElement != null)
            {
                FilesTreeView.Nodes.Clear();

                IEnumerable<ReferencedFileSave> files = GlueState.Self.CurrentElement.ReferencedFiles;

                if (!string.IsNullOrEmpty(this.SearchTextBox.Text))
                {
                    files = files.Where(item => item.Name.ToLowerInvariant().Contains(SearchTextBox.Text.ToLowerInvariant()));
                }

                foreach (var file in files)
                {
                    TreeNode treeNode = new TreeNode(file.Name);
                    treeNode.Tag = file;
                    this.FilesTreeView.Nodes.Add(treeNode);
                }
            }
        }


        private void PopulateEntities()
        {
            if(ObjectFinder.Self.GlueProject != null)
            {
                EntitiesTreeView.Nodes.Clear();

                foreach (EntitySave entitySave in AvailableEntities)
                {
                    // Eventually we may want this to be embedded in folders
                    // but we'll do a flat view for now as an intial implementation.
                    TreeNode treeNode = new TreeNode(entitySave.Name.Substring("Entities/".Length));
                    treeNode.Tag = entitySave;
                    EntitiesTreeView.Nodes.Add(treeNode);
                }
            }
        }

        private void PopulateFlatRedBallAndCustomTypes()
        {
            List<string> addedTypes = new List<string>();
            addedTypes.AddRange(AvailableClassTypeConverter.GetAvailableTypes(false, SourceType.FlatRedBallType));
            addedTypes.Sort();

            IEnumerable<string> availableTypes = addedTypes;

            if (!string.IsNullOrEmpty(this.SearchTextBox.Text))
            {
                availableTypes = availableTypes.Where(item => item.ToLower().Contains(SearchTextBox.Text.ToLower()));
            }
            
            FlatRedBallTypesTreeView.Nodes.Clear();

            foreach (var available in availableTypes)
            {
                FlatRedBallTypesTreeView.Nodes.Add(available);
            }
        }

        private void FlatRedBallTypeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            this.SearchTextBox.Text = "";

            this.EntitiesTreeView.Visible = false;
            this.FilesTreeView.Visible = false;
            this.FlatRedBallTypesTreeView.Visible = true;

            RefreshComboBoxVisibility();
        }

        private void EntityRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            this.SearchTextBox.Text = "";

            this.FilesTreeView.Visible = false;
            this.EntitiesTreeView.Visible = true;
            this.FlatRedBallTypesTreeView.Visible = false;

            RefreshComboBoxVisibility();

        }

        private void FromFileRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            this.SearchTextBox.Text = "";

            this.FilesTreeView.Visible = true;
            this.EntitiesTreeView.Visible = false;
            this.FlatRedBallTypesTreeView.Visible = false;

            RefreshComboBoxVisibility();

        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (FromFileRadioButton.Checked)
            {
                PopulateFiles();
            }
            else if (EntityRadioButton.Checked)
            {
                PopulateEntities();
            }
            else if (FlatRedBallTypeRadioButton.Checked)
            {
                PopulateFlatRedBallAndCustomTypes();
            }
            else
            {
                // Used to throw an exception here, but it's possible nothing is selected
                //throw new NotImplementedException();
            }
        }

        private void FlatRedBallTypesTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (FlatRedBallTypesTreeView.SelectedNode != null && AfterStrongSelect != null)
            {
                AfterStrongSelect(this, null);
            }

        }

        private void EntitiesTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (EntitiesTreeView.SelectedNode != null && AfterStrongSelect != null)
            {
                AfterStrongSelect(this, null);
            }
        }

        private void FilesTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (FilesTreeView.SelectedNode != null && AfterStrongSelect != null)
            {
                AfterStrongSelect(this, null);
            }
        }

        private void SourceNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AfterSelect != null)
            {
                AfterSelect(this, e);
            }
        }
    }
}
