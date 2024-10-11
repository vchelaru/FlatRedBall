using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueFormsCore.Controls;
using Microsoft.Build.Evaluation;
using Mono.Cecil;
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

            aboutViewModel.DllSyntaxVersion = GetDllSyntaxSupportedVersion();
        }

        private static int? GetDllSyntaxSupportedVersion()
        {
            // for now we'll use the main project, but eventually we may want to include synced projects too:
            var project = GlueState.Self.CurrentMainProject;
            var referenceItems = project.EvaluatedItems.Where(item =>
            {
                return item.ItemType == "PackageReference" && item.EvaluatedInclude.StartsWith("FlatRedBall");
            });

            foreach(var item in referenceItems)
            {
                var path = GetFilePathFor(item);

                if (path != null)
                {
                    var module = ModuleDefinition.ReadModule(path.FullPath);
                    var frbServicesType = module.Types.FirstOrDefault(item => item.FullName == "FlatRedBall.FlatRedBallServices");
                    foreach(var attribute in frbServicesType.CustomAttributes)
                    {
                        if(attribute.AttributeType.Name == "SyntaxVersionAttribute" && attribute.Fields.Count > 0)
                        {
                            var version = int.Parse( attribute.Fields[0].Argument.Value.ToString());
                            return version;
                        }
                    }
                }

            }
            return null;
        }

        private static FilePath GetFilePathFor(ProjectItem item)
        {
            string packageName = item.EvaluatedInclude;
            string packageVersion = item.Metadata.FirstOrDefault(item => item.Name == "Version")?.EvaluatedValue;

            var userName = System.Environment.UserName;


            if (userName != null)
            {
                string[] searchPaths = {
                    @"C:\Program Files\dotnet\packs",
                    $@"C:\Users\{userName}\.nuget\packages"
                };

                foreach (string path in searchPaths)
                {
                    string fullPath = System.IO.Path.Combine(path, $"{packageName}",$"{packageVersion}", $"{packageName}.{packageVersion}.nupkg");
                    if (System.IO.File.Exists(fullPath))
                    {
                        var directory = FileManager.GetDirectory(fullPath);
                        // find a .dll with matching file
                        var allFiles = FlatRedBall.IO.FileManager.GetAllFilesInDirectory(directory, "dll");
                        foreach (var file in allFiles)
                        {
                            if (file.Contains($"{packageName}.dll"))
                            {
                                return file;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
