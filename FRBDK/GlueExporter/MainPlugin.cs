using FlatRedBall.Gum.Managers;
using Gum.Plugins.BaseClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum
{
        [Export(typeof(PluginBase))]
    class MainPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get { return "FlatRedBall Glue Exporter"; }
        }

        public override Version Version
        {
            get {  return new Version(1,0,0,0) ; }
        }

        public override void StartUp()
        {
            CreateMenuItems();
        }

        private void CreateMenuItems()
        {

            var item = AddMenuItem(new string[] { "Export", "Entire .glux" });
            item.Click += ExportManager.Self.PerformExport;
        }

        public override bool ShutDown(global::Gum.Plugins.PluginShutDownReason shutDownReason)
        {
            return true;
        }
    }
}
