using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.Compiler.Managers
{
    class VariableSendingManager : Singleton<VariableSendingManager>
    {
        #region Fields/Properties

        public CompilerViewModel ViewModel
        {
            get; set;
        }

        #endregion



        public async void HandleNamedObjectValueChanged(string changedMember, object oldValue, NamedObjectSave nos, AssignOrRecordOnly assignOrRecordOnly)
        {

            string typeName = null;
            object currentValue = null;

            var nosAti = nos.GetAssetTypeInfo();
            var variableDefinition = nosAti?.VariableDefinitions.FirstOrDefault(item => item.Name == changedMember);
            var instruction = nos?.GetCustomVariable(changedMember);
            var property = nos.Properties.FirstOrDefault(item => item.Name == changedMember);

            #region Identify the typeName
            if (variableDefinition != null)
            {
                typeName = variableDefinition.Type;
            }
            else if(instruction != null)     
            {
                typeName = instruction?.Type ?? instruction.Value?.GetType().ToString() ?? oldValue?.GetType().ToString();
            }
            else if(property != null)
            {
                typeName = property.Value?.GetType().ToString() ?? oldValue?.GetType().ToString();
            }

            if(instruction != null)
            {
                currentValue = instruction.Value;
            }
            else if(property != null)
            {
                currentValue = property.Value;
            }
            #endregion


            var currentElement = GlueState.Self.CurrentElement;
            var nosName = nos.InstanceName;
            var ati = nos.GetAssetTypeInfo();
            string value;

            var gameScreenName = await CommandSender.GetScreenName(ViewModel.PortNumber);
            string glueScreenName = null;
            if(!string.IsNullOrEmpty(gameScreenName))
            {
                glueScreenName = string.Join('\\', gameScreenName.Split('.').Skip(2));

            }

            ConvertValue(ref changedMember, oldValue, currentValue, nos, currentElement, glueScreenName, ref nosName, ati, ref typeName, out value);

            TaskManager.Self.Add(() =>
            {
                try
                {
                    var task = TryPushVariable(nosName, changedMember, typeName, value, currentElement, assignOrRecordOnly);
                    task.Wait();
                    var response = task.Result;
                    if (!string.IsNullOrWhiteSpace(response?.Exception))
                    {
                        GlueCommands.Self.PrintError(response.Exception);
                        Output.Print(response.Exception);

                    }
                    if(response?.WasVariableAssigned != true)
                    {
                        // wasn't assigned, the game didn't know what to do, so restart
                        RefreshManager.Self.StopAndRestartTask($"Unhandled variable {changedMember} changed");

                    }

                }
                catch
                {
                    // no biggie...
                }
            }, "Pushing variable to game", TaskExecutionPreference.Asap);
        }

        private void ConvertValue(ref string changedMember, object oldValue, 
            object currentValue, NamedObjectSave nos, GlueElement currentElement, string glueScreenName,
            ref string nosName, FlatRedBall.Glue.Elements.AssetTypeInfo ati, 
            ref string type, out string value)
        {
            value = currentValue?.ToString();
            var originalMemberName = changedMember;

            // to properly convert value we may need to squash multiple inheritance levels of 
            // NamedObjectSaves. But to know which to squash, we need to know the current game
            // screen
            //var gameScreenName = await CommandSender.GetScreenName(ViewModel.PortNumber);
            //var glueScreenName = gameScreenName

            #region X, Y, Z
            if (currentElement is EntitySave && nos.AttachToContainer &&
                (changedMember == "X" || changedMember == "Y" || changedMember == "Z"))
            {
                changedMember = $"Relative{changedMember}";
            }
            #endregion

            if(type.StartsWith("System.Collections.Generic.List"))
            {
                value = JsonConvert.SerializeObject(currentValue);
            }

            #region Collision Relationships

            if(nos.IsCollisionRelationship())
            {
                if(changedMember == "IsCollisionActive")
                {
                    changedMember = "IsActive";
                }
                // If one of a few variables have changed, we are going to send over the entire collision relationship 
                // so the game can re-create it 
                else
                {
                    var shouldSerializeEntireNos = false;
                    switch(changedMember)
                    {
                        case "CollisionType":
                        case "FirstCollisionMass":
                        case "SecondCollisionMass":
                        case "FirstSubCollisionSelectedItem":
                        case "SecondSubCollisionSelectedItem":
                        case "FirstCollisionName":
                        case "SecondCollisionName":
                        case "CollisionElasticity":
                            shouldSerializeEntireNos = true;
                            break;
                    }

                    if(shouldSerializeEntireNos)
                    {
                        changedMember = "Entire CollisionRelationship";
                        type = "NamedObjectSave";
                        value = JsonConvert.SerializeObject(GetCombinedNos(nos, glueScreenName));
                    }
                }
            }

            #endregion

            #region TileShapeCollection

            var isTileShapeCollection =
                nos.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection";

            var glueScreen = ObjectFinder.Self.GetScreenSave(glueScreenName);

            if (isTileShapeCollection)
            {
                var shouldSerializeEntireNos = false;
                switch(changedMember)
                {
                    
                    case "CollisionCreationOptions":

                    case "CollisionTileSize":

                    case "CollisionFillLeft":
                    case "CollisionFillTop":

                    case "CollisionFillWidth":
                    case "CollisionFillHeight":

                    case "BorderOutlineType":

                    case "InnerSizeWidth":
                    case "InnerSizeHeight":

                    case "CollisionPropertyName":

                    case "CollisionLayerName":

                    case "CollisionLayerTileType":

                    case "IsCollisionMerged":

                    case "SourceTmxName":
                    case "CollisionTileTypeName":
                    case "RemoveTilesAfterCreatingCollision":


                        shouldSerializeEntireNos = true;
                        break;
                }

                if(shouldSerializeEntireNos)
                {
                    changedMember = "Entire TileShapeCollection";
                    type = "NamedObjectSave";
                    value = JsonConvert.SerializeObject(GetCombinedNos(nos, glueScreenName));
                }
            }


            #endregion

            #region InstanceName

            if (changedMember == nameof(NamedObjectSave.InstanceName))
            {
                type = "string";
                value = nos.InstanceName;
                changedMember = "Name";
                nosName = (string)oldValue;
            }
            #endregion



            else if (ati?.VariableDefinitions.Any(item => item.Name == originalMemberName) == true)
            {
                var variableDefinition = ati.VariableDefinitions.First(item => item.Name == originalMemberName);
                type = variableDefinition.Type;
                value = currentValue?.ToString();
            
                var isFile =
                    variableDefinition.Type == "Microsoft.Xna.Framework.Texture2D" ||
                    variableDefinition.Type == "Texture2D" ||
                    variableDefinition.Type == "FlatRedBall.Graphics.Animation.AnimationChainList" ||
                    variableDefinition.Type == "AnimationChainList";

                if (isFile)
                {
                    var wasModified = false;
                    var referencedFile = currentElement.GetReferencedFileSaveRecursively(value);
                    if (referencedFile != null)
                    {
                        value = FileManager.MakeRelative(GlueCommands.Self.GetAbsoluteFilePath(referencedFile).FullPath,
                            GlueState.Self.CurrentGlueProjectDirectory);
                        wasModified = true;
                    }
                    if (!wasModified)
                    {
                        // set it to null
                        value = string.Empty;
                    }
                }
            }


            if (value == null)
            {
                switch(type)
                {
                    case "float":
                    case nameof(Single):
                    case "System.Single":

                    case "int":
                    case nameof(Int32):
                    case "System.Int32":

                    case "long":
                    case nameof(Int64):
                    case "System.Int64":

                    case "double":
                    case nameof(Double):
                    case "System.Double":
                        value = "0";
                        break;

                    case "bool":
                    case nameof(Boolean):
                    case "System.Boolean":
                        value = "false";
                        break;
                }
            }
        }

        private NamedObjectSave GetCombinedNos(NamedObjectSave nos, string glueScreenName)
        {
            var clone = nos.Clone();


            var screen = ObjectFinder.Self.GetScreenSave(glueScreenName);

            if(screen != null)
            {
                var inheritance = ObjectFinder.Self.GetInheritanceChain(screen);

                foreach(var currentScreenInChain in inheritance)
                {
                    // base -> more derived
                    var foundNos = currentScreenInChain.AllNamedObjects.FirstOrDefault(item => item.InstanceName == nos.InstanceName);

                    if(foundNos != null)
                    {
                        // apply this instance's properties and variables:
                        foreach(var property in foundNos.Properties)
                        {
                            clone.SetProperty(property.Name, property.Value);
                        }
                        foreach(var variable in foundNos.InstructionSaves)
                        {
                            clone.SetVariable(variable.Member, variable.Value);
                        }
                    }
                }
            }
            return clone;
        }

        private string ToGameType(GlueElement element) =>
            GlueState.Self.ProjectNamespace + "." + element.Name.Replace("\\", ".");

        private async Task<GlueVariableSetDataResponse> TryPushVariable(string variableOwningNosName, string rawMemberName, string type, string value, GlueElement currentElement,
            AssignOrRecordOnly assignOrRecordOnly)
        {
            GlueVariableSetDataResponse response = null;
            if (ViewModel.IsRunning)
            {
                if (currentElement != null)
                {
                    var data = new GlueVariableSetData();
                    data.InstanceOwnerGameType = ToGameType(currentElement);
                    data.Type = type;
                    data.VariableValue = value;
                    data.VariableName = rawMemberName;
                    data.AssignOrRecordOnly = assignOrRecordOnly;
                    if (!string.IsNullOrEmpty(variableOwningNosName))
                    {
                        data.VariableName = "this." + variableOwningNosName + "." + data.VariableName;
                    }
                    else
                    {
                        data.VariableName = "this." + data.VariableName;
                    }

                    var responseAsString = await CommandSender.Send(data, ViewModel.PortNumber);

                    if (!string.IsNullOrEmpty(responseAsString))
                    {
                        response = JsonConvert.DeserializeObject<GlueVariableSetDataResponse>(responseAsString);
                    }
                }
            }
            return response;
        }


        internal void HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            var nos = GlueState.Self.CurrentNamedObjectSave;
            HandleNamedObjectValueChanged(changedMember, oldValue, nos, AssignOrRecordOnly.Assign);
        }

        internal async void HandleVariableChanged(IElement variableElement, CustomVariable variable)
        {
            if (RefreshManager.Self.ShouldRestartOnChange)
            {
                var type = variable.Type;
                var value = variable.DefaultValue?.ToString();
                string name = null;
                if (variable.IsShared)
                {
                    name = ToGameType(variableElement as GlueElement) + "." + variable.Name;
                }
                else
                {
                    name = "this." + variable.Name;
                }
                await TryPushVariable(null, name, type, value, GlueState.Self.CurrentElement, AssignOrRecordOnly.Assign);
            }
            else
            {
                RefreshManager.Self.StopAndRestartTask($"Object variable {variable.Name} changed");
            }
        }
    }
}
