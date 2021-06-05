using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPluginsCore.Compiler.CommandReceiving
{
    static class CommandReceiver
    {
        public static async Task HandleCommandsFromGame(string commandAsString, int gamePortNumber)
        {
            var commandArray = JsonConvert.DeserializeObject<string[]>(commandAsString);

            foreach (var command in commandArray)
            {
                await HandleIndividualCommand(command, gamePortNumber);
            }
        }

        private static async Task HandleIndividualCommand(string command, int gamePortNumber)
        {
            var firstColon = command.IndexOf(":");
            if(firstColon == -1)
            {
                GlueCommands.Self.PrintOutput($"Received unknown command: {command}");
            }
            else
            {
                var action = command.Substring(0, firstColon);
                var data = command.Substring(firstColon + 1);

                switch(action)
                {
                    case "AddObject":
                        var deserializedNos = JsonConvert.DeserializeObject<NamedObjectSave>(data);

                        var screenName = await CommandSender.GetScreenName(gamePortNumber);

                        // remove prefix:
                        var screensDotStart = screenName.IndexOf("Screens.");
                        screenName = screenName.Substring(screensDotStart).Replace(".", "\\");
                        var screen = ObjectFinder.Self.GetScreenSave(screenName);

                        if(screen != null)
                        {
                            var listToAddTo = screen.NamedObjects.FirstOrDefault(item =>
                            {
                                return item.IsList && item.SourceClassGenericType == deserializedNos.SourceClassType;
                            });



                            GlueCommands.Self.GluxCommands.AddNamedObjectTo(deserializedNos, screen, listToAddTo);
                        }


                        break;
                }
            }
        }
    }
}
