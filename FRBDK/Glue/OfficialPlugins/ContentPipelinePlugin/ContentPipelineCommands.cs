using System;
using System.Threading.Tasks;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.VSHelpers.Projects;
using OfficialPlugins.ContentPipelinePlugin;

namespace OfficialPlugins.MonoGameContent
{
    /// <summary>The MonoGame Content plugin's <see cref="IContentPipelineCommands"/>.</summary>
    public class ContentPipelineCommands : IContentPipelineCommands
    {
        readonly Func<bool> useContentPipelineOnPngs;
        readonly ContentPipelineController controller;

        /// <param name="useContentPipelineOnPngs">The plugin's current setting, read at each call.</param>
        public ContentPipelineCommands(Func<bool> useContentPipelineOnPngs, ContentPipelineController controller)
        {
            this.useContentPipelineOnPngs = useContentPipelineOnPngs;
            this.controller = controller;
        }

        public Task RefreshXnbsForProjectAsync(VisualStudioProject project) =>
            TaskManager.Self.AddAsync(async () =>
            {
                try
                {
                    await BuildLogic.Self.RefreshBuiltFilesFor(project, useContentPipelineOnPngs(), controller);
                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError($"MonoGame Content Plugin failed building .xnb files\n{e}");
                }
            }, "MonoGame Content Plugin: refresh .xnb files");
    }
}
