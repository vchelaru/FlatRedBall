using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.Managers;
using OfficialPluginsCore.Compiler.CommandSending;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                HandleIndividualCommand(command, gamePortNumber);
            }
        }

        private static void HandleIndividualCommand(string command, int gamePortNumber)
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
                    case nameof(SetVariableDto):
                        HandleSetVariable(gamePortNumber, JsonConvert.DeserializeObject<SetVariableDto>(data));
                        break;
                    case nameof(SelectObjectDto):
                        HandleSelectObject(gamePortNumber, JsonConvert.DeserializeObject<SelectObjectDto>(data));
                        break;
                }
            }
        }


        private static void HandleAddObject(int gamePortNumber, string data)
        {
            TaskManager.Self.Add(() =>
            {
                ScreenSave screen = GetCurrentInGameScreen(gamePortNumber);
                var deserializedNos = JsonConvert.DeserializeObject<NamedObjectSave>(data);

                foreach (var variable in deserializedNos.InstructionSaves)
                {
                    if (variable.Value is double)
                    {
                        variable.Value = (float)(double)variable.Value;
                    }
                }


                NamedObjectSave listToAddTo = null;
                if (screen != null)
                {
                    listToAddTo = screen.NamedObjects.FirstOrDefault(item =>
                    {
                        return item.IsList && item.SourceClassGenericType == deserializedNos.SourceClassType;
                    });
                }

                if (listToAddTo != null)
                {
                    var lastSlash = deserializedNos.SourceClassType.LastIndexOf("\\");
                    var newName = deserializedNos.SourceClassType.Substring(lastSlash + 1) + "1";

                    var oldName = deserializedNos.InstanceName;
                    while (screen.GetNamedObjectRecursively(newName) != null)
                    {
                        newName = StringFunctions.IncrementNumberAtEnd(newName);
                    }

                    deserializedNos.InstanceName = newName;

                    GlueCommands.Self.DoOnUiThread(() =>
                        GlueCommands.Self.GluxCommands.AddNamedObjectTo(deserializedNos, screen, listToAddTo));

                    //RefreshManager.Self.HandleNamedObjectValueChanged(nameof(deserializedNos.InstanceName), oldName, deserializedNos);

                    var data = new GlueVariableSetData();
                    data.Type = "string";
                    data.VariableValue = newName;
                    data.VariableName = "this." + oldName + ".Name";

                    var serialized = JsonConvert.SerializeObject(data);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    // That's okay, this is fire-and-forget, we just send this back to the game and we don't care to await it
                    CommandSender.SendCommand($"SetVariable:{serialized}", gamePortNumber);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }, "Adding NOS");
        }

        private static void HandleSetVariable(int gamePortNumber, SetVariableDto setVariableDto)
        {
            if(setVariableDto.VariableValue == null)
            {
                int m = 3;
            }
            TaskManager.Self.Add(() =>
            {
                ScreenSave screen = GetCurrentInGameScreen(gamePortNumber);

                var nos = screen.GetNamedObjectRecursively(setVariableDto.ObjectName);

                object value = setVariableDto.VariableValue;

                var floatConverter =
                    TypeDescriptor.GetConverter(typeof(float));

                var convertToFloat = setVariableDto.VariableName == "X" ||
                    setVariableDto.VariableName == "Y" ||
                    setVariableDto.VariableName == "Z";
                if(convertToFloat)
                {
                    if(value is double asDouble)
                    {
                        value = (float)(double)asDouble;
                    }
                    else
                    {
                        value = floatConverter.ConvertFrom(value);
                    }
                }

                nos.SetVariableValue(setVariableDto.VariableName, value);

                // this may not be the current screen:
                var nosParent = ObjectFinder.Self.GetElementContaining(nos);

                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);
            }, "Handling set variable from game");
        }

        private static ScreenSave GetCurrentInGameScreen(int gamePortNumber)
        {
            var screenNameTask = CommandSender.GetScreenName(gamePortNumber);
            screenNameTask.Wait(6000);
            var screenName = screenNameTask.Result; 

            if(!string.IsNullOrEmpty(screenName))
            {
                // remove prefix:
                var screensDotStart = screenName.IndexOf("Screens.");
                screenName = screenName.Substring(screensDotStart).Replace(".", "\\");
                var screen = ObjectFinder.Self.GetScreenSave(screenName);
                return screen;
            }
            else
            {
                return null;
            }
        }

        private static void HandleSelectObject(int gamePortNumber, SelectObjectDto selectObjectDto)
        {

            TaskManager.Self.Add(() =>
            {
                var screen = GetCurrentInGameScreen(gamePortNumber);
                var nos = screen.GetNamedObjectRecursively(selectObjectDto.ObjectName);

                if(nos != null)
                {
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        GlueState.Self.CurrentNamedObjectSave = nos;
                        GlueCommands.Self.DialogCommands.FocusTab("Variables");
                    });
                }
            }, "Selecting object from game command");
        }
    }
}
