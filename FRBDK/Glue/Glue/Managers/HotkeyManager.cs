using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.Plugins;
using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                    MainGlueWindow.Self.SearchTextbox.Focus();
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
                    if (MainGlueWindow.Self.SearchTextbox.Focused)
                    {
                        MainGlueWindow.Self.SearchTextbox.Focus();
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
                    break;
                default:
                    return false;
            }
        }

    }
}
