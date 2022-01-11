using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.Managers
{
    public static class DragDropManagerGameWindow
    {
        public static CompilerViewModel CompilerViewModel { get; set; }
        public static async Task HandleDragDropOnGameWindow(ITreeNode treeNode)
        {

            /////////////////////////Early Out////////////////////////////////
            if (!CompilerViewModel.IsRunning || !CompilerViewModel.IsEditChecked)
            {
                return;
            }
            ScreenSave screen = await CommandSender.GetCurrentInGameScreen();
            if (screen == null)
            {
                return;
            }
            ///////////////////////End Early Out//////////////////////////////

            
            if(treeNode?.Tag is EntitySave entitySave)
            {
                await DragDropManager .Self.DropEntityOntoElement(entitySave, screen);
            }
            else if(treeNode?.Tag is StateSave stateSave)
            {

            }
        }
    }
}
