using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
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

                NamedObjectSave listToAddTo = null;
                if (screen != null)
                {
                    if (addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.Circle" ||
                        addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.AxisAlignedRectangle" ||
                        addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.Polygon")
                    {
                        listToAddTo = screen.NamedObjects.FirstOrDefault(item => item.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection);
                    }
                    else
                    {
                        listToAddTo = screen.NamedObjects.FirstOrDefault(item =>
                        {
                            return item.IsList && item.SourceClassGenericType == addObjectDto.SourceClassType;
                        });
                    }
                }

                string newName = GetNewName(screen, addObjectDto);
                var oldName = addObjectDto.InstanceName;

                #region Send the new name back to the game so the game uses the actual Glue name rather than the AutoName
                    // do this before adding the NOS to Glue since adding the NOS to Glue results in an AddToList command
                    // sent to the game, and we want the right name before the AddToList command
                    var data = new GlueVariableSetData();
                    data.Type = "string";
                    data.VariableValue = newName;
                    data.VariableName = "this." + oldName + ".Name";
                    data.InstanceOwner = addObjectDto.ElementNameGame;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    // That's okay, this is fire-and-forget, we just send this back to the game and we don't care to await it
                    CommandSender.Send(data, gamePortNumber);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    #endregion

                var nos = JsonConvert.DeserializeObject<NamedObjectSave>(dataAsString);
                nos.InstanceName = newName;

                foreach (var variable in nos.InstructionSaves)
                {
                    object value = variable.Value;
                    var typeName = variable.Type;
                    value = ConvertVariable(value, typeName);
                    variable.Value = value;
                }

                GlueCommands.Self.DoOnUiThread(() =>
                {
                    RefreshManager.Self.IgnoreNextObjectAdd = true;
                    RefreshManager.Self.IgnoreNextObjectSelect = true;
                    GlueCommands.Self.GluxCommands.AddNamedObjectTo(nos, screen, listToAddTo);
                });

                //RefreshManager.Self.HandleNamedObjectValueChanged(nameof(deserializedNos.InstanceName), oldName, deserializedNos);

            }, "Adding NOS");
        }

        private static object ConvertVariable(object value, string typeName)
        {
            switch (typeName)
            {
                case "float":
                case nameof(Single):
                    {
                        if (value is double asDouble)
                        {
                            value = (float)asDouble;
                        }
                        else if(value is int asInt)
                        {
                            value = (float)asInt;
                        }
                    }
                    break;
                case "Microsoft.Xna.Framework.Graphics.Texture2D":
                case nameof(Texture2D):
                case "FlatRedBall.Graphics.Animation.AnimationChainList":
                case nameof(AnimationChainList):
                    if (value is string asString && !string.IsNullOrEmpty(asString))
                    {
                        value =
                            FileManager.RemovePath(FileManager.RemoveExtension(asString));
                    }
                    break;
                case nameof(TextureAddressMode):
                case "Microsoft.Xna.Framework.Graphics.TextureAddressMode":
                    {
                        if (value is int asInt)
                        {
                            value = (TextureAddressMode)asInt;
                        }
                        else if (value is long asLong)
                        {
                            value = (TextureAddressMode)asLong;
                        }
                    }

                    break;
            }

            return value;
        }

        private static string GetNewName(ScreenSave screen, AddObjectDto addObjectDto)
        {
            string newName = null;
            if (addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.Circle")
            {
                newName = "Circle1";
            }
            else if (addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.AxisAlignedRectangle")
            {
                newName = "AxisAlignedRectangle1";
            }
            else if (addObjectDto.SourceClassType == "FlatRedBall.Math.Geometry.Polygon")
            {
                newName = "Polygon1";
            }
            else if(addObjectDto.SourceClassType == "FlatRedBall.Sprite")
            {
                newName = "Sprite1";
            }
            else
            {
                var lastSlash = addObjectDto.SourceClassType.LastIndexOf("\\");
                newName = addObjectDto.SourceClassType.Substring(lastSlash + 1) + "1";
            }
            while (screen.GetNamedObjectRecursively(newName) != null)
            {
                newName = StringFunctions.IncrementNumberAtEnd(newName);
            }

            return newName;
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
                    var typeName = setVariableDto.Type;

                    if(string.IsNullOrEmpty(typeName))
                    {
                        throw new InvalidOperationException($"Variable {setVariableDto.VariableName} came from glue with a value of {typeName} but didn't have a type");
                    }

                    value = ConvertVariable(value, typeName);

                    nos.SetVariable(setVariableDto.VariableName, value);

                    GlueCommands.Self.DoOnUiThread(() =>
                        RefreshManager.Self.HandleNamedObjectValueChanged(setVariableDto.VariableName, null, nos, 
                        // record only - this variable change came from the game, we don't want to re-assign it and wipe other active edits
                        AssignOrRecordOnly.RecordOnly)
                    );
                    // this may not be the current screen:
                    var nosParent = ObjectFinder.Self.GetElementContaining(nos);

                    GlueCommands.Self.GluxCommands.SaveGlux();
                    GlueCommands.Self.DoOnUiThread(GlueCommands.Self.RefreshCommands.RefreshVariables);
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(nosParent);

                }
            }, "Handling set variable from game", 
            // This is going to push the change back to the game, and we don't want to sit and wait for codegen to finish, etc. Do it immediately!
            TaskExecutionPreference.Asap);
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
