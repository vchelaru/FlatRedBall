using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Glue;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace FlatRedBall.Glue.Managers
{
    public class HotkeyManager : Singleton<HotkeyManager>
    {
        #region old TryHandleKeys taking the Keys parameter - not used anymore
        // Sept 4, 2022 - I don't think this is used anymore:
        //public async Task<bool> TryHandleKeys(Keys keyData)
        //{
        //    switch (keyData)
        //    {
        //        // CTRL+F, control f, search focus, ctrl f, ctrl + f
        //        case Keys.Control | Keys.F:
        //            PluginManager.ReactToCtrlF();
        //            return true;
        //        case Keys.Control | Keys.C:
        //            CopyPasteManager.Self.HandleCopy();
        //            return true;
        //        case Keys.Control | Keys.V:
        //            CopyPasteManager.Self.HandlePaste();
        //            return true;
        //        case Keys.Alt | Keys.Left:
        //            TreeNodeStackManager.Self.GoBack();
        //            return true;
        //        case Keys.Alt | Keys.Right:
        //            TreeNodeStackManager.Self.GoForward();
        //            return true;
        //        case Keys.Alt | Keys.Up:
        //            RightClickHelper.MoveSelectedObjectUp();
        //            return true;
        //        case Keys.Alt | Keys.Down:
        //            RightClickHelper.MoveSelectedObjectDown();
        //            return true;
        //        case Keys.Alt | Keys.Shift | Keys.Down:
        //            RightClickHelper.MoveToBottom();
        //            return true;
        //        case Keys.Alt | Keys.Shift | Keys.Up:
        //            RightClickHelper.MoveToTop();
        //            return true;
        //        case Keys.F5:
        //            await PluginManager.CallPluginMethodAsync(
        //                "Glue Compiler",
        //                "BuildAndRun");
        //            return true;
        //        case Keys.F12:
        //            GlueCommands.Self.DialogCommands.GoToDefinitionOfSelection();
        //            return true;
        //        case Keys.Delete:
        //            HandleDeletePressed();
        //            return true;
        //        default:
        //            return false;
        //    }
        //}
        #endregion

        public async Task<bool> TryHandleKeys(System.Windows.Input.KeyEventArgs e, bool isTextBoxFocused)
        {
            var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            var shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            switch (key)
            {
                case System.Windows.Input.Key.F:
                    if(ctrlDown)
                    {
                        PluginManager.ReactToCtrlF();
                        return true;
                    }
                    break;

                case System.Windows.Input.Key.C:
                    if (ctrlDown && !isTextBoxFocused)
                    {
                        CopyPasteManager.Self.HandleCopy();
                        return true;
                    }
                    break;
                case System.Windows.Input.Key.V:
                    if (ctrlDown && !isTextBoxFocused)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        // Don't await this or it freezes Glue
                        CopyPasteManager.Self.HandlePaste();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        return true;
                    }
                    break;
                case Key.Left:
                    if(altDown)
                    {
                        TreeNodeStackManager.Self.GoBack();
                        return true;
                    }
                    break;
                case Key.Right:
                    if (altDown)
                    {
                        TreeNodeStackManager.Self.GoForward();
                        return true;
                    }
                    break;
                case Key.Up:
                    if (altDown)
                    {
                        if(shiftDown)
                        {
                            RightClickHelper.MoveToTop();
                        }
                        else
                        {
                            RightClickHelper.MoveSelectedObjectUp();
                        }
                        return true;
                    }
                    break;

                case Key.Down:
                    if (altDown)
                    {
                        if(shiftDown)
                        {
                            RightClickHelper.MoveToBottom();
                        }
                        else
                        {
                            RightClickHelper.MoveSelectedObjectDown();
                        }
                        return true;
                    }
                    break;
                case Key.F5:
                    // fire and forget it, otherwise this blocks the app:

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    PluginManager.CallPluginMethodAsync(
                        "Glue Compiler",
                        "BuildAndRun");
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.DialogCommands.FocusTab("Build");
                    return true;
                case Key.F12:
                    GlueCommands.Self.DialogCommands.GoToDefinitionOfSelection();
                    return true;

                // Handle this in the tree view itself, otherwise all other controls
                // start raising this
                //case Key.Delete:
                //    HandleDeletePressed();
                    //return true;
            }

            // fi we got here, it's not handled, so let's see if it was some CTRL+something hotkey. If so, we can let the plugin handle it
            if(ctrlDown)
            {
                PluginManager.ReactToCtrlKey(key);
                return true;
            }

            return false;
        }
        public static async void HandleDeletePressed()
        {
            var treeNode = GlueState.Self.CurrentTreeNode;
            if(
                // Don't try to delete the root Files folder
                //treeNode.IsFilesContainerNode() || 
                treeNode.IsFolderInFilesContainerNode() || treeNode.IsDirectoryNode())
            {
                RightClickHelper.DeleteFolderClick(treeNode);
            }
            else
            {
                await RightClickHelper.RemoveFromProjectToolStripMenuItem();
            }
        }
    }
}
