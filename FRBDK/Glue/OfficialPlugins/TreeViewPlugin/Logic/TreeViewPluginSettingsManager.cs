using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Newtonsoft.Json;
using OfficialPlugins.TreeViewPlugin.Models;
using OfficialPlugins.TreeViewPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.Logic
{
    internal static class TreeViewPluginSettingsManager
    {
        public const string RelativePath = "GlueSettings/TreeViewPlugin.settings.user.json";


        static FilePath SettingsFullFile => GlueState.Self.CurrentGlueProject == null
            ? null
            : GlueState.Self.CurrentGlueProjectDirectory + RelativePath;

        public static TreeViewPluginSettings LoadSettings()
        {
            try
            {
                if(SettingsFullFile?.Exists() == true)
                {
                    var json = System.IO.File.ReadAllText(SettingsFullFile.FullPath);

                    return JsonConvert.DeserializeObject<TreeViewPluginSettings>(json);
                }

            }
            catch(Exception ex)
            {
                GlueCommands.Self.PrintError($"Error loading TreeView settings:\n{ex}");
            }
            return null;
        }

        public static void SaveSettings(TreeViewPluginSettings settings)
        {
            try
            {
                if(SettingsFullFile != null && settings != null)
                {
                    var serialized = JsonConvert.SerializeObject(settings);

                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        System.IO.Directory.CreateDirectory(SettingsFullFile.GetDirectoryContainingThis().FullPath);

                        GlueCommands.Self.FileCommands.SaveIfDiffers(SettingsFullFile, serialized);
                    });
                }
            }
            catch(Exception ex)
            {
                GlueCommands.Self.PrintError($"Error saving TreeView settings:\n{ex}");
            }
        }

        internal static TreeViewPluginSettings CreateSettingsFrom(MainTreeViewViewModel mainViewModel)
        {
            var settings = new TreeViewPluginSettings();

            settings.TreeNodeStates.Add(CreateTreeNodeStateFor(mainViewModel.EntityRootNode));
            settings.TreeNodeStates.Add(CreateTreeNodeStateFor(mainViewModel.ScreenRootNode));
            settings.TreeNodeStates.Add(CreateTreeNodeStateFor(mainViewModel.GlobalContentRootNode));

            return settings;
        }

        private static TreeNodeState CreateTreeNodeStateFor(NodeViewModel nodeViewModel)
        {
            var treeNodeState = new TreeNodeState();
            treeNodeState.IsExpanded = nodeViewModel.IsExpanded;
            treeNodeState.Text = nodeViewModel.Text;

            foreach(var viewModelChild in nodeViewModel.Children)
            {
                treeNodeState.Children.Add(CreateTreeNodeStateFor(viewModelChild));
            }

            return treeNodeState;
        }

        internal static void ApplySettingsToViewModel(TreeViewPluginSettings settings, MainTreeViewViewModel viewModel)
        {
            var entitiesSettings = settings.TreeNodeStates.Find(item => item.Text == viewModel.EntityRootNode.Text);
            if(entitiesSettings != null)
            {
                ApplySettingsToNodeViewModel(entitiesSettings, viewModel.EntityRootNode);
            }

            var screenSettings = settings.TreeNodeStates.Find(item => item.Text == viewModel.ScreenRootNode.Text);
            if(screenSettings != null)
            {
                ApplySettingsToNodeViewModel(screenSettings, viewModel.ScreenRootNode);
            }

            var globalContentSettings = settings.TreeNodeStates.Find(item => item.Text == viewModel.GlobalContentRootNode.Text);
            if(globalContentSettings != null)
            {
                ApplySettingsToNodeViewModel(globalContentSettings, viewModel.GlobalContentRootNode);
            }

        }

        private static void ApplySettingsToNodeViewModel(TreeNodeState treeNodeState, NodeViewModel nodeViewModel)
        {
            nodeViewModel.IsExpanded = treeNodeState.IsExpanded;

            foreach(var childState in treeNodeState.Children)
            {
                var matchingVm = nodeViewModel.Children.FirstOrDefault(item => item.Text == childState.Text);

                if(matchingVm != null)
                {
                    ApplySettingsToNodeViewModel(childState, matchingVm);
                }
            }
        }
    }
}
