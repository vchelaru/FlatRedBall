using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ScreenPlugin
{
    [Export(typeof(PluginBase))]
    public class MainScreenPlugin : PluginBase
    {
        public override void StartUp()
        {
            AssignEvents();

            AvailableAssetTypes.CommonAtis.Screen.VariableDefinitions.Add(
            new VariableDefinition
            {
                Name = nameof(FlatRedBall.Screens.Screen.DefaultLayer),
                Type = "string",
                DefaultValue = null,
                Category = "Layer",
                CustomGetForcedOptionFunc = (element, instance, file) =>
                {
                    return AvailableLayersTypeConverter.GetAvailableLayers(element as GlueElement);
                },
                UsesCustomCodeGeneration = true,
                CustomGenerationFunc = (element, instance, rfs, name) =>
                {
                    // Easiest way to handle optionally generating this is by checking the glux version here
                    if(!DoesScreenHaveDefaultLayer)
                    {
                        return null;
                    }
                    var variable = element.GetCustomVariable(name);
                    if(variable != null)
                    {
                        var value = variable.DefaultValue as string;
                        if(string.IsNullOrEmpty(value) || value.Equals("<none>", StringComparison.OrdinalIgnoreCase))
                        {
                            return null;
                        }
                        else
                        {
                            return $"this.DefaultLayer = {LayerNameToCode(value)};";
                        }
                    }
                    return null;
                }
            }
            );
        }

        bool DoesScreenHaveDefaultLayer =>
            GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.ScreensHaveDefaultLayer;

        string LayerNameToCode(string layerName)
        {
            if(layerName == AvailableLayersTypeConverter.UnderEverythingLayerName)
            {
                return AvailableLayersTypeConverter.UnderEverythingLayerCode;
            }
            else if(layerName == AvailableLayersTypeConverter.TopLayerName)
            {
                return AvailableLayersTypeConverter.TopLayerCode;
            }
            else
            {
                return layerName;
            }
        }


        private void AssignEvents()
        {
            this.ReactToNewObjectListAsync += HandleNewObjectList;

            this.ReactToGlueElementVariableChanged += HandleElementVariableChange;
        }

        private void HandleElementVariableChange(GlueElement element, CustomVariable variable, object oldValue)
        {
            List<NamedObjectSave> namedObjectsToChange = new List<NamedObjectSave>();

            if(variable.Name == nameof(FlatRedBall.Screens.Screen.DefaultLayer) && DoesScreenHaveDefaultLayer)
            {
                var screen = element as ScreenSave;

                if(screen != null)
                {
                    List<NamedObjectSave> namedObjects = new List<NamedObjectSave>();
                    HashSet<ScreenSave> screens = new HashSet<ScreenSave>();

                    GetObjectsOnOldLayer(screen, oldValue as string, namedObjects, screens);

                    if(namedObjects.Count != 0)
                    {
                        var window = new ListBoxWindowWpf();
                        var layerDisplayName = variable.DefaultValue ?? "<NONE>";
                        window.Message = $"The following objects use the layer {oldValue}.  Would you like to move the following objects to the new layer {layerDisplayName}?";
                        foreach(var item in namedObjects)
                        {
                            window.AddItem(item.ToString());
                        }

                        window.ShowYesNoButtons();
                        var result = window.ShowDialog();

                        if(result == true)
                        {
                            var layerName = screen.GetVariableValueRecursively(nameof(FlatRedBall.Screens.Screen.DefaultLayer)) as string;
                            foreach(var item in namedObjects)
                            {
                                if(layerName != "<NONE>")
                                {
                                    item.LayerOn = layerName;
                                }
                                else
                                {
                                    item.LayerOn = null;
                                }
                            }

                            foreach(var screenToSave in screens)
                            {
                                GlueCommands.Self.GluxCommands.SaveElementAsync(screenToSave);
                            }
                        }

                    }
                }
            }
        }

        private void GetObjectsOnOldLayer(ScreenSave screen, string oldLayerOn, List<NamedObjectSave> namedObjects, HashSet<ScreenSave> screens)
        {
            foreach(var nos in screen.NamedObjects)
            {
                if(nos.LayerOn == oldLayerOn && CanBeLayered(nos))
                {
                    namedObjects.Add(nos);
                    screens.Add(screen);
                }
                foreach (var subNos in nos.ContainedObjects)
                {
                    if (subNos.LayerOn == oldLayerOn && CanBeLayered(subNos))
                    {
                        namedObjects.Add(subNos);
                        screens.Add(screen as ScreenSave);
                    }
                }
            }

            var derivedScreens = ObjectFinder.Self.GetAllDerivedElementsRecursive(screen);

            foreach(var derivedScreen in derivedScreens)
            {
                foreach (var nos in derivedScreen.NamedObjects)
                {
                    if (nos.LayerOn == oldLayerOn && CanBeLayered(nos))
                    {
                        namedObjects.Add(nos);
                        screens.Add(derivedScreen as ScreenSave);
                    }
                    foreach(var subNos in nos.ContainedObjects)
                    {
                        if(subNos.LayerOn == oldLayerOn && CanBeLayered(subNos))
                        {
                            namedObjects.Add(subNos);
                            screens.Add(derivedScreen as ScreenSave);
                        }
                    }
                }
            }

            bool CanBeLayered(NamedObjectSave nos)
            {
                if(nos.SourceType == SourceType.Entity || nos.GetAssetTypeInfo()?.LayeredAddToManagersMethod?.Count > 0)
                {
                    // todo - need to figure out the defined by base/instantiate in derived. For now, we will only include
                    // objects that are defined in the screen
                    if(nos.DefinedByBase == false)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private async Task HandleNewObjectList(List<NamedObjectSave> list)
        {
            if(DoesScreenHaveDefaultLayer)
            {
                await TaskManager.Self.AddAsync(() =>
                {
                    foreach(var item in list)
                    {
                        var screen = ObjectFinder.Self.GetElementContaining(item) as ScreenSave;

                        if(screen != null)
                        {
                            var layerName = screen.GetVariableValueRecursively(nameof(FlatRedBall.Screens.Screen.DefaultLayer)) as string;

                            if(layerName != "<NONE>")
                            {
                                item.LayerOn =  layerName;
                            }
                        }
                    }
                }, "Setting LayerOn for newly created NamedObjects");
            }
        }
    }
}
