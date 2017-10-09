using ContentPipelinePluginBase;
using EditorObjects.IoC;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GlueConsole
{
    internal class CommandLineCommandProcessor
    {
        static IGlueCommands GlueCommands => Container.Get<IGlueCommands>();
        static IGlueState GlueState => Container.Get<IGlueState>();

        internal static void Process(string line)
        {
            List<string> commandLineWords = line.Split(' ').ToList();

            string firstWord = commandLineWords[0];

            commandLineWords.RemoveAt(0);

            switch(firstWord.ToLowerInvariant())
            {
                case "loadglux":
                    PerformLoad(commandLineWords);
                    break;
                case "buildcontent":
                    PerformBuildContent();
                    break;
                default:
                    throw new Exception($"Unknown command:{firstWord}");
            }
        }
        private static void PerformLoad(List<string> commandLineWords)
        {
            var fileName = commandLineWords[0];

            GlueCommands.LoadProject(fileName);

            var currentProject = GlueState.CurrentGlueProject;
            GlueCommands.PrintOutput($"Successfully loaded {fileName} with {currentProject.Screens.Count} screens and {currentProject.Entities.Count} entities");
        }

        private static void PerformBuildContent()
        {
            if(GlueState.CurrentGlueProject == null)
            {
                throw new InvalidOperationException("A Glue project must be loaded before building content");
            }

            // for now, we just create and use the plugin. Eventually this may get loaded dynamically:
            var plugin = new CommandLinePlugin();

            GlueCommands.PrintOutput("Building monogame content and generating code...");

            plugin.HandleLoadedGlux();

            TaskManager.Self.WaitForAllTasksFinished(pumpEvents:false);



            // no need to build because when it's loaded it performs a build:

        }

    }
}