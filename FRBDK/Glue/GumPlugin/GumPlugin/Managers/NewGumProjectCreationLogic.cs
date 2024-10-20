using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.DataGeneration;
using GumPlugin.ViewModels;

namespace GumPlugin.Managers
{
    public class NewGumProjectCreationLogic
    {
        GumxPropertiesManager _gumxPropertiesManager;
        public NewGumProjectCreationLogic(GumxPropertiesManager gumxPropertiesManager)
        {
            _gumxPropertiesManager = gumxPropertiesManager;
        }

        public async Task AskToCreateGumProject()
        {
            var mbmb = new MultiButtonMessageBoxWpf();
            mbmb.AddButton(Localization.Texts.GumIncludeRecommendedFormsControls, true);
            mbmb.AddButton(Localization.Texts.GumNoForms, false);
            mbmb.MessageText = Localization.Texts.GumAddFRBForms;
            var showDialogResult = mbmb.ShowDialog();

            if (showDialogResult == true)
            {
                var shouldAlsoAddForms = (bool)mbmb.ClickedResult;
                await CreateGumProjectInternal(shouldAlsoAddForms, askToOverwrite:true);
            }
        }

        public async Task CreateGumProjectInternal(bool shouldAlsoAddForms, bool askToOverwrite)
        {
            var assembly = typeof(FormsControlAdder).Assembly;
            var shouldSave = true;
            if (askToOverwrite)
            {
                shouldSave = FormsControlAdder.AskToSaveIfOverwriting(assembly);
            }

            if (GlueState.Self.CurrentGlueProject == null)
            {
                MessageBox.Show("You must first create a FlatRedBall project before adding a Gum project");
                shouldSave = false;
            }

            else if (GumProjectManager.Self.GetIsGumProjectAlreadyInGlueProject())
            {
                MessageBox.Show("A Gum project already exists");
                shouldSave = false;
            }

            if (shouldSave)
            {

                await TaskManager.Self.AddAsync(async () =>
                {
                    _gumxPropertiesManager.IsReactingToProperyChanges = false;
                    GumProjectManager.Self.AddNewGumProject();

                    var gumRfs = GumProjectManager.Self.GetRfsForGumProject();

                    var behavior = MainGumPlugin.GetBehavior(gumRfs);
                    EmbeddedResourceManager.Self.UpdateCodeInProjectPresence(behavior);


                    // When we first add the RFS to Glue, the RFS tries to refresh its file cache.
                    // But since the .glux hasn't yet been assigned as the currently-loaded project, 
                    // the Gum plugin doesn't track its references and returns an empty list. That empty
                    // list return is then cached, and future calls will always treat the .gumx as having 
                    // no referenced files. Now that we've assigned the custom project, clear the cache so
                    // it can properly be set up.
                    GlueCommands.Self.FileCommands.ClearFileCache(GlueCommands.Self.GetAbsoluteFilePath(gumRfs));
                    GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(gumRfs);



                    if (shouldAlsoAddForms == true)
                    {
                        // add forms:

                        //viewModel.IncludeFormsInComponents = true;
                        gumRfs.SetProperty(nameof(GumViewModel.IncludeFormsInComponents), true);
                        //viewModel.IncludeComponentToFormsAssociation = true;
                        gumRfs.SetProperty(nameof(GumViewModel.IncludeComponentToFormsAssociation), true);

                        await FormsControlAdder.SaveElements(assembly);
                        await FormsControlAdder.SaveBehaviors(assembly);

                        await MainGumPlugin.HandleBuildMissingFonts();

                    }
                    GlueCommands.Self.GluxCommands.SaveProjectAndElements();

                    await CodeGeneratorManager.Self.GenerateDerivedGueRuntimesAsync(forceReload: true);

                    _gumxPropertiesManager.IsReactingToProperyChanges = true;
                }, "Creating Gum Project");





            }
        }

    }
}
