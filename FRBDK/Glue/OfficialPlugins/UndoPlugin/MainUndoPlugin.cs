using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.UndoPlugin.NewFolder;
using OfficialPlugins.UndoPlugin.Views;
using OfficialPluginsCore.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.UndoPlugin
{
    [Export(typeof(PluginBase))]
    public class MainUndoPlugin : PluginBase
    {
        public override string FriendlyName => "Undo Plugin";

        public override Version Version => new Version(1,0);

        UndoDisplay view;
        ViewModels.UndoViewModel ViewModel;

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
            CreateView();
            CreateViewModel();
        }

        private void CreateViewModel()
        {
            ViewModel = new ViewModels.UndoViewModel();
            UndoManager.UndoViewModel = ViewModel;
            view.DataContext = ViewModel;
        }

        private void CreateView()
        {
            view = new UndoDisplay();
            this.CreateAndAddTab(view, "Undo", TabLocation.Bottom);
        }

        private void AssignEvents()
        {
            this.ReactToCtrlKey += UndoManager.ReactToCtrlKey;
            //this.ReactToChangedNamedObjectPropertyList += UndoManager.;
            this.ReactToNamedObjectChangedValueList += UndoManager.HandleNamedObjectValueChanges;

            this.ReactToLoadedGlux += () => ViewModel?.Undos.Clear();
        }

        public void Undo() => UndoManager.HandleUndo();
    }
}
