using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

using SpriteEditor.SEPositionedObjects;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
//using FlatRedBall.Texture;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Collections;
using FlatRedBall.Utilities;
using FlatRedBall.Input;

using EditorObjects;

using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;

using SpriteEditor.Gui;
using FlatRedBall.Graphics.Animation;
using EditorObjects.EditorSettings;

namespace SpriteEditor
{
	/// <summary>
	/// Summary description for SECursor.
	/// </summary>
	public class SECursor : Cursor
	{
		#region Fields
		
        SpriteFrameManager sfMan;

        SpriteList currentSprites;

        AttachableList<SpriteFrame> currentSpriteFrames;


		EditAxes axes;
        public bool axesClicked;

        public bool soloEdit = false;

		#region variables for Shift-Move
		/* Shift-Moving Sprites will only allow movement on one axis.  When a Sprite is moved with Shift held down,
		 * either the y or x values are not changed.  Which is changed depends on the position of the cursor
		 * vs. the original starting point.  If dX is greater than dY, then the Y is constant and x changes.
		 * Whenever we do shift movement, we need to make sure we keep track of the old location.  That means when
		 * clicking on a Sprite in move mode, mark the original position by storing it in this value.
		 */

		Vector3 accumulatedMovement = new Vector3(0,0,0);

		List<Vector3> startingPosition;


		#endregion

        // When dealing with only one kind of object it's common to use the ObjectGrabbed
        // property that belongs in the base Cursor class. However, in this application we
        // deal with a variety of types.  Therefore, we use lists for each type.  The ObjectGrabbed
        // should never be used in this class.
        SpriteList mSpritesOver = new SpriteList();
        SpriteList mSpritesGrabbed = new SpriteList();
        ReadOnlyCollection<Sprite> mSpritesOverReadOnly;
        ReadOnlyCollection<Sprite> mSpritesGrabbedReadOnly;

        PositionedObjectList<SpriteFrame> mSpriteFramesOver = new PositionedObjectList<SpriteFrame>();
        PositionedObjectList<SpriteFrame> mSpriteFramesGrabbed = new PositionedObjectList<SpriteFrame>();
        ReadOnlyCollection<SpriteFrame> mSpriteFramesOverReadOnly;

        PositionedObjectList<PositionedModel> mPositionedModelsOver = new PositionedObjectList<PositionedModel>();
        PositionedObjectList<PositionedModel> mPositionedModelsGrabbed = new PositionedObjectList<PositionedModel>();
        ReadOnlyCollection<PositionedModel> mPositionedModelsOverReadOnly;

        PositionedObjectList<Text> mTextsOver = new PositionedObjectList<Text>();
        PositionedObjectList<Text> mTextsGrabbed = new PositionedObjectList<Text>();
        ReadOnlyCollection<Text> mTextsOverReadOnly;


        // keeps track of if a Sprite has been copied during during this click by control click/drag
		public bool mCtrlPushCopyThisFrame = false; 

        EditorObjects.RectangleSelector rectangleSelector;

		#endregion

        #region Properties

        #region Public Properties

        public ReadOnlyCollection<PositionedModel> PositionedModelsOver
        {
            get { return mPositionedModelsOverReadOnly; }
        }

        public ReadOnlyCollection<Sprite> SpritesOver
        {
            get { return mSpritesOverReadOnly; }
        }

        public ReadOnlyCollection<SpriteFrame> SpriteFramesOver
        {
            get { return mSpriteFramesOverReadOnly; }
        }

        public ReadOnlyCollection<Text> TextsOver
        {
            get { return mTextsOverReadOnly; }
        }

        public ReadOnlyCollection<Sprite> SpritesGrabbed
        {
            get { return mSpritesGrabbedReadOnly; }
        }

        #endregion

        #region Private Properties

        private bool HasObjectGrabbed
        {
            get
            {


                return mSpritesGrabbed.Count != 0 ||
                    mSpriteFramesGrabbed.Count != 0 ||
                    mTextsGrabbed.Count != 0 ||
                    mPositionedModelsGrabbed.Count != 0 ||
                    GameData.sesgMan.SpriteGrabbed != null;
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Constructor

        public SECursor(EditorCamera cameraTouse, System.Windows.Forms.Form formToUse) :
            base(cameraTouse, formToUse)
		{
            PositionedObjectMover.AllowZMovement = true;
			startingPosition = new List<Vector3>();

            #region Create the read only collections

            mSpritesOverReadOnly = new ReadOnlyCollection<Sprite>(mSpritesOver);
            mSpriteFramesOverReadOnly = new ReadOnlyCollection<SpriteFrame>(mSpriteFramesOver);
            mPositionedModelsOverReadOnly = new ReadOnlyCollection<PositionedModel>(mPositionedModelsOver);
            mTextsOverReadOnly = new ReadOnlyCollection<Text>(mTextsOver);

            mSpritesGrabbedReadOnly = new ReadOnlyCollection<Sprite>(mSpritesGrabbed);

            

            #endregion
        }

        #endregion

        #region Public Methods

        public void Initialize()
		{

            sfMan = GameData.sfMan;

            axes = GameData.EditorLogic.EditAxes;

			currentSprites = GameData.EditorLogic.CurrentSprites;

            currentSpriteFrames = GameData.EditorLogic.CurrentSpriteFrames;

            rectangleSelector = new EditorObjects.RectangleSelector();
		}


        public void Activity()
        {
            GetObjectOver();

            GrabObjects();
            // now move the grabbed object
            ControlGrabbedObject();

            SelectOnClick();

            TestObjectGrabbedRelease();
        }


        public void ClickSprite(Sprite spriteClicked)
        {
            ClickObject(spriteClicked, GameData.EditorLogic.CurrentSprites, false, false);
        }


        public void ClickObject<T>(T objectClicked, AttachableList<T> currentObjects, bool simulateShiftDown) where T : PositionedObject, ICursorSelectable, IAttachable, new()
        {
            ClickObject(objectClicked, currentObjects, simulateShiftDown, false);
        }


        public void ClickObject<T>(T objectClicked, AttachableList<T> currentObjects, bool simulateShiftDown, bool forceSelection) where T : PositionedObject, ICursorSelectable, IAttachable, new()
        {
            bool performSelection = forceSelection ||
                                    (   (WindowOver == null && PrimaryPush) ||
                                        (WindowOver == null && SecondaryPush) ||
                                        (WindowOver != null && PrimaryClick) ||
                                        (WindowOver != null && PrimaryPush) || // on list boxes
                                        PrimaryDoubleClick
                                    ) &&
                                    this.axesClicked == false;

            mCtrlPushCopyThisFrame = false;

            #region Clicking activity (when the mouse is released rather than pushed

            if (PrimaryClick)
            {

                #region Dragging an attribute from the Attribute List to an object
                if (GuiManager.CollapseItemDraggedOff != null && GuiManager.CollapseItemDraggedOff.parentBox.Name == "Attributes ListBox" &&
                    objectClicked != null)
                {
                    if (objectClicked.Name.Contains(GuiManager.CollapseItemDraggedOff.Text) == false)
                    {
                        // The objectClicked doesn't have this attribute so add it.
                        // Remove the number, add the attribute, append the number, then make sure this name is unique
                        string temporaryString = StringFunctions.RemoveNumberAtEnd(objectClicked.Name);

                        int numberAtEnd = StringFunctions.GetIntAfter(temporaryString, objectClicked.Name);

                        temporaryString += GuiManager.CollapseItemDraggedOff.Text + numberAtEnd;

                        objectClicked.Name = GameData.GetUniqueNameForObject(temporaryString, objectClicked);

                        GuiData.ListWindow.UpdateItemName(objectClicked);
                    }
                }
                #endregion

            }
            #endregion

            #region Didn't click on anything.  Get rid of the target box and axes
            if (objectClicked == null && axes.CursorPushedOnAxis == false && axesClicked == false)
            {
                if (simulateShiftDown == false && InputManager.Keyboard.KeyDown(Key.LeftShift) == false && InputManager.Keyboard.KeyDown(Key.RightShift) == false)
                {
                    // deselect all Sprites if any are selected
                    GameData.DeselectCurrentObjects(currentObjects);


                    return;

                }
            }
            #endregion

            #region We clicked on the axes, so return.
            else if (objectClicked == null && axes.CursorPushedOnAxis)
            {
                return;
            }
            #endregion

            #region ATTACH - We are going to attach a sprite
            else if (GuiData.ToolsWindow.attachSprite.IsPressed && currentObjects.Count != 0 && currentObjects[0] != objectClicked)
            {
                foreach (PositionedObject positionedObject in currentObjects)
                {
                    GameData.AttachObjects(positionedObject as PositionedObject, objectClicked);
                }
            }
            #endregion

            #region Painting
            else if (GuiData.ToolsWindow.paintButton.IsPressed)
            {
                PaintObjectClicked(objectClicked);
            }
            #endregion

            #region Eyedropper - we are retreiving the texture of the Sprite with the eyedropper

            else if (GuiData.ToolsWindow.eyedropper.IsPressed)
            {
                UseEyedropperOnObject(objectClicked);
            }

            #endregion

            #region clicking on a Sprite that has a control point parent
            else if (objectClicked is ISpriteEditorObject && ((ISpriteEditorObject)objectClicked).type == "Root Control")
            {

                ClickSprite(objectClicked.TopParent as Sprite);
            }

            #endregion

            #region simply clicking on an object to select it
            else if(performSelection)
            {
                #region Add the ObjectGrabbed to the UndoManager

                if (mSpritesGrabbed.Count != 0)
                {
                    
                    UndoManager.AddToWatch(mSpritesGrabbed[0] as Sprite);
                }
                else
                {
                    // Clear out the objects that the UndoManager is watching
                    UndoManager.RecordUndos<Sprite>();
                }

                #endregion

                #region if the spriteClicked is already in currentSprites

                // only when pushing

                if (currentObjects.Contains(objectClicked) && PrimaryPush)
                {
                    // If shift is down, remove the Sprite from the group.
                    if ((InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift)) &&
                        objectClicked is ISpriteEditorObject)
                    {
                        GameData.DeselectSprite(objectClicked as Sprite);
                    }
                    // Clicking a Sprite in the group brings it to index 0.
                    // This allows the user to click on a Sprite in a group and
                    // display the properties of the just-clicked Sprite in the Properties Window.
                    else if (currentObjects.IndexOf(objectClicked) != 0)
                    {
                        currentObjects.Remove(objectClicked);
                        currentObjects.Insert(0, objectClicked);

                        GuiData.SpritePropertyGrid.SelectedObject = 
                            currentObjects[0] as Sprite;

                    }
                    return;
                }
                #endregion

                #region Either Add Sprite to current Sprites if shift clicking or clear out current Sprites and select this one
                else if (
                    (SpriteEditorSettings.EditingSprites || SpriteEditorSettings.EditingSpriteFrames || SpriteEditorSettings.EditingModels || SpriteEditorSettings.EditingTexts) && // only consider shift click on push
                    (simulateShiftDown || InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift)))
                {
                    // If we hit this code shift is either pressed or simulated.
                    currentObjects.AddUnique(objectClicked);
                }
                else
                {
                    GameData.DeselectCurrentObjects(currentObjects);

                    if (currentObjects.Count == 0)
                        currentObjects.Add(objectClicked);
                    else
                        currentObjects[0] = objectClicked;

                    GuiData.ListWindow.Highlight(currentObjects[0]);

                }

                #endregion

                #region if the selected Sprite is an ISpriteEditorObject (regular, as opposed to a Sprite in a SpriteGrid) update collision, pixelSizeExemption, and stored variables
                ISpriteEditorObject es = currentObjects[0] as ISpriteEditorObject;

                #endregion

                #region update the GUI to the new Sprite


                #region properties window updates

                if (currentObjects[0] is Sprite)
                {
                    GuiData.SpritePropertyGrid.SelectedObject = currentObjects[0] as Sprite;
                    GuiManager.BringToFront(GuiData.SpritePropertyGrid);
                }

                #endregion

                if (SpriteEditorSettings.EditingSpriteGrids == false)
                {
                    GuiData.ToolsWindow.attachSprite.Enabled = true;
                }


                if (currentObjects[0].Parent == null) GuiData.ToolsWindow.detachSpriteButton.Enabled = false;
                else GuiData.ToolsWindow.detachSpriteButton.Enabled = true;

                if (currentObjects[0].Parent != null || currentObjects[0].Children.Count != 0)
                    GuiData.ToolsWindow.setRootAsControlPoint.Enabled = true;
                else
                    GuiData.ToolsWindow.setRootAsControlPoint.Enabled = false;

                GuiData.ToolsWindow.convertToSpriteGridButton.Enabled = true;
                GuiData.ToolsWindow.convertToSpriteFrame.Enabled = true;

                if (currentObjects[0] is ISpriteEditorObject && ((ISpriteEditorObject)( currentObjects[0])).type == "Top Root Control")
                {
                    GuiData.ToolsWindow.setRootAsControlPoint.Text = "Clear Root Control Point";
                }
                else
                {
                    GuiData.ToolsWindow.setRootAsControlPoint.Text = "Set Root As Control Point";
                }

                #region attach axes, target box, and set the current sprites

                if(objectClicked is Sprite)
                    SetObjectRelativePosition(objectClicked as Sprite);

                if (objectClicked is Sprite)
                {
                    axes.CurrentObject = currentObjects[0];

                }
                #endregion

                #endregion

            }
            #endregion
        }


        public void ClickSpriteFrame(SpriteFrame spriteFrameClicked)
        {
            if (true)
            {
                ClickObject<SpriteFrame>(spriteFrameClicked, GameData.EditorLogic.CurrentSpriteFrames, false);
                return;
            }



            #region Didn't click on anything.  Get rid of the target box and axes
            if (spriteFrameClicked == null && axes.CursorPushedOnAxis == false)
            {
                GameData.DeselectCurrentSpriteFrames();
                return;

            }
            #endregion

            #region We clicked on the axes, so return.
            else if (spriteFrameClicked == null && axes.CursorPushedOnAxis)
            {
                return;
            }
            #endregion

            #region Painting the SpriteFrame
            else if (GuiData.ToolsWindow.paintButton.IsPressed &&
                GuiData.ListWindow.HighlightedTexture != null)
            {

                spriteFrameClicked.Texture = GuiData.ListWindow.HighlightedTexture;

            }
            #endregion

            #region simply clicking on a SpriteFrame to select it
            else
            {
                #region if the spriteClicked is already in currentSprites, update target boxes and return
                if (GameData.EditorLogic.CurrentSpriteFrames.Contains(spriteFrameClicked))
                {

                    if (GameData.EditorLogic.CurrentSpriteFrames.IndexOf(spriteFrameClicked) != 0)
                    {
                        GameData.EditorLogic.CurrentSpriteFrames.Remove(spriteFrameClicked);
                        GameData.EditorLogic.CurrentSpriteFrames.Insert(0, spriteFrameClicked);

                    }
                    return;
                }
                #endregion

                #region Update the listWindow to reflect the newly-selected object
                else if (SpriteEditorSettings.EditingSprites &&
                    (InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift)))
                {
                    GameData.EditorLogic.CurrentSpriteFrames.Add(spriteFrameClicked);

                    GuiData.ListWindow.SpriteFrameListBox.HighlightObject(spriteFrameClicked, true);
                }
                else
                {
                    GameData.DeselectCurrentSpriteFrames();

                    if (GameData.EditorLogic.CurrentSpriteFrames.Count == 0)
                        GameData.EditorLogic.CurrentSpriteFrames.Add(spriteFrameClicked);
                    else
                        GameData.EditorLogic.CurrentSpriteFrames[0] = spriteFrameClicked;

                    GuiData.ListWindow.SpriteFrameListBox.HighlightObject(GameData.EditorLogic.CurrentSpriteFrames[0], false);

                    // why was this next line here?  Commented out to see if removing it causes problems
                    // ihMan.SetWatch(sfMan.currentSpriteFrames);
                }
                #endregion

                #region update the GUI to the new SpriteFrame


                if (SpriteEditorSettings.EditingSpriteGrids == false)
                {
                    GuiData.ToolsWindow.attachSprite.Enabled = true;

                }


                if (GameData.EditorLogic.CurrentSpriteFrames[0].Parent == null) GuiData.ToolsWindow.detachSpriteButton.Enabled = false;
                else GuiData.ToolsWindow.detachSpriteButton.Enabled = true;

                if (GameData.EditorLogic.CurrentSpriteFrames[0].Parent != null || GameData.EditorLogic.CurrentSpriteFrames[0].Children.Count != 0)
                    GuiData.ToolsWindow.setRootAsControlPoint.Enabled = true;
                else
                    GuiData.ToolsWindow.setRootAsControlPoint.Enabled = false;

                GuiData.ToolsWindow.convertToSpriteGridButton.Enabled = true;
                GuiData.ToolsWindow.convertToSpriteFrame.Enabled = true;

                #region set the border side buttons

                SpriteFrame.BorderSides borderSides = GameData.EditorLogic.CurrentSpriteFrames[0].Borders;

                //               FlatRedBall.MSG.SpriteFram borders = 

                #endregion

                #region attach axes, target box, and set the current sprites
                SetObjectRelativePosition(GameData.EditorLogic.CurrentSpriteFrames[0]);


                axes.Visible = true;
                axes.origin.AttachTo(GameData.EditorLogic.CurrentSpriteFrames[0], false);

                #endregion

                #endregion
            }
            #endregion
        }


        private int GetObjectIndexOver<T>(AttachableList<T> allSprites, AttachableList<T> currentSpriteArray) where T: PositionedObject, IAttachable, ICursorSelectable
        {

            if (axes.CursorPushedOnAxis == true) return -1;




            #region checking to see which object we are over, considering double clicking, inactives, and the current object selected

            // this is a temporary object that we will use
            T temporaryObjectOver = default(T);
         
             
            // first see if we are over the current sprites.  They have prescedence over the other sprites, except when the attachSprite is Pressed
            // When the attachSprite is pressed, we don't want to highlight any of the currentSprites, since they can't be attached to themselves.  Highlighting
            // other Sprites makes attachment eaiser for the user.
            if (GuiData.ToolsWindow.attachSprite.IsPressed == false)
            {
                foreach (T s in currentSpriteArray)
                {
                   
                    bool isOn = false;

                    if(s is Text)
                        isOn = IsOn3D(s as Text);
                    else
                        isOn = IsOn3D(s);
                    
                    if (isOn)
                    {
                        temporaryObjectOver = s;
                        break;
                    }
                }
            }




            // if we are over the current object, but we double clicked, we need to see if there is a different 
            // object that the cursor is over.
            // Double clicking tells the SE to "skip" over the current object and grab the next, 
            // allowing the user to grab overlapped objects.  Also
            // if the attachSprite button is pressed, we want to skip over our currentSprite
            if ((temporaryObjectOver != null && PrimaryDoubleClick == true) || GuiData.ToolsWindow.attachSprite.IsPressed == true)
            {
                // what we want to do is remove the Sprite from the spriteArray, then search through it, then insert it back where it
                // belongs

                // store the index
                int index = allSprites.IndexOf(temporaryObjectOver);
                // store the Sprite (since tempSpriteGrabbed will be overwritten)
                T temporarilyRemovedSprite = temporaryObjectOver;

                if (temporaryObjectOver != null)
                {
                    // remove the sprite from the array
                    allSprites.Remove(temporaryObjectOver);
                    // now, see what the cursor is over
                    temporaryObjectOver = GetSpriteOver(allSprites);
                    // finally, put the Sprite back
                    allSprites.Insert(index, temporarilyRemovedSprite);
                }
                else
                {
                    // skip over our current Sprite
                    if (currentSpriteArray.Count != 0)
                    {
                        temporarilyRemovedSprite = currentSpriteArray[0];
                        index = allSprites.IndexOf(temporarilyRemovedSprite);
                        allSprites.Remove(temporarilyRemovedSprite);
                        // now, see what the cursor is over
                        temporaryObjectOver = GetSpriteOver(allSprites);
                        // finally, put the Sprite back
                        allSprites.Insert(index, temporarilyRemovedSprite);
                    }
                }

            }

            // even though we are not over our current object, we need to test for double 
            // clicks, because double clicking also allows
            // the user to select inactive sprites
            else if (temporaryObjectOver == null)
            {
                if (InputManager.Keyboard.KeyDown(Key.D))
                {
                    int m = 3;
                }  

                temporaryObjectOver = GetSpriteOver(allSprites, PrimaryDoubleClick);
            }
            
             
            #endregion

            return allSprites.IndexOf(temporaryObjectOver);
             
        }


        public void ToggleSoloEdit()
        {
            if(currentSprites.Count != 0)
            {
                soloEdit = !soloEdit;
            }
        }


        public string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append(base.ToString()).Append("\n").Append("# spriteGrabbed: ").Append(mSpritesGrabbed.Count);
            sb.Append("\n").Append(rectangleSelector.ToString());
            return sb.ToString();
        }


        public void VerifyAndUpdateGrabbedAgainstCurrent()
        {
            if (mSpritesGrabbed.Count != 0 && GameData.EditorLogic.CurrentSprites.Count != 0 &&
                GameData.EditorLogic.CurrentSprites.Contains(mSpritesGrabbed[0]) == false)
            {
                mSpritesGrabbed.Clear();
                mSpritesGrabbed.Add(GameData.EditorLogic.CurrentSprites[0]);
            }
        }

        #endregion

        #region Private Methods

        #region XML Docs
        /// <summary>
        /// Moves, scales, and rotates grabbed objects.
        /// </summary>
        #endregion
        private void ControlGrabbedObject()
        {

            if (HasObjectGrabbed && axes.CursorPushedOnAxis == false) // if we have a sprite grabbed
            {
                #region Get distanceFromCamera

                float distanceFromCamera = 0;

                if (mSpritesGrabbed.Count != 0)
                {
                    distanceFromCamera = (float)(mSpritesGrabbed[0].Z - mCamera.Z);

                }
                else if (mSpriteFramesGrabbed.Count != 0)
                {
                    distanceFromCamera = (float)(mSpriteFramesGrabbed[0].Z - mCamera.Z);

                }
                else if (mTextsGrabbed.Count != 0)
                {
                    distanceFromCamera = (float)(mTextsGrabbed[0].Z - mCamera.Z);

                }

                else if (mPositionedModelsGrabbed.Count != 0)
                {
                    distanceFromCamera = (float)(mPositionedModelsGrabbed[0].Z - mCamera.Z);

                }
                #endregion


                if (GuiData.ToolsWindow.MoveButton.IsPressed)
                    MoveGrabbedObject();
                else if (GuiData.ToolsWindow.RotateButton.IsPressed)
                    RotateObjects();
                else if (GuiData.ToolsWindow.ScaleButton.IsPressed)
                    ScaleObjects(distanceFromCamera);
            }
        }

        private MovementStyle GetCurrentMovementStyle()
        {
            bool isAltDown = InputManager.Keyboard.KeyDown(Key.LeftAlt) || InputManager.Keyboard.KeyDown(Key.RightAlt);

            // default to Hierarchy - change if appropriate
            MovementStyle movementStyle = MovementStyle.Hierarchy;

            if (isAltDown)
            {
                movementStyle = MovementStyle.IgnoreAttachments;
            }
            else if (GuiData.ToolsWindow.groupHierarchyControlButton.IsPressed)
            {
                movementStyle = MovementStyle.Hierarchy;
            }
            else
            {
                movementStyle = MovementStyle.Group;
            }

            return movementStyle;
        }

        private Sprite GetSpriteOver()
        {
            int index = GetObjectIndexOver<Sprite>(GameData.Scene.Sprites, currentSprites);
            if (index != -1)
            {
                return GameData.Scene.Sprites[index];
            }
            else
            {
                return null;
            }
        }

        private SpriteFrame GetSpriteFrameOver()
        {
            int index = GetObjectIndexOver(GameData.Scene.SpriteFrames, GameData.EditorLogic.CurrentSpriteFrames);
            if (index != -1)
                return GameData.Scene.SpriteFrames[index];
            else
                return null;
        }

        private Text GetTextOver()
        {
            int index = GetObjectIndexOver(GameData.Scene.Texts, GameData.EditorLogic.CurrentTexts);
            if (index != -1)
                return GameData.Scene.Texts[index];
            else
                return null;
        }

        private PositionedModel GetPositionedModelOver()
        {

            int index = GetObjectIndexOver(GameData.Scene.PositionedModels, GameData.EditorLogic.CurrentPositionedModels);
            if (index != -1)
                return GameData.Scene.PositionedModels[index];
            else
                return null;
        }

        private void GetObjectOver()
        {
            GetSpritesOver();

            GetSpriteFramesOver();

            GetPositionedModelsOver();

            GetTextsOver();

        }

        private void GetSpritesOver()
        {
            mSpritesOver.Clear();
            if (SpriteEditorSettings.EditingSprites)
            {

                if (this.WindowPushed == null && !HasObjectGrabbed && axes.CursorPushedOnAxis == false)
                {
                    rectangleSelector.Control();
                    rectangleSelector.GetObjectsOver(GameData.Scene.Sprites, mSpritesOver);
                }

                if (mSpritesOver.Count == 0)
                {
                    Sprite spriteOver = GetSpriteOver();

                    if (spriteOver != null)
                        mSpritesOver.Add(spriteOver);
                }
            }
        }

        private void GetSpriteFramesOver()
        {
            mSpriteFramesOver.Clear();
            if (SpriteEditorSettings.EditingSpriteFrames)
            {

                if (this.WindowPushed == null && HasObjectGrabbed == false && axes.CursorPushedOnAxis == false)
                {
                    rectangleSelector.Control();
                    rectangleSelector.GetObjectsOver(GameData.Scene.SpriteFrames, mSpriteFramesOver);
                }

                if (mSpriteFramesOver.Count == 0)
                {
                    SpriteFrame spriteFrameOver = GetSpriteFrameOver();

                    if (spriteFrameOver != null)
                        mSpriteFramesOver.Add(spriteFrameOver);
                }
            }
        }

        private void GetPositionedModelsOver()
        {
            mPositionedModelsOver.Clear();
            if (SpriteEditorSettings.EditingModels)
            {

                if (this.WindowPushed == null && HasObjectGrabbed == false && axes.CursorPushedOnAxis == false)
                {
                    rectangleSelector.Control();
                    rectangleSelector.GetObjectsOver(GameData.Scene.PositionedModels, mPositionedModelsOver);
                }

                if (mPositionedModelsOver.Count == 0)
                {
                    PositionedModel positionedModelOver = GetPositionedModelOver();

                    if (positionedModelOver != null)
                        mPositionedModelsOver.Add(positionedModelOver);
                }
            }
        }

        private void GetTextsOver()
        {
            mTextsOver.Clear();
            if (SpriteEditorSettings.EditingTexts)
            {


                if (this.WindowPushed == null && HasObjectGrabbed == false && axes.CursorPushedOnAxis == false)
                {
                    rectangleSelector.Control();
                    rectangleSelector.GetObjectsOver(GameData.Scene.Texts, mTextsOver);
                }

                if (mTextsOver.Count == 0)
                {
                    Text text = GetTextOver();

                    if (text != null)
                        mTextsOver.Add(text);
                }
            }
        }

        private void GrabObjects()
        {
            GrabSprites();

            GrabSpriteFrames();

            GrabPositionedModels();

            GrabTexts();
        }

        private void GrabPositionedModels()
        {
            Cursor cursor = GuiManager.Cursor;

            if (SpriteEditorSettings.EditingModels && cursor.WindowOver == null && cursor.PrimaryPush)
            {
                mPositionedModelsGrabbed.Clear();

                mPositionedModelsGrabbed.AddRange(mPositionedModelsOver);

                if (mPositionedModelsOver.Count != 0)
                {
                    PositionedObjectMover.SetStartPosition(mPositionedModelsOver[0]);

                    bool shouldDeselect = true;

                    // If the user selects a PositionedModel that is already selected, then don't deselect.
                    if (mPositionedModelsOver.Count == 1 && GameData.EditorLogic.CurrentPositionedModels.Contains(mPositionedModelsOver[0]))
                        shouldDeselect = false;

                    if (GuiData.ToolsWindow.attachSprite.IsPressed)
                        shouldDeselect = false;

                    if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                        GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);

                    // See GrabSprites for info on this call
                    foreach (PositionedModel positionedModel in mPositionedModelsOver)
                        this.ClickObject(positionedModel, GameData.EditorLogic.CurrentPositionedModels, true, true);
                }
            }
        }

        private void GrabSprites()
        {
            Cursor cursor = GuiManager.Cursor;

            if (SpriteEditorSettings.EditingSprites && cursor.WindowOver == null && 
                (cursor.PrimaryPush || cursor.SecondaryPush))
            {
                mSpritesGrabbed.Clear();

                mSpritesGrabbed.AddRange(mSpritesOver);

                if (mSpritesOver.Count != 0)
                {
                    PositionedObjectMover.SetStartPosition(mSpritesOver[0]);

                    bool shouldDeselect = true;

                    // If the user selects a Sprite that is already selected, then don't deselect.
                    if (mSpritesOver.Count == 1 && GameData.EditorLogic.CurrentSprites.Contains(mSpritesOver[0]))
                        shouldDeselect = false;

                    if (GuiData.ToolsWindow.attachSprite.IsPressed)
                        shouldDeselect = false;

                    if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                        GameData.DeselectCurrentSprites();

                    // This selects all Sprites in the rectangle selector.
                    // the arguments are as follows:
                    // s - The Sprite to select
                    // GameData.EditorLogic.CurrentSprites - the list of objects which contains the current object.  This is used to determine
                    //      the type of the object to select
                    // true (first) - Whether to simulate shift down.  This simulates the behavior of selecting every
                    //      sprite in newlySelectedSprites with the Shift key down.
                    // true (second) - This tells the ClickObject method that we are performing a valid selection.  Normally selections
                    //      are only performed when pushing the mouse button and not on a in-SpriteEditor Window or when clicking the
                    //      mouse button when on a window.  This code is only reached when clicking when not in a window.  Basically
                    //      we are telling the ClickObject method "I know you normally don't select objects in this kind of situation,
                    //      but just trust us this time."
                    foreach (Sprite sprite in mSpritesOver)
                        this.ClickObject(sprite, GameData.EditorLogic.CurrentSprites, true, true);
                }
            }
        }

        private void GrabSpriteFrames()
        {
            Cursor cursor = GuiManager.Cursor;

            if (SpriteEditorSettings.EditingSpriteFrames && cursor.WindowOver == null && cursor.PrimaryPush)
            {
                mSpriteFramesGrabbed.Clear();

                mSpriteFramesGrabbed.AddRange(mSpriteFramesOver);

                if (mSpriteFramesOver.Count != 0)
                {
                    PositionedObjectMover.SetStartPosition(mSpriteFramesOver[0]);

                    bool shouldDeselect = true;

                    // If the user selects a Sprite that is already selected, then don't deselect.
                    if (mSpriteFramesOver.Count == 1 && GameData.EditorLogic.CurrentSpriteFrames.Contains(mSpriteFramesOver[0]))
                        shouldDeselect = false;

                    if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                        GameData.DeselectCurrentSpriteFrames();

                    // See GrabSprites for info on this call
                    foreach (SpriteFrame spriteFrame in mSpriteFramesOver)
                        this.ClickObject(spriteFrame, GameData.EditorLogic.CurrentSpriteFrames, true, true);
                }
            }
        }

        private void GrabTexts()
        {
            Cursor cursor = GuiManager.Cursor;

            if (SpriteEditorSettings.EditingTexts && cursor.WindowOver == null && cursor.PrimaryPush)
            {
                mTextsGrabbed.Clear();

                mTextsGrabbed.AddRange(mTextsOver);

                if (mTextsOver.Count != 0)
                {
                    PositionedObjectMover.SetStartPosition(mTextsOver[0]);

                    bool shouldDeselect = true;

                    // If the user selects a Sprite that is already selected, then don't deselect.
                    if (mTextsOver.Count == 1 && GameData.EditorLogic.CurrentTexts.Contains(mTextsOver[0]))
                        shouldDeselect = false;

                    if (GuiData.ToolsWindow.attachSprite.IsPressed)
                        shouldDeselect = false;

                    if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                        GameData.DeselectCurrentTexts();

                    // See GrabSprites for info on this call
                    foreach (Text text in mTextsOver)
                        this.ClickObject(text, GameData.EditorLogic.CurrentTexts, true, true);
                }
            }
        }

        #region Move/Scale/Rotate


        private void MoveGrabbedObject()
        {

            if (PrimaryDown || SecondaryDown)
            {

                #region ctrl + click occured, so copy the object grabbed

                if (mCtrlPushCopyThisFrame == false && PrimaryPush &&
                    (InputManager.Keyboard.KeyDown(Key.LeftControl) || InputManager.Keyboard.KeyDown(Key.RightControl)) &&
                    (mSpritesGrabbed.Count != 0 || mSpriteFramesGrabbed.Count != 0 || mPositionedModelsGrabbed.Count != 0 || mTextsGrabbed.Count != 0)
                    )
                {
                    GuiData.ToolsWindow.DuplicateClick();

                    mCtrlPushCopyThisFrame = true;
                }
                #endregion

                AttachableList<PositionedObject> objectsMoving = null;

                #region If editing SpriteGrids
                if(SpriteEditorSettings.EditingSpriteGrids)
                {

                    PositionedObject spriteToMove2 = GameData.sesgMan.SpriteGrabbed;

                    #region when pushing down button, reset the accumulatedMovement vector to 0
                    if (PrimaryPush || SecondaryPush || startingPosition.Count == 0)
                    {
                        accumulatedMovement = new Vector3(0, 0, 0);

                        startingPosition.Clear();

                        if (HasObjectGrabbed)
                            startingPosition.Add(new Vector3(spriteToMove2.X, spriteToMove2.Y, spriteToMove2.Z));
                    }
                    #endregion

                    Vector3 movementVector = new Vector3(0, 0, 0);

                    #region get the movementVector depending on whether cursorPlaneXY is pressed

                    Sprite objectGrabbed = GameData.sesgMan.SpriteGrabbed;

                    if (PrimaryDown)
                    {

                            movementVector.X = WorldXChangeAt(objectGrabbed.Z);
                            movementVector.Y = WorldYChangeAt(objectGrabbed.Z);

                    }
                    else if (SecondaryDown)
                    {
                        StaticPosition = true;

                        if (YVelocity != 0)
                        {
                            movementVector.Z = YVelocity / 10.0f;
                        }
                    }
                    #endregion

                    #region if G is not down, move the entire grid
                    if (!InputManager.Keyboard.KeyDown(Key.G))
                    {
                        SpriteGrid spriteGrid = SESpriteGridManager.CurrentSpriteGrid;


                        spriteGrid.Shift(movementVector.X,
                                movementVector.Y, movementVector.Z);

                        spriteGrid.XLeftBound += movementVector.X;
                        spriteGrid.XRightBound += movementVector.X;
                        spriteGrid.YTopBound += movementVector.Y;
                        spriteGrid.YBottomBound += movementVector.Y;
                        spriteGrid.ZCloseBound += movementVector.Z;
                        spriteGrid.ZFarBound += movementVector.Z;

                        SESpriteGridManager.oldPosition.X = currentSprites[0].X;
                        SESpriteGridManager.oldPosition.Y = currentSprites[0].Y;
                        SESpriteGridManager.oldPosition.Z = currentSprites[0].Z;

                    }
                    #endregion
                    #region G is down, so move just one Sprite
                    else
                    {
                        accumulatedMovement.X += movementVector.X;
                        accumulatedMovement.Y += movementVector.Y;
                        accumulatedMovement.Z += movementVector.Z;

                        spriteToMove2.X = startingPosition[0].X + accumulatedMovement.X;
                        spriteToMove2.Y = startingPosition[0].Y + accumulatedMovement.Y;

                        if (accumulatedMovement.Z != 0)
                        {
                            spriteToMove2.Z = startingPosition[0].Z + accumulatedMovement.Z;
                            GameData.Scene.Sprites.SortZInsertionDescending();
                        }
                    }
                    #endregion
                }
                #endregion

                #region else editing something else
                else
                {
                    MovementStyle movementStyle = GetCurrentMovementStyle();

                    if (SpriteEditorSettings.EditingSprites)
                    {
                        PositionedObjectMover.MouseMoveObjects(GameData.EditorLogic.CurrentSprites, movementStyle);
                    }
                    else if (SpriteEditorSettings.EditingSpriteFrames)
                    {
                        PositionedObjectMover.MouseMoveObjects(GameData.EditorLogic.CurrentSpriteFrames, movementStyle);
                    }
                    else if (SpriteEditorSettings.EditingModels)
                    {
                        PositionedObjectMover.MouseMoveObjects(GameData.EditorLogic.CurrentPositionedModels, movementStyle);
                    }
                    else if (SpriteEditorSettings.EditingTexts)
                    {
                        PositionedObjectMover.MouseMoveObjects(GameData.EditorLogic.CurrentTexts, movementStyle);
                    }

                }

                #endregion


            }
        }

        private void ApplyMovementVector(Vector3 movementVector, AttachableList<PositionedObject> spritesToApplyTo)
        {
            for (int i = 0; i < spritesToApplyTo.Count; i++)
            {
                spritesToApplyTo[i].X = startingPosition[i].X + movementVector.X;
                spritesToApplyTo[i].Y = startingPosition[i].Y + movementVector.Y;
                spritesToApplyTo[i].Z = startingPosition[i].Z + movementVector.Z;
            }
        }

        private void RotateObjects()
        {
            if (YVelocity == 0) return;

            MovementStyle movementStyle = GetCurrentMovementStyle();

            if (SpriteEditorSettings.EditingSprites)
            {
                PositionedObjectRotator.MouseRotateObjects(GameData.EditorLogic.CurrentSprites, movementStyle);
            }
            else if (SpriteEditorSettings.EditingSpriteFrames)
            {
                PositionedObjectRotator.MouseRotateObjects(GameData.EditorLogic.CurrentSpriteFrames, movementStyle);
            }
            else if (SpriteEditorSettings.EditingModels)
            {
                PositionedObjectRotator.MouseRotateObjects(GameData.EditorLogic.CurrentPositionedModels, movementStyle);
            }
            else if (SpriteEditorSettings.EditingTexts)
            {
                PositionedObjectRotator.MouseRotateObjects(GameData.EditorLogic.CurrentTexts, movementStyle);
            }
        }


        private void ScaleObjects(float distanceFromCamera)
        {

            if (GameData.Cursor.XVelocity == 0 && GameData.Cursor.YVelocity == 0) return;

            #region loop through all currentSprites

            foreach (Sprite s in GameData.EditorLogic.CurrentSprites)
            {
                #region T key is not down, so we are not doing sticky scaling

                if (InputManager.Keyboard.KeyDown(Key.T) == false)
                {
                    StaticPosition = true;

                    #region Current Sprite is ISpriteEditorObject
                    if (s as ISpriteEditorObject != null)
                    {

                        if (PrimaryDown)
                        {
                            float xMultiplier;
                            float yMultiplier;

                            if ((s.RotationZ > (float)System.Math.PI * 0.25f && s.RotationZ < (float)System.Math.PI * 0.75f) ||
                                 (s.RotationZ > (float)System.Math.PI * 1.25f && s.RotationZ < (float)System.Math.PI * 1.75f))
                            {
                                // Since the object is rotated us the Y for ScaleX and X for Scaley
                                xMultiplier = YVelocity / 100.0f;
                                yMultiplier = XVelocity / 100.0f;
                            }
                            else
                            {
                                xMultiplier = XVelocity / 100.0f;
                                yMultiplier = YVelocity / 100.0f;
                            }


                            (s).ScaleX *= xMultiplier + 1;
                            (s).ScaleY *= yMultiplier + 1;

                            if (InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift))
                            {
                                (s).ScaleX = (s).ScaleY * GameData.EditorLogic.xToY;
                                xMultiplier = yMultiplier;
                            }

                            if (mSpritesGrabbed.Count != 0 && (((EditorSprite)mSpritesGrabbed[0]).type == "Top Root Control"))
                            {
                                for (int i = s.Children.Count - 1; i > -1; i--)
                                {
                                    GameData.ApplyRelativeScale((Sprite)s.Children[i], xMultiplier, yMultiplier);
                                }
                            }
                        }
                        else // secondary down
                        {
                            /*
                            if (spriteGrabbed.type == "Top Root Control")
                            {
                                foreach (PositionedObject po in s.Children)
                                {
                                    GameData.ApplyRelativeScale((Sprite)po, 0, 0, yVelocity / 4.0f);
                                }

                            }
                             */
                        }
                    }
                    #endregion

                    #region Current Sprite is not ISpriteEditorObject
                    else
                    {

                        if (s.RotationZ == (float)System.Math.PI * .5f || s.RotationZ == (float)System.Math.PI * 1.5f)
                        {
                            s.ScaleX *= 1 + YVelocity / 100.0f;
                            s.ScaleY *= 1 + XVelocity / 100.0f;
                        }
                        else
                        {
                            s.ScaleX *= 1 + XVelocity / 100.0f;
                            s.ScaleY *= 1 + YVelocity / 100.0f;
                        }
                        if (InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift))
                            s.ScaleX = s.ScaleY * GameData.EditorLogic.xToY;

                    }
                    #endregion


                }
                #endregion

                #region Sticky is down so don't make the cursor static
                else
                    StaticPosition = false;
                #endregion

            }
            #endregion

            #region loop through all SpriteFrames

            foreach (SpriteFrame sf in GameData.EditorLogic.CurrentSpriteFrames)
            {
                if (sf.RotationZ == (float)System.Math.PI * .5f || sf.RotationZ == (float)System.Math.PI * 1.5f)
                {
                    sf.ScaleX *= 1 + YVelocity / 100.0f;
                    sf.ScaleY *= 1 + XVelocity / 100.0f;
                }
                else
                {
                    sf.ScaleX *= 1 + XVelocity / 100.0f;
                    sf.ScaleY *= 1 + YVelocity / 100.0f;
                }
                if (InputManager.Keyboard.KeyDown(Key.LeftShift) || InputManager.Keyboard.KeyDown(Key.RightShift))
                    sf.ScaleX = sf.ScaleY * GameData.EditorLogic.xToY;
                StaticPosition = true;

            }

            #endregion

            foreach (Text text in GameData.EditorLogic.CurrentTexts)
            {
                float scaleAmount = 1 + YVelocity / 100.0f;

                text.Scale *= scaleAmount;
                text.Spacing *= scaleAmount;
                text.NewLineDistance *= scaleAmount;

                StaticPosition = true;
            }

            #region Loop through all Models
            foreach (PositionedModel model in GameData.EditorLogic.CurrentPositionedModels)
            {
                if (PrimaryDown)
                {
                    model.ScaleX *= 1 + XVelocity / 100.0f;
                    model.ScaleY *= 1 + YVelocity / 100.0f;
                }
                else if (SecondaryDown)
                {
                    model.ScaleZ *= 1 + YVelocity / 100.0f;
                }

                StaticPosition = true;


            }
            #endregion

        }
        #endregion

        private void PaintObjectClicked<T>(T objectClicked) where T : PositionedObject, ICursorSelectable, IAttachable, new()
        {
            // painting SpriteGrid is handled in the sesgManager's grabGridSprite method
            if (SESpriteGridManager.CurrentSpriteGrid == null)
            {
                #region If editing Sprites

                if (objectClicked is EditorSprite)
                {
                    EditorSprite asEditorSprite = objectClicked as EditorSprite;

                    #region Painting AnimationChain
                    if (GameData.EditorLogic.CurrentAnimationChainList != null)
                    {
                        // make sure the same AnimationChainList isn't being applied
                        if (GameData.EditorLogic.CurrentAnimationChainList.Name != asEditorSprite.AnimationChains.Name)
                        {
                            asEditorSprite.AnimationChains = GameData.EditorLogic.CurrentAnimationChainList.Clone();

                            // Start the animation too
                            if (asEditorSprite.AnimationChains.Count != 0)
                            {
                                asEditorSprite.SetAnimationChain(asEditorSprite.AnimationChains[0]);
                                asEditorSprite.Animate = true;
                            }
                        }
                    }
                    #endregion

                    #region else, painting Texture with possible display region

                    else if(GuiData.ListWindow.HighlightedTexture != null)
                    {
                        // In case it's animated, stop the animation
                        asEditorSprite.Animate = false;

                        if (asEditorSprite.ColorOperation == Microsoft.DirectX.Direct3D.TextureOperation.SelectArg2 &&
                            asEditorSprite.Texture == null)
                        {
                            // This thing was an untextured Sprite.  Let's give it a texture AND change its color
                            // operation so the texture shows up
                            asEditorSprite.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg1;
                        }

                        asEditorSprite.Texture = GuiData.ListWindow.HighlightedTexture;

                        DisplayRegion displayRegion = GuiData.ListWindow.HighlightedDisplayRegion;

                        if (displayRegion != null)
                        {
                            asEditorSprite.TopTextureCoordinate = displayRegion.Top;
                            asEditorSprite.BottomTextureCoordinate = displayRegion.Bottom;
                            asEditorSprite.LeftTextureCoordinate = displayRegion.Left;
                            asEditorSprite.RightTextureCoordinate = displayRegion.Right;
                        }
                        else
                        {
                            asEditorSprite.TopTextureCoordinate = 0;
                            asEditorSprite.BottomTextureCoordinate = 1;
                            asEditorSprite.LeftTextureCoordinate = 0;
                            asEditorSprite.RightTextureCoordinate = 1;
                        }
                    }
                    #endregion
                }
                #endregion

                #region If editing SpriteFrames

                else if (objectClicked is SpriteFrame)
                {
                    if (GuiData.ListWindow.HighlightedTexture != null)
                    {
                        SpriteFrame asSpriteFrame = objectClicked as SpriteFrame;
                        asSpriteFrame.Texture = GuiData.ListWindow.HighlightedTexture;
                    }
                }

                #endregion
            }
        }

        private void SelectOnClick()
        {

            if (this.WindowPushed == null && PrimaryClick && axesClicked == false && soloEdit == false)
            {
                #region Sprite select on click
                bool shouldDeselect = true;

                // If the user selects a Sprite that is already selected, then don't deselect.
                if (mSpritesOver.Count == 1 && GameData.EditorLogic.CurrentSprites.Contains(mSpritesOver[0]))
                    shouldDeselect = false;

                if (SESpriteGridManager.CurrentSpriteGrid != null)
                    shouldDeselect = false;

                if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                    GameData.DeselectCurrentSprites();

                // This selects all Sprites in the rectangle selector.
                // the arguments are as follows:
                // s - The Sprite to select
                // GameData.EditorLogic.CurrentSprites - the list of objects which contains the current object.  This is used to determine
                //      the type of the object to select
                // true (first) - Whether to simulate shift down.  This simulates the behavior of selecting every
                //      sprite in newlySelectedSprites with the Shift key down.
                // true (second) - This tells the ClickObject method that we are performing a valid selection.  Normally selections
                //      are only performed when pushing the mouse button and not on a in-SpriteEditor Window or when clicking the
                //      mouse button when on a window.  This code is only reached when clicking when not in a window.  Basically
                //      we are telling the ClickObject method "I know you normally don't select objects in this kind of situation,
                //      but just trust us this time."
                foreach (Sprite s in mSpritesOver)
                    this.ClickObject(s, GameData.EditorLogic.CurrentSprites, true, true);
                #endregion

                #region SpriteFrame select on click
                shouldDeselect = true;

                // If the user selects a Sprite that is already selected, then don't deselect.
                if (mSpriteFramesOver.Count == 1 && GameData.EditorLogic.CurrentSpriteFrames.Contains(mSpriteFramesOver[0]))
                    shouldDeselect = false;

                if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                    GameData.DeselectCurrentSpriteFrames();

                foreach (SpriteFrame spriteFrame in mSpriteFramesOver)
                    this.ClickObject(spriteFrame, GameData.EditorLogic.CurrentSpriteFrames, true, true);

                #endregion

                #region PositionedModels select on click
                shouldDeselect = true;

                // If the user selects a Sprite that is already selected, then don't deselect.
                if (mPositionedModelsOver.Count == 1 && GameData.EditorLogic.CurrentPositionedModels.Contains(mPositionedModelsOver[0]))
                    shouldDeselect = false;

                if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                    GameData.DeselectCurrentObjects<PositionedModel>(GameData.EditorLogic.CurrentPositionedModels);

                foreach (PositionedModel positionedModel in mPositionedModelsOver)
                    this.ClickObject(positionedModel, GameData.EditorLogic.CurrentPositionedModels, true, true);

                #endregion

                #region Text select on click

                shouldDeselect = true;

                // If the user selects a Text that is already selected, then don't deselect.
                if (mTextsOver.Count == 1 && GameData.EditorLogic.CurrentTexts.Contains(this.mTextsOver[0]))
                    shouldDeselect = false;

                if (shouldDeselect && !InputManager.Keyboard.KeyDown(Key.LeftShift) && !InputManager.Keyboard.KeyDown(Key.RightShift))
                    GameData.DeselectCurrentObjects<Text>(GameData.EditorLogic.CurrentTexts);

                foreach (Text text in mTextsOver)
                    this.ClickObject(text, GameData.EditorLogic.CurrentTexts, true, true);


                #endregion
            }


        }

        private void TestObjectGrabbedRelease()
        {
            if (!PrimaryDown && !SecondaryDown)
            {
                #region Release logic for Sprites

                if (mSpritesGrabbed.Count != 0)
                {
                    if (SpriteEditorSettings.EditingSprites)
                    {
                        if (GameData.EditorLogic.SnappingManager.ShouldSnap)
                        {
                            // later will want to consider parents and multiple Sprites.
                            mSpritesGrabbed[0].Position = GameData.EditorLogic.SnappingManager.SnappingPosition;
                        }
                    }


                    UndoManager.RecordUndos<Sprite>();

                    UndoManager.ClearObjectsWatching<Sprite>();

                    mSpritesGrabbed.Clear();
                }

                #endregion

                if (mSpriteFramesGrabbed.Count != 0)
                {
                    if (SpriteEditorSettings.EditingSpriteFrames)
                    {
                        if (GameData.EditorLogic.SnappingManager.ShouldSnap)
                        {
                            // later will want to consider parents and multiple Sprites.
                            mSpriteFramesGrabbed[0].Position = GameData.EditorLogic.SnappingManager.SnappingPosition;
                        }
                    }


                    UndoManager.RecordUndos<SpriteFrame>();

                    UndoManager.ClearObjectsWatching<SpriteFrame>();

                    mSpriteFramesGrabbed.Clear();
                }
                if (mTextsGrabbed.Count != 0)
                {
                    UndoManager.RecordUndos<Text>();

                    UndoManager.ClearObjectsWatching<Text>();

                    mTextsGrabbed.Clear();

                }
                if (mPositionedModelsGrabbed.Count != 0)
                {
                    UndoManager.RecordUndos<PositionedModel>();

                    UndoManager.ClearObjectsWatching<PositionedModel>();

                    mPositionedModelsGrabbed.Clear();
                }
            }
        }

        private void UseEyedropperOnObject<T>(T target) where T : PositionedObject, ICursorSelectable, IAttachable, new()
        {
            #region First check if the object has an AnimationChainList
            IAnimationChainAnimatable animationChainAnimatable = target as IAnimationChainAnimatable;

            if (animationChainAnimatable != null && animationChainAnimatable.Animate &&
                animationChainAnimatable.AnimationChains != null && animationChainAnimatable.AnimationChains.Count != 0)
            {
                GuiData.ListWindow.HighlightAnimationChainListByName(
                    animationChainAnimatable.AnimationChains.Name);

                GuiData.ListWindow.ViewingAnimationChains = true;
            }

            #endregion

            #region else, try to get the object's texture
            else if (target is ITexturable)
            {
                ITexturable texturable = target as ITexturable;

                GuiData.ListWindow.Highlight(texturable.Texture);

                if (texturable.Texture != null)
                {
                    GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(texturable.Texture, null);
                    // and set the texture coordinates

                    if(texturable is Sprite)
                    {
                        Sprite asSprite = texturable as Sprite;
                        GuiData.TextureCoordinatesSelectionWindow.LeftTU = asSprite.LeftTextureCoordinate;
                        GuiData.TextureCoordinatesSelectionWindow.RightTU = asSprite.RightTextureCoordinate;
                        GuiData.TextureCoordinatesSelectionWindow.TopTV = asSprite.TopTextureCoordinate;
                        GuiData.TextureCoordinatesSelectionWindow.BottomTV = asSprite.BottomTextureCoordinate;
                    }
                }
                else
                    GuiData.ToolsWindow.currentTextureDisplay.SetOverlayTextures(null, null);
            }
            #endregion

        }
        

        #endregion

        #endregion
    }
}
