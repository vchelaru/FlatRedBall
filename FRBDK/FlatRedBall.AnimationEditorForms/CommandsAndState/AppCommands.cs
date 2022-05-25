using FlatRedBall.Content.AnimationChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.AnimationEditorForms.CommandsAndState
{
    public class AppCommands : Singleton<AppCommands>
    {
        public void DoOnUiThread(Action action)
        {
            MainControl.Self.Invoke(action);
        }

        public Task DoOnUiThread(Func<Task> func) => MainControl.Self.Invoke(func);

        public T DoOnUiThread<T>(Func<T> func) => MainControl.Self.Invoke(func);

        public void RefreshTreeNode(AnimationChainSave animationChain) => TreeViewManager.Self.RefreshTreeNode(animationChain);

        public void RefreshTreeNode(AnimationFrameSave animationFrame) => TreeViewManager.Self.RefreshTreeNode(animationFrame);
    }
}
