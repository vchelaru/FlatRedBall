using System;
using FlatRedBall;
using FlatRedBall.Gui;
using System.Collections.Generic;
using FlatRedBall.Instructions;
using FlatRedBall.Input;
using FlatRedBall.Instructions.ScriptedAnimations;
using FlatRedBall.Utilities;

namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for ListBoxWindow.
	/// </summary>
	public class ListBoxWindow : Window
    {
        #region Fields

        CollapseListBox mInstructionSetListBox;

		Button mAddKeyframeListButton;
        Button mAddKeyframeButton;

        ComboBox mTimelineSelection;


        ListDisplayWindow mAnimationSequenceListBox;

        #endregion       

        #region Properties

        public CollapseListBox InstructionSetListBox
        {
            get { return mInstructionSetListBox; }
        }

        //public Button AddKeyframeListButton
        //{
        //    get { return mAddKeyframeListButton; }
        //}


        #endregion

        #region Event Methods

        private void AddKeyframeListClick(Window callingWindow)
        {
            if (EditorData.EditorLogic.CurrentInstructionSet == null)
            {
                GuiManager.ShowMessageBox("Currently the InstructionEditor is under \"Current'\" editing mode." +
                    "  To add an Animation, you must have a selected object first.", "Error");

                return;
            }

            TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter a name for the new Animation:", "Enter name");
            if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.Current)
            {
                if (EditorData.EditorLogic.CurrentInstructionSet != null)
                {
                    tiw.Text = "Keyframe List " + EditorData.EditorLogic.CurrentInstructionSet.Count;
                }
                else
                {
                    tiw.Text = "Keyframe List " + 0;
                }
            }
            else
            {
                tiw.Text = "Animation " + EditorData.GlobalInstructionSets.Count;
            }

            tiw.OkClick += new GuiMessage(AddKeyframeListOk);
        }

        private void AddKeyframeListOk(Window callingWindow)
        {
            string name = ((TextInputWindow)callingWindow).Text;

            if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.All)
            {
                AnimationSequence newSequence = new AnimationSequence();
                newSequence.Name = name;
                EditorData.GlobalInstructionSets.Add(newSequence);
            }
            else
            {

                KeyframeList keyframeList = new KeyframeList();

                EditorData.EditorLogic.CurrentInstructionSet.Add(keyframeList);
                keyframeList.Name = name;

                //GuiData.ListBoxWindow.InstructionSetListBox.HighlightItem(item);
            }
            GuiData.ListBoxWindow.UpdateLists();


        }


        private void AddKeyframe(Window callingWindow)
        {


            #region See if adding is allowed (Are there objects to record).  If not, show a message

            if (EditorData.CurrentSpriteMembersWatching.Count == 0 &&
                EditorData.CurrentSpriteFrameMembersWatching.Count == 0 &&
                EditorData.CurrentPositionedModelMembersWatching.Count == 0 &&
                EditorData.CurrentTextMembersWatching.Count == 0)
            {
                GuiManager.ShowMessageBox("There are no members being recorded.  Try opening the " +
                    "\"used members\" window through Window->Used Members menu item.", "No members");
                return;
            }

            if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.Current)
            {
                if (EditorData.EditorLogic.CurrentKeyframeList == null)
                {
                    GuiManager.ShowMessageBox("There is no Keyframe List currently selected", "Error");
                    return;
                }

                if (EditorData.EditorLogic.CurrentSprites.Count == 0 &&
                    EditorData.EditorLogic.CurrentSpriteFrames.Count == 0 &&
                    EditorData.EditorLogic.CurrentPositionedModels.Count == 0 &&
                    EditorData.EditorLogic.CurrentTexts.Count == 0)
                {
                    GuiManager.ShowMessageBox("No object is selected.  Select an object to record.", "No selected object.");
                    return;
                }
            }
            else if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.All)
            {
                if (EditorData.EditorLogic.CurrentAnimationSequence == null)
                {
                    GuiManager.ShowMessageBox("There is no Animation currently selected", "Error");
                    return;
                }
            }
            #endregion

            if (GuiData.TimeLineWindow.InstructionMode == InstructionMode.All)
            {

                KeyframeListSelectionWindow klsw = new KeyframeListSelectionWindow(GuiManager.Cursor);
                GuiManager.AddWindow(klsw);
                klsw.PopulateComboBoxes(EditorData.BlockingScene, EditorData.ObjectInstructionSets);

                klsw.OkClick += AddKeyframeToGlobalInstrutionSet;
            }
            else
            {
                TextInputWindow tiw = GuiManager.ShowTextInputWindow("Enter a name for the new keyframe:", "Enter name");
                tiw.Text = "Keyframe " + EditorData.EditorLogic.CurrentKeyframeList.Count;

                tiw.OkClick += new GuiMessage(AddKeyframeOk);
            }

        }

        public static void AddKeyframeOk(Window callingWindow)
        {
            if (EditorData.EditorLogic.CurrentKeyframeList == null)
            {
                GuiManager.ShowMessageBox("There is no Keyframe List currently selected", "Error");
                return;
            }

            if (EditorData.CurrentSpriteMembersWatching.Count == 0 &&
                EditorData.CurrentSpriteFrameMembersWatching.Count == 0 &&
                EditorData.CurrentPositionedModelMembersWatching.Count == 0 &&
                EditorData.CurrentTextMembersWatching.Count == 0)
            {
                GuiManager.ShowMessageBox("There are no members being recorded.  Try opening the " +
                    "\"used members\" window through Window->Used Members menu item.", "No members");
                return;
            }

            InstructionList instructionList = new InstructionList();
            instructionList.Name = ((TextInputWindow)callingWindow).Text;
            double timeToExecute = GuiData.TimeLineWindow.timeLine.CurrentValue;
            EditorData.AddInstructionsToList(instructionList, timeToExecute);

            GuiData.TimeLineWindow.UpdateToCurrentSet();
            EditorData.EditorLogic.CurrentKeyframeList.Add(instructionList);

            GuiData.ListBoxWindow.UpdateLists();
        }

        public static void AddKeyframeToGlobalInstrutionSet(Window callingWindow)
        {
            KeyframeList keyframeList = ((KeyframeListSelectionWindow)callingWindow).SelectedKeyframeList;

            INameable targetNameable = ((KeyframeListSelectionWindow)callingWindow).SelectedNameable;

            TimedKeyframeList timedKeyframeList = new TimedKeyframeList(keyframeList, targetNameable.Name);
            timedKeyframeList.TimeToExecute = GuiData.TimeLineWindow.CurrentValue;

            // Add the selected KeyframeList to the Global InstructionSet
            EditorData.EditorLogic.CurrentAnimationSequence.Add(timedKeyframeList);

        }

        private void AdjustPositionsAndScales(Window callingWindow)
        {
            mInstructionSetListBox.SetPositionTL(ScaleX, ScaleY - 2.3f);
            mInstructionSetListBox.ScaleX = ScaleX - .5f;
            mInstructionSetListBox.ScaleY = ScaleY - 3f ;

            mAnimationSequenceListBox.X = mInstructionSetListBox.X;
            mAnimationSequenceListBox.Y = mInstructionSetListBox.Y;
            mAnimationSequenceListBox.ScaleX = mInstructionSetListBox.ScaleX;
            mAnimationSequenceListBox.ScaleY = mInstructionSetListBox.ScaleY;

            mAddKeyframeListButton.ScaleX = 6.2f;
            mAddKeyframeListButton.ScaleY = 2.2f;
            mAddKeyframeListButton.SetPositionTL(6.8f, 2 * ScaleY - 2.9f);

            mAddKeyframeButton.ScaleX = 6.2f;
            mAddKeyframeButton.ScaleY = 2.2f;
            mAddKeyframeButton.SetPositionTL(19.4f, 2 * ScaleY - 2.9f);

        }

        private void AnimationSequenceFocusUpdate(IInputReceiver inputReceiver)
        {
            Keyboard keyboard = InputManager.Keyboard;

            #region Delete

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                // Check to see which object is current and delete it

                // If a CurrentTimedKeyframeList is not null, then the CurrentAnimationSequence is also not null.
                // Therefore, always check "bottom up" or else the wrong thing will be deleted.
                if (EditorData.EditorLogic.CurrentTimedKeyframeList != null)
                {
                    EditorData.EditorLogic.CurrentAnimationSequence.Remove(EditorData.EditorLogic.CurrentTimedKeyframeList);
                    UpdateLists();

                }

                else if (EditorData.EditorLogic.CurrentAnimationSequence != null)
                {
                    EditorData.GlobalInstructionSets.Remove(EditorData.EditorLogic.CurrentAnimationSequence);
                    EditorData.EditorLogic.CurrentAnimationSequence = null;
                    UpdateLists();

                }

            }

            #endregion
        }
        
        private void DoubleClickAnimationSequenceListBox(Window callingWindow)
        {
            object highlightedObject = mAnimationSequenceListBox.GetFirstHighlightedObject();

            if (highlightedObject != null)
            {
                if (highlightedObject is AnimationSequence)
                {
                    // Any logic here?
                }
                else if (highlightedObject is TimedKeyframeList)
                {
                    // Find the object that this references and select it.
                    TimedKeyframeList asTimedKeyframeList = highlightedObject as TimedKeyframeList;

                    INameable target = EditorData.BlockingScene.FindByName(asTimedKeyframeList.TargetName);

                    if (target is Sprite)
                    {
                        EditorData.EditorLogic.SelectObject<Sprite>(target as Sprite, EditorData.EditorLogic.CurrentSprites);
                    }
                    // else other types

                    GuiData.TimeLineWindow.InstructionMode = InstructionMode.Current;

                    EditorData.EditorLogic.CurrentKeyframeList = asTimedKeyframeList.KeyframeList;
                }
            }
        }

        private string GetTimedKeyframeListStringRepresentation(object timedKeyframeList)
        {
            TimedKeyframeList asTimedKeyframeList = timedKeyframeList as TimedKeyframeList;

            return asTimedKeyframeList.TargetName + " : " + asTimedKeyframeList.Name;
        }

        private void HighlightInstructionSetListBox(Window callingWindow)
        {
            CollapseItem item = mInstructionSetListBox.GetFirstHighlightedItem();

            if (item != null)
            {
                // Set the current InstructionSet
                CollapseItem topParentItem = item.TopParent;

                EditorData.EditorLogic.CurrentKeyframeList = topParentItem.ReferenceObject as KeyframeList;

                EditorData.EditorLogic.CurrentKeyframe = mInstructionSetListBox.GetFirstHighlightedItem().ReferenceObject as InstructionList;

                // if the object is an instruction list, execute em
                if (EditorData.EditorLogic.CurrentKeyframe != null && EditorData.EditorLogic.CurrentKeyframe.Count != 0)
                {
                    GuiData.TimeLineWindow.CurrentValue =
                        EditorData.EditorLogic.CurrentKeyframe[0].TimeToExecute;
                    EditorData.EditorLogic.CurrentKeyframe.Execute();
                }

                //GuiData.SpritePropertyGrid.UpdateDisplayedProperties();
            }
            else
            {
                EditorData.EditorLogic.CurrentKeyframeList = null;

                EditorData.EditorLogic.CurrentKeyframe = null;

            }

        }

        private void InstructionListHotkeyUpdate(IInputReceiver receiver)
        {
            #region Delete

            if (InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                object highlightedObject = mInstructionSetListBox.GetFirstHighlightedObject();

                if (highlightedObject is KeyframeList)
                {
                    EditorData.EditorLogic.CurrentInstructionSet.Remove(highlightedObject as KeyframeList);

                    EditorData.EditorLogic.CurrentKeyframe = null;

                    EditorData.EditorLogic.CurrentKeyframeList = null;

                    UpdateLists();
                }
                else if (highlightedObject is InstructionList)
                {
                    EditorData.EditorLogic.CurrentKeyframeList.Remove(highlightedObject as InstructionList);
                    UpdateLists();
                }
            }

            #endregion
        }

        private void HighlightAnimationSequenceListBox(Window callingWindow)
        {
            object highlightedObject = mAnimationSequenceListBox.GetFirstHighlightedObject();

            if (highlightedObject == null)
            {

            }
            else
            {

                if (highlightedObject is AnimationSequence)
                {
                    EditorData.EditorLogic.CurrentAnimationSequence = highlightedObject as AnimationSequence;
                }
                else if (highlightedObject is TimedKeyframeList)
                {
                    CollapseItem highlightedItem = mAnimationSequenceListBox.GetFirstHighlightedItem();

                    CollapseItem parentItem = highlightedItem.TopParent;

                    EditorData.EditorLogic.CurrentAnimationSequence =
                        parentItem.ReferenceObject as AnimationSequence;

                    EditorData.EditorLogic.CurrentTimedKeyframeList = highlightedObject as TimedKeyframeList;
                }
            }
        }

        private void UpdateAddButtonVisibility(Window callingWindow)
        {
            mAddKeyframeButton.Enabled = true;
            mAddKeyframeListButton.Enabled = true;
        }

        #endregion

        #region Methods

        #region Constructor

        public ListBoxWindow() : 
            base(GuiManager.Cursor)
        {
            #region Set "this" properties.
            GuiManager.AddWindow(this);
			SetPositionTL(97.3f, 25.2f);
			ScaleX = 13.1f;
			ScaleY = 19.505f;
			mName = "Keyframes";
			mMoveBar = true;

            MinimumScaleX = ScaleX;
            MinimumScaleY = 7;

            this.Resizable = true;

            this.Resizing += AdjustPositionsAndScales;


            #endregion

            #region List Box

            mInstructionSetListBox = AddCollapseListBox();
            mInstructionSetListBox.Highlight += new GuiMessage(UpdateAddButtonVisibility);
            mInstructionSetListBox.Highlight += HighlightInstructionSetListBox;
                                                  
            mInstructionSetListBox.FocusUpdate += InstructionListHotkeyUpdate;
            #endregion

            #region AnimationSequence ListDisplayWindow

            mAnimationSequenceListBox = new ListDisplayWindow(mCursor);
            this.AddWindow(mAnimationSequenceListBox);
            mAnimationSequenceListBox.ListBox.Highlight += HighlightAnimationSequenceListBox;
            mAnimationSequenceListBox.ListBox.StrongSelect += DoubleClickAnimationSequenceListBox;
            mAnimationSequenceListBox.ListBox.FocusUpdate += new FocusUpdateDelegate(AnimationSequenceFocusUpdate);
            ListDisplayWindow.SetStringRepresentationMethod(typeof(TimedKeyframeList), GetTimedKeyframeListStringRepresentation);

            #endregion

            #region Add Keyframe List Button

            mAddKeyframeListButton = AddButton();
            mAddKeyframeListButton.Text = "Add Animation";
            mAddKeyframeListButton.Click += AddKeyframeListClick; // this will call UpdateVisibleWindows

            #endregion

            #region Add Keyframe Button

            mAddKeyframeButton = AddButton();
            mAddKeyframeButton.Text = "Add Keyframe";
            mAddKeyframeButton.Click += AddKeyframe;

            #endregion

            AdjustPositionsAndScales(null);
        }

        #endregion

        #region Public Methods

        public void HighlightNoCall(InstructionList keyframe)
        {
            UpdateLists();

            mInstructionSetListBox.HighlightObjectNoCall(keyframe, false);
        }

        public void HighlightNoCall(KeyframeList keyframeList)
        {
            UpdateLists();

            mInstructionSetListBox.HighlightObjectNoCall(keyframeList, false);
        }

        public void UpdateLists()
        {

            #region Update List window visibility 

            mInstructionSetListBox.Visible = GuiData.TimeLineWindow.InstructionMode == InstructionMode.Current;
            mAnimationSequenceListBox.Visible = GuiData.TimeLineWindow.InstructionMode == InstructionMode.All;

            #endregion

            #region Update the Global InstructionSets ListDisplayWindow

            mAnimationSequenceListBox.ListShowing = EditorData.GlobalInstructionSets;

            #endregion

            #region If there is not a CurrentInstructionSet

            if (EditorData.EditorLogic.CurrentInstructionSet == null)
            {
                mInstructionSetListBox.Clear();
            }

            #endregion

            #region Else, there is
            else
            {
                #region See if there are any KeyframeLists that aren't shown in the list
                for (int i = 0; i < EditorData.EditorLogic.CurrentInstructionSet.Count; i++)
                {
                    if (mInstructionSetListBox.ContainsObject(
                        EditorData.EditorLogic.CurrentInstructionSet[i]) == false)
                    {
                        CollapseItem item = mInstructionSetListBox.AddItem(
                            EditorData.EditorLogic.CurrentInstructionSet[i].Name,
                            EditorData.EditorLogic.CurrentInstructionSet[i]);
                    }
                    else
                    {
                        mInstructionSetListBox.GetItem(EditorData.EditorLogic.CurrentInstructionSet[i]).Text =
                            EditorData.EditorLogic.CurrentInstructionSet[i].Name;
                    }
                }
                #endregion

                #region See if there are any Keyframes that aren't in the ListBox

                foreach (KeyframeList keyframeList in EditorData.EditorLogic.CurrentInstructionSet)
                {
                    CollapseItem itemForKeyframe = mInstructionSetListBox.GetItem(keyframeList);

                    for (int i = 0; i < keyframeList.Count; i++)
                    {
                        InstructionList list = keyframeList[i];

                        string listName = "";

                        if (list.Count != 0)
                        {
                            string numberString = list[0].TimeToExecute.ToString("00.000");
                            listName = numberString + "  " + list.Name;

                        }
                        else
                            listName = list.Name;

                        if (itemForKeyframe.Contains(list) == false)
                        {
                            itemForKeyframe.InsertItem(i,
                                listName, list);
                        }
                        else
                        {
                            itemForKeyframe.GetItem(list).Text = listName;
                        }
                    }
                }

                #endregion

                #region See if there are any KeyframeLists or Keyframes in the ListBox that aren't in the List

                for (int itemNumber = 0; itemNumber < mInstructionSetListBox.Items.Count; itemNumber++)
                {
                    CollapseItem item = mInstructionSetListBox.Items[itemNumber];

                    KeyframeList keyframeList = item.ReferenceObject as KeyframeList;

                    if (EditorData.EditorLogic.CurrentInstructionSet.Contains(keyframeList) == false)
                    {
                        mInstructionSetListBox.RemoveCollapseItem(item);
                    }
                    else
                    {

                        for (int i = item.Count - 1; i > -1; i--)
                        {
                            CollapseItem subItem = item[i];

                            InstructionList keyframe = subItem.ReferenceObject as InstructionList;

                            if (keyframeList.Contains(keyframe) == false)
                            {
                                item.RemoveObject(keyframe);
                            }
                        }
                    }

                }

                #endregion
            }
            #endregion

        }

        #endregion

        #endregion
    }
}
