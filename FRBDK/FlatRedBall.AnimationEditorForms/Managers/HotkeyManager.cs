using FlatRedBall.AnimationEditorForms.CommandsAndState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Managers
{
    public class HotkeyManager : Singleton<HotkeyManager>
    {
        public bool TryHandleKeys(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Alt | Keys.Up:
                    TreeViewManager.Self.MoveUpClick(null, null);
                    return true;
                case Keys.Alt | Keys.Down:
                    TreeViewManager.Self.MoveDownClick(null, null);
                    return true;
                case Keys.Alt | Keys.Shift | Keys.Down:
                    TreeViewManager.Self.MoveToBottomClick(null, null);
                    return true;
                case Keys.Alt | Keys.Shift | Keys.Up:
                    TreeViewManager.Self.MoveToTopClick(null, null);
                    return true;
                case Keys.Control | Keys.C:
                    CopyManager.Self.HandleCopy();
                    return true;
                case Keys.Control | Keys.V:
                    CopyManager.Self.HandlePaste();
                    return true;
                case Keys.Control | Keys.D:
                    CopyManager.Self.HandleDuplicate();
                    return true;
                case Keys.Delete:
                    if(SelectedState.Self.SelectedAxisAlignedRectangle != null)
                    {
                        AppCommands.Self.AskToDelete(SelectedState.Self.SelectedRectangles);
                    }
                    else if(SelectedState.Self.SelectedCircle != null)
                    {
                        AppCommands.Self.AskToDelete(SelectedState.Self.SelectedCircles);
                    }
                    else if(SelectedState.Self.SelectedFrames.Count > 0)
                    {
                        AppCommands.Self.AskToDelete(SelectedState.Self.SelectedFrames);

                    }
                    else if(SelectedState.Self.SelectedChains.Count > 0)
                    {
                        AppCommands.Self.AskToDelete(SelectedState.Self.SelectedChains);

                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
