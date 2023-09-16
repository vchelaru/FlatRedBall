using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.FormHelpers;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin;
using GlueFormsCore.ViewModels;
using Newtonsoft.Json;
using OfficialPluginsCore.Wizard.Managers;
using OfficialPluginsCore.Wizard.Models;
using OfficialPluginsCore.Wizard.ViewModels;
using OfficialPluginsCore.Wizard.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WpfDataUi;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace OfficialPluginsCore.Wizard
{
    [Export(typeof(PluginBase))]
    public class MainWizardPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "New Project Wizard";

        public override Version Version => new Version(1, 1);

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
            
            AddMenuItemTo(Localization.Texts.CreateObjectsJson, Localization.MenuIds.CreateObjectsJsonId, HandleCreateObjectsJsonClicked, Localization.MenuIds.ProjectId);
        }

        private void HandleCreateObjectsJsonClicked()
        {
            var window = new CreateObjectJsonSelectionWindow();

            var vm = new CreateObjectJsonViewModel();

            ElementViewModel ToVm(GlueElement element)
            {
                var toReturn = new ElementViewModel();

                toReturn.Name = element.Name;

                return toReturn;
            }

            NamedObjectSaveViewModel ToNosVm(NamedObjectSave nos)
            {
                var toReturn = new NamedObjectSaveViewModel();
                toReturn.TextDisplay = nos.InstanceName;
                toReturn.IsEnabled = nos.DefinedByBase == false;
                toReturn.BackingObject = nos;
                toReturn.PropertyChanged += HandleNosVmPropertyChanged;

                foreach(var subNos in nos.ContainedObjects)
                {
                    toReturn.ContainedObjects.Add(ToNosVm(subNos));
                }

                return toReturn;
            }
            void HandleNosVmPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if(e.PropertyName == nameof(NamedObjectSaveViewModel.IsSelected))
                {
                    UpdateGeneratedJson();
                }
            }

            void UpdateGeneratedJson()
            {
                Dictionary<string, List<NamedObjectSave>> toSerialize = new Dictionary<string, List<NamedObjectSave>>();

                foreach(var element in vm.Elements)
                {
                    foreach(var nosVm in element.NamedObjects)
                    {
                        if(nosVm.IsSelected)
                        {
                            if(toSerialize.ContainsKey(element.Name) == false)
                            {
                                toSerialize.Add(element.Name, new List<NamedObjectSave>());
                            }

                            // We only want to include top-level objects, so if it's got contained children, add a clone without children:
                            if(nosVm.BackingObject.ContainedObjects?.Count > 0)
                            {
                                var clone = nosVm.BackingObject.Clone();
                                clone.ContainedObjects.Clear();
                                toSerialize[element.Name].Add(clone);
                            }
                            else
                            {
                                // Just add it as-is, no need for clones...
                                toSerialize[element.Name].Add(nosVm.BackingObject);
                            }
                        }
                        foreach(var subnosVm in nosVm.ContainedObjects)
                        {
                            if(subnosVm.IsSelected)
                            {
                                if(toSerialize.ContainsKey(element.Name) == false)
                                {
                                    toSerialize.Add(element.Name, new List<NamedObjectSave>());
                                }

                                // subs cannot contain subs, so we can add without clearing out the ContainedObjects
                                toSerialize[element.Name].Add(subnosVm.BackingObject);
                            }
                        }
                    }
                }

                

                vm.GeneratedJson = JsonConvert.SerializeObject(toSerialize, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                });
            }

            foreach (var screen in GlueState.Self.CurrentGlueProject.Screens)
            {
                var screenVm = ToVm(screen);
                vm.Elements.Add(screenVm);
                foreach(var nos in screen.NamedObjects)
                {
                    screenVm.NamedObjects.Add(ToNosVm(nos));
                }
            }
            foreach (var entity in GlueState.Self.CurrentGlueProject.Entities)
            {
                var entityVm = ToVm(entity);
                vm.Elements.Add(entityVm);
                foreach(var nos in entity.NamedObjects)
                {
                    entityVm.NamedObjects.Add(ToNosVm(nos));
                }
            }

            window.DataContext = vm;

            window.ShowDialog();
        }


        private void AssignEvents()
        {
            this.ReactToTreeViewRightClickHandler += HandleTreeViewRightClick;
        }

        private void HandleTreeViewRightClick(ITreeNode rightClickedTreeNode, List<GeneralToolStripMenuItem> menuToModify)
        {
            if(rightClickedTreeNode.Tag is NamedObjectSave nos)
            {
                menuToModify.Add("Copy Object JSON to Clipboard", (not, used) =>
                {
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Formatting = Formatting.Indented;
                    jsonSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    var serialized = JsonConvert.SerializeObject(nos, jsonSettings);

                    Clipboard.SetText(serialized);
                });
            }
        }

        public void RunWizard()
        {
            var window = new WizardWindow();

            GlueCommands.Self.DialogCommands.MoveToCursor(window);

            window.DoneClicked += () => WizardProjectLogic.Self.Apply(window.WizardData);

            window.ShowDialog();

        }

    }
}
