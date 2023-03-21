using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainErrorReportingPlugin : PluginBase
    {
        public override string FriendlyName => "Error reporting plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AddErrorReporter(new NamedObjectSaveErrorReporter());
            AddErrorReporter(new ReferencedFileSaveErrorReporter());

            this.ReactToFileChangeHandler += HandleFileChanged;
            //this.ReactToNamedObjectChangedValue += HandleNamedObjectChangedValue;
            this.ReactToChangedNamedObjectPropertyList += HandleChangedNamedObjectPropertyList;
        }

        private async void HandleChangedNamedObjectPropertyList(List<PluginManager.NamedObjectSavePropertyChange> obj)
        {
            await GlueCommands.Self.RefreshCommands.ClearFixedErrors();
            this.RefreshErrors();
        }

        //private async void HandleNamedObjectChangedValue(string changedMember, object oldValue, NamedObjectSave namedObject)
        //{
        //    await GlueCommands.Self.RefreshCommands.ClearFixedErrors();
        //    this.RefreshErrors();
        //}


        private void HandleFileChanged(string fileName)
        {
            this.RefreshErrors();
        }
    }
}
