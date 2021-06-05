using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Managers;
using OfficialPluginsCore.Compiler.CommandSending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

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
                        HandleAddObject(gamePortNumber, data);

                        break;
                }
            }
        }

        private static void HandleAddObject(int gamePortNumber, string data)
        {
            TaskManager.Self.Add(async () =>
            {
                var deserializedNos = JsonConvert.DeserializeObject<NamedObjectSave>(data);

                foreach(var variable in deserializedNos.InstructionSaves)
                {
                    if(variable.Value is double)
                    {
                        variable.Value = (float)(double)variable.Value;
                    }
                }

                var screenName = await CommandSender.GetScreenName(gamePortNumber);

                // remove prefix:
                var screensDotStart = screenName.IndexOf("Screens.");
                screenName = screenName.Substring(screensDotStart).Replace(".", "\\");
                var screen = ObjectFinder.Self.GetScreenSave(screenName);

                NamedObjectSave listToAddTo = null;
                if (screen != null)
                {
                    listToAddTo = screen.NamedObjects.FirstOrDefault(item =>
                    {
                        return item.IsList && item.SourceClassGenericType == deserializedNos.SourceClassType;
                    });
                }

                if(listToAddTo != null)
                { 
                    var lastSlash = deserializedNos.SourceClassType.LastIndexOf("\\");
                    var newName = deserializedNos.SourceClassType.Substring(lastSlash + 1) + "1";

                    var oldName = deserializedNos.InstanceName;
                    while (screen.GetNamedObjectRecursively(newName) != null)
                    {
                        newName = StringFunctions.IncrementNumberAtEnd(newName);
                    }

                    deserializedNos.InstanceName = newName;

                    
                    GlueCommands.Self.GluxCommands.AddNamedObjectTo(deserializedNos, screen, listToAddTo);

                    //RefreshManager.Self.HandleNamedObjectValueChanged(nameof(deserializedNos.InstanceName), oldName, deserializedNos);

                    var data = new GlueVariableSetData();
                    data.Type = "string";
                    data.Value = newName;
                    data.VariableName = "this." + oldName + ".Name";

                    var serialized = JsonConvert.SerializeObject(data);

                    await CommandSender.SendCommand($"SetVariable:{serialized}", gamePortNumber);
                }
            }, "Adding NOS", doOnUiThread:true);
        }
    }
}
