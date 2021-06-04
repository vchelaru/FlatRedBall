using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPluginsCore.Compiler.CommandReceiving
{
    static class CommandReceiver
    {
        public static void HandleCommandsFromGame(string commandAsString)
        {
            var commandArray = JsonConvert.DeserializeObject<string[]>(commandAsString);

            foreach (var command in commandArray)
            {
                HandleIndividualCommand(command);
            }
        }

        private static void HandleIndividualCommand(string command)
        {
            GlueCommands.Self.PrintOutput($"Received command form game: {command}");
        }
    }
}
