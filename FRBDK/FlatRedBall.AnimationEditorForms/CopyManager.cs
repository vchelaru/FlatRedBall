using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Content.AnimationChain;
using System.Windows.Forms;
using FlatRedBall.Utilities;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Math.Geometry;

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
            if (SelectedState.Self.SelectedRectangle != null)
            {
                PutInClipboard(SelectedState.Self.SelectedRectangle);
            }
            else if (SelectedState.Self.SelectedFrame != null)
            {
                PutInClipboard(SelectedState.Self.SelectedFrame);
            }
            else if (SelectedState.Self.SelectedChain != null)
            {
                PutInClipboard(SelectedState.Self.SelectedChain);
            }

            // I tried to use Clipboard.SetData and for some reason every time I did CTRL+V, the data
            // in "frame" was null. So instead I'm going with Text which seems more reliable:
            void PutInClipboard<T>(T objectToPlace)
            {
                Clipboard.Clear();
                FileManager.XmlSerialize(objectToPlace, out string serializedString);
                Clipboard.SetText($"{objectToPlace.GetType().Name}:" + serializedString);
            }
        }

        internal AnimationChainSave HandleDuplicate(string requestedName = null)
        {
            if(SelectedState.Self.SelectedFrame != null)
            {
                MessageBox.Show("Cannot currently duplicate frames - create an issue on github if needed");
                return null;
            }
            else
            {
                return Duplicate(FileManager.CloneObject(SelectedState.Self.SelectedChain), requestedName);
            }
        }

        internal void HandlePaste()
        {
            var text = Clipboard.GetText();
            AnimationFrameSave pastedFrame = null;
            AnimationChainSave pastedChain = null;
            AxisAlignedRectangleSave pastedRectangle = null;
            if(text?.Contains(":") == true)
            {
                var before = text.Substring(0, text.IndexOf(":"));
                var after = text.Substring(text.IndexOf(":") + 1);

                if(before == "AnimationFrameSave")
                {
                    try
                    {
                        pastedFrame = FileManager.XmlDeserializeFromString<AnimationFrameSave>(after);
                    }
                    catch { } // no biggie
                }
                else if(before == "AnimationChainSave")
                {
                    try
                    {
                        pastedChain = FileManager.XmlDeserializeFromString<AnimationChainSave>(after);
                    }
                    catch { } // no biggie
                }
                else if(before == nameof(AxisAlignedRectangleSave))
                {
                    try { pastedRectangle = FileManager.XmlDeserializeFromString<AxisAlignedRectangleSave>(after); }
                    catch { }
                }
            }
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                if(pastedRectangle != null && SelectedState.Self.SelectedFrame != null)
                {
                    var originalName = pastedRectangle.Name;
                    var newRectangles = new List<AxisAlignedRectangleSave>();
                    foreach(var frame in SelectedState.Self.SelectedFrames)
                    {
                        var rectangleToPaste = FileManager.CloneObject(pastedRectangle);

                        // do this before adding it to the list:
                        rectangleToPaste.Name = StringFunctions.MakeStringUnique(originalName,
                            frame.ShapeCollectionSave.AxisAlignedRectangleSaves
                                .Select(item => item.Name).ToList()
                            );

                        frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(rectangleToPaste);
                        newRectangles.Add(rectangleToPaste);
                        AppCommands.Self.RefreshTreeNode(frame);
                    }
                    AppCommands.Self.RefreshAnimationFrameDisplay();
                    SelectedState.Self.SelectedRectangles = newRectangles;
                    AppCommands.Self.SaveCurrentAnimationChainList();
                }
                else if (pastedFrame != null && SelectedState.Self.SelectedChain != null)
                {
                    if(pastedFrame.ShapeCollectionSave == null)
                    {
                        pastedFrame.ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();
                    }
                    SelectedState.Self.SelectedChain.Frames.Add(pastedFrame);
                    AppCommands.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
                    SelectedState.Self.SelectedFrame = pastedFrame;
                    ApplicationEvents.Self.RaiseAnimationChainsChanged();
                }
                else if (pastedChain != null)
                {
                    Duplicate(pastedChain);
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
