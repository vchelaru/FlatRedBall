using EditorObjects.Parsing;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;
using GlueFormsCore.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.MenuStripPlugin
{

    [Export(typeof(PluginBase))]
    public class MainMenuStripPlugin : EmbeddedPlugin
    {
        public MainMenuStripPlugin() : base()
        {
            DesiredOrder = DesiredOrder.Critical;
        }
        public override void StartUp()
        {
            var File = AddTopLevelMenuItem("File");
            {
                File.Add("New Project", GlueCommands.Self.ProjectCommands.CreateNewProject);
                File.Add("Load Project...", GlueCommands.Self.DialogCommands.ShowLoadProjectDialog);
                File.Add("Close Project", () => GlueCommands.Self.CloseGlueProject());
            }

            var Edit = AddTopLevelMenuItem("Edit");
            {
                Edit.Add("Find file references...", findFileReferencesToolStripMenuItem_Click);
            }

            var Project = AddTopLevelMenuItem("Project");
            {
                Project.Add("Error Check", RightClickHelper.ErrorCheckClick);
                Project.DropDownItems.Add(new ToolStripSeparator());
                Project.Add("Export Group...", exportToolStripMenuItem_Click);
                Project.Add("Import Group...", ElementImporter.AskAndImportGroup);
            }

            var Content = AddTopLevelMenuItem("Content");
            {
                var AdditionalContent = Content.Add("Additional Content", (Action)null) ;
                {
                    var ViewAdditionalContent = AdditionalContent.Add("View Additional Content Types", (Action)null);
                    {
                        ViewAdditionalContent.Add("For All Projects", forAllProjectsToolStripMenuItem_Click);
                        ViewAdditionalContent.Add("For This Project Only", forThisProjectOnlyToolStripMenuItem_Click);
                    }
                    AdditionalContent.Add("New Content CSV...", newContentCSVToolStripMenuItem_Click);
                    AdditionalContent.Add("View New File Template Folder", viewNewFileTemplateFolderToolStripMenuItem_Click);
                }
                Content.DropDownItems.Add(new ToolStripSeparator());

            }
            // cotinue here

            var Settings = AddTopLevelMenuItem("Settings");
            {
                Settings.Add(
                    "File Associations", 
                    () => new FileAssociationWindow().ShowDialog(MainGlueWindow.Self));

                Settings.Add(
                    "File Build Tools",
                    () => new FileBuildToolAssociationWindow(GlueState.Self.GlueSettingsSave.BuildToolAssociations).Show(MainGlueWindow.Self));

                Settings.Add(
                    "Performance Settings",
                    () => new PerformanceSettingsWindow().ShowDialog(MainGlueWindow.Self));

                Settings.Add(
                    "Preferences",
                    () => new PreferencesWindow().Show());

                Settings.DropDownItems.Add(new ToolStripSeparator());

                Settings.Add(
                    "Custom Game Class",
                    customGameClassToolStripMenuItem_Click);
            }


            var Update = AddTopLevelMenuItem("Update");

            var Plugins = AddTopLevelMenuItem("Plugins");
            {
                Plugins.Add("Install Plugin", () => new InstallPluginWindow().Show(MainGlueWindow.Self));
                Plugins.Add("Uninstall Plugin", () => new UninstallPluginWindow().Show(MainGlueWindow.Self));
                Plugins.Add("Create Plugin", () => new CreatePluginWindow().Show(MainGlueWindow.Self));
                Plugins.DropDownItems.Add(new ToolStripSeparator());
            }

            // No one uses experimental, so get rid of it...
            //var Experimental = AddTopLevelMenuItem("Experimental");

            var Help = AddTopLevelMenuItem("Help");
            {
                Help.Add("Tutorials", () => OpenInBrowser("http://flatredball.com/documentation/tutorials/"));
                Help.Add("Report a Bug", () => OpenInBrowser("https://github.com/vchelaru/flatredball/issues"));
            }
        }


        private void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                var message = 
                    $"Could not open a browser to the URL:\n\n{url}\n\nTry entering the address in a browser manually.";

                GlueCommands.Self.DialogCommands.ShowMessageBox(message);
            }
        }

        private void findFileReferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();

            tiw.Message = "Enter the file name with extension, but no path (for example \"myfile.png\")";



            if (tiw.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                List<ReferencedFileSave> matchingReferencedFileSaves = new List<ReferencedFileSave>();
                List<string> matchingRegularFiles = new List<string>();

                string result = tiw.Result.ToLower();

                List<ReferencedFileSave> allReferencedFiles = ObjectFinder.Self.GetAllReferencedFiles();

                foreach (ReferencedFileSave rfs in allReferencedFiles)
                {
                    if (FileManager.RemovePath(rfs.Name.ToLower()) == result)
                    {
                        matchingReferencedFileSaves.Add(rfs);
                    }

                    string absoluteFileName = GlueCommands.Self.GetAbsoluteFileName(rfs);

                    if (File.Exists(absoluteFileName))
                    {
                        List<FilePath> referencedFiles = null;

                        try
                        {
                            referencedFiles = FileReferenceManager.Self.GetFilesReferencedBy(absoluteFileName, TopLevelOrRecursive.Recursive);
                        }
                        catch (FileNotFoundException fnfe)
                        {
                            ErrorReporter.ReportError(absoluteFileName, "Trying to find file references, but could not find contained file " + fnfe.FileName, true);
                        }

                        if (referencedFiles != null)
                        {
                            foreach (var referencedFile in referencedFiles)
                            {
                                if (result == referencedFile.NoPath.ToLower())
                                {
                                    matchingRegularFiles.Add(referencedFile + " in " + rfs.ToString() + "\n");
                                }
                            }
                        }
                    }
                }

                if (matchingReferencedFileSaves.Count == 0 && matchingRegularFiles.Count == 0)
                {
                    MessageBox.Show("There are no files referencing " + result, "No files found");
                }
                else
                {
                    string message = "Found the following:\n\n";

                    foreach (string s in matchingRegularFiles)
                    {
                        message += s + "\n";
                    }

                    foreach (ReferencedFileSave rfs in matchingReferencedFileSaves)
                    {
                        message += rfs.ToString() + "\n";
                    }
                    MessageBox.Show(message, "Files found");
                }



            }

        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GroupExportForm groupExportForm = new GroupExportForm();
            DialogResult result = groupExportForm.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ElementExporter.ExportGroup(groupExportForm.SelectedElements, GlueState.Self.CurrentGlueProject);
            }
        }

        private void viewNewFileTemplateFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string directory = FlatRedBall.Glue.Plugins.EmbeddedPlugins.NewFiles.NewFilePlugin.CustomFileTemplateFolder;

            System.Diagnostics.Process.Start(directory);
        }

        private void newContentCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter new CSV name";

            ComboBox comboBox = new ComboBox();

            // project-specific CSVs are always named ProjectSpecificContent.csv
            //const string allProjects = "For all projects";
            //const string thisProjectOnly = "For this project only";

            //comboBox.Items.Add(allProjects);
            //comboBox.Text = allProjects;
            //comboBox.Items.Add(thisProjectOnly);
            //comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            //comboBox.Width = 136;
            //tiw.AddControl(comboBox);

            DialogResult result = tiw.ShowDialog();

            // CSVs can be added to be project-specific or shared across all projects (installed to a centralized location)

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string textResult = tiw.Result;
                if (textResult.ToLower().EndsWith(".csv"))
                {
                    textResult = FileManager.RemoveExtension(textResult);
                }

                GlobalOrProjectSpecific globalOrProjectSpecific;

                //if (comboBox.SelectedItem == allProjects)
                //{
                globalOrProjectSpecific = GlobalOrProjectSpecific.Global;
                //}
                //else
                //{
                //    globalOrProjectSpecific = GlobalOrProjectSpecific.ProjectSpecific;
                //}

                AvailableAssetTypes.Self.CreateAdditionalCsvFile(tiw.Result, globalOrProjectSpecific);

                ViewAdditionalContentTypes(globalOrProjectSpecific);
            }
        }

        private void forAllProjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ViewAdditionalContentTypes(GlobalOrProjectSpecific.Global);
        }

        private void forThisProjectOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ViewAdditionalContentTypes(GlobalOrProjectSpecific.ProjectSpecific);
        }

        private void ViewAdditionalContentTypes(GlobalOrProjectSpecific globalOrProjectSpecific)
        {
            string whatToView;
            if (globalOrProjectSpecific == GlobalOrProjectSpecific.Global)
            {
                whatToView = AvailableAssetTypes.Self.GlobalCustomContentTypesFolder;
            }
            else
            {
                whatToView = AvailableAssetTypes.Self.ProjectSpecificContentTypesFolder;
                // only do this if viewing project specific, as Glue probably can't access the folder where projects are shown
                Directory.CreateDirectory(whatToView);
            }

            if (System.IO.Directory.Exists(whatToView))
            {
                Process.Start(whatToView);
            }
            else
            {
                MessageBox.Show("Could not open " + whatToView);
            }
        }

        private void customGameClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ProjectManager.GlueProjectSave == null)
            {
                MessageBox.Show("There is no loaded Glue project");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.DisplayText = "Enter the custom class name.  Delete the contents to not use a custom class.";
                tiw.Result = ProjectManager.GlueProjectSave.CustomGameClass;

                DialogResult result = tiw.ShowDialog();


                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ProjectManager.GlueProjectSave.CustomGameClass = tiw.Result;
                    GluxCommands.Self.SaveGlux();

                    ProjectManager.FindGameClass();

                    if (string.IsNullOrEmpty(ProjectManager.GameClassFileName))
                    {
                        MessageBox.Show("Couldn't find the game class.");
                    }
                    else
                    {
                        MessageBox.Show("Game class found:\n\n" + ProjectManager.GameClassFileName);
                    }
                }
            }

        }

    }

    static class ToolStripMenuItemExtensions
    {
        public static ToolStripMenuItem Add(this ToolStripMenuItem menu, string text, Action action)
        {
            var newItem = new ToolStripMenuItem(text);

            if(action != null)
            {
                newItem.Click += (not, used) => action();
            }

            menu.DropDownItems.Add(newItem);
            return newItem;
        }

        public static ToolStripMenuItem Add(this ToolStripMenuItem menu, string text, EventHandler eventHandler)
        {
            var newItem = new ToolStripMenuItem(text);

            if (eventHandler != null)
            {
                newItem.Click += eventHandler;
            }

            menu.DropDownItems.Add(newItem);

            return newItem;
        }
    }
}
