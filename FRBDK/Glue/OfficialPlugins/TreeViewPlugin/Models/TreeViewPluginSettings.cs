using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.Models
{
    class TreeViewPluginSettings
    {
        public const string RelativePath = "GlueSettings/TreeViewPlugin.settings.user.json";
        public List<TreeNodeState> TreeNodeStates { get; set; } = new List<TreeNodeState>();


    }
}
