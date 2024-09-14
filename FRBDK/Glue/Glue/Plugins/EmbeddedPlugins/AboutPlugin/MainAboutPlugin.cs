using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Controls;
using PropertyTools.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Windows.Forms;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AboutPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAboutPlugin : EmbeddedPlugin
    {
        PluginTab tab;
        AboutViewModel aboutViewModel;

        public override void StartUp()
        {
            this.AddMenuItemTo(Localization.Texts.About, Localization.MenuIds.AboutId, HandleAboutClicked, Localization.MenuIds.HelpId);

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

            tab.Show();
            tab.Focus();
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
        }
    }
}
