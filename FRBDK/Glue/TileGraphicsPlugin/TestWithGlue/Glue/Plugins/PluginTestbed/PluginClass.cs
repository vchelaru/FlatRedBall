using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.SaveClasses;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins;

using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.IO;
using FlatRedBall.Glue;

namespace PluginTestbed
{

    [Export(typeof(IGluxLoad))]
    public class PluginClass : IGluxLoad
    {
        [Import("GlueProjectSave")]
        public GlueProjectSave GlueProjectSave
        {
            get;
            set;
        }

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }
        
        #region INewObject Members

        public void ReactToNewObject(NamedObjectSave newNamedObject)
        {
            //MessageBox.Show("Added an object called " + newNamedObject.InstanceName);
        }

        #endregion

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Plugin class for testing"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
            //MessageBox.Show("Successfully loaded test plugin");
        }

        public bool ShutDown(PluginShutDownReason reason)
        {
            return true;
        }

        #endregion



        //#region IMenuStripPlugin Members

        //public void InitializeMenu(MenuStrip menuStrip)
        //{
        //    //MessageBox.Show("Adding to menu");
        //    //menuStrip.SuspendLayout();
        //    ToolStripMenuItem parentItem = new ToolStripMenuItem("Plugin Item");
                
        //    menuStrip.Items.Add(parentItem);

        //    parentItem.DropDownItems.Add("Item 1");
        //    parentItem.DropDownItems.Add("Item 2");
        //    parentItem.DropDownItems.Add("Item 3");
        //    parentItem.DropDownItems.Add("Item 4");

        //    //menuStrip.ResumeLayout();
        //    //menuStrip.PerformLayout();
        //}

        //#endregion

        #region IPropertyGridRightClick Members

        public void ReactToRightClick(PropertyGrid rightClickedPropertyGrid, ContextMenu menuToModify)
        {
            menuToModify.MenuItems.Add("Beef Treasure Chest");
        }

        #endregion

        #region INewFile Members

        public void ReactToNewFile(ReferencedFileSave newFile)
        {
            if (FileManager.GetExtension(newFile.Name) == "scnx")
            {
                DialogResult result = MessageBox.Show("Make an object for " +
                    "the entire scene?", "Make object?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {

                    NamedObjectSave namedObjectSave = new NamedObjectSave();
                    namedObjectSave.InstanceName = "EntireSceneInstance";

                    namedObjectSave.AddToManagers = true;

                    namedObjectSave.SourceType = SourceType.File;
                    namedObjectSave.SourceFile = newFile.Name;
                    namedObjectSave.SourceName = "Entire File (Scene)";

                    EditorLogic.CurrentElement.NamedObjects.Add(namedObjectSave);

                    GlueCommands.RefreshCommands.RefreshUiForSelectedElement();
                    GlueCommands.GluxCommands.SaveGlux();
                }
            }
        }

        #endregion

        #region IGluxLoad Members

        public void ReactToGluxLoad(GlueProjectSave newGlux, string fileName)
        {
            //MessageBox.Show("Loaded " + fileName + " and it has this many Entities " + newGlux.Entities.Count);

        }

        #endregion


        public void ReactToGluxSave()
        {
        }

        public void ReactToGluxUnload(bool isExiting)
        {
        }

        public void RefreshGlux()
        {
        }
    }
}
