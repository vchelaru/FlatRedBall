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
using System.Linq;
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
            var File = AddTopLevelMenuItem(Localization.Texts.File, Localization.MenuIds.FileId);
            {
                File.Add(Localization.Texts.ProjectNew, GlueCommands.Self.ProjectCommands.CreateNewProject);
                File.Add(Localization.Texts.ProjectLoad, GlueCommands.Self.DialogCommands.ShowLoadProjectDialog);
                File.Add(Localization.Texts.ProjectClose, () => GlueCommands.Self.CloseGlueProject());
            }

            var Edit = AddTopLevelMenuItem(Localization.Texts.Edit, Localization.MenuIds.EditId);
            {
                Edit.Add(Localization.Texts.FindFileReferences, findFileReferencesToolStripMenuItem_Click);
            }

            var Project = AddTopLevelMenuItem(Localization.Texts.Project, Localization.MenuIds.ProjectId);
            {
                Project.Add(Localization.Texts.ErrorCheck, RightClickHelper.ErrorCheckClick);
                Project.DropDownItems.Add(new ToolStripSeparator());
                Project.Add(Localization.Texts.GroupExport, exportToolStripMenuItem_Click);
                Project.Add(Localization.Texts.GroupImport, ElementImporter.AskAndImportGroup);
            }

            var Content = AddTopLevelMenuItem(Localization.Texts.Content, Localization.MenuIds.ContentId);
            {
                var AdditionalContent = Content.Add(Localization.Texts.ContentAdditional, (Action)null);
                {
                    var ViewAdditionalContent = AdditionalContent.Add(Localization.Texts.ContentAdditionalViewTypes, (Action)null);
                    {
                        ViewAdditionalContent.Add(Localization.Texts.ProjectForAll, forAllProjectsToolStripMenuItem_Click);
                        ViewAdditionalContent.Add(Localization.Texts.ProjectThisOnly, forThisProjectOnlyToolStripMenuItem_Click);
                    }
                    AdditionalContent.Add(Localization.Texts.NewContentCSV, newContentCSVToolStripMenuItem_Click);
                    AdditionalContent.Add(Localization.Texts.ViewNewFileTemplateFolder, viewNewFileTemplateFolderToolStripMenuItem_Click);
                }
                Content.DropDownItems.Add(new ToolStripSeparator());

            }
            // cotinue here

            var Settings = AddTopLevelMenuItem(Localization.Texts.Settings, Localization.MenuIds.SettingsId);
            {
                Settings.Add(
                    Localization.Texts.FileAssociations,
                    () => new FileAssociationWindow().ShowDialog(MainGlueWindow.Self));

                Settings.Add(
                    Localization.Texts.FileBuildTools,
                    () => new FileBuildToolAssociationWindow(GlueState.Self.GlueSettingsSave.BuildToolAssociations).Show(MainGlueWindow.Self));

                Settings.Add(
                    Localization.Texts.PerformanceSettings,
                    () => new PerformanceSettingsWindow().ShowDialog(MainGlueWindow.Self));

                Settings.Add(
                    Localization.Texts.Preferences,
                    () => new PreferencesWindow().Show());

                Settings.DropDownItems.Add(new ToolStripSeparator());

                Settings.Add(
                    Localization.Texts.CustomGameClass,
                    customGameClassToolStripMenuItem_Click);
            }


            var Update = AddTopLevelMenuItem(Localization.Texts.Update,  Localization.MenuIds.UpdateId);

            var Plugins = AddTopLevelMenuItem(Localization.Texts.Plugins, Localization.MenuIds.PluginId);
            {
                Plugins.Add(Localization.Texts.PluginInstall, () => new InstallPluginWindow().Show(MainGlueWindow.Self));
                Plugins.Add(Localization.Texts.PluginUninstall, () => new UninstallPluginWindow().Show(MainGlueWindow.Self));
                Plugins.Add(Localization.Texts.PluginCreate, () => new CreatePluginWindow().Show(MainGlueWindow.Self));
                Plugins.DropDownItems.Add(new ToolStripSeparator());
            }

            // No one uses experimental, so get rid of it...
            //var Experimental = AddTopLevelMenuItem("Experimental");

            var Help = AddTopLevelMenuItem(Localization.Texts.Help, Localization.MenuIds.HelpId);
            {
                Help.Add(Localization.Texts.Tutorials, () => OpenInBrowser("http://flatredball.com/documentation/tutorials/"));
                Help.Add(Localization.Texts.ReportABug, () => OpenInBrowser("https://github.com/vchelaru/flatredball/issues"));
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
                var message = String.Format(Localization.Texts.ErrorCannotOpenBrowser, url);

                GlueCommands.Self.DialogCommands.ShowMessageBox(message);
            }
        }

        private void findFileReferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var tiw = new TextInputWindow();
            tiw.Message = Localization.Texts.EnterFileWithExtension;



            if (tiw.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                List<ReferencedFileSave> matchingReferencedFileSaves = new List<ReferencedFileSave>();
                List<string> matchingRegularFiles = new List<string>();

                List<ReferencedFileSave> allReferencedFiles = ObjectFinder.Self.GetAllReferencedFiles();

                foreach (ReferencedFileSave rfs in allReferencedFiles)
                {
                    if (String.Equals(FileManager.RemovePath(rfs.Name), tiw.Result, StringComparison.OrdinalIgnoreCase))
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
                            ErrorReporter.ReportError(absoluteFileName, String.Format(Localization.Texts.AttemptFindFileReferences,fnfe.FileName), true);
                        }

                        if (referencedFiles == null) continue;

                        matchingRegularFiles.AddRange(
                            from referencedFile 
                            in referencedFiles 
                            where String.Equals(tiw.Result, referencedFile.NoPath, StringComparison.OrdinalIgnoreCase) 
                            select referencedFile + " in " + rfs + "\n"
                        );
                    }
                }

                if (matchingReferencedFileSaves.Count == 0 && matchingRegularFiles.Count == 0)
                {
                    MessageBox.Show(String.Format(Localization.Texts.NoFilesReferencing, tiw.Result), Localization.Texts.NoFilesFound);
                }
                else
                {
                    string message = $"{Localization.Texts.FoundTheFollowing}\n\n";

                    foreach (string s in matchingRegularFiles)
                    {
                        message += s + "\n";
                    }

                    foreach (ReferencedFileSave rfs in matchingReferencedFileSaves)
                    {
                        message += rfs + "\n";
                    }
                    MessageBox.Show(message, Localization.Texts.FoundFiles);
                }
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GroupExportForm groupExportForm = new GroupExportForm();
            DialogResult result = groupExportForm.ShowDialog();

            if (result == DialogResult.OK)
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
            tiw.DisplayText = Localization.Texts.EnterNewCSVName;

            var comboBox = new ComboBox();

            // project-specific CSVs are always named ProjectSpecificContent.csv
            comboBox.Items.Add(Localization.Texts.ProjectForAll);
            comboBox.Items.Add(Localization.Texts.ProjectThisOnly);
            // May 11 2023 - probably want to default to this project
            comboBox.Text = Localization.Texts.ProjectThisOnly;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.Width = 136;
            tiw.AddControl(comboBox);

            DialogResult result = tiw.ShowDialog();

            // CSVs can be added to be project-specific or shared across all projects (installed to a centralized location)

            if (result == DialogResult.OK)
            {
                string textResult = tiw.Result;
                if (textResult.ToLower().EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    textResult = FileManager.RemoveExtension(textResult);
                }

                GlobalOrProjectSpecific globalOrProjectSpecific;

                if (comboBox.SelectedItem is string asString && asString == Localization.Texts.ProjectForAll)
                {
                    globalOrProjectSpecific = GlobalOrProjectSpecific.Global;
                }
                else
                {
                    globalOrProjectSpecific = GlobalOrProjectSpecific.ProjectSpecific;
                }

                AvailableAssetTypes.Self.CreateAdditionalCsvFile(textResult, globalOrProjectSpecific);

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
                GlueCommands.Self.FileCommands.ViewInExplorer(whatToView);
            }
            else
            {
                MessageBox.Show(String.Format(Localization.Texts.ErrorCouldNotOpen, whatToView));
            }
        }

        private void customGameClassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ProjectManager.GlueProjectSave == null)
            {
                MessageBox.Show(Localization.Texts.NoLoadedGlueProject);
            }
            else
            {
                var tiw = new CustomizableTextInputWindow();
                tiw.Message = Localization.Texts.EnterCustomClassDeleteContents;
                tiw.Result = ProjectManager.GlueProjectSave.CustomGameClass;

                var result = tiw.ShowDialog();


                if (result == true)
                {
                    ProjectManager.GlueProjectSave.CustomGameClass = tiw.Result;
                    GluxCommands.Self.SaveGlux();

                    ProjectManager.FindGameClass();

                    if (string.IsNullOrEmpty(ProjectManager.GameClassFileName))
                    {
                        MessageBox.Show(Localization.Texts.ErrorCouldntFindGameClass);
                    }
                    else
                    {
                        MessageBox.Show(String.Format(Localization.Texts.GameClassFound, ProjectManager.GameClassFileName));
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

            if (action != null)
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
