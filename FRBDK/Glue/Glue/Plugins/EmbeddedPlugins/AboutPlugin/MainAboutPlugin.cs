using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
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
        }

        private void HandleAboutClicked(object sender, EventArgs e)
        {
            if(tab == null)
            {
                aboutViewModel = new AboutViewModel();


                var view = new AboutControl();
                view.DataContext = aboutViewModel;
                tab = CreateTab(view, "About");
            }

            // update view model
            aboutViewModel.CopyrightText = "FlatRedBall " + DateTime.Now.Year;
            aboutViewModel.Version = Version.Parse( Application.ProductVersion);
            aboutViewModel.RefreshVersionInfo();
            var glueProject = GlueState.Self.CurrentGlueProject;

            if(glueProject == null)
            {
                aboutViewModel.GluxVersionText = "<No Project Loaded>";
                aboutViewModel.MainProjectTypeText = "<No Project Loaded>";
            }
            else
            {
                aboutViewModel.GluxVersionText = glueProject.FileVersion.ToString();
                aboutViewModel.MainProjectTypeText = GlueState.Self.CurrentMainProject?.GetType().Name;

            }

            tab.Show();
            tab.Focus();




        }
    }
}
