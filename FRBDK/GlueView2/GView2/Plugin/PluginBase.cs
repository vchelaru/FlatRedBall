using GlueView2.AppState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.AvalonDock.Layout;

namespace GlueView2.Plugin
{
    public abstract class PluginBase : IPlugin
    {

        List<LayoutAnchorable> LayoutAnchorables = new List<LayoutAnchorable>();

        public abstract string FriendlyName { get; }

        public string UniqueId
        {
            get;
            set;
        }

        public abstract Version Version { get; }


        public abstract void StartUp();
        public abstract bool ShutDown(PluginShutDownReason shutDownReason);

        protected LayoutAnchorable AddUi(UIElement element)
        {
            var layoutAnchorable = GlueViewState.Self.MainWindow.AddTab(element);

            LayoutAnchorables.Add(layoutAnchorable);

            return layoutAnchorable;
        }
    }
}
