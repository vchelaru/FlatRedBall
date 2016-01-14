using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Input;

using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using FlatRedBall.Instructions;
using InstructionEditor.Gui;
using Microsoft.Xna.Framework.Input;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Math.Geometry;
using ToolTemplate;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;
using EditorObjects;
using FlatRedBall.Instructions.ScriptedAnimations;

namespace InstructionEditor
{
    public class EditorLogic
    {
        #region Fields

        private ReactiveHud mReactiveHud = new ReactiveHud();

        private SpriteList mCurrentSprites = new SpriteList();
        private PositionedObjectList<SpriteFrame> mCurrentSpriteFrames = new PositionedObjectList<SpriteFrame>();
        private PositionedObjectList<PositionedModel> mCurrentPositionedModels = new PositionedObjectList<PositionedModel>();
        private PositionedObjectList<Text> mCurrentTexts = new PositionedObjectList<Text>();

        public static SpriteList currentSpriteMarkers = new SpriteList();

        private KeyframeList mCurrentKeyframeList;
        private InstructionList mCurrentKeyframe;

        static PositionedObject oldSpritePosition;


        static string recordedInfo;

        static Sprite tempPositionVerifier;

        AnimationSequence mCurrentAnimationSequence;
        TimedKeyframeList mCurrentTimedKeyframeList;

        PositionedObject mObjectGrabbed;


        #endregion

        #region Properties

        public AnimationSequence CurrentAnimationSequence
        {
            get { return mCurrentAnimationSequence; }
            set { mCurrentAnimationSequence = value; }
        }

        public InstructionList CurrentKeyframe
        {
            get { return mCurrentKeyframe; }
            set
            {
                if (mCurrentKeyframe != value)
                {
                    mCurrentKeyframe = value;
                    GuiData.KeyframePropertyGrid.SelectedObject = value;
                    GuiData.ListBoxWindow.HighlightNoCall(mCurrentKeyframe);

                }
            }
        }

        public KeyframeList CurrentKeyframeList
        {
            get { return mCurrentKeyframeList; }
            set
            {
                if (mCurrentKeyframeList != value)
                {
                    mCurrentKeyframeList = value;

                    GuiData.ListBoxWindow.HighlightNoCall(mCurrentKeyframeList);
                }
            }
        }

        public InstructionSet CurrentInstructionSet
        {
            get
            {
                switch (GuiData.TimeLineWindow.InstructionMode)
                {
                    case InstructionMode.All:
                        return null;// EditorData.GlobalInstructionSet;
                        break;
                    case InstructionMode.Current:

                        if (CurrentSprites.Count != 0)
                        {
                            return EditorData.ObjectInstructionSets[CurrentSprites[0]];
                        }
                        else if (CurrentTexts.Count != 0)
                        {
                            return EditorData.ObjectInstructionSets[CurrentTexts[0]];
                        }
                        else
                        {
                            return null;
                        }

                        break;
                }

                return null;// EditorData.GlobalInstructionSet;

            }
        }

        public PositionedObjectList<PositionedModel> CurrentPositionedModels
        {
            get { return mCurrentPositionedModels; }
        }

        public SpriteList CurrentSprites
        {
            get { return mCurrentSprites; }
        }

        public PositionedObjectList<SpriteFrame> CurrentSpriteFrames
        {
            get { return mCurrentSpriteFrames; }
        }

        public PositionedObjectList<Text> CurrentTexts
        {
            get { return mCurrentTexts; }
        }

        public TimedKeyframeList CurrentTimedKeyframeList
        {
            get { return mCurrentTimedKeyframeList; }
            set { mCurrentTimedKeyframeList = value; }
        }

        public Sprite SpriteGrabbed
        {
            get { return mObjectGrabbed as Sprite; }
        }

        public SpriteFrame SpriteFrameGrabbed
        {
            get { return mObjectGrabbed as SpriteFrame; }
        }

        public PositionedModel PositionedModelGrabbed
        {
            get { return mObjectGrabbed as PositionedModel; }
        }

        public Text TextGrabbed
        {
            get { return mObjectGrabbed as Text; }
        }

        #endregion

        #region Methods

        #region Public Methods

        public void CopyCurrentObjects()
        {
            // for now ignore attachments - just copy whatever's selected.
            foreach(Sprite sprite in mCurrentSprites)
            {
                Sprite newSprite = sprite.Clone();
                StringFunctions.MakeNameUnique<Sprite>(newSprite, EditorData.BlockingScene.Sprites);
                EditorData.BlockingScene.Sprites.Add(newSprite);
                SpriteManager.AddSprite(newSprite);
            }

            foreach (SpriteFrame spriteFrame in mCurrentSpriteFrames)
            {
                SpriteFrame newSpriteFrame = spriteFrame.Clone();
                StringFunctions.MakeNameUnique<SpriteFrame>(newSpriteFrame, EditorData.BlockingScene.SpriteFrames);
                EditorData.BlockingScene.SpriteFrames.Add(newSpriteFrame);
                SpriteManager.AddSpriteFrame(newSpriteFrame);
            }

            foreach (PositionedModel positionedModel in mCurrentPositionedModels)
            {
                PositionedModel newPositionedModel = positionedModel.Clone(EditorData.ContentManagerName);
                StringFunctions.MakeNameUnique<PositionedModel>(newPositionedModel, EditorData.BlockingScene.PositionedModels);
                EditorData.BlockingScene.PositionedModels.Add(newPositionedModel);
                ModelManager.AddModel(newPositionedModel);
            }

            foreach (Text text in mCurrentTexts)
            {
                Text newText = text.Clone();
                StringFunctions.MakeNameUnique<Text>(newText, EditorData.BlockingScene.Texts);
                EditorData.BlockingScene.Texts.Add(newText);
                TextManager.AddText(newText);
            }
        }

        public void SelectObject<T>(T objectToSelect, IList<T> list) where T : PositionedObject, ICursorSelectable
        {
            #region Performing attachment
            if (GuiData.ToolsWindow.AttachButton.IsPressed)
            {
                throw new NotImplementedException("Attachments not implemented currently.");
            }
            #endregion

            #region Shift selecting an object - to select multiple objects
            else if ((InputManager.Keyboard.KeyDown(Keys.LeftShift) || InputManager.Keyboard.KeyDown(Keys.RightShift)) &&
                      list.Contains(objectToSelect) == false)
            {
                SelectSpriteAdd(objectToSelect, list);

            }
            #endregion

            #region Simply selecting an object
            else
            {
                #region if objectToSelect == null or not in current list, refresh movement path and timeline markers, clear currentSprites, add spriteToSelect
                if (objectToSelect == null || list.Contains(objectToSelect) == false)
                {
                    #region set the currentSprites to the spriteToSelect, show currentSpriteMarker, update property window, and add the Sprite.

                    list.Clear();
                    if (objectToSelect != null)
                    {
                        list.Add(objectToSelect);


                        // this is removed because currently this is a feature not implemented
                        // in the instruction editor.  Have to come back to this later

                        /*
						#region if the current time was outside of the selected sprite's lifespan, make it visible so the user can add keyframes
						if(!SpriteManager.Contains(currentSprites[0]))
							SpriteManager.AddSprite(currentSprites[0]);
						#endregion
                         */


                    }
                    else
                    {

                        foreach (Sprite s in currentSpriteMarkers)
                            s.Detach();
                        SpriteManager.RemoveSpriteList(currentSpriteMarkers);

                    }
                    #endregion
                }

                #endregion
            }
            #endregion
        }

        /// <summary>
        /// Adds a Sprite to the currentSprites SpriteArray and visibly marks it as selected.
        /// </summary>
        /// <remarks>
        /// This method should not be called for one Sprite when no Sprites are selected yet.  This should only
        /// be called when multiple Sprites are going to be selected, or if a Sprite is already selected before
        /// this is called.
        /// </remarks>
        /// <param name="spriteToSelect">Sprite to select.  Should not be null.</param>
        public void SelectSpriteAdd<T>(T objectToAdd, IList<T> list) where T : PositionedObject, ICursorSelectable
        {
            if (list.Contains(objectToAdd) == false)
            {
                list.Add(objectToAdd);
            }

        }

        public void Update()
        {
            mReactiveHud.Update();

            MouseControlOverObjects();

            KeyboardShortcuts();

            #region Sort current Keyframe List
            if (mCurrentKeyframeList != null)
            {
                mCurrentKeyframeList.InsertionSortAscendingTimeToExecute();
            }

            #endregion

            #region SortYSpritesSecondary if necessary
            if (EditorData.EditorOptions.SortYSpritesSecondary)
            {
                SpriteManager.SortYSpritesSecondary();
            }
            #endregion

            UndoManager.EndOfFrameActivity();
        }

        #endregion

        #region Private Methods

        private PositionedObject GetObjectOver()
        {
            if (EditorData.BlockingScene == null)
            {
                return null;
            }

            #region Over any Sprites?

            PositionedObject objectOver = null;

            objectOver = GuiManager.Cursor.GetSpriteOver(mCurrentSprites);

            if (objectOver == null)
            {
                objectOver = GuiManager.Cursor.GetSpriteOver(EditorData.BlockingScene.Sprites);
            }
            #endregion

            #region If not, over any SpriteFrames?

            if (objectOver == null)
            {
                objectOver = GuiManager.Cursor.GetSpriteOver(mCurrentSpriteFrames);
            }

            if (objectOver == null)
            {
                objectOver = GuiManager.Cursor.GetSpriteOver(EditorData.BlockingScene.SpriteFrames);
            }

            #endregion

            #region If not, over any PositionedModels?

            if (objectOver == null)
            {
                foreach (PositionedModel positionedModel in mCurrentPositionedModels)
                {
                    if (GuiManager.Cursor.IsOn3D<PositionedModel>(positionedModel))
                    {
                        objectOver = positionedModel;
                        break;
                    }
                }
            }

            if (objectOver == null)
            {
                foreach (PositionedModel positionedModel in EditorData.BlockingScene.PositionedModels)
                {
                    if (GuiManager.Cursor.IsOn3D<PositionedModel>(positionedModel))
                    {
                        objectOver = positionedModel;
                        break;
                    }
                }
            }

            #endregion

            #region If not, over any Texts?

            if (objectOver == null)
            {
                foreach (Text text in mCurrentTexts)
                {
                    if (GuiManager.Cursor.IsOn3D((Text)text))
                    {
                        objectOver = text;
                        break;
                    }
                }
            }

            if (objectOver == null)
            {
                foreach (Text text in EditorData.BlockingScene.Texts)
                {
                    if (GuiManager.Cursor.IsOn3D((Text)text))
                    {
                        objectOver = text;
                        break;
                    }
                }
            }

            #endregion

            return objectOver;
        }

        private void KeyboardShortcuts()
        {
            if (InputManager.ReceivingInput != null) return;

            #region Move the Camera

            InputManager.Keyboard.ControlPositionedObject(SpriteManager.Camera);

            #endregion

            #region toolbar hotkeys


            if (InputManager.Keyboard.KeyPushed(Keys.Delete))
            {
                if (this.mCurrentKeyframe != null)
                {
                    this.mCurrentKeyframeList.Remove(mCurrentKeyframe);

                    GuiData.ListBoxWindow.InstructionSetListBox.RemoveHighlightedItems();

                    CurrentKeyframe = null;
                }
                else if (this.mCurrentKeyframeList != null && CurrentInstructionSet != null)
                {
                    GuiData.ListBoxWindow.InstructionSetListBox.RemoveItemAndChildren(mCurrentKeyframeList);

                    CurrentInstructionSet.Remove(mCurrentKeyframeList);

                    CurrentKeyframeList = null;
                }

            }
            if (InputManager.Keyboard.KeyPushed(Keys.K))
                GuiData.TimeLineWindow.insertKeyframeButton.OnClick();

            if (InputManager.Keyboard.ControlCPushed())
            {
                GuiData.ToolsWindow.CopyButton.OnClick();
            }
            if ((InputManager.Keyboard.KeyPushed(Keys.LeftShift) || InputManager.Keyboard.KeyPushed(Keys.RightShift)) && InputManager.Keyboard.KeyPushed(Keys.T))
            {
//                GuiData.ToolsWindow.shiftKeyframeTimeButton.OnClick();
            }
            #endregion

            #region CTRL + A:  Selecting all visible Sprites
            if (InputManager.Keyboard.KeyPushed(Keys.A) &&
                (InputManager.Keyboard.KeyDown(Keys.LeftControl) || InputManager.Keyboard.KeyDown(Keys.RightControl)))
            {

                foreach (Sprite s in EditorData.ActiveSprites)
                {
                    if (CurrentSprites.Contains(s) == false)
                    {
                        SelectSpriteAdd(s, CurrentSprites);
                    }
                }
            }
            #endregion

            #region Key.I to move the Sprite's Instruction Set.
            if (InputManager.Keyboard.KeyPushed(Keys.I) && CurrentSprites.Count != 0)
            {
                oldSpritePosition = new PositionedObject();
                oldSpritePosition.X = CurrentSprites[0].X;
                oldSpritePosition.Y = CurrentSprites[0].Y;
                oldSpritePosition.Z = CurrentSprites[0].Z;
            }
            //			if(InputManager.KeyDown(Key.I) && currentSprites.Count != 0 && oldSpritePosition == null)
            //			{
            //				oldSpritePosition = new PositionedObject();
            //				oldSpritePosition.X = currentSprites[0].X;
            //				oldSpritePosition.Y = currentSprites[0].Y;
            //				oldSpritePosition.Z = currentSprites[0].Z;
            //			}
            if (InputManager.Keyboard.KeyReleased(Keys.I) && CurrentSprites.Count != 0)
            {
                // AdjustMovementPath();

            }
            #endregion

            #region CTRL+C copying current objects

            if (InputManager.Keyboard.ControlCPushed())
            {
                CopyCurrentObjects();
            }

            #endregion

            #region recording currentSprite
            if (CurrentSprites.Count != 0)
            {
                if (InputManager.Keyboard.KeyPushed(Keys.R))
                {
                    tempPositionVerifier = new Sprite();


                }

            }

            #endregion

        }

        private void MouseControlOverObjects()
        {
            #region If over a window, exit out

            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null)
                return;
            #endregion

            // See if the mouse is over anything
            PositionedObject objectOver = GetObjectOver();

            #region Cursor Push
            if (GuiManager.Cursor.PrimaryPush)
            {
                mObjectGrabbed = objectOver;

                if (mObjectGrabbed != null)
                {
                    PositionedObjectMover.SetStartPosition(mObjectGrabbed);
                }

                if (mObjectGrabbed == null)
                {
                    SelectObject<Sprite>(null, mCurrentSprites);
                    SelectObject<Text>(null, mCurrentTexts);
                }
                else if (SpriteGrabbed != null)
                {
                    SelectObject(SpriteGrabbed, mCurrentSprites);
                }
                else if (SpriteFrameGrabbed != null)
                {
                    SelectObject(SpriteFrameGrabbed, mCurrentSpriteFrames);
                }
                else if (PositionedModelGrabbed != null)
                {
                    SelectObject(PositionedModelGrabbed, mCurrentPositionedModels);
                }
                else if (TextGrabbed != null)
                {
                    SelectObject(TextGrabbed, mCurrentTexts);
                }
            }
            #endregion

            #region Cursor Down (Drag)

            if (cursor.PrimaryDown)
            {
                PerformDraggingObjectControl();
            }

            #endregion

            #region Cursor Click

            if (cursor.PrimaryClick)
            {
                cursor.StaticPosition = false;
            }
            #endregion
        }

        private void PerformDraggingObjectControl()
        {
            Cursor cursor = GuiManager.Cursor;

            if (mObjectGrabbed == null || cursor.WindowOver != null)
                return;

            #region Move

            if (GuiData.ToolsWindow.MoveButton.IsPressed)
            {
                PositionedObjectMover.MouseMoveObject(mObjectGrabbed);
            }

            #endregion

            #region Scale

            else if (GuiData.ToolsWindow.ScaleButton.IsPressed && mObjectGrabbed is IScalable)
            {
                cursor.StaticPosition = true;

                IScalable asIScalable = mObjectGrabbed as IScalable;

                asIScalable.ScaleX *= 1 + cursor.XVelocity / 100.0f;
                asIScalable.ScaleY *= 1 + cursor.YVelocity / 100.0f;

            }

            #endregion

            #region Rotate

            else if (GuiData.ToolsWindow.RotateButton.IsPressed && mObjectGrabbed is IRotatable)
            {
                cursor.StaticPosition = true;

                IRotatable asiRotatable = mObjectGrabbed as IRotatable;

                asiRotatable.RotationZ += cursor.YVelocity * SpriteManager.Camera.Z / 100.0f;

                if (mObjectGrabbed.Parent != null)
                {
                    mObjectGrabbed.SetRelativeFromAbsolute();
                }
            }

            #endregion
        }

        #endregion

        #endregion

    }
}
