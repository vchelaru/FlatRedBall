using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.Managers;
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
        public static void HandleCommandsFromGame(string commandAsString, int gamePortNumber)
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
                    case nameof(AddObjectDto):
                        HandleAddObject(gamePortNumber, data);

                        break;
                    case nameof(SetVariableDto):
                        HandleSetVariable(gamePortNumber, JsonConvert.DeserializeObject<SetVariableDto>(data));
                        break;
                    case nameof(SelectObjectDto):
                        HandleSelectObject(gamePortNumber, JsonConvert.DeserializeObject<SelectObjectDto>(data));
                        break;
                    case nameof(RemoveObjectDto):
                        HandleRemoveObject(gamePortNumber, JsonConvert.DeserializeObject<RemoveObjectDto>(data));
                        break;
                }
            }
        }

        private static void HandleRemoveObject(int gamePortNumber, RemoveObjectDto removeObjectDto)
        {
            TaskManager.Self.Add(() =>
            {
                ScreenSave screen = GetCurrentInGameScreen(gamePortNumber);

                var nos = screen.GetNamedObjectRecursively(removeObjectDto.ObjectName);

                if (nos != null)
                {
                    GlueCommands.Self.GluxCommands.RemoveNamedObject(nos);
                }
            }, "Handling removing object from screen");
        }

        private static void HandleAddObject(int gamePortNumber, string dataAsString)
        {
            TaskManager.Self.Add(() =>
            {
                ScreenSave screen = GetCurrentInGameScreen(gamePortNumber);

                var addObjectDto = JsonConvert.DeserializeObject<AddObjectDto>(dataAsString);

                foreach (var variable in addObjectDto.InstructionSaves)
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
                        return item.IsList && item.SourceClassGenericType == addObjectDto.SourceClassType;
                    });
                }

                if (listToAddTo != null)
                {
                    var lastSlash = addObjectDto.SourceClassType.LastIndexOf("\\");
                    var newName = addObjectDto.SourceClassType.Substring(lastSlash + 1) + "1";

                    var oldName = addObjectDto.InstanceName;
                    while (screen.GetNamedObjectRecursively(newName) != null)
                    {
                        newName = StringFunctions.IncrementNumberAtEnd(newName);
                    }

                    var nos = JsonConvert.DeserializeObject<NamedObjectSave>(dataAsString);
                    nos.InstanceName = newName;
                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        RefreshManager.Self.IgnoreNextObjectAdd = true;
                        RefreshManager.Self.IgnoreNextObjectSelect = true;
                        GlueCommands.Self.GluxCommands.AddNamedObjectTo(nos, screen, listToAddTo);

                    });

                    //RefreshManager.Self.HandleNamedObjectValueChanged(nameof(deserializedNos.InstanceName), oldName, deserializedNos);

                    var data = new GlueVariableSetData();
                    data.Type = "string";
                    data.VariableValue = newName;
                    data.VariableName = "this." + oldName + ".Name";
                    data.InstanceOwner = addObjectDto.ElementName;

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
            // push back to the game so the game can re-run this whenever the screen changes.

            TaskManager.Self.Add(() =>
            {
                var type = string.Join('\\', setVariableDto.InstanceOwner.Split('.').Skip(1));

                var element = ObjectFinder.Self.GetIElement(type);

                var nos = element.GetNamedObjectRecursively(setVariableDto.ObjectName);

                if (nos != null)
                {
                    object value = setVariableDto.VariableValue;

                    var floatConverter =
                        TypeDescriptor.GetConverter(typeof(float));

                    HashSet<string> floatVariables = new HashSet<string>
                    {
                        "X",
                        "Y",
                        "Z",
                        "Width",
                        "Height",
                        "TextureScale",
                        "Radius"
                    };

                    var convertToFloat = floatVariables.Contains(
                        setVariableDto.VariableName);

                    if (convertToFloat)
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

                    GlueCommands.Self.DoOnUiThread(() =>
                        RefreshManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos)
                    );
                    // this may not be the current screen:
                    var nosParent = ObjectFinder.Self.GetElementContaining(nos);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);

                }
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
                NamedObjectSave nos = null;
                var screen = GetCurrentInGameScreen(gamePortNumber);

                if(screen == null)
                {
                    var entityName = selectObjectDto.ElementName;
                    var split = entityName.Split('.').ToList().Skip(1);
                    entityName = string.Join('\\', split);
                    EntitySave currentEntity = ObjectFinder.Self.GetEntitySave(entityName);
                    
                    if(currentEntity != null)
                    {
                        nos = currentEntity.GetNamedObjectRecursively(selectObjectDto.ObjectName);
                        if(nos == null && 
                            selectObjectDto.ObjectName?.StartsWith('m') == true && selectObjectDto.ObjectName.Length > 1)
                        {
                            nos = currentEntity.GetNamedObjectRecursively(selectObjectDto.ObjectName[1..]);
                        }
                    }
                }
                else
                {
                    nos = screen.GetNamedObjectRecursively(selectObjectDto.ObjectName);
                }

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
