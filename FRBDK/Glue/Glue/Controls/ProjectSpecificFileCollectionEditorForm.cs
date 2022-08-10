using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.ViewModels;
using System.Collections.Generic;

namespace FlatRedBall.Glue.Controls
{
    public partial class ProjectSpecificFileCollectionEditorForm : Form
    {
        private List<ProjectSpecificFile> _value;

        public List<ProjectSpecificFile> Value
        {
            get { return _value; }
            set
            {
                _value = new List<ProjectSpecificFile>();

                foreach (var item in value)
                {
                    _value.Add((ProjectSpecificFile)item);
                }
            }

        }
        public ReferencedFileSave Rfs { get; set; }

        public ProjectSpecificFileCollectionEditorForm()
        {
            InitializeComponent();
        }

        private void ProjectSpecificFileCollectionEditorFormLoad(object sender, System.EventArgs e)
        {
            lbFiles.DataSource = Value;

            foreach (var syncedProject in ProjectManager.SyncedProjects)
            {
                cboProjectType.Items.Add(syncedProject.Name);
            }
        }

        private void LbFilesSelectedIndexChanged(object sender, System.EventArgs e)
        {
            btnRemove.Enabled = lbFiles.SelectedItem != null;
        }

        private void BtnRemoveClick(object sender, System.EventArgs e)
        {
            //var result = MessageBox.Show(@"Do you want to delete the file?", @"Delete File",
            //                             MessageBoxButtons.YesNoCancel);

            //if (result == DialogResult.Cancel)
            //    return;

            //var psf = ((ProjectSpecificFile)lbFiles.SelectedItem);

            //if (result == DialogResult.Yes)
            //{
            //    string fileToDelete = ProjectManager.MakeAbsolute(psf.FilePath);

            //    if (File.Exists(fileToDelete))
            //    {
            //        File.Delete(fileToDelete);
            //    }
            //}

            var item = (ProjectSpecificFile)lbFiles.SelectedItem;

            ProjectManager.RemoveItemFromProject(ProjectManager.GetProjectByName(item.ProjectName), item.File.FullPath);
            Value.Remove(item);
            Rfs.ProjectSpecificFiles = Value;
            UnreferencedFilesManager.Self.NeedsRefreshOfUnreferencedFiles = true;
            UnreferencedFilesManager.Self.ProcessRefreshOfUnreferencedFiles();
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

            GlueCommands.Self.ProjectCommands.SaveProjects();

            //ProjectManager.RefreshUnreferencedFiles();

            //bool shouldRefreshAgainst = false;

            //foreach (ProjectSpecificFile projectSpecificFile in ProjectManager.LastAddedUnreferencedFiles)
            //{
            //    if (File.Exists(ProjectManager.MakeAbsolute(projectSpecificFile.FilePath)))
            //    {
            //        result =
            //            System.Windows.Forms.MessageBox.Show(
            //                "The following file is no longer referenced by the project\n\n" +
            //                projectSpecificFile +
            //                "\n\nRemove and delete this file?", "Remove unreferenced file?", MessageBoxButtons.YesNo);

            //        if (result == DialogResult.Yes)
            //        {
            //            ProjectManager.GetProject(projectSpecificFile.ProjectId).ContentProject.RemoveItem(
            //                projectSpecificFile.FilePath);

            //            File.Delete(ProjectManager.MakeAbsolute(projectSpecificFile.FilePath));
            //            shouldRefreshAgainst = true;
            //        }
            //    }
            //}

            //if (shouldRefreshAgainst)
            //{
            //    ProjectManager.RefreshUnreferencedFiles();
            //}

            //GluxCommands.Self.SaveGlux();


            RefreshList();
        }

        private void BtnAddNewFileClick(object sender, System.EventArgs e)
        {

            if (!ProjectTypeIsValid()) return;

            var nfw = new CustomizableNewFileWindow();
            var viewModel = new AddNewFileViewModel();
            nfw.DataContext = viewModel;
            foreach (var ati in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if (!string.IsNullOrEmpty(ati.Extension) && !string.IsNullOrEmpty(ati.QualifiedSaveTypeName))
                {
                    nfw.AddOption(ati);
                }

                // special case .txt
                if (ati.Extension == "txt")
                {
                    nfw.AddOption(ati);
                }

            }

            // Also add CSV files
            //nfw.AddOption(new AssetTypeInfo("csv", "", null, "Spreadsheet (.csv)", "", ""));
            nfw.AddOption(AvailableAssetTypes.Self.AllAssetTypes.First(item => item.FriendlyName == "Spreadsheet (.csv)"));

            var dialogResult = nfw.ShowDialog();

            if (dialogResult == true)
            {
                var resultAssetTypeInfo = nfw.SelectedItem;

                string name = viewModel.FileName;

                string createdFile = PluginManager.CreateNewFile(resultAssetTypeInfo, false, FileManager.GetDirectoryKeepRelative(Rfs.Name), name);

                //var createdFile = resultAssetTypeInfo.CreateNewFile(FileManager.GetDirectoryKeepRelative(Rfs.Name) + name, "", make2D);
                createdFile = ProjectManager.MakeRelativeContent(createdFile);

                var psf = new ProjectSpecificFile
                {
                    ProjectName = cboProjectType.Text,
                    File = createdFile
                };

                Value.Add(psf);
                Rfs.ProjectSpecificFiles = Value;
                GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(Rfs);
                GlueCommands.Self.ProjectCommands.SaveProjects();
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();

                RefreshList();
            }
        }

        private bool ProjectTypeIsValid()
        {
            if (!ProjectManager.SyncedProjects.Any(syncedProject => cboProjectType.Text == syncedProject.Name))
            {
                MessageBox.Show(@"Must select a valid project type and not already have an entry.", @"Error");
                return false;
            }

            return true;
        }

        private void BtnAddExistingFileClick(object sender, System.EventArgs e)
        {
            if (!ProjectTypeIsValid()) return;

            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;


            

            string projectDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
            string directoryThatFileShouldBeRelativeTo = ProjectManager.ContentDirectory + FileManager.GetDirectoryKeepRelative(Rfs.Name);
            string fileToAdd;

            if (!FileManager.IsRelativeTo(openFileDialog.FileName, projectDirectory))
            {
                fileToAdd = directoryThatFileShouldBeRelativeTo + FileManager.RemovePath(openFileDialog.FileName);
                fileToAdd = FileManager.MakeRelative(fileToAdd, projectDirectory);
                FileHelper.RecursivelyCopyContentTo(openFileDialog.FileName,
                    FileManager.GetDirectory(openFileDialog.FileName),
                    directoryThatFileShouldBeRelativeTo);
            }

            else
            {
                fileToAdd = FileManager.MakeRelative(openFileDialog.FileName, ProjectManager.ContentProject.GetAbsoluteContentFolder());
            }

            var psf = new ProjectSpecificFile
                          {
                              ProjectName = cboProjectType.Text,
                              File = fileToAdd
                          };

            Value.Add(psf);
            Rfs.ProjectSpecificFiles = Value;
            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(Rfs);
            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            RefreshList();
        }

        private void RefreshList()
        {
            ((CurrencyManager)lbFiles.BindingContext[lbFiles.DataSource]).Refresh();
        }

        private void DoneButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public class ProjectSpecificFileCollectionEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context == null || context.Instance == null)
                return base.GetEditStyle(context);

            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null || context.Instance == null || provider == null)
                return value;

            var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

            ReferencedFileSave rfs = ((ReferencedFileSavePropertyGridDisplayer)context.Instance).Instance as ReferencedFileSave;

#if DEBUG
            if (rfs == null)
            {
                throw new Exception("The ReferencedFileSave being used by the UI for adding project specific files is null.  It shouldn't be");
            }
#endif

            var CollectionEditor = new ProjectSpecificFileCollectionEditorForm
            {
                Value = ((ProjectSpecificFileDisplayer)value).Instance as List<ProjectSpecificFile>,
                Rfs = ((ReferencedFileSavePropertyGridDisplayer)context.Instance).Instance as ReferencedFileSave
            };

            editorService.ShowDialog(CollectionEditor);

            return CollectionEditor.Value;

            //return base.EditValue(context, provider, value);
        }
    }
}
