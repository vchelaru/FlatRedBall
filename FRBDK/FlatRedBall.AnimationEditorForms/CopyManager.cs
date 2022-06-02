using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using System.Windows.Forms;
using FlatRedBall.Utilities;
using FlatRedBall.AnimationEditorForms.CommandsAndState;

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

        internal AnimationChainSave HandleDuplicate(string requestedName = null)
        {
            return Duplicate(FileManager.CloneObject(SelectedState.Self.SelectedChain), requestedName);
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
                    AppCommands.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
                    SelectedState.Self.SelectedFrame = newAfs;
                    ApplicationEvents.Self.RaiseAnimationChainsChanged();
                }
                else if (dataObject.GetDataPresent("chain"))
                {
                    object data = dataObject.GetData("chain");
                    AnimationChainSave whatToCopy = data as AnimationChainSave;
                    Duplicate(whatToCopy);

                }
            }
        }

        private static AnimationChainSave Duplicate(AnimationChainSave whatToCopy, string requestedName = null)
        {
            AnimationChainSave newAcs = FileManager.CloneObject(whatToCopy);
            if(requestedName != null)
            {
                newAcs.Name = requestedName;
            }
            List<string> existingNames = ProjectManager.Self.AnimationChainListSave.AnimationChains.Select(item => item.Name).ToList();

            newAcs.Name = StringFunctions.MakeStringUnique(newAcs.Name, existingNames, 2);


            ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(newAcs);
            AppCommands.Self.RefreshTreeNode(newAcs);

            ApplicationEvents.Self.RaiseAnimationChainsChanged();

            SelectedState.Self.SelectedChain = newAcs;

            return newAcs;
        }
    }
}
