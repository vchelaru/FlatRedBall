using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Newtonsoft.Json;
using OfficialPlugins.TreeViewPlugin.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.Logic
{
    internal static class TreeViewPluginSettingsManager
    {
        public const string RelativePath = "GlueSettings/TreeViewPlugin.settings.user.json";

        public static TreeViewPluginSettings CurrentSettings { get; private set; } = new TreeViewPluginSettings();

        static FilePath SettingsFullFile => GlueState.Self.CurrentGlueProject == null
            ? null
            : GlueState.Self.CurrentGlueProjectDirectory + RelativePath;

        public static void LoadSettings()
        {
            try
            {
                if(SettingsFullFile?.Exists() == true)
                {
                    var json = System.IO.File.ReadAllText(SettingsFullFile.FullPath);

                    CurrentSettings = JsonConvert.DeserializeObject<TreeViewPluginSettings>(json);
                }

            }
            catch(Exception ex)
            {
                GlueCommands.Self.PrintError($"Error loading TreeView settings:\n{ex}");
            }

        }

        public static void SaveSettings()
        {
            try
            {
                if(SettingsFullFile != null && CurrentSettings != null)
                {
                    var serialized = JsonConvert.SerializeObject(CurrentSettings);

                    GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(SettingsFullFile.FullPath, serialized));
                }
            }
            catch(Exception ex)
            {
                GlueCommands.Self.PrintError($"Error saving TreeView settings:\n{ex}");
            }
        }
    }
}
