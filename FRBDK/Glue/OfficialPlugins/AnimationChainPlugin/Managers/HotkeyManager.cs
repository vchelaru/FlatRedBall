using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using OfficialPlugins.AnimationChainPlugin.ViewModels;

namespace OfficialPlugins.AnimationChainPlugin.Managers;
internal class HotkeyManager
{
    public void HandleTreeViewKey(System.Windows.Input.KeyEventArgs e, AchxViewModel viewModel)
    {
        var ctrlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        Key key = (e.Key == Key.System ? e.SystemKey : e.Key);


        // Check if CTRL+C was pressed
        if (ctrlDown && key == Key.C)
        {
            AnimationChainCopyPasteManager.HandleCopy(viewModel);
            e.Handled = true;
        }
        // Check if CTRL+V was pressed
        else if (ctrlDown && key == Key.V)
        {
            viewModel.HandlePaste(AnimationChainCopyPasteManager.CopiedXml, AnimationChainCopyPasteManager.CopiedType);
            //AnimationChainCopyPasteManager.HandlePaste(viewModel);
            e.Handled = true;
        }
        else if(key == Key.Delete)
        {
            viewModel.HandleDelete();
            e.Handled = true;
        }
        else if(altDown && key == Key.Up)
        {
            viewModel.MoveSelectionUp();
            e.Handled = true;
        }
        else if(altDown && key == Key.Down)
        {
            viewModel.MoveSelectionDown();
            e.Handled = true;
        }
    }
}
