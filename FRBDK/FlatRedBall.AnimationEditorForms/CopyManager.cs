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
using System.Collections;

namespace FlatRedBall.AnimationEditorForms
{
    public class CopyManager
    {
        #region Fields/Properties

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

        #endregion

        internal void HandleCopy()
        {
            // XML Serialization requires a typed call, so we can't pass an object
            //if (SelectedState.Self.SelectedShape != null)
            //{
            //    PutInClipboard(SelectedState.Self.SelectedShape);
            //}
            if (SelectedState.Self.SelectedAxisAlignedRectangle != null)
            {
                PutInClipboard(SelectedState.Self.SelectedAxisAlignedRectangle);
            }
            else if (SelectedState.Self.SelectedCircle != null)
            {
                PutInClipboard(SelectedState.Self.SelectedCircle);
            }
            else if (SelectedState.Self.SelectedFrame != null)
            {
                PutInClipboard(SelectedState.Self.SelectedFrames);
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

                var type = objectToPlace.GetType();
                var namePrefix = type.Name;
                if (type.IsGenericType)
                {
                    namePrefix = $"List<{type.GenericTypeArguments[0].Name}>";
                }

                Clipboard.SetText($"{namePrefix}:" + serializedString);
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
            List<AnimationFrameSave> pastedFrames = null;
            AnimationChainSave pastedChain = null;
            AxisAlignedRectangleSave pastedRectangle = null;
            CircleSave pastedCircle = null;
            if (text?.Contains(":") == true)
            {
                var typeName = text.Substring(0, text.IndexOf(":"));
                var after = text.Substring(text.IndexOf(":") + 1);

                if(typeName == $"List<{nameof(AnimationFrameSave)}>")
                {
                    try
                    {
                        pastedFrames = FileManager.XmlDeserializeFromString<List<AnimationFrameSave>>(after);
                    }
                    catch { } // no biggie
                }
                else if(typeName == nameof(AnimationChainSave))
                {
                    try
                    {
                        pastedChain = FileManager.XmlDeserializeFromString<AnimationChainSave>(after);
                    }
                    catch { } // no biggie
                }
                else if(typeName == nameof(AxisAlignedRectangleSave))
                {
                    try { pastedRectangle = FileManager.XmlDeserializeFromString<AxisAlignedRectangleSave>(after); }
                    catch { }
                }
                else if(typeName == nameof(CircleSave))
                {
                    try { pastedCircle = FileManager.XmlDeserializeFromString<CircleSave>(after); }
                    catch { }
                }
            }

            string GetUniqueShapeName(string originalName, AnimationFrameSave frame)
            {
                var rectangleNames = frame.ShapeCollectionSave.AxisAlignedRectangleSaves
                                .Select(item => item.Name);
                var circleNames = frame.ShapeCollectionSave.CircleSaves
                                .Select(item => item.Name);

                var allNames = rectangleNames.Concat(circleNames).ToList();

                return StringFunctions.MakeStringUnique(originalName,allNames);
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
                        rectangleToPaste.Name = GetUniqueShapeName(originalName, frame);

                        frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(rectangleToPaste);
                        newRectangles.Add(rectangleToPaste);
                        AppCommands.Self.RefreshTreeNode(frame);
                    }
                    AppCommands.Self.RefreshAnimationFrameDisplay();
                    SelectedState.Self.SelectedRectangles = newRectangles;
                    AppCommands.Self.SaveCurrentAnimationChainList();
                }
                else if(pastedCircle != null && SelectedState.Self.SelectedFrame != null)
                {
                    var originalName = pastedCircle.Name;
                    var newCircles = new List<CircleSave>();
                    foreach(var frame in SelectedState.Self.SelectedFrames)
                    {
                        var circleToPaste = FileManager.CloneObject(pastedCircle);

                        // do this before adding it to the list:
                        circleToPaste.Name = GetUniqueShapeName(originalName, frame);

                        frame.ShapeCollectionSave.CircleSaves.Add(circleToPaste);
                        newCircles.Add(circleToPaste);
                        AppCommands.Self.RefreshTreeNode(frame);
                    }
                    AppCommands.Self.RefreshAnimationFrameDisplay();
                    SelectedState.Self.SelectedCircles = newCircles;
                    AppCommands.Self.SaveCurrentAnimationChainList();
                }
                else if (pastedFrames != null && SelectedState.Self.SelectedChain != null)
                {
                    foreach(var pastedFrame in pastedFrames)
                    {
                        if(pastedFrame.ShapeCollectionSave == null)
                        {
                            pastedFrame.ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();
                        }
                        SelectedState.Self.SelectedChain.Frames.Add(pastedFrame);
                        AppCommands.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);
                        SelectedState.Self.SelectedFrame = pastedFrame;
                    }
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
