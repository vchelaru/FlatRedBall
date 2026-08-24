using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using GlueFormsCore.Controls;
using Microsoft.Build.Evaluation;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAboutPlugin : EmbeddedPlugin
    {
        PluginTab tab;
        AboutViewModel aboutViewModel;
        bool hasCheckedSourceStatus;

        public override void StartUp()
        {
            this.AddMenuItemTo("About", Localization.MenuIds.AboutId, HandleAboutClicked, Localization.MenuIds.HelpId);

            this.ReactToLoadedGlux += () =>
            {
                if(aboutViewModel != null)
                {
                    RefreshAboutViewModel(GlueState.Self.CurrentGlueProject);
                }
            };
            this.ReactToUnloadedGlux += () =>
            {
                if (aboutViewModel != null)
                {
                    // When a Glue project is unloaded, GlueState.Self.CurrentGlueProject is still
                    // a valid reference so we have to pass an explicit null.
                    RefreshAboutViewModel(glueProject:null);
                }
            };
        }

        private void HandleAboutClicked(object sender, EventArgs e)
        {
            if (tab == null)
            {
                aboutViewModel = new AboutViewModel();


                var view = new AboutControl();
                view.DataContext = aboutViewModel;
                tab = CreateTab(view, "About");
            }
            RefreshAboutViewModel(GlueState.Self.CurrentGlueProject);
            CheckSourceStatusIfFirstShow();

            tab.Show();
            tab.Focus();
        }

        private void CheckSourceStatusIfFirstShow()
        {
            if (hasCheckedSourceStatus)
            {
                return;
            }

            var mainProject = GlueState.Self.CurrentMainProject;

            string frbRoot = null;
            string gumRoot = null;

            if (mainProject is Frb2Project frb2Project)
            {
                SourceRepoLocator.TryGetFrb2SourceRoot(frb2Project, out frbRoot);
            }
            else if (mainProject != null)
            {
                SourceRepoLocator.TryGetFrb1SourceRoots(mainProject, out frbRoot, out gumRoot);
            }

            if (frbRoot == null)
            {
                // No project loaded yet, or not linked to source - nothing to check yet. Leave
                // hasCheckedSourceStatus false so this runs again next time the tab is shown.
                return;
            }

            hasCheckedSourceStatus = true;
            aboutViewModel.InitializeSourceStatus(frbRoot, gumRoot);
        }

        private void RefreshAboutViewModel(GlueProjectSave glueProject)
        {

            // update view model
            aboutViewModel.CopyrightText = "FlatRedBall " + DateTime.Now.Year;
            // ProductVersion can include a + at the end like:
            // 2022.06.27.675+05e1a322330656d5225ed141495bb391916ec600
            if (Application.ProductVersion.Contains("+"))
            {
                aboutViewModel.Version = Version.Parse(Application.ProductVersion.Substring(0, Application.ProductVersion.IndexOf("+")));
            }
            else
            {
                aboutViewModel.Version = Version.Parse(Application.ProductVersion);
            }
            aboutViewModel.RefreshVersionInfo();
            aboutViewModel.RefreshDailyBuildInfo();

            if (glueProject == null)
            {
                aboutViewModel.GluxVersionText = "<No Project Loaded>";
                aboutViewModel.MainProjectTypeText = "<No Project Loaded>";
            }
            else
            {
                aboutViewModel.GluxVersionText = glueProject.FileVersion.ToString();
                aboutViewModel.MainProjectTypeText = GlueState.Self.CurrentMainProject?.GetType().Name;
            }

            aboutViewModel.DllSyntaxVersion = GlueState.Self.EngineDllSyntaxVersion;
            aboutViewModel.IsUsingSource = GlueState.Self.IsReferencingFrbSource;
        }
    }
}
