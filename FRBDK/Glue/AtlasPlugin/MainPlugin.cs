using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using EditorObjects.Parsing;
using FlatRedBall.Instructions.Reflection;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using AtlasPlugin.TypeConverters;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using AtlasPlugin.CodeGeneration;
using AtlasPlugin.Controls;
using AtlasPlugin.ViewModels;
using AtlasPlugin.Managers;
using FlatRedBall.Glue.Managers;
using System.Windows.Forms;
using System.Drawing;

namespace AtlasPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        // Plugin history:
        // Initially my thought was to have the workflow be that a user can add a PNG to Glue the same
        // as when working without atlases. Then the user could switch that PNG over to an AtlasedTexture,
        // and it would "just work" somehow. There are a few problems with this.
        // The first is that we can't use the same AssetTypeInfo or variable name, so that means we have to
        // create a new variable name, making GlueView no longer "just work". Furthermore, we don't want the
        // user to have to manage the PNGs both in the atlas file as well as in the Entity/Screen files. They
        // should just do it in one place (the atlas file), and that's it. That means that the files will no longer
        // show up in Glue.
        // What we'll do instead is have the user pick an atlas and a texture. This will result in code gen grabbing the
        // atlas from the right location, and grabbing the texture out of the atlas. This will require the generated code
        // to do more than simply assigning values, which means we can't have the atlas and texture be simple variables. 
        // But if they're more than just variables...how do we store what those values are? Variables are built to do that, but
        // we need custom codegen for those variables.
        // So do I make Glue support custom codegen for variables? Or do I make them non-variables, and have the data stored elsewhere.
        // Maybe variables will have a UsesStandardGeneration value... If it's false, then plugins can get a chance at making codegen for 
        // them.

        MainAtlasControl control;

        AtlasFileManager atlasFileManager;

        ToolStripMenuItem addSpriteSheetMenuItem;

        public override string FriendlyName
        {
            get
            {
                return "Atlas Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            AtiManager.Self.RemoveAllAtis();
            return true;
        }

        public override void StartUp()
        {
            atlasFileManager = new AtlasFileManager();

            CreateAtis();

            this.GetTypeConverter += HandleGetTypeConverter;

            this.CanFileReferenceContent += HandleCanFileReferenceContent;

            this.GetFilesReferencedBy += HandleGetFilesReferencedBy;

            this.WriteInstanceVariableAssignment += HandleWriteInstanceVariableAssignment;

            this.ReactToItemSelectHandler += HandleItemSelected;

            this.ReactToTreeViewRightClickHandler += RightClickManager.Self.HandleTreeViewRightClick;

            addSpriteSheetMenuItem = this.AddMenuItemTo("Add New Texture Packer Sprite Sheet", HandleAddNewTps, "Content");
            addSpriteSheetMenuItem.Image = new Bitmap(AtlasPlugin.Resource1.TexturePackerIcon);

            this.ReactToFileChangeHandler += HandleFileChange;
        }

        private void HandleAddNewTps(object sender, EventArgs e)
        {
            atlasFileManager.CreateNewProject();
        }

        private void HandleItemSelected(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            bool shouldShowTab = GetIfShouldShowTpsTab(selectedTreeNode);

            if(shouldShowTab)
            {

                if (control == null)
                {
                    control = new MainAtlasControl();
                    var viewModel = new AtlasListViewModel();
                    control.DataContext = viewModel;
                    atlasFileManager.ViewModel = viewModel;
                    this.AddToTab(PluginManager.CenterTab, control, "TPS Properties");
                }
                else
                {
                    this.AddTab();    
                }

                atlasFileManager.SetRfs(selectedTreeNode.Tag as ReferencedFileSave);
            }
            else
            {
                atlasFileManager.SetRfs(null);
                this.RemoveTab();
            }

        }

        private bool GetIfShouldShowTpsTab(System.Windows.Forms.TreeNode selectedTreeNode)
        {
            bool shouldShowTab = false;

            if (selectedTreeNode != null)
            {
                var referencedFile = selectedTreeNode.Tag as ReferencedFileSave;

                if (referencedFile != null)
                {
                    var name = referencedFile.Name;

                    var extension = FileManager.GetExtension(name);

                    shouldShowTab = extension == "tps";
                }
            }
            return shouldShowTab;
        }

        private void HandleWriteInstanceVariableAssignment(NamedObjectSave instance, ICodeBlock code, InstructionSave variable)
        {
            VariableAssignmentCodeGenerator.HandleWriteInstanceVariableAssignment(instance, code, variable);
        }

        private TypeConverter HandleGetTypeConverter(IElement container, NamedObjectSave instance, TypedMemberBase member)
        {
            if(member.CustomTypeName == "AtlasedTextureName")
            {
                return new AtlasTextureTypeConverter();
            }
            else
            {
                return null;
            }
        }

        private void HandleGetFilesReferencedBy(string fileName, TopLevelOrRecursive depth, List<string> referencedFiles)
        {
            var extension = FileManager.GetExtension(fileName);

            if (extension == "atlas")
            {
                var associatedPng = FileManager.RemoveExtension(fileName) + ".png";

                referencedFiles.Add(associatedPng);

                // Extend this in the future if we support multiple pages, etc
            }
        }

        private bool HandleCanFileReferenceContent(string fileName)
        {
            var extension = FileManager.GetExtension(fileName);

            return extension == "atlas";
        }

        private void HandleFileChange(string filename)
        {
            if (FileManager.GetExtension(filename) == "tps")
            {
                TaskManager.Self.AddAsyncTask(
                    atlasFileManager.LoadedFile.CreateAtlasFiles,
                    "Rebuilding Atlas Files");
            }
        }



        private void CreateAtis()
        {
            AtiManager.Self.PerformStartupLogic();
        }

        //private void AddAtlasedTextureAti()
        //{
        //    var allAssetTypes = AvailableAssetTypes.Self.AllAssetTypes;
        //    // We'll create a copy of the Texture ATI which doesn't copy the file:

        //    // For now we'll just support PNGs, but we could extend support
        //    var existingPng = allAssetTypes.FirstOrDefault(item => item.Extension == "png");

        //    atlasedPng = FileManager.CloneObject<AssetTypeInfo>(existingPng);
        //    atlasedPng.QualifiedRuntimeTypeName.QualifiedType = "FlatRedBall.Graphics.Texture.AtlasedTexture";
        //    atlasedPng.ExcludeFromContentProject = true;


        //    atlasedPng.CustomLoadMethod =
        //        "{THIS} = TexturePackerLoader.SpriteSheetLoader.LoadAtlasedTexture(\"{FILE_NAME}\", {CONTENT_MANAGER_NAME});";

        //    AvailableAssetTypes.Self.AddAssetType(atlasedPng);
        //}
    }
}
