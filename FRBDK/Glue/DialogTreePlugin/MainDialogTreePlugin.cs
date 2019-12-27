using System;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using DialogTreePlugin.Views;
using DialogTreePlugin.Controllers;
using FlatRedBall.Glue.SaveClasses;
using DialogTreePlugin.Generators;
using EditorObjects.Parsing;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.CodeGeneration;
using Newtonsoft.Json;
using static DialogTreePlugin.SaveClasses.DialogTreeRaw;

namespace DialogTreePlugin
{
    [System.ComponentModel.Composition.Export(typeof(PluginBase))]
    public class MainDialogTreePlugin : PluginBase
    {
        #region Fields/Properties

        MainControl mainControl;

        public override string FriendlyName => "Dialog Tree Plugin";

        //v1.1.0 The string keys are no longer editable.
        // - We can now dynamically size the columns for any number of languages.
        //v1.2.0 removed unused files and general code cleanup.
        // - Removed the extra blank line from the data grid.
        // - Implemented code-gen for the dialog tree files so Design can use DialogTrees.FileName
        //v2.0.0 Implementing .json -> .glsn conversion.
        // - We now auto generate the string ids for the dialog trees.
        //v2.1.0 Plugin generates string constants for tags.
        // - Modifed the dialog tree name constant to use the file name.
        //v2.2.0 Generating a switch statement for the consts so we can set the tree from tiled
        //v3.0.0 Code gen now generates static references to each tree which will be cached after deserialization.
        // - There is a option to clear the trees.
        // - Preserving the story name in the string ids to help design find troublesome strings.
        //V3.1.0 Fixed a bug where we were keeping track of the same tag multiple times.
        //V3.2.0 Fixed a bug where we were deleting LocalizationDB entries which were not added by the plugin.
        // 4.0.0
        // - New dialog tree plugin that doesn't use source and reduced files.
        public override Version Version => new Version(4, 0, 0);

        const string rawFileType = "json";
        const string convertedFileType = "glsn";

        private AssetTypeInfo mJsonType;
        AssetTypeInfo JsonAti
        {
            get
            {
                if (mJsonType == null)
                {
                    mJsonType = new AssetTypeInfo();
                    mJsonType.FriendlyName = "Raw Dialog Tree Json (.json)";
                    mJsonType.QualifiedRuntimeTypeName = new PlatformSpecificType();
                    mJsonType.QualifiedRuntimeTypeName.QualifiedType = "DialogTreePlugin.SaveClasses.DialogTreeRaw.RootObject";

                    mJsonType.QualifiedSaveTypeName = null;
                    mJsonType.Extension = "json";
                    mJsonType.AddToManagersMethod = new List<string>();
                    mJsonType.CustomLoadFunc = GetRawDialogTreeLoadCode;
                    mJsonType.DestroyMethod = null;
                    mJsonType.SupportsMakeOneWay = false;
                    mJsonType.ShouldAttach = false;
                    mJsonType.MustBeAddedToContentPipeline = false;
                    mJsonType.CanBeCloned = false;
                    mJsonType.HasCursorIsOn = false;
                    mJsonType.HasVisibleProperty = false;
                    mJsonType.CanIgnorePausing = false;
                    mJsonType.HideFromNewFileWindow = false;
                }

                return mJsonType;
            }
        }

        private string GetRawDialogTreeLoadCode(IElement element, NamedObjectSave namedObject, ReferencedFileSave referencedFile, string contentManager)
        {

            var fileName = ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(referencedFile, referencedFile.GetAssetTypeInfo());

            return $"{referencedFile.GetInstanceName()} = DialogTreePlugin.SaveClasses.DialogTreeRaw.RootObject.FromJson(\"{fileName}\");";
        }

        private AssetTypeInfo mGlsnType;
        AssetTypeInfo GlsnAti
        {
            get
            {
                if (mGlsnType == null)
                {
                    mGlsnType = new AssetTypeInfo();
                    mGlsnType.FriendlyName = "Dialog Tree Json (.glsn)";
                    mGlsnType.QualifiedRuntimeTypeName = new PlatformSpecificType();

                    mGlsnType.QualifiedSaveTypeName = null;
                    mGlsnType.Extension = "glsn";
                    mGlsnType.AddToManagersMethod = new List<string>();
                    mGlsnType.CustomLoadMethod = null;
                    mGlsnType.DestroyMethod = null;
                    mGlsnType.SupportsMakeOneWay = false;
                    mGlsnType.ShouldAttach = false;
                    mGlsnType.MustBeAddedToContentPipeline = false;
                    mGlsnType.CanBeCloned = false;
                    mGlsnType.HasCursorIsOn = false;
                    mGlsnType.HasVisibleProperty = false;
                    mGlsnType.CanIgnorePausing = false;
                    mGlsnType.HideFromNewFileWindow = false;
                }

                return mGlsnType;
            }
        }

        #endregion

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            var toReturn = true;

            return toReturn;
        }

        public override void StartUp()
        {
            AddEvents();

            JsonToGlsnConverter.Self.currentPluginVersion = Version.ToString();
        }

        private void AddEvents()
        {
            this.ReactToLoadedGluxEarly += HandleEarlyGluxLoad;
            this.ReactToLoadedGlux += GetJsonRefs;
            this.ReactToItemSelectHandler += HandleItemSelected;
            this.ReactToFileChangeHandler += HandleFileChanged;
            this.ReactToNewFileHandler += HandleNewFile;
        }

        private void HandleEarlyGluxLoad()
        {
            AddAssetTypeIfNotPresent(JsonAti);
            AddAssetTypeIfNotPresent(GlsnAti);
        }

        private void AddAssetTypeIfNotPresent(AssetTypeInfo ati)
        {
            if (AvailableAssetTypes.Self.AllAssetTypes.Any(item => item.FriendlyName == ati.FriendlyName) == false)
            {
                AvailableAssetTypes.Self.AddAssetType(ati);
            }
        }

        private void GetJsonRefs()
        {
            //Generate the dialog tree file constants.
            var jsonRefs = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Where(item => FileManager.GetExtension(item.Name) == rawFileType);
            if(jsonRefs.Count() > 0)
            {
                //foreach(var fileRef in jsonRefs)
                //{
                //    JsonToGlsnConverter.Self.HandleJsonFile(fileRef, isGlueLoad: true);
                //}

                RootObjectCodeGenerator.Self.GenerateAndSave();

            }
        }

        private void HandleItemSelected(TreeNode selectedTreeNode)
        {
            bool shouldShow = GlueState.Self.CurrentReferencedFileSave != null &&
                selectedTreeNode?.Tag == GlueState.Self.CurrentReferencedFileSave &&
                GlueState.Self.CurrentReferencedFileSave.Name.EndsWith(convertedFileType);

            if (shouldShow)
            {
                if (mainControl == null)
                {
                    mainControl = TabConroller.Self.GetControl();
                    this.AddToTab(PluginManager.CenterTab, mainControl, "Dialog Tree");
                }
                else
                {
                    this.AddTab();
                }

                TabConroller.Self.UpdateTo(GlueState.Self.CurrentReferencedFileSave);
            }
            else
            {
                this.RemoveTab();
            }
        }
        private void HandleNewFile(ReferencedFileSave newFile)
        {
            //If the new file is a .json, add it to the dialog tree constants.
            //if(newFile.Name.EndsWith(rawFileType))
            //{ 
            //    JsonToGlsnConverter.Self.HandleJsonFile(newFile);
            //}
            // if the file is a JSON file and properly deserializes to a root object, then set its asset type info:
            var shouldAssignType = false;
            if(newFile.Name.EndsWith(".json"))
            {
                try
                {
                    var fullFile = GlueCommands.Self.GetAbsoluteFileName(newFile);
                    var content = System.IO.File.ReadAllText(fullFile);
                    if(!string.IsNullOrEmpty(content))
                    {
                        var parsed = JsonConvert.DeserializeObject<RootObject>(content);

                        shouldAssignType = parsed?.creator != null;
                    }

                }
                catch
                {

                }
            }

            if(shouldAssignType)
            {
                newFile.RuntimeType = JsonAti.QualifiedRuntimeTypeName.QualifiedType;
            }
        }

        private void HandleFileChanged(string fileName)
        {
            if (fileName.EndsWith(TabConroller.RelativeToGlobalContentLocalizationDbCsvFile))
            {
                TabConroller.Self.ReactToLocalizationDbChange();
            }

            if(fileName.EndsWith(rawFileType))
            {
                //var file = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().FirstOrDefault(item => fileName.EndsWith(item.Name));
                //if(file != null)
                //{
                //    JsonToGlsnConverter.Self.HandleJsonFile(file);
                //}
            }
        }
    }
}
