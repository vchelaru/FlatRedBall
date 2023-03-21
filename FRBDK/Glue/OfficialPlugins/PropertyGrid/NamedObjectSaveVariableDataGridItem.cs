using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics.Animation;
using Glue;
using OfficialPlugins.VariableDisplay;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.FormHelpers;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;
using FlatRedBall.Utilities;
using FlatRedBall.Attributes;
using GluePropertyGridClasses.StringConverters;
using OfficialPlugins.VariableDisplay.Controls;
using OfficialPlugins.VariableDisplay.Data;
using System.Reflection;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using System.Security.RightsManagement;

namespace OfficialPlugins.PropertyGrid
{
    internal class NamedObjectSaveVariableDataGridItem : DataGridItem, IFileInstanceMember
    {
        public GlueElement Container { get; set; }
        public NamedObjectSave NamedObjectSave { get; set; }

        // todo - can we refactor this out?
        public IEnumerable<MemberCategory> categories;

        public VariableDefinition VariableDefinition { get; set; }

        public string NameOnInstance { get; set; }

        /// <summary>
        /// If false, changes will not get pushed to the plugin manager. This should be set to false when refreshing
        /// </summary>
        public bool PushChangesToPluginManager = true;

        public event Action View;
        public void OnView()
        {
            View?.Invoke();

        }
        public bool IsFile { get; set; }

        public NamedObjectSaveVariableDataGridItem()
        {
            CustomGetEvent += HandleCustomGet;

            CustomSetEvent += HandleVariableSet;

            SetValueError += (newValue) =>
            {
                if (newValue is string && string.IsNullOrEmpty(newValue as string))
                {
                    MakeDefault(NamedObjectSave, NameOnInstance);
                }
            };


            IsDefaultSet += (owner, args) =>
            {
                if (IsDefault)
                {
                    // June 29 2021 - this used to get called whenever
                    // IsDefault is set to either true or false, but we
                    // only want to call MakeDefault if the value is set to true.
                    MakeDefault(NamedObjectSave, NameOnInstance);
                }
            };


            View = null;

            View += () =>
            {
                var rfs = (TypeConverter as IObjectsInFileConverter).ReferencedFileSave;

                if (rfs != null)
                {
                    var value = this.Value as string;
                    GlueCommands.Self.SelectCommands.Select(
                        rfs,
                        value);
                }
            };

            CustomGetTypeEvent += (throwaway) => MemberType;
        }

        public void RefreshFrom(NamedObjectSave namedObjectSave, VariableDefinition variableDefinition, GlueElement container, IEnumerable<MemberCategory> categories, string customTypeName, string nameOnInstance)
        {
            var wasPushing = PushChangesToPluginManager;
            PushChangesToPluginManager = false;

            this.NamedObjectSave= namedObjectSave;
            VariableDefinition = variableDefinition;
            Container = container;
            NameOnInstance = nameOnInstance;
            this.categories = categories;


            TypeConverter typeConverter = null;
            
            //if(MemberType != null)
            {
                typeConverter = GetTypeConverter(NamedObjectSave, container, NameOnInstance, MemberType, customTypeName, VariableDefinition);
            }

            bool isObjectInFile = typeConverter is IObjectsInFileConverter;

            PreferredDisplayer = null;
            if (isObjectInFile)
            {
                PreferredDisplayer = typeof(FileReferenceComboBox);
            }

            if (variableDefinition?.PreferredDisplayer != null)
            {
                PreferredDisplayer = variableDefinition.PreferredDisplayer;

                if (PreferredDisplayer == typeof(SliderDisplay) && variableDefinition.MinValue != null && variableDefinition.MaxValue != null)
                {
                    PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                        variableDefinition.MaxValue.Value;
                    PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                        variableDefinition.MinValue.Value;
                }

                foreach (var item in variableDefinition.PropertiesToSetOnDisplayer)
                {
                    PropertiesToSetOnDisplayer[item.Key] = item.Value;
                }

            }
            else if (variableDefinition?.Name == nameof(FlatRedBall.PositionedObject.RotationZ) && variableDefinition.Type == "float")
            {
                PreferredDisplayer = typeof(AngleSelectorDisplay);
            }
            else if (variableDefinition?.MinValue != null && variableDefinition?.MaxValue != null)
            {
                PreferredDisplayer = typeof(SliderDisplay);
                PropertiesToSetOnDisplayer[nameof(SliderDisplay.MaxValue)] =
                    variableDefinition.MaxValue.Value;
                PropertiesToSetOnDisplayer[nameof(SliderDisplay.MinValue)] =
                variableDefinition.MinValue.Value;
            }

            if (PreferredDisplayer == typeof(AngleSelectorDisplay))
            {
                PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.TypeToPushToInstance)] =
                    AngleType.Radians;

                // this used to be 1, then 5, but 10 is prob enough resolution. Numbers can be typed.
                // 15 is better, gives the user access to 45
                PropertiesToSetOnDisplayer[nameof(AngleSelectorDisplay.SnappingInterval)] =
                15m;
            }


            FirstGridLength = new System.Windows.GridLength(140);

            UnmodifiedVariableName = NameOnInstance;
            string displayName = StringFunctions.InsertSpacesInCamelCaseString(NameOnInstance);
            DisplayName = displayName;


            // hack! Certain ColorOperations aren't supported in MonoGame. One day they will be if we ever get the
            // shader situation solved. But until then, these cause crashes so let's remove them.
            // Do this after setting the type converter
            if (variableDefinition?.Type == nameof(FlatRedBall.Graphics.ColorOperation))
            {
                TypeConverter = null;
                // one day?
                CustomOptions = new List<object>();
                CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Texture);
                CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Add);
                CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Color);
                CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.ColorTextureAlpha);
                CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate);
                //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Subtract);
                //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.InverseTexture);
                //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate2X);
                //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.Modulate4X);
                //instanceMember.CustomOptions.Add(FlatRedBall.Graphics.ColorOperation.InterpolateColor);
            }
            else
            {
                TypeConverter = typeConverter;
            }

            IsDefault = NamedObjectSave.GetCustomVariable(NameOnInstance) == null;

            PushChangesToPluginManager = wasPushing;

        }

        Type cachedMemberType = null;
        Type MemberType
        {
            get
            {
                if (cachedMemberType == null)
                {
                    var typeName = VariableDefinition?.Type;
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        cachedMemberType = TypeManager.GetTypeFromString(typeName);
                    }

                    if(cachedMemberType == null)
                    {
                        // if we got here, we don't know the type, but it seems Glue operates on there it needing to be typeof object:
                        cachedMemberType = typeof(object);
                    }
                }
                return cachedMemberType;
            }
        }

        public object HandleCustomGet(object throwaway)
        {
            if (VariableDefinition.CustomVariableGet != null)
            {
                return VariableDefinition.CustomVariableGet(Container, NamedObjectSave, NameOnInstance);
            }
            else
            {

                return ObjectFinder.Self.GetValueRecursively(NamedObjectSave, Container, NameOnInstance, MemberType, VariableDefinition);
            }
        }

        private async void HandleVariableSet(object owner, object value)
        {
            if (GlueState.Self.CurrentGlueProject == null)
                return;
            //NamedObjectVariableChangeLogic.ReactToValueSet(instance, memberName, value, out bool makeDefault);

            //static void ReactToValueSet(NamedObjectSave instance, string memberName, object value, out bool makeDefault)
            //{
            // If setting AnimationChianList to null then also null out the CurrentChainName to prevent
            // runtime errors.
            //

            if (VariableDefinition.CustomVariableSet != null)
            {
                VariableDefinition.CustomVariableSet(Container, NamedObjectSave, NameOnInstance, value);
            }
            else
            {
                bool makeDefault = false;
                var ati = NamedObjectSave.GetAssetTypeInfo();
                var foundVariable = ati?.VariableDefinitions.FirstOrDefault(item => item.Name == VariableDefinition.Name);
                if (foundVariable?.Type == nameof(AnimationChainList))
                {
                    if (value is string && ((string)value) == "<NONE>")
                    {
                        value = null;
                        makeDefault = true;

                        // Let's also set the CurrentChainName to null
                        GlueCommands.Self.GluxCommands.SetVariableOn(
                            NamedObjectSave,
                            "CurrentChainName",
                            null);
                    }
                }
                IsDefault = makeDefault;

                // If we ignore the next refresh, then AnimationChains won't update when the user
                // picks an AnimationChainList from a combo box:
                //RefreshLogic.IgnoreNextRefresh();

                // Discussion about SetVariableOn vs SetVariableOnAsync:
                // SetVariableOn happens immediately - it does not respect
                // the task system. SetVariableOnAsync does use the task system,
                // which is safer, since setting the value immediately can cause bugs
                // due to variables changing while other tasks are running. However, if
                // SetVariableOnAsync is used, then that means the logic for setting the
                // variable will not run until the TaskManager gets to this task. If there
                // are other tasks running, then that means the variable will not get set right
                // away. This can cause the property grid to display the old value after the user
                // presses ENTER. Therefore, for now we need to use the obsolete SetVariableOn, and 
                // think of a more sophisticated solution.
                GlueCommands.Self.GluxCommands.SetVariableOn(
                NamedObjectSave,
                    NameOnInstance,
                    value, performSaveAndGenerateCode: false, updateUi: false);


                // We're going to delay updating all UI, saving, and codegen for a half second to not spam the system:
                await System.Threading.Tasks.Task.Delay(400);

                // Set subtext before refreshing property grid
                NamedObjectVariableShowingLogic.AssignVariableSubtext(NamedObjectSave, categories.ToList(), NamedObjectSave.GetAssetTypeInfo());

                IsDefault = makeDefault;

                await TaskManager.Self.AddAsync(async () =>
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(Container);
                    EditorObjects.IoC.Container.Get<GlueErrorManager>().ClearFixedErrors();

                    GlueCommands.Self.DoOnUiThread(() =>
                    {
                        MainGlueWindow.Self.PropertyGrid.Refresh();
                        PropertyGridHelper.UpdateNamedObjectDisplay();
                        if (DisplayName == "Name")
                        {
                            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(Container,
                                // We can be faster by doing only a NamedObject refresh, since the only way this could change is the Name...right?
                                FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces.TreeNodeRefreshType.NamedObjects);
                        }
                    });

                    if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.SeparateJsonFilesForElements)
                    {
                        await GlueCommands.Self.GluxCommands.SaveElementAsync(Container);
                    }
                    else
                    {
                        GlueCommands.Self.GluxCommands.SaveGlux(TaskExecutionPreference.AddOrMoveToEnd);
                    }


                }, $"Delayed task to do all updates for {NamedObjectSave}", TaskExecutionPreference.AddOrMoveToEnd);

            }
        }

        public void RefreshAddContextMenuEvents()
        {
            var isAlreadyTunneled = Container.CustomVariables.Any(item =>
                item.SourceObject == NamedObjectSave.InstanceName && item.SourceObjectProperty == NameOnInstance);

            if (!isAlreadyTunneled)
            {
                ContextMenuEvents.Add("Tunnel Variable...", (not, used) =>
                {
                    string variableToTunnel = null;
                    if (VariableDefinition != null)
                    {
                        variableToTunnel = VariableDefinition?.Name;
                    }
                    else if (!string.IsNullOrWhiteSpace(VariableDefinition.Name))
                    {
                        variableToTunnel = VariableDefinition.Name;
                    }
                    GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(
                        FlatRedBall.Glue.Controls.CustomVariableType.Tunneled,
                        NamedObjectSave.InstanceName,
                        variableToTunnel);
                });

                ContextMenuEvents[$"Tunnel as {NamedObjectSave.InstanceName}{VariableDefinition.Name}"] = (not, used) =>
                {
                    //GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog();
                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Name = NamedObjectSave.InstanceName + VariableDefinition.Name;
                    newVariable.Type = VariableDefinition.Type;
                    newVariable.SourceObject = NamedObjectSave.InstanceName;
                    newVariable.SourceObjectProperty = VariableDefinition.Name;

                    newVariable.Category = VariableDefinition?.Category;

                    GlueCommands.Self.GluxCommands.ElementCommands.AddCustomVariableToElement(newVariable, Container);

                };
            }
        }

        private void MakeDefault(NamedObjectSave instance, string memberName)
        {
            var oldValue = instance.GetCustomVariable(memberName)?.Value;


            if(PushChangesToPluginManager)
            {
                PropertyGridRightClickHelper.SetVariableToDefault(instance, memberName);
                var element = ObjectFinder.Self.GetElementContaining(instance);

                if (element != null)
                {
                    // do we want to run this async?
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }

                GlueCommands.Self.GluxCommands.SaveGlux();

                MainGlueWindow.Self.PropertyGrid.Refresh();


                PluginManager.ReactToChangedProperty(memberName, oldValue, element, new PluginManager.NamedObjectSavePropertyChange
                {
                    NamedObjectSave = instance,
                    ChangedPropertyName = memberName
                });

                PluginManager.ReactToNamedObjectChangedValueList(new List<VariableChangeArguments>
                {
                    new VariableChangeArguments
                    {
                        NamedObject = instance,
                        ChangedMember = memberName,
                        OldValue = oldValue
                    }
                });
            }
        }

        private static TypeConverter GetTypeConverter(NamedObjectSave instance, GlueElement container, string memberName, Type memberType, string customTypeName,
            VariableDefinition variableDefinition)
        {
            var toReturn = PluginManager.GetTypeConverter(
                 container, instance, memberType, memberName, customTypeName);

            if (variableDefinition?.ForcedOptions?.Count > 0)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.ForcedOptions);
                    return list;
                };
                return converter;
            }
            else if (variableDefinition?.CustomGetForcedOptionFunc != null)
            {
                var converter = new DelegateBasedTypeConverter();
                converter.CustomDelegate = () =>
                {
                    var list = new List<string>();
                    list.AddRange(variableDefinition.CustomGetForcedOptionFunc(container, instance, null));
                    return list;
                };
                return converter;
            }

            return toReturn;
        }

    }
}
