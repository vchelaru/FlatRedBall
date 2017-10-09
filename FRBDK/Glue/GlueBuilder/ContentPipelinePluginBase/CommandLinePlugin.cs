using OfficialPlugins.ContentPipelinePlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficialPlugins.MonoGameContent;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using EditorObjects.IoC;

namespace ContentPipelinePluginBase
{
    public class CommandLinePlugin
    {
        ControlViewModel viewModel;
        ContentPipelineController controller;
        AliasCodeGenerator aliasCodeGenerator;

        public CommandLinePlugin()
        {
            viewModel = new ControlViewModel();
            //viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            controller = new ContentPipelineController();

            aliasCodeGenerator = new AliasCodeGenerator();
            aliasCodeGenerator.Initialize(controller);
            // todo? 
            //CodeWriter.GlobalContentCodeGenerators.Add(aliasCodeGenerator);
        }

        private void HandleViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //var propertyName = e.PropertyName;

            //switch (propertyName)
            //{
            //    case nameof(ControlViewModel.UseContentPipelineOnPngs):
            //        // so handling file changes will probably do this but let's force it so we know it happens:
            //        aliasCodeGenerator.GenerateFileAliasLogicCode(controller.Settings.UseContentPipelineOnAllPngs);
            //        break;
            //}
        }

        public void HandleLoadedGlux()
        {
            viewModel.IsProjectLoaded = true;
            controller.LoadOrCreateSettings();
            viewModel.UseContentPipelineOnPngs = controller.Settings.UseContentPipelineOnAllPngs;

            BuildAndGenerate();
        }

        public void BuildAndGenerate()
        {
            if (viewModel.UseContentPipelineOnPngs)
            {
                aliasCodeGenerator.GenerateFileAliasLogicCode(controller.Settings.UseContentPipelineOnAllPngs);
            }
            BuildLogic.Self.RefreshBuiltFilesFor(Container.Get<IGlueState>().CurrentMainProject, viewModel.UseContentPipelineOnPngs);
        }
    }
}
