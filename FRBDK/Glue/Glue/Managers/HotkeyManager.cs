using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
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
        public bool TryHandleKeys(Keys keyData)
        {
            switch (keyData)
            {
                // CTRL+F, control f, search focus, ctrl f, ctrl + f
                case Keys.Control | Keys.F:
                    MainExplorerPlugin.Self.SearchTextbox.Focus();
                    return true;
                case Keys.Alt | Keys.Left:
                    TreeNodeStackManager.Self.GoBack();
                    return true;
                case Keys.Alt | Keys.Right:
                    TreeNodeStackManager.Self.GoForward();
                    return true;
                case Keys.Alt | Keys.Up:
                    return RightClickHelper.MoveSelectedObjectUp();

                case Keys.Alt | Keys.Down:
                    return RightClickHelper.MoveSelectedObjectDown();
                case Keys.Alt | Keys.Shift | Keys.Down:
                    return RightClickHelper.MoveToBottom();
                case Keys.Alt | Keys.Shift | Keys.Up:
                    return RightClickHelper.MoveToTop();
                case Keys.Escape:
                    if (MainExplorerPlugin.Self.SearchTextbox.Focused)
                    {
                        MainExplorerPlugin.Self.SearchTextbox.Focus();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case Keys.F5:
                    PluginManager.CallPluginMethod(
                        "Glue Compiler",
                        "BuildAndRun");
                    return true;
                default:
                    return false;
            }
        }

        internal bool TryHandleKeys(System.Windows.Input.KeyEventArgs e)
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
                        MainExplorerPlugin.Self.SearchTextbox.Focus();
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
                case Key.Escape:
                    if (MainExplorerPlugin.Self.SearchTextbox.Focused)
                    {
                        MainExplorerPlugin.Self.SearchTextbox.Focus();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case Key.F5:
                    PluginManager.CallPluginMethod(
                        "Glue Compiler",
                        "BuildAndRun");
                    FlatRedBall.Glue.Plugins.ExportedImplementations.GlueCommands.Self.DialogCommands.FocusTab("Build");
                    return true;
            }

            return false;
        }
    }
}
