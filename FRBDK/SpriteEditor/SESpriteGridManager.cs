using System;
using System.Collections;
using System.Collections.Generic;

using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Gui;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using SpriteEditor.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Animation;
using SpriteGrid = FlatRedBall.ManagedSpriteGroups.SpriteGrid;
using EditorObjects;
using EditorObjects.EditorSettings;

namespace SpriteEditor
{
	/// <summary>
	/// Summary description for SESpriteGridManager.
	/// </summary>
	public class SESpriteGridManager
	{
		#region Fields

		Cursor cursor;
		Camera camera;
        static SpriteList currentSprites;

		GuiMessages messages;

        List<TextureLocation<Texture2D>> tla;

        public static SpriteGrid CurrentSpriteGrid = null;
		public SpriteGrid SpriteGridGrabbed = null;
        Sprite mSpriteGrabbed;

        /* When clicking on a new Sprite in a SpriteGrid, we don't want to select it until we apply
         * the changes to the old SpriteGrid we had selected. That is, if I have Sprite X selected, and I click on Y,
         * I need to keep track of X so that I can apply changes, then I select Sprite Y.  Y is stored
         * in the newlySelectedCurrentSprite.
         * 
         */
        public Sprite newlySelectedCurrentSprite = null;
        public SpriteGrid newlySelectedCurrentSpriteGrid = null;

		public static Vector3 oldPosition = Vector3.Empty;

        SpriteGridBorder mSpriteGridBorder;

//		public Sprite currentGridMarker;


		#endregion

        #region Properties

        public Sprite SpriteGrabbed
        {
            get { return mSpriteGrabbed; }
            set { mSpriteGrabbed = value; }
        }

        #endregion

        #region Delegate Methods

        private void RepopulateSpriteGrid(object spriteGrid)
        {
            SpriteGrid asSpriteGrid = spriteGrid as SpriteGrid;

            asSpriteGrid.PopulateGrid();
        }

        #endregion

        #region Methods

        #region Constructor

        public SESpriteGridManager()
		{
			camera = GameData.Camera;
			cursor = GameData.Cursor;
			currentSprites = GameData.EditorLogic.CurrentSprites;

			messages = GuiData.messages;

            mSpriteGridBorder = new SpriteGridBorder(cursor,
                camera);

            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "XLeftBound", RepopulateSpriteGrid);
            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "XRightBound", RepopulateSpriteGrid);
            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "YTopBound", RepopulateSpriteGrid);
            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "YBottomBound", RepopulateSpriteGrid);
            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "ZCloseBound", RepopulateSpriteGrid);
            UndoManager.SetAfterUpdateDelegate(typeof(SpriteGrid), "ZFarBound", RepopulateSpriteGrid);

            tla = new List<TextureLocation<Texture2D>>();
        }

        #endregion

        #region Public Methods

        public void Activity()
        {
            if (GuiManager.Cursor.WindowOver == null)
            {
                if (GameData.EditorLogic.EditAxes.CursorOverAxis == false ||
                    // This second line makes sure that things will still work if the user
                    // happens to be over an axis but didn't grab it.
                    GameData.EditorLogic.EditAxes.CursorPushedOnAxis == false)
                {
                    mSpriteGridBorder.Activity();

                    if (mSpriteGridBorder.IsHandleGrabbed == false)
                        grabGridSprite();
                }
            }
            UpdateGridBorder();
        }


		public void ClickGrid(SpriteGrid gridClicked, Sprite spriteClicked)
        {
            #region if eyedropper is pressed, don't select the grid, just grab the texture
            if (GuiData.ToolsWindow.eyedropper.IsPressed)
            {
                GameData.Cursor.ClickSprite(spriteClicked);
            }
            #endregion

            #region Select the grid - eyedropper is not down
            else
            {
                newlySelectedCurrentSprite = null;
                newlySelectedCurrentSpriteGrid = null;

                #region if selecting a new grid, not painting, and changes have been made
                if ((gridClicked != CurrentSpriteGrid || currentSprites.Contains(spriteClicked) == false || spriteClicked == null) &&
                    GuiData.ToolsWindow.paintButton.IsPressed == false &&
                    ShouldAskUserIfChangesShouldBeApplied(gridClicked, spriteClicked))
                {
                    // The user may have changed the texture coordinates on the
                    // selected Sprite.  This should result in the new texture coordinates
                    // being "painted" on the SpriteGrid at the given location.
                    PaintTextureCoordinatesOnCurrentSpriteInGrid();
                    newlySelectedCurrentSprite = spriteClicked;
                    newlySelectedCurrentSpriteGrid = gridClicked;
                    AskIfChangesShouldBeApplied(null);


                }
                #endregion

                #region else, do not need to ask if changes should be applied
                else
                {
                    if (GuiData.ToolsWindow.paintButton.IsPressed == false &&
                        GameData.EditorLogic.CurrentSprites.Contains(spriteClicked) == false)
                    {
                        if (spriteClicked != null)
                            oldPosition = spriteClicked.Position;
                        else
                            oldPosition = Vector3.Empty;

                        PaintTextureCoordinatesOnCurrentSpriteInGrid();
                    }
                    
                    if (CurrentSpriteGrid != gridClicked)  GameData.DeselectCurrentSprites();

                    CurrentSpriteGrid = gridClicked;

                    // attach the grid border
                    mSpriteGridBorder.AttachTo(CurrentSpriteGrid);

                    GameData.Cursor.ClickSprite(spriteClicked);

                    SpriteGridGrabbed = gridClicked;
                    mSpriteGrabbed = spriteClicked;

                    if (gridClicked != null)
                    {
                        UndoManager.AddToWatch<Sprite>(gridClicked.Blueprint);


                        UndoManager.AddToWatch<SpriteGrid>(gridClicked);
                    }
                    GuiData.ListWindow.SpriteGridListBox.HighlightObject(gridClicked, false);
                }
                #endregion

                cursor.ObjectGrabbed = spriteClicked;

                if (gridClicked == null) 
                    return;

                #region Change the convert to sprite grid button to "Modify Sprite Grid"
                GuiData.ToolsWindow.convertToSpriteGridButton.Enabled = true;
                GuiData.ToolsWindow.convertToSpriteGridButton.Text = "Modify Sprite Grid";
                #endregion
            }
            #endregion
        }

        private static void PaintTextureCoordinatesOnCurrentSpriteInGrid()
        {
            if (GameData.EditorLogic.CurrentSprites.Count != 0)
            {
                Sprite sprite = GameData.EditorLogic.CurrentSprites[0];

                FloatRectangle floatRectangle = new FloatRectangle(
                        sprite.TopTextureCoordinate,
                        sprite.BottomTextureCoordinate,
                        sprite.LeftTextureCoordinate,
                        sprite.RightTextureCoordinate);

                CurrentSpriteGrid.PaintSpriteDisplayRegion(sprite.X, sprite.Y, sprite.Z,
                    ref floatRectangle);

            }
        }

        public static void AskIfChangesShouldBeApplied(GuiMessage extraMessage)
        {
            OkCancelWindow tempBox = GuiManager.ShowOkCancelWindow("Apply changes to grid?", "Grid Update");
            tempBox.OkClick += new GuiMessage(SpriteGridGuiMessages.updateSpriteGridOk);
            tempBox.CancelClick += new GuiMessage(SpriteGridGuiMessages.updateSpriteGridCancel);

            if (extraMessage != null)
            {
                tempBox.OkClick += extraMessage;
            }
        }

	
		public void DeleteGrid(SpriteGrid gridToDelete)
		{
            if (gridToDelete == null)
            {
                int m = 3;
            }
            gridToDelete.Destroy();

            GameData.Scene.SpriteGrids.Remove(gridToDelete);

            if (gridToDelete == CurrentSpriteGrid)
            {
                GameData.DeselectCurrentSprites();
                mSpriteGridBorder.AttachTo(null);
                CurrentSpriteGrid = null;
            }
        }


        public void grabGridSprite()
        {
            if (GameData.EditorLogic.EditAxes.CursorPushedOnAxis == true || GuiManager.DominantWindowActive) return;



            #region checking to see which sprite we are over, considering double clicking and current sprite selected
            // this is a temporary Sprite that we will use
            Sprite tempSpriteGrabbed = null;
            SpriteGrid tempSpriteGridGrabbed = null;
            

            // first see if we are over the current Sprite.  It has prescedence over the other sprites
            if (GameData.EditorLogic.CurrentSprites.Count != 0 && GameData.Cursor.IsOn3D(GameData.EditorLogic.CurrentSprites[0]) == true)
                tempSpriteGrabbed = GameData.EditorLogic.CurrentSprites[0];

            // if not over the current Sprite, or there is no currentSprite, see if over any Sprites in the current grid
            if (tempSpriteGrabbed == null && CurrentSpriteGrid != null)
            {
                tempSpriteGrabbed = cursor.GetSpriteOver(CurrentSpriteGrid.VisibleSprites);
            }

            // if we are not over our current Sprite, we need to see if we are over any other sprites
            if (tempSpriteGrabbed == null)
            {

                tempSpriteGrabbed = GameData.Cursor.GetSpriteOver(GameData.Scene.SpriteGrids);

                if (tempSpriteGrabbed != null)
                {
                    tempSpriteGridGrabbed = null;

                    foreach (SpriteGrid spriteGrid in GameData.Scene.SpriteGrids)
                    {
                        foreach (var list in spriteGrid.VisibleSprites)
                        {
                            if (list.Contains(tempSpriteGrabbed))
                            {
                                tempSpriteGridGrabbed = spriteGrid;
                                break;
                            }
                        }
                        if (tempSpriteGridGrabbed != null)
                        {
                            break;
                        }
                    }

                }
            }
            else
                tempSpriteGridGrabbed = CurrentSpriteGrid;

            #endregion

            #region Push either mouse button or double clicked, call click sprite, grab or attach, and update stored variables for old sprite
            if (cursor.PrimaryPush || cursor.SecondaryPush || cursor.PrimaryDoubleClick)
            {
                // we already know if we are over a Sprite.  If we have clicked with either mouse button or double clicked with the primary
                // then we can call click sprite

                // store the last currentSprite, because we need to update stored variables of the old Sprite if we select a new one
                Sprite lastCurrentSprite;
                SpriteGrid lastCurrentGrid;

                if (currentSprites.Count != 0) lastCurrentSprite = currentSprites[0];
                else lastCurrentSprite = null;

                lastCurrentGrid = CurrentSpriteGrid;

                if (newlySelectedCurrentSpriteGrid == null)
                    ClickGrid(tempSpriteGridGrabbed, tempSpriteGrabbed);

            }
            #endregion

            #region if the mouse button is held down - Painting
            if (CurrentSpriteGrid != null &&
                GuiData.ToolsWindow.paintButton.IsPressed &&
                (GuiData.ListWindow.HighlightedAnimationChain != null || GuiData.ListWindow.HighlightedTexture != null))
            {
                PaintSpriteGrid(tempSpriteGrabbed);
            }
            #endregion

            #region we released the mouse the mouse, so release any sprite grabbed
            if (cursor.PrimaryClick)
            {

                if (mSpriteGrabbed != null)
                {
                    UndoManager.RecordUndos<Sprite>();
                    // Vic says - Because of the way the UndoManager works 
                    // I can only attach the recreation of the grid to the undos
                    // for the SpriteGrid and not the Blueprint Sprite...however,
                    // the reason we need to recreate the SpriteGrid is because the
                    // Blueprint values might change from an undo.  
                    // That means that the blueprint undos should come first, then the
                    // SpriteGrid undos so that they use the proper values.  To do this
                    // we simply need to record the undos of the blueprint first.


                    bool createNewList = false; // Makes it so one undo will record everything
                    UndoManager.RecordUndos<SpriteGrid>(createNewList);

                    UndoManager.ClearObjectsWatching<SpriteGrid>();
                    UndoManager.ClearObjectsWatching<Sprite>();
                }
                SpriteGridGrabbed = null;
                mSpriteGrabbed = null;



            }
            #endregion
        }


        public static bool HasBlueprintChanged()
        {
            if (currentSprites.Count == 0)
            {
                return false;
            }
            else
            {
                return oldPosition.X != currentSprites[0].X ||
                        oldPosition.Y != currentSprites[0].Y ||
                        oldPosition.Z != currentSprites[0].Z ||
                        currentSprites[0].ScaleX != CurrentSpriteGrid.Blueprint.ScaleX ||
                        currentSprites[0].ScaleY != CurrentSpriteGrid.Blueprint.ScaleY ||
                        currentSprites[0].RotationX != CurrentSpriteGrid.Blueprint.RotationX ||
                        currentSprites[0].RotationY != CurrentSpriteGrid.Blueprint.RotationY ||
                        currentSprites[0].RotationZ != CurrentSpriteGrid.Blueprint.RotationZ;
                // Don't consider the texture coordinates - the texture coordinates on a Sprite can be changed
                // without changing the Blueprint.
            }
        }


        public void PopulateAndAddGridToEngine(SpriteGrid gridToAdd, Sprite spriteToUseAsPopulationSource)
        {
            #region select the base point for population and populate the grid
            if (gridToAdd.GridPlane == SpriteGrid.Plane.XY)
            {
                gridToAdd.PopulateGrid(camera.X,
                    camera.Y,
                    spriteToUseAsPopulationSource.Z);
            }
            else
            {
                gridToAdd.PopulateGrid(camera.X,
                    spriteToUseAsPopulationSource.Y,
                    spriteToUseAsPopulationSource.Z);
            }
            #endregion

            GameData.Scene.SpriteGrids.Add(gridToAdd);



            /* When converting a Sprite to a SpriteGrid, the editGrids is not pressed
             * (yet) when this method is called.  Therefore, if the editGrids button
             * is not pressed, then the grid was created from the currentSprite, so
             * the currentSprite should be deleted.
             * 
             * If it is pressed, the Grid has been created from a CTRL+C command
             * (copying a SpriteGrid).  In that case, no deleting of currentSprites
             * should be executed.
             */
            if ( !SpriteEditorSettings.EditingSpriteGrids && spriteToUseAsPopulationSource != null)
            {
                SpriteManager.RemoveSprite(spriteToUseAsPopulationSource);
            }

            this.ClickGrid(gridToAdd, null);

        }


        public void spriteGridArrayLogic()
        {
            if (GameData.EditorProperties.CullSpriteGrids == true)
            {
                for (int i = 0; i < GameData.Scene.SpriteGrids.Count; i++)
                {
                    SpriteGrid sg = GameData.Scene.SpriteGrids[i];

                    sg.Manage();
                    if (!sg.CreatesAutomaticallyUpdatedSprites)
                    {
                        sg.ManualAnimationUpdate();
                    }
                }
            }

        }


        public static void ShowMessageBoxIfBoundsAreInvalid()
        {
            for (int i = 0; i < GameData.Scene.SpriteGrids.Count; i++)
            {
                if (DoBoundsOverlapSprites(GameData.Scene.SpriteGrids[i]))
                {
                    GuiManager.ShowMessageBox("The SpriteGrid " + GameData.Scene.SpriteGrids[i].Name + " has bounds " +
                        "that overlap its Sprites.  You should expand the bounds to prevent unexpected behavior when re-loading your Scene",
                        "Warning");

                }
            }
        }


        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("tla.Count = ").Append(tla.Count.ToString());
            sb.Append("\nOldPosition:").Append(oldPosition.ToString());
            sb.Append("\n").Append(mSpriteGridBorder.ToString());

            return sb.ToString();
        }


        public void UpdateGridBorder()
        {
            this.mSpriteGridBorder.SetLinePolygonPositions();

        }


        #endregion

        #region Private Methods

        #region XML Docs
        /// <summary>
        /// Deselects the grid on the list window and returns whether to ask to keep changes
        /// </summary>
        /// <remarks>
        /// When either a new SpriteGrid or a different Sprite in the same SpriteGrid is selected,
        /// the ShouldAskUserIfChangesShouldBeApplied() method must be called.  This
        /// method does not deselect the currentSpriteGrid, but returns whether changes
        /// have been made so that the
        /// code calling this method can store the newly-selected Sprite and SpriteGrid 
        /// in the newlySelectedCurrentSprite and newlySelectedCurrentSpriteGrid variables.
        /// 
        /// For this, this method is private and only called from within this class.  To deselect
        /// a grid from outside of this class, call clickGrid(null, null).
        /// </remarks>
        /// <param name="tempSpriteGridGrabbed"></param>
        /// <param name="tempSpriteGrabbed"></param>
        /// <returns>Whether the currentSpriteGrid has been changed</returns>
        #endregion
        private bool ShouldAskUserIfChangesShouldBeApplied(SpriteGrid tempSpriteGridGrabbed, Sprite tempSpriteGrabbed)
		{
			GuiData.ToolsWindow.convertToSpriteGridButton.Enabled = false;

			if(tempSpriteGridGrabbed == null)
			{
                GuiData.ListWindow.SpriteGridListBox.HighlightItem(null, false);
			}

			#region we clicked, but already have a different Sprite selected on a grid
			if(CurrentSpriteGrid != null && 
				(GameData.EditorLogic.CurrentSprites.Count == 0 || tempSpriteGrabbed != GameData.EditorLogic.CurrentSprites[0]))
			{
				if(GameData.EditorLogic.CurrentSprites.Count != 0 && 
					HasBlueprintChanged())
				{
                    /* There SpriteGrid that has the Sprite being deselected has been changed.
                     * Need to store the new Sprite in newlySelectedCurrentSprite and 
                     * SpriteGrid in newlySelectedCurrentSpriteGrid and hold on to the current Sprite/
                     * SpriteGrid so that the changes can be applied if the user chooses to do so
                     * through the OKCancelWindow that is being created
                     */
                    return true;
					
				}
				else
				{
                    return false;
				}
			}
			#endregion
			
            #region if we clicked on a sprite and didn't have a grid selected before
			else if(tempSpriteGridGrabbed != null && oldPosition == Vector3.Empty)
			{
                return false;

            }

			#endregion
            
            return false;
		}


        private static bool DoBoundsOverlapSprites(SpriteGrid spriteGrid)
        {
            return spriteGrid.FurthestTopY > spriteGrid.YTopBound ||
                spriteGrid.FurthestBottomY < spriteGrid.YBottomBound ||
                spriteGrid.FurthestLeftX < spriteGrid.XLeftBound ||
                spriteGrid.FurthestRightX > spriteGrid.XRightBound ||
                
                spriteGrid.FurthestTopY < spriteGrid.FurthestBottomY ||
                spriteGrid.FurthestRightX < spriteGrid.FurthestLeftX;

        }


        private void PaintGridAt(float x, float y, float z)
        {
            #region Paint Texture
            Texture2D newTexture = GuiData.ListWindow.HighlightedTexture;
            if (newTexture != null)
            {
                Texture2D lastTexture = CurrentSpriteGrid.PaintSprite(x, y, z, newTexture);

                if (lastTexture != newTexture)
                    tla.Add(new TextureLocation<Texture2D>(lastTexture, x, y));
            }
            #endregion

            #region Paint Display Region (FloatRectangle)
            DisplayRegion newDisplayRegion = GuiData.ListWindow.HighlightedDisplayRegion;
            FloatRectangle newAsFloatRectangle = null;



            if (newDisplayRegion != null)
            {
                newAsFloatRectangle = newDisplayRegion.ToFloatRectangle();
            }
            else
            {

                // If the newDisplayRegion is null, then there's not a DisplayRegion selected in the
                // list box.  Therefore, just use the TextureDisplayWindow's coordinates.
                newAsFloatRectangle = new FloatRectangle(
                    GuiData.TextureCoordinatesSelectionWindow.TopTV,
                    GuiData.TextureCoordinatesSelectionWindow.BottomTV,
                    GuiData.TextureCoordinatesSelectionWindow.LeftTU,
                    GuiData.TextureCoordinatesSelectionWindow.RightTU);
            }

            FloatRectangle lastFloatRectangle = CurrentSpriteGrid.PaintSpriteDisplayRegion(
                x, y, z, ref newAsFloatRectangle);

            #endregion

            #region Paint AnimationChainList
            AnimationChain newAnimationChain = GuiData.ListWindow.HighlightedAnimationChain;

            if (newAnimationChain != null)
            {
                CurrentSpriteGrid.PaintSpriteAnimationChain(x, y, z, newAnimationChain);
                // Paint AnimationChainList here
            }
            #endregion

        }


        private void PaintSpriteGrid(Sprite tempSpriteGrabbed)
        {

            if (cursor.PrimaryPush)
            {
                this.tla.Clear();
                //					GameData.ihMan.SetWatch(currentSpriteGrid);
            }

            if (cursor.PrimaryDown)
            {
                Sprite spriteOver = tempSpriteGrabbed;

                if (spriteOver != null)
                {
                    PaintGridAt(spriteOver.X, spriteOver.Y, spriteOver.Z);

                    if (GuiData.ToolsWindow.brushSize.Text == "3X3" || GuiData.ToolsWindow.brushSize.Text == "5X5")
                    {
                        float sz = CurrentSpriteGrid.GridSpacing;
                        float x = (float)spriteOver.X;
                        float y = (float)spriteOver.Y;
                        float z = (float)spriteOver.Z;

                        #region 3X3 or inner 3X3 on 5X5 painting on XY grid

                        if (CurrentSpriteGrid.GridPlane == SpriteGrid.Plane.XY)
                        {
                            PaintGridAt(x + sz, y, z);

                            PaintGridAt(x + sz, y + sz, z);

                            PaintGridAt(x, y + sz, z);

                            PaintGridAt(x - sz, y + sz, z);

                            PaintGridAt(x - sz, y, z);

                            PaintGridAt(x - sz, y - sz, z);

                            PaintGridAt(x, y - sz, z);

                            PaintGridAt(x + sz, y - sz, z);
                        }
                        #endregion

                        #region 3X3 or inner 3X3 on 5X5 painting on XZ grid

                        else
                        {

                            PaintGridAt(x + sz, y, z);

                            PaintGridAt(x + sz, y, z + sz);

                            PaintGridAt(x, y, z + sz);

                            PaintGridAt(x - sz, y, z + sz);

                            PaintGridAt(x - sz, y, z);

                            PaintGridAt(x - sz, y, z - sz);

                            PaintGridAt(x, y, z - sz);

                            PaintGridAt(x + sz, y, z - sz);

                        }
                        #endregion

                        if (GuiData.ToolsWindow.brushSize.Text == "5X5")
                        {
                            #region 5X5 painting on XY grid
                            if (CurrentSpriteGrid.GridPlane == SpriteGrid.Plane.XY)
                            {
                                PaintGridAt(x + 2 * sz, y, z);

                                PaintGridAt(x + 2 * sz, y + sz, z);

                                PaintGridAt(x + 2 * sz, y + 2 * sz, z);

                                PaintGridAt(x + sz, y + 2 * sz, z);

                                PaintGridAt(x, y + 2 * sz, z);

                                PaintGridAt(x - sz, y + 2 * sz, z);

                                PaintGridAt(x - 2 * sz, y + 2 * sz, z);

                                PaintGridAt(x - 2 * sz, y + sz, z);

                                PaintGridAt(x - 2 * sz, y, z);

                                PaintGridAt(x - 2 * sz, y - sz, z);

                                PaintGridAt(x - 2 * sz, y - 2 * sz, z);

                                PaintGridAt(x - sz, y - 2 * sz, z);

                                PaintGridAt(x, y - 2 * sz, z);

                                PaintGridAt(x + sz, y - 2 * sz, z);

                                PaintGridAt(x + 2 * sz, y - 2 * sz, z);

                                PaintGridAt(x + 2 * sz, y - sz, z);

                            }
                            #endregion

                            #region 5X5 painting on XZ grid
                            else
                            {
                                PaintGridAt(x + 2 * sz, y, z);

                                PaintGridAt(x + 2 * sz, y, z + sz);

                                PaintGridAt(x + 2 * sz, y, z + 2 * sz);

                                PaintGridAt(x + sz, y, z + 2 * sz);

                                PaintGridAt(x, y, z + 2 * sz);

                                PaintGridAt(x - sz, y, z + 2 * sz);

                                PaintGridAt(x - 2 * sz, y, z + 2 * sz);

                                PaintGridAt(x - 2 * sz, y, z + sz);

                                PaintGridAt(x - 2 * sz, y, z);

                                PaintGridAt(x - 2 * sz, y, z - sz);

                                PaintGridAt(x - 2 * sz, y, z - 2 * sz);

                                PaintGridAt(x - sz, y, z - 2 * sz);

                                PaintGridAt(x, y, z - 2 * sz);

                                PaintGridAt(x + sz, y, z - 2 * sz);

                                PaintGridAt(x + 2 * sz, y, z - 2 * sz);

                                PaintGridAt(x + 2 * sz, y, z - sz);
                            }
                            #endregion
                        }
                    }

                    CurrentSpriteGrid.RefreshPaint();
                }
            }

            if (cursor.PrimaryClick)
            {
                if (tla.Count != 0)
                {
                    tla = new List<TextureLocation<Texture2D>>();
                }
            }
        }


        #endregion

        #endregion

    }
}
