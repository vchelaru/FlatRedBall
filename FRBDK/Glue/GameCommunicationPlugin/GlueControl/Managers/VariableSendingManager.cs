using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using Newtonsoft.Json;
using GameCommunicationPlugin.GlueControl.CommandSending;
using GameCommunicationPlugin.GlueControl.Dtos;
using GameCommunicationPlugin.GlueControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Parsing;

namespace GameCommunicationPlugin.GlueControl.Managers
{
    class VariableIgnoreData
    {
        public const int SecondsToKeepIgnoreItem = 5;

        public NamedObjectSave NamedObjectSave { get; set; }
        public string VariableName { get; set; }
        // Since orphans can exist, let's expire variable assignments after X seconds
        public DateTime TimeIgnoreCreated { get; set; }
    }

    class VariableSendingManager
    {
        public VariableSendingManager(RefreshManager refreshManager, CommandSender commandSender)
        {
            _refreshManager = refreshManager;
            _refreshManager.VariableSendingManager = this;
            _commandSender = commandSender;
        }

        #region Fields/Properties

        List<VariableIgnoreData> Ignores = new List<VariableIgnoreData>();
        private RefreshManager _refreshManager;
        private CommandSender _commandSender;

        public CompilerViewModel ViewModel
        {
            get; set;
        }

        public GlueViewSettingsViewModel GlueViewSettingsViewModel { get; set; }

        #endregion

        public void AddOneTimeIgnore(NamedObjectSave nos, string variableName)
        {
            var ignore = new VariableIgnoreData();
            ignore.NamedObjectSave = nos;
            ignore.VariableName = variableName;
            ignore.TimeIgnoreCreated = DateTime.Now;
            Ignores.Add(ignore);
        }

        bool GetIfChangedMemberIsIgnored(NamedObjectSave nos, string changedMember)
        {
            Ignores.RemoveAll(item => item.TimeIgnoreCreated + TimeSpan.FromSeconds(VariableIgnoreData.SecondsToKeepIgnoreItem) < DateTime.Now);
            var match = Ignores.FirstOrDefault(item => item.NamedObjectSave == nos && item.VariableName == changedMember);
            if(match != null)
            {
                Ignores.Remove(match);
                return true;
            }
            // todo - add more here over time, including making this a HashSet
            return changedMember == nameof(NamedObjectSave.ExposedInDerived);
        }


        internal async Task HandleNamedObjectVariableListChanged(List<VariableChangeArguments> variableList, AssignOrRecordOnly assignOrRecordOnly)
        {
            var gameScreenName = await _commandSender.GetScreenName();
            List<GlueVariableSetData> listOfVariables = new List<GlueVariableSetData>();
            List<NamedObjectSave> nosList = new List<NamedObjectSave>();
            foreach (var variable in variableList)
            {
                List<GlueVariableSetData> inner = GetNamedObjectValueChangedDtos(variable.ChangedMember, variable.OldValue, variable.NamedObject, assignOrRecordOnly, gameScreenName);
                nosList.Add(variable.NamedObject);
                listOfVariables.AddRange(inner);
            }

            await PushVariableChangesToGame(listOfVariables, nosList);
        }

        public async Task HandleNamedObjectVariableChanged(string changedMember, object oldValue, NamedObjectSave nos, AssignOrRecordOnly assignOrRecordOnly, object forcedCurrentValue = null)
        {
            var gameScreenName = await _commandSender.GetScreenName();
            var listOfVariables = GetNamedObjectValueChangedDtos(changedMember, oldValue, nos, assignOrRecordOnly, gameScreenName, forcedCurrentValue);

            await PushVariableChangesToGame(listOfVariables, new List<NamedObjectSave> { nos });
        }

        public async Task PushVariableChangesToGame(List<GlueVariableSetData> listOfVariables, List<NamedObjectSave> namedObjectsToUpdate)
        {
            await TaskManager.Self.AddAsync(async () =>
            {
                try
                {
                    var dto = new GlueVariableSetDataList();
                    dto.Data.AddRange(listOfVariables);

                    foreach(var nos in namedObjectsToUpdate)
                    {
                        var container = ObjectFinder.Self.GetElementContaining(nos);
                        var namedObjectWithElement = new NamedObjectWithElementName();
                        namedObjectWithElement.NamedObjectSave = nos;
                        namedObjectWithElement.GlueElementName = container?.Name;
                        var listNos = container?.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(nos));
                        namedObjectWithElement.ContainerName = listNos?.InstanceName;

                        dto.NamedObjectsToUpdate.Add(namedObjectWithElement);
                    }

                    var sendGeneralResponse = await _commandSender.Send(dto);

                    GlueVariableSetDataResponseList response = null;
                    if (sendGeneralResponse.Succeeded)
                    {
                        response =
                            JsonConvert.DeserializeObject<GlueVariableSetDataResponseList>(sendGeneralResponse.Data);
                    }

                    var failed = response == null || response.Data.Any(item => !string.IsNullOrEmpty(item?.Exception));

                    if(failed)
                    {
                        var exception = response?.Data.FirstOrDefault(item => !string.IsNullOrEmpty(item.Exception)).Exception;
                        if(exception != null)
                        {
                            GlueCommands.Self.PrintError(exception);
                            Output.Print(exception);
                        }

                        _refreshManager.CreateStopAndRestartTask($"Unhandled variable changed");
                    }
                }
                catch
                {
                    // no biggie...
                }
            }, $"Pushing {listOfVariables.Count} variables to game", TaskExecutionPreference.Asap);
        }

        public List<GlueVariableSetData> GetNamedObjectValueChangedDtos(string changedMember, object oldValue, NamedObjectSave nos, AssignOrRecordOnly assignOrRecordOnly, string gameScreenName, object forcedCurrentValue = null)
        {
            List<GlueVariableSetData> toReturn = new List<GlueVariableSetData>();

            // If the value set is "ignored" that means we want to push a command to the game to only
            // re-run the command on screen change, but don't actually set the variable now.
            var isIgnored = GetIfChangedMemberIsIgnored(nos, changedMember);

            if(isIgnored)
            {
                assignOrRecordOnly = AssignOrRecordOnly.RecordOnly;
            }
            ////////////////////Early Out//////////////////////////////
            //if(isIgnored)
            //{
            //    return toReturn;
            //}
            //////////////////End Early Out////////////////////////////

            string typeName = null;
            object currentValue = null;
            bool isState = false;
            StateSaveCategory category = null;

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
                typeName = GetQualifiedStateTypeName(changedMember, oldValue, nos, out isState, out category);
            }
            else if(property != null)
            {
                typeName = property.Value?.GetType().ToString() ?? oldValue?.GetType().ToString();
            }
            if(typeName == null)
            {
                // maybe it's a strongly typed property on the NOS?
                typeName = typeof(NamedObjectSave).GetProperty(changedMember)?.PropertyType.ToString();
            }
            if(typeName == null && nos.SourceType == SourceType.Entity)
            {
                // try to get it from a PositionedObject
                typeName = typeof(FlatRedBall.PositionedObject).GetProperty(changedMember)?.PropertyType.ToString();
            }
            if (typeName == null && nos.SourceType == SourceType.Entity)
            {
                var nosEntity = ObjectFinder.Self.GetElement(nos);
                var variableInEntity = nosEntity.GetCustomVariable(changedMember);
                typeName = variableInEntity?.Type;

                if(variableInEntity != null)
                {
                    var getStateResult = ObjectFinder.Self.GetStateSaveCategory(variableInEntity, nosEntity);
                    isState = getStateResult.IsState;
                    category = getStateResult.Category;
                    if(isState && category != null)
                    {
                        typeName = nosEntity.Name.Replace("/", ".").Replace("\\", ".") + "." + category.Name;
                    }
                }
            }


            if (forcedCurrentValue != null)
            {
                currentValue = forcedCurrentValue;
            }
            else if (instruction != null)
            {
                currentValue = instruction.Value;
            }
            else if(property != null)
            {
                currentValue = property.Value;
            }
            else if(changedMember == "IncludeInICollidable")
            {
                currentValue = nos.IncludeInICollidable;
            }
            #endregion


            var currentElement = GlueState.Self.CurrentElement;
            var nosName = nos.InstanceName;
            var ati = nos.GetAssetTypeInfo();
            string value;

            string glueScreenName = null;
            if(!string.IsNullOrEmpty(gameScreenName))
            {
                glueScreenName = string.Join('\\', gameScreenName.Split('.').Skip(2));

            }

            ConvertValue(ref changedMember, oldValue, currentValue, nos, currentElement, glueScreenName, ref nosName, ati, ref typeName, out value);

            GlueVariableSetData data = GetGlueVariableSetDataDto(nosName, changedMember, typeName, value, currentElement, assignOrRecordOnly, isState);

            toReturn.Add(data);

            if(category != null && ( value == null || value == "<NONE>"))
            {
                // we need to un-assign the state. We can do this by looping through all variables controlled by the
                // category and setting them to null values:
                var ownerOfCategory = ObjectFinder.Self.GetElementContaining(category);

                var variablesToAssign = ownerOfCategory?.CustomVariables
                    .Where(item => !category.ExcludedVariables.Contains(item.Name)).ToArray();
                if(variablesToAssign != null)
                {
                    foreach(var variableToAssign in variablesToAssign)
                    {
                        var defaultValue = ObjectFinder.Self.GetValueRecursively(nos, ownerOfCategory, variableToAssign.Name);
                        toReturn.AddRange(GetNamedObjectValueChangedDtos(variableToAssign.Name, null, nos, assignOrRecordOnly, gameScreenName, forcedCurrentValue:defaultValue));
                    }
                }
            }

            return toReturn;
        }

        public string GetQualifiedStateTypeName(string changedMember, object oldValue, NamedObjectSave nos, out bool isState, out StateSaveCategory category)
        {
            var instruction = nos?.GetCustomVariable(changedMember);
            isState = false;
            category = null;
            string typeName = instruction?.Type ?? instruction.Value?.GetType().ToString() ?? oldValue?.GetType().ToString();
            var nosElement = ObjectFinder.Self.GetElement(nos);
            if (nosElement != null)
            {
                var variable = nosElement.GetCustomVariableRecursively(changedMember);
                if (variable != null)
                {
                    var isStateResult = ObjectFinder.Self.GetStateSaveCategory(variable, nosElement);
                    category = isStateResult.Category;
                    isState = isStateResult.IsState;
                }
                if (!isState && typeName != null && typeName.StartsWith("Current") && changedMember.EndsWith("State"))
                {
                    var strippedName = changedMember.Substring("Current".Length, changedMember.Length - "Current".Length - "State".Length);

                    isState = nosElement.GetStateCategoryRecursively(strippedName) != null;
                }
                if (isState)
                {
                    if (changedMember == "VariableState")
                    {
                        typeName = $"{GlueState.Self.ProjectNamespace}.{nosElement.Name.Replace('\\', '.')}.VariableState";
                    }
                    else if (changedMember.StartsWith("Current") && changedMember.EndsWith("State"))
                    {
                        var strippedName = changedMember.Substring("Current".Length, changedMember.Length - "Current".Length - "State".Length);
                        typeName = $"{GlueState.Self.ProjectNamespace}.{nosElement.Name.Replace('\\', '.')}.{strippedName}";
                    }
                    else
                    {
                        typeName = variable.Type;

                        if (typeName.StartsWith("Entities.") || typeName.StartsWith("Screens."))
                        {
                            typeName = GlueState.Self.ProjectNamespace + "." + typeName;
                        }
                    }

                    isState = true;
                }
            }

            return typeName;
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
            //var gameScreenName = await _commandSender.GetScreenName(ViewModel.PortNumber);
            //var glueScreenName = gameScreenName

            #region X, Y, Z
            if (currentElement is EntitySave && nos.AttachToContainer &&

                (changedMember == "X" || changedMember == "Y" || changedMember == "Z" ||
                 changedMember == "RotationX" || changedMember == "RotationY" || changedMember == "RotationZ"))
            {
                if(ati != null && ati.IsPositionedObject == false)
                {
                    // do nothing
                }
                else
                {
                    changedMember = $"Relative{changedMember}";
                }
            }
            #endregion

            var isConversionHandled = false;
            if(type?.StartsWith("System.Collections.Generic.List") == true || type?.StartsWith("List<") == true)
            {
                value = JsonConvert.SerializeObject(currentValue);
                isConversionHandled = true;
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
                if(!isConversionHandled)
                {
                    value = currentValue?.ToString();
                }
            
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
                value = TypeManager.GetDefaultForType(type);
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

        private async Task<GlueVariableSetDataResponse> TryPushVariable(GlueVariableSetData data)
        {
            GlueVariableSetDataResponse response = null;
            if (ViewModel.IsRunning )
                // why do we care if the GlueElement is null or not?
                // && data.GlueElement != null)
            {
                var sendGeneralResponse = await _commandSender.Send(data);
                var responseAsString = sendGeneralResponse.Succeeded ? sendGeneralResponse.Data : string.Empty;

                if (!string.IsNullOrEmpty(responseAsString))
                {
                    response = JsonConvert.DeserializeObject<GlueVariableSetDataResponse>(responseAsString);
                }
            }
            return response;
        }

        private GlueVariableSetData GetGlueVariableSetDataDto(string variableOwningNosName, string rawMemberName, string type, string value, GlueElement currentElement, AssignOrRecordOnly assignOrRecordOnly, bool isState)
        {
            var data = new GlueVariableSetData();
            data.InstanceOwnerGameType = ToGameType(currentElement);
            data.Type = type;
            data.VariableValue = value;
            data.VariableName = rawMemberName;
            data.AssignOrRecordOnly = assignOrRecordOnly;
            data.IsState = isState;

            // March 15, 2022
            // Why do we set this
            // here? It results in 
            // a large serialization/deserializaiton
            // which can be very slow when sending lots
            // of variables over at once.
            //data.ScreenSave = currentElement as ScreenSave;
            //data.EntitySave = currentElement as EntitySave;

            if (!string.IsNullOrEmpty(variableOwningNosName))
            {
                data.VariableName = "this." + variableOwningNosName + "." + data.VariableName;
            }
            else
            {
                data.VariableName = "this." + data.VariableName;
            }

            return data;
        }

        internal async Task HandleNamedObjectValueChanged(string changedMember, object oldValue)
        {
            var nos = GlueState.Self.CurrentNamedObjectSave;
            await HandleNamedObjectVariableChanged(changedMember, oldValue, nos, AssignOrRecordOnly.Assign);
        }

        internal async void HandleVariableChanged(GlueElement variableElement, CustomVariable variable)
        {
            if (_refreshManager.ShouldRestartOnChange)
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
                    // don't prefix "this", the inner call will do it
                    name = variable.Name;
                }

                var isState = variable.GetIsVariableState(variableElement);

                GlueVariableSetData data = GetGlueVariableSetDataDto(null, name, type, value, GlueState.Self.CurrentElement, AssignOrRecordOnly.Assign, isState);

                await TryPushVariable(data);
            }
            else
            {
                _refreshManager.CreateStopAndRestartTask($"Object variable {variable.Name} changed");
            }
        }

    }
}
