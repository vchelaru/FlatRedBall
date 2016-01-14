using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using System.Windows.Forms;
using FlatRedBall.Utilities;

namespace FlatRedBall.AnimationEditorForms
{
    public class CopyManager
    {
        static CopyManager mSelf;
        object mCopiedObject;


        public static CopyManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new CopyManager();
                }
                return mSelf;
            }
        }

        public AnimationFrameSave FrameInClipboard
        {
            get
            {
                return mCopiedObject as AnimationFrameSave;
            }
        }


        internal void HandleCopy()
        {

            if (SelectedState.Self.SelectedFrame != null)
            {
                Clipboard.Clear();
                DataObject dataObject = new DataObject("frame", FileManager.CloneObject(SelectedState.Self.SelectedFrame));
                var item = dataObject.GetData("frame");
                Clipboard.SetDataObject(
                    dataObject, false, 10, 40);
            }
            else if (SelectedState.Self.SelectedChain != null)
            {
                Clipboard.Clear();
                var toAdd = FileManager.CloneObject(SelectedState.Self.SelectedChain);
                DataObject dataObject = new DataObject("chain", toAdd);
                Clipboard.SetDataObject(
                    dataObject, false, 10, 40);
            }
        }

        internal void HandlePaste()
        {
            var dataObject = Clipboard.GetDataObject();
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                if (dataObject.GetDataPresent("frame") && SelectedState.Self.SelectedChain != null)
                {
                    // paste this in the chain
                    // clone it, in case multiple pastes occur:
                    AnimationFrameSave whatToCopy = dataObject.GetData("frame") as AnimationFrameSave;
                    AnimationFrameSave newAfs = FileManager.CloneObject(whatToCopy);
                    SelectedState.Self.SelectedChain.Frames.Add(newAfs);
                    TreeViewManager.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
                    SelectedState.Self.SelectedFrame = newAfs;
                    MainControl.Self.RaiseAnimationChainChanges(null, null);
                }
                else if (dataObject.GetDataPresent("chain"))
                {
                    object data = dataObject.GetData("chain");
                    AnimationChainSave whatToCopy = data as AnimationChainSave;
                    AnimationChainSave newAcs = FileManager.CloneObject(whatToCopy);

                    List<string> existingNames = ProjectManager.Self.AnimationChainListSave.AnimationChains.Select(item => item.Name).ToList();

                    newAcs.Name = StringFunctions.MakeStringUnique(newAcs.Name, existingNames, 2);


                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(newAcs);
                    TreeViewManager.Self.RefreshTreeNode(newAcs);

                    MainControl.Self.RaiseAnimationChainChanges(null, null);

                    SelectedState.Self.SelectedChain = newAcs;


                }
            }
        }


    }
}
