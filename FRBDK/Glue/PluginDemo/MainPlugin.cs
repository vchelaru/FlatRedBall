using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;

namespace PluginDemo
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        PluginControl pluginControl;
        PluginTab pluginTab;

        public override string FriendlyName
        {
            get { return "Demo for Jesse and Rick"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
            this.ReactToItemSelectHandler += HandleItemSelect;
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.GetFilesReferencedBy += HandleGetReferencedFiles;

        }

        private void HandleGetReferencedFiles(string fileName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive, List<string> referencedFiles)
        {

        }

        private void HandleFileChanged(string fileName)
        {
            var extension = FlatRedBall.IO.FileManager.GetExtension(fileName);

            if (extension == "xui")
            {
                

            }
        }





        private void HandleItemSelect(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            //object tag = selectedTreeNode.Tag;

            //if(tag is EntitySave)
            //{
            //    AddOrShowTab();
            //}
            //else
            //{
            //    RemoveTab(pluginTab);
            //}

            //var glueState = GlueState.Self;
            //var selectedObject = glueState.CurrentNamedObjectSave;
            //var rfs = glueState.CurrentReferencedFileSave;

            //if (rfs != null)
            //{
            //    bool shouldShow = rfs.Name.EndsWith(".tmx");
            //}

            //if (selectedObject != null)
            //{
            //    AddOrShowTab();
            //    pluginControl.WhatToShow = "You just selected " + selectedObject.ToString();
            //}
            //else
            //{
            //    RemoveTab();
            //}
        }

        private void AddOrShowTab()
        {

            if (pluginControl == null)
            {
                pluginControl = new PluginControl();
                pluginTab = this.AddToTab(PluginManager.LeftTab, pluginControl, "Demo");
            }
            else
            {
                this.ShowTab(pluginTab);
            }
        }




        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            // todo - clean stuff up

            return true;
        }
    }
}
