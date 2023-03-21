#if WINDOWS
#define USE_CUSTOM_SHADER
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBall.Graphics
{
    #region Enums
    #region XML Docs
    /// <summary>
    /// Rendering modes available in FlatRedBall
    /// </summary>
    #endregion
    public enum RenderMode
    {
        #region XML Docs
        /// <summary>
        /// Default rendering mode (uses embedded effects in models)
        /// </summary>
        #endregion
        Default,
        #region XML Docs
        /// <summary>
        /// Color rendering mode - renders color values for a model
        /// (does not include lighting information)
        /// Effect technique: RenderColor
        /// </summary>
        #endregion
        Color,
        #region XML Docs
        /// <summary>
        /// Normals rendering mode - renders normals
        /// Effect technique: RenderNormals
        /// </summary>
        #endregion
        Normals,
        #region XML Docs
        /// <summary>
        /// Depth rendering mode - renders depth
        /// Effect technique: RenderDepth
        /// </summary>
        #endregion
        Depth,
        #region XML Docs
        /// <summary>
        /// Position rendering mode - renders position
        /// Effect technique: RenderPosition
        /// </summary>
        #endregion
        Position
    }
    #endregion


    static partial class Renderer
    {
        static IComparer<Sprite> mSpriteComparer;
        static IComparer<Text> mTextComparer;
        static IComparer<IDrawableBatch> mDrawableBatchComparer;

        #region XML Docs
        /// <summary>
        /// Gets the default Camera (SpriteManager.Camera)
        /// </summary>
        #endregion
        static public Camera Camera
        {
            get { return SpriteManager.Camera; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the list of cameras (SpriteManager.Cameras)
        /// </summary>
        #endregion
        static public PositionedObjectList<Camera> Cameras
        {
            get { return SpriteManager.Cameras; }
        }

        public static IComparer<Sprite> SpriteComparer
        {
            get { return mSpriteComparer; }
            set { mSpriteComparer = value; }
        }

        public static IComparer<Text> TextComparer
        {
            get { return mTextComparer; }
            set { mTextComparer = value; }
        }

        public static IComparer<IDrawableBatch> DrawableBatchComparer
        {
            get { return mDrawableBatchComparer; }
            set { mDrawableBatchComparer = value; }
        }

        private static void DrawIndividualLayer(Camera camera, RenderMode renderMode, Layer layer, Section section, ref RenderTarget2D lastRenderTarget)
        {
            bool hasLayerModifiedCamera = false;

            if (layer.Visible)
            {
                Renderer.CurrentLayer = layer;

                if (section != null)
                {
                    string layerName = "No Layer";
                    if(layer != null)
                    {
                        layerName = layer.Name;
                    }
                    Section.GetAndStartContextAndTime("Layer: " + layerName);
                }

                bool didSetRenderTarget = layer.RenderTarget != lastRenderTarget;
                if(didSetRenderTarget)
                {
                    lastRenderTarget = layer.RenderTarget;
                    GraphicsDevice.SetRenderTarget(layer.RenderTarget);

                    if(layer.RenderTarget != null)
                    {
                        mGraphics.GraphicsDevice.Clear(ClearOptions.Target,
                            Color.Transparent,
                            1, 0);

                    }
                }

                // No need to clear depth buffer if it's a render target
                if(!didSetRenderTarget)
                {
                    ClearBackgroundForLayer(camera);
                }


                #region Set View and Projection
                // Store the camera's FieldOfView in the oldFieldOfView and
                // set the camera's FieldOfView to the layer's OverridingFieldOfView
                // if necessary.
                mOldCameraLayerSettings.SetFromCamera(camera);

                Vector3 oldPosition = camera.Position;
                var oldUpVector = camera.UpVector;

                if (layer.LayerCameraSettings != null)
                {

                    layer.LayerCameraSettings.ApplyValuesToCamera(camera, SetCameraOptions.PerformZRotation, null, layer.RenderTarget);
                    hasLayerModifiedCamera = true;
                }

                camera.SetDeviceViewAndProjection(mCurrentEffect, layer.RelativeToCamera);
                #endregion


                if (renderMode == RenderMode.Default)
                {
                    if (layer.mZBufferedSprites.Count > 0)
                    {
                        DrawZBufferedSprites(camera, layer.mZBufferedSprites);
                    }

                    // Draw the camera's layer
                    DrawMixed(layer.mSprites, layer.mSortType,
                        layer.mTexts, layer.mBatches, layer.RelativeToCamera, camera, section);

                    #region Draw Shapes

                    DrawShapes(camera,
                        layer.mSpheres,
                        layer.mCubes,
                        layer.mRectangles,
                        layer.mCircles,
                        layer.mPolygons,
                        layer.mLines,
                        layer.mCapsule2Ds,
                        layer);

                    #endregion
                }

                // Set the Camera's FieldOfView back
                // Vic asks:  What if the user wants to have a wacky field of view?
                // Does that mean that this will regulate it on layers?  This is something
                // that may need to be fixed in the future, but it seems rare and will bloat
                // the visible property list considerably.  Let's leave it like this for now
                // to establish a pattern then if the time comes to change this we'll be comfortable
                // with the overriding field of view pattern so a better decision can be made.
                if (hasLayerModifiedCamera)
                {
                    // use the render target here, because it may not have been unset yet.
                    mOldCameraLayerSettings.ApplyValuesToCamera(camera, SetCameraOptions.ApplyMatrix, layer.LayerCameraSettings, layer.RenderTarget);
                    camera.Position = oldPosition;
                    camera.UpVector = oldUpVector;
                }


                if (section != null)
                {
                    Section.EndContextAndTime();
                }
            }
        }


        static List<Sprite> mVisibleSprites = new List<Sprite>();
        static List<Text> mVisibleTexts = new List<Text>();

        private static void DrawMixed(SpriteList spriteListUnfiltered, SortType sortType,
            PositionedObjectList<Text> textListUnfiltered, List<IDrawableBatch> batches,
            bool relativeToCamera, Camera camera, Section section)
        {
            if (section != null)
            {
                Section.GetAndStartContextAndTime("Start of Draw Mixed");
            }
            DrawMixedStart(camera);

            

            int spriteIndex = 0;
            int textIndex = 0;
            int batchIndex = 0;

            // The sort values can represent different
            // things depending on the sortType argument.
            // They can either represent pure Z values or they
            // can represent distance from the camera (squared).
            // The problem is that a larger Z means closer to the
            // camera, but a larger distance means further from the
            // camera.  Therefore, to fix this problem if these values
            // represent distance from camera, they will be multiplied by
            // negative 1.
            float nextSpriteSortValue = float.PositiveInfinity;
            float nextTextSortValue = float.PositiveInfinity;
            float nextBatchSortValue = float.PositiveInfinity;




            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Sort Lists");
            }

            SortAllLists(spriteListUnfiltered, sortType, textListUnfiltered, batches, relativeToCamera, camera);

            mVisibleSprites.Clear();
            mVisibleTexts.Clear();
            for (int i = 0; i < spriteListUnfiltered.Count; i++)
            {
                Sprite sprite = spriteListUnfiltered[i];
                bool isVisible = sprite.AbsoluteVisible && 
                    (sprite.ColorOperation == ColorOperation.InterpolateColor || sprite.Alpha > .0001) &&
                    camera.IsSpriteInView(sprite, relativeToCamera);

                if (isVisible)
                {
                    mVisibleSprites.Add(sprite);
                }
            }

            for (int i = 0; i < textListUnfiltered.Count; i++)
            {
                Text text = textListUnfiltered[i];
                if (text.AbsoluteVisible && text.Alpha > .0001 && camera.IsTextInView(text, relativeToCamera))
                {
                    mVisibleTexts.Add(text);
                }
            }
            int indexOfNextSpriteToReposition = 0;


            GetNextZValuesByCategory(mVisibleSprites, sortType, mVisibleTexts, batches, camera, ref spriteIndex, ref textIndex, ref nextSpriteSortValue, ref nextTextSortValue, ref nextBatchSortValue);

            int numberToDraw = 0;
            // This is used as a temporary variable for Z or distance from camera
            float sortingValue = 0;
            Section performDrawingSection = null;
            if (section != null)
            {
                Section.EndContextAndTime();
                performDrawingSection = Section.GetAndStartContextAndTime("Perform Drawing");
            }

            while (spriteIndex < mVisibleSprites.Count || textIndex < mVisibleTexts.Count ||
                (batches != null && batchIndex < batches.Count))
            {
                #region only 1 array remains to be drawn so finish it off completely
                
                #region Draw Texts
                if (spriteIndex >= mVisibleSprites.Count && (batches == null || batchIndex >= batches.Count) &&
                    textIndex < mVisibleTexts.Count)
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw Texts");
                    }

                    if (sortType == SortType.DistanceAlongForwardVector)
                    {
                        int temporaryCount = mVisibleTexts.Count;
                        for (int i = textIndex; i < temporaryCount; i++)
                        {
                            mVisibleTexts[i].Position = mVisibleTexts[i].mOldPosition;
                        }
                    }
                    // TEXTS: draw all texts from textIndex to numberOfVisibleTexts - textIndex
                    DrawTexts(mVisibleTexts, textIndex, mVisibleTexts.Count - textIndex, camera, section);
                    break;
                }
                #endregion

                #region Draw Sprites
                else if (textIndex >= mVisibleTexts.Count && (batches == null || batchIndex >= batches.Count) &&
                    spriteIndex < mVisibleSprites.Count)
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw Sprites");
                    }

                    numberToDraw = mVisibleSprites.Count - spriteIndex;
                    if (sortType == SortType.DistanceAlongForwardVector)
                    {
                        int temporaryCount = mVisibleSprites.Count;
                        for (int i = indexOfNextSpriteToReposition; i < temporaryCount; i++)
                        {
                            mVisibleSprites[i].Position = mVisibleSprites[i].mOldPosition;
                            indexOfNextSpriteToReposition++;
                        }
                    }


                    PrepareSprites(
                        mSpriteVertices, mSpriteRenderBreaks,
                        mVisibleSprites, spriteIndex, numberToDraw
                        );

                    DrawSprites(
                        mSpriteVertices, mSpriteRenderBreaks,
                        mVisibleSprites, spriteIndex,
                        numberToDraw, camera);

                    break;
                }

                #endregion

                #region Draw DrawableBatches
                else if (spriteIndex >= mVisibleSprites.Count && textIndex >= mVisibleTexts.Count &&
                    batches != null && batchIndex < batches.Count)
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw IDrawableBatches");
                    }
                    // DRAWABLE BATCHES:  Only DrawableBatches remain so draw them all.
                    while (batchIndex < batches.Count)
                    {
                        IDrawableBatch batchAtIndex = batches[batchIndex];
                        if (batchAtIndex.UpdateEveryFrame)
                        {
                            batchAtIndex.Update();
                        }

                        if (Renderer.RecordRenderBreaks)
                        {
                            // Even though we aren't using a RenderBreak here, we should record a render break
                            // for this batch as it does cause rendering to be interrupted:
                            RenderBreak renderBreak = new RenderBreak();
#if DEBUG
                            renderBreak.ObjectCausingBreak = batchAtIndex;
#endif
                            renderBreak.LayerName = CurrentLayerName;
                            LastFrameRenderBreakList.Add(renderBreak);
                        }

                        batchAtIndex.Draw(camera);

                        batchIndex++;
                    }

                    FixRenderStatesAfterBatchDraw();
                    break;
                }
                #endregion

                #endregion

                #region more than 1 list remains so find which group of objects to render

                #region Sprites

                else if (nextSpriteSortValue <= nextTextSortValue && nextSpriteSortValue <= nextBatchSortValue && spriteIndex < mVisibleSprites.Count)
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw Sprites");
                    }
                    // The next furthest object is a Sprite.  Find how many to draw.

                    #region Count how many Sprites to draw and store it in numberToDraw
                    numberToDraw = 0;

                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY)
                        sortingValue = mVisibleSprites[spriteIndex + numberToDraw].Position.Z;
                    else
                        sortingValue = -(camera.Position - mVisibleSprites[spriteIndex + numberToDraw].Position).LengthSquared();

                    while (sortingValue <= nextTextSortValue &&
                           sortingValue <= nextBatchSortValue)
                    {
                        numberToDraw++;
                        if (spriteIndex + numberToDraw == mVisibleSprites.Count)
                        {
                            break;
                        }

                        if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY)
                            sortingValue = mVisibleSprites[spriteIndex + numberToDraw].Position.Z;
                        else
                            sortingValue = -(camera.Position - mVisibleSprites[spriteIndex + numberToDraw].Position).LengthSquared();

                    }
                    #endregion

                    if (sortType == SortType.DistanceAlongForwardVector)
                    {
                        for (int i = indexOfNextSpriteToReposition; i < numberToDraw + spriteIndex; i++)
                        {
                            mVisibleSprites[i].Position = mVisibleSprites[i].mOldPosition;
                            indexOfNextSpriteToReposition++;
                        }
                    }

                    PrepareSprites(
                        mSpriteVertices, mSpriteRenderBreaks,
                        mVisibleSprites, spriteIndex,
                        numberToDraw);

                    DrawSprites(
                        mSpriteVertices, mSpriteRenderBreaks,
                        mVisibleSprites, spriteIndex,
                        numberToDraw, camera);

                    // numberToDraw represents a range so increase spriteIndex by that amount.
                    spriteIndex += numberToDraw;

                    if (spriteIndex >= mVisibleSprites.Count)
                    {
                        nextSpriteSortValue = float.PositiveInfinity;
                    }
                    else
                    {
                        if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY)
                            nextSpriteSortValue = mVisibleSprites[spriteIndex].Position.Z;
                        else
                            nextSpriteSortValue = -(camera.Position - mVisibleSprites[spriteIndex].Position).LengthSquared();
                    }
                }

                #endregion

                #region Texts


                else if (nextTextSortValue <= nextSpriteSortValue && nextTextSortValue <= nextBatchSortValue)// draw texts
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw Texts");
                    }
                    numberToDraw = 0;

                    if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector)
                        sortingValue = mVisibleTexts[textIndex + numberToDraw].Position.Z;
                    else
                        sortingValue = -(camera.Position - mVisibleTexts[textIndex + numberToDraw].Position).LengthSquared();


                    while (sortingValue <= nextSpriteSortValue &&
                           sortingValue <= nextBatchSortValue)
                    {
                        numberToDraw++;
                        if (textIndex + numberToDraw == mVisibleTexts.Count)
                        {
                            break;
                        }

                        if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector)
                            sortingValue = mVisibleTexts[textIndex + numberToDraw].Position.Z;
                        else
                            sortingValue = -(camera.Position - mVisibleTexts[textIndex + numberToDraw].Position).LengthSquared();

                    }

                    if (sortType == SortType.DistanceAlongForwardVector)
                    {
                        for (int i = textIndex; i < textIndex + numberToDraw; i++)
                        {
                            mVisibleTexts[i].Position = mVisibleTexts[i].mOldPosition;
                        }
                    }

                    DrawTexts(mVisibleTexts, textIndex, numberToDraw, camera, section);

                    textIndex += numberToDraw;

                    if (textIndex == mVisibleTexts.Count)
                        nextTextSortValue = float.PositiveInfinity;
                    else
                    {
                        if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY)
                            nextTextSortValue = mVisibleTexts[textIndex].Position.Z;
                        else
                            nextTextSortValue = -(camera.Position - mVisibleTexts[textIndex].Position).LengthSquared();
                    }

                }

                #endregion

                #region Batches


                else if (nextBatchSortValue <= nextSpriteSortValue && nextBatchSortValue <= nextTextSortValue)
                {
                    if (section != null)
                    {
                        if (Section.Context != performDrawingSection)
                        {
                            Section.EndContextAndTime();
                        }
                        Section.GetAndStartMergedContextAndTime("Draw IDrawableBatches");
                    }
                    while (nextBatchSortValue <= nextSpriteSortValue && nextBatchSortValue <= nextTextSortValue && batchIndex < batches.Count)
                    {
                        IDrawableBatch batchAtIndex = batches[batchIndex];

                        if (batchAtIndex.UpdateEveryFrame)
                        {
                            batchAtIndex.Update();
                        }

                        if(Renderer.RecordRenderBreaks)
                        {
                            // Even though we aren't using a RenderBreak here, we should record a render break
                            // for this batch as it does cause rendering to be interrupted:
                            RenderBreak renderBreak = new RenderBreak();
#if DEBUG
                            renderBreak.ObjectCausingBreak = batchAtIndex;
#endif
                            renderBreak.LayerName = CurrentLayerName;
                            LastFrameRenderBreakList.Add(renderBreak);
                        }

                        batchAtIndex.Draw(camera);

                        batchIndex++;

                        if (batchIndex == batches.Count)
                        {
                            nextBatchSortValue = float.PositiveInfinity;
                        }
                        else
                        {
                            batchAtIndex = batches[batchIndex];

                            if (sortType == SortType.Z || sortType == SortType.ZSecondaryParentY)
                            {
                                nextBatchSortValue = batchAtIndex.Z;
                            }
                            else if (sortType == SortType.DistanceAlongForwardVector)
                            {
                                Vector3 vectorDifference = new Vector3(
                                batchAtIndex.X - camera.X,
                                batchAtIndex.Y - camera.Y,
                                batchAtIndex.Z - camera.Z);

                                float firstDistance;
                                Vector3 forwardVector = camera.RotationMatrix.Forward;

                                Vector3.Dot(ref vectorDifference, ref forwardVector, out firstDistance);

                                nextBatchSortValue = -firstDistance;
                            }
                            else
                            {
                                nextBatchSortValue = -(batchAtIndex.Z * batchAtIndex.Z);
                            }
                        }
                    }

                    FixRenderStatesAfterBatchDraw();
                }


                #endregion

                #endregion
            }

            if (section != null)
            {
                // Hop up a level
                if (Section.Context != performDrawingSection)
                {
                    Section.EndContextAndTime();
                }
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("End of Draw Mixed");
            }

            // return the position of any objects not drawn
            if (sortType == SortType.DistanceAlongForwardVector)
            {
                for (int i = indexOfNextSpriteToReposition; i < mVisibleSprites.Count; i++)
                {
                    mVisibleSprites[i].Position = mVisibleSprites[i].mOldPosition;
                }
            }

            Renderer.Texture = null;
            Renderer.TextureOnDevice = null;

            if (section != null)
            {
                Section.EndContextAndTime();
            }
        }

        private static void FixRenderStatesAfterBatchDraw()
        {


            // do nothing with alpha?
            FlatRedBallServices.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            // reset the texture filter:
            FlatRedBallServices.GraphicsOptions.TextureFilter = FlatRedBallServices.GraphicsOptions.TextureFilter;
            ForceSetBlendOperation();
            SetCurrentEffect(Effect, SpriteManager.Camera);
        }

        private static void GetNextZValuesByCategory(List<Sprite> spriteList, SortType sortType, List<Text> textList, List<IDrawableBatch> batches, Camera camera, ref int spriteIndex, ref int textIndex, ref float nextSpriteSortValue, ref float nextTextSortValue, ref float nextBatchSortValue)
        {
            #region find out the initial Z values of the 3 categories of objects to know which to render first


            if (sortType == SortType.Z || sortType == SortType.DistanceAlongForwardVector || sortType == SortType.ZSecondaryParentY || 
                // Custom comparers define how objects sort within the category, but outside we just have to rely on Z
                sortType == SortType.CustomComparer)
            {
                lock (spriteList)
                {
                    int spriteNumber = 0;
                    while (spriteNumber < spriteList.Count)
                    {
                        nextSpriteSortValue = spriteList[spriteNumber].Z;
                        spriteIndex = spriteNumber;
                        break;
                    }
                }

                if (textList != null && textList.Count != 0)
                    nextTextSortValue = textList[0].Position.Z;
                else if (textList != null)
                    textIndex = textList.Count;

                if (batches != null && batches.Count != 0)
                {
                    if (sortType == SortType.Z || sortType == SortType.ZSecondaryParentY)
                    {
                        // The Z value of the current batch is used.  Batches are always visible
                        // to this code.
                        nextBatchSortValue = batches[0].Z;
                    }
                    else
                    {
                        Vector3 vectorDifference = new Vector3(
                            batches[0].X - camera.X,
                            batches[0].Y - camera.Y,
                            batches[0].Z - camera.Z);

                        float firstDistance;
                        Vector3 forwardVector = camera.RotationMatrix.Forward;

                        Vector3.Dot(ref vectorDifference, ref forwardVector, out firstDistance);

                        nextBatchSortValue = -firstDistance;

                    }
                }
            }

            else if (sortType == SortType.Texture)
            {
                throw new Exception("Sorting based on texture is not supported on non z-buffered Sprites");
            }

            else if (sortType == SortType.DistanceFromCamera)
            {
                // code duplication to prevent tight-loop if statements
                lock (spriteList)
                {
                    int spriteNumber = 0;
                    while (spriteNumber < spriteList.Count)
                    {
                        nextSpriteSortValue =
                            -(camera.Position - spriteList[spriteNumber].Position).LengthSquared();
                        spriteIndex = spriteNumber;
                        break;
                    }
                }

                if (textList != null && textList.Count != 0)
                {
                    nextTextSortValue = -(camera.Position - textList[0].Position).LengthSquared();
                }
                else if (textList != null)
                {
                    textIndex = textList.Count;
                }

                // The Z value of the current batch is used.  Batches are always visible
                // to this code.
                if (batches != null && batches.Count != 0)
                {
                    // workign with squared length, so use that here
                    nextBatchSortValue = -(batches[0].Z * batches[0].Z);
                }
            }

            #endregion
        }

        private static void SortAllLists(SpriteList spriteList, SortType sortType, PositionedObjectList<Text> textList, List<IDrawableBatch> batches, bool relativeToCamera, Camera camera)
        {
            StoreOldPositionsForDistanceAlongForwardVectorSort(spriteList, sortType, textList, batches, camera);


            #region Sort the SpriteList and get the number of visible Sprites in numberOfVisibleSprites
            if (spriteList != null && spriteList.Count != 0)
            {
                lock (spriteList)
                {
                    switch (sortType)
                    {
                        case SortType.Z:
                        case SortType.DistanceAlongForwardVector:
                            // Sorting ascending means everything will be drawn back to front.  This
                            // is slower but necessary for translucent objects.
                            // Sorting descending means everything will be drawn back to front.  This
                            // is faster but will cause problems for translucency.
                            spriteList.SortZInsertionAscending();
                            break;
                        case SortType.DistanceFromCamera:
                            spriteList.SortCameraDistanceInsersionDescending(camera);
                            break;
                        case SortType.ZSecondaryParentY:
                            spriteList.SortZInsertionAscending();

                            spriteList.SortParentYInsertionDescendingOnZBreaks();

                            break;
                        case SortType.CustomComparer:

                            if (mSpriteComparer != null)
                            {
                                spriteList.Sort(mSpriteComparer);
                            }
                            else
                            {
                                spriteList.SortZInsertionAscending();
                            }

                            break;
                        case SortType.None:
                            // This will improve render times slightly...maybe?
                            spriteList.SortTextureInsertion();
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion

            #region Sort the TextList
            if (textList != null && textList.Count != 0)
            {
                switch (sortType)
                {
                    case SortType.Z:
                    case SortType.DistanceAlongForwardVector:
                        textList.SortZInsertionAscending();
                        break;
                    case SortType.DistanceFromCamera:
                        textList.SortCameraDistanceInsersionDescending(camera);
                        break;
                    case SortType.CustomComparer:
                        if (mTextComparer != null)
                        {
                            textList.Sort(mTextComparer);
                        }
                        else
                        {
                            textList.SortZInsertionAscending();
                        }


                        break;
                    default:
                        break;
                }
            }
            #endregion

            #region Sort the Batches
            if (batches != null && batches.Count != 0)
            {
                switch (sortType)
                {
                    case SortType.Z:
                        // Z serves as the radius if using SortType.DistanceFromCamera.
                        // If Z represents actual Z or radius, the larger the value the further
                        // away from the camera the object will be.
                        SortBatchesZInsertionAscending(batches);
                        break;

                    case SortType.DistanceAlongForwardVector:
                        batches.Sort(new FlatRedBall.Graphics.BatchForwardVectorSorter(camera));
                        break;
                    case SortType.ZSecondaryParentY:
                        SortBatchesZInsertionAscending(batches);

                        // Even though the sort type is by parent, IDB doesn't have a Parent object, so we'll just rely on Y.
                        // May need to revisit this if it causes problems
                        SortBatchesYInsertionDescendingOnZBreaks(batches);

                        break;
                    case SortType.CustomComparer:

                        if (mDrawableBatchComparer != null)
                        {
                            batches.Sort(mDrawableBatchComparer);
                        }
                        else
                        {
                            SortBatchesZInsertionAscending(batches);
                        }

                        break;

                }
            }
            #endregion
        }

        static List<int> batchZBreaks = new List<int>(10);

        private static void SortBatchesYInsertionDescendingOnZBreaks(List<IDrawableBatch> batches)
        {
            GetBatchZBreaks(batches, batchZBreaks);

            batchZBreaks.Insert(0, 0);
            batchZBreaks.Add(batches.Count);


            for (int i = 0; i < batchZBreaks.Count - 1; i++)
            {
                SortBatchInsertionDescending(batches, batchZBreaks[i], batchZBreaks[i + 1]);
            }
        }

        private static void SortBatchInsertionDescending(List<IDrawableBatch> batches, int firstObject, int lastObjectExclusive)
        {
            int whereObjectBelongs;

            float yAtI;
            float yAtIMinusOne;

            for (int i = firstObject + 1; i < lastObjectExclusive; i++)
            {
                yAtI = batches[i].Y;
                yAtIMinusOne = batches[i - 1].Y;

                if (yAtI > yAtIMinusOne)
                {
                    if (i == firstObject + 1)
                    {
                        batches.Insert(firstObject, batches[i]);
                        batches.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereObjectBelongs = i - 2; whereObjectBelongs > firstObject - 1; whereObjectBelongs--)
                    {
                        if (yAtI <= (batches[whereObjectBelongs]).Y)
                        {
                            batches.Insert(whereObjectBelongs + 1, batches[i]);
                            batches.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereObjectBelongs == firstObject && yAtI > (batches[firstObject]).Y)
                        {
                            batches.Insert(firstObject, batches[i]);
                            batches.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }

        private static void GetBatchZBreaks(List<IDrawableBatch> batches, List<int> zBreaks)
        {
            zBreaks.Clear();

            if (batches.Count == 0 || batches.Count == 1)
                return;

            for (int i = 1; i < batches.Count; i++)
            {
                if (batches[i].Z != batches[i - 1].Z)
                    zBreaks.Add(i);
            }
        }

        static LayerCameraSettings mOldCameraLayerSettings = new LayerCameraSettings();
        private static void DrawLayers(Camera camera, RenderMode renderMode, Section section)
        {

            //TimeManager.SumTimeSection("Set device settings");

            RenderTarget2D lastRenderTarget = null;

            #region Draw World Layers
            // Draw layers that belong to the World "SpriteEditor"
            if (camera.DrawsWorld)
            {
                // These layers are still considered in the "world" because all
                // Cameras can see them.
                for (int i = 0; i < SpriteManager.LayersWriteable.Count; i++)
                {
                    Layer layer = SpriteManager.LayersWriteable[i];

                    DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget);

                }
            }
            #endregion

            //TimeManager.SumTimeSection("Draw World Layers");

            #region Draw Camera Layers
            if (camera.DrawsCameraLayer)
            {
                int layerCount = camera.Layers.Count;
                for (int i = 0; i < layerCount; i++)
                {
                    Layer layer = camera.Layers[i];
                    DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget);
                }
            }
            #endregion

            //TimeManager.SumTimeSection("Draw Camera Layers");

            #region Last, draw the top layer

            if (camera.DrawsWorld && !SpriteManager.TopLayer.IsEmpty)
            {
                Layer layer = SpriteManager.TopLayer;

                DrawIndividualLayer(camera, renderMode, layer, section, ref lastRenderTarget);

            }
            #endregion

            if(lastRenderTarget != null)
            {
                mGraphics.GraphicsDevice.SetRenderTarget(null);
            }

            //TimeManager.SumTimeSection("Last, draw the top layer");
        }

        private static void DrawUnlayeredObjects(Camera camera, RenderMode renderMode, Section section)
        {
            CurrentLayer = null;
            if (section != null)
            {
                Section.GetAndStartContextAndTime("Draw above shapes");
            }

            #region Draw Shapes if UnderEverything

            if (camera.DrawsWorld && renderMode == RenderMode.Default && camera.DrawsShapes &&
                ShapeManager.ShapeDrawingOrder == FlatRedBall.Math.Geometry.ShapeDrawingOrder.UnderEverything)
            {
                // Draw shapes
                DrawShapes(
                    camera,
                    ShapeManager.mSpheres,
                    ShapeManager.mCubes,
                    ShapeManager.mRectangles,
                    ShapeManager.mCircles,
                    ShapeManager.mPolygons,
                    ShapeManager.mLines,
                    ShapeManager.mCapsule2Ds,
                    null
                    );
            }

            #endregion
            
            //TimeManager.SumTimeSection("Draw models");

            #region Draw ZBuffered Sprites and Mixed

            // Only draw the rest if in default rendering mode
            if (renderMode == RenderMode.Default)
            {
                if (camera.DrawsWorld)
                {
                    if (section != null)
                    {
                        Section.EndContextAndTime();
                        Section.GetAndStartContextAndTime("Draw Z Buffered Sprites");
                    }
                    if (SpriteManager.ZBufferedSpritesWriteable.Count != 0)
                    {
#if !USE_CUSTOM_SHADER
                        // Note, this means that we can't use the "Color" color operation.
                        // For PC we do clip() in the shader.  We can't do that on WP7 so we use an alpha test effect
                        SetCurrentEffect(mAlphaTestEffect, camera);
#endif
                        // Draw the Z Buffered Sprites
                        DrawZBufferedSprites(camera, SpriteManager.ZBufferedSpritesWriteable);

#if !USE_CUSTOM_SHADER
                        SetCurrentEffect(mEffect, camera);
#endif
                    }

                    foreach (var drawableBatch in SpriteManager.mZBufferedDrawableBatches)
                    {
                        if (drawableBatch.UpdateEveryFrame)
                        {
                            drawableBatch.Update();
                        }
                        drawableBatch.Draw(camera);
                    }

                    if (section != null)
                    {
                        Section.EndContextAndTime();
                        Section.GetAndStartContextAndTime("Draw Ordered objects");
                    }
                    // Draw the OrderedByDistanceFromCamera Objects (Sprites, Texts, DrawableBatches)
                    DrawOrderedByDistanceFromCamera(camera, section);
                }
            }


            #endregion

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Draw below shapes");
            }

            #region Draw Shapes if OverEverything

            if (camera.DrawsWorld &&
                renderMode == RenderMode.Default &&
                camera.DrawsShapes &&
                ShapeManager.ShapeDrawingOrder == ShapeDrawingOrder.OverEverything)
            {

                // Draw shapes
                DrawShapes(
                    camera,
                    ShapeManager.mSpheres,
                    ShapeManager.mCubes,
                    ShapeManager.mRectangles,
                    ShapeManager.mCircles,
                    ShapeManager.mPolygons,
                    ShapeManager.mLines,
                    ShapeManager.mCapsule2Ds,
                    null
                    );
            }

            #endregion
            //TimeManager.SumTimeSection("Draw ZBuffered and Mixed");
            if (section != null)
            {
                Section.EndContextAndTime();
            }
        }


        public static void DrawCamera(Camera camera, Section section)
        {
            DrawCamera(camera, RenderMode.Default, section);
        }

        static void DrawCamera(Camera camera, RenderMode renderMode, Section section)
        {
            if (section != null)
            {
                Section.GetAndStartContextAndTime("Start of camera draw");
            }

            PrepareForDrawScene(camera, renderMode);

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Draw UnderAllLayer");
            }

            if (camera.DrawsWorld && !SpriteManager.UnderAllDrawnLayer.IsEmpty)
            {
                Layer layer = SpriteManager.UnderAllDrawnLayer;

                RenderTarget2D lastRenderTarget = null;
                DrawIndividualLayer(camera, RenderMode.Default, layer, section, ref lastRenderTarget);

                if(lastRenderTarget != null)
                {
                    GraphicsDevice.SetRenderTarget(null);
                }
            }

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Draw Unlayered");
            }

            DrawUnlayeredObjects(camera, renderMode, section);


            // Draw layers - this method will check internally for the camera's DrawsWorld and DrawsCameraLayers properties
            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Draw Regular Layers");
            }
            DrawLayers(camera, renderMode, section);
            if (section != null)
            {
                Section.EndContextAndTime();
            }
        }


        private static void StoreOldPositionsForDistanceAlongForwardVectorSort(PositionedObjectList<Sprite> spriteList, SortType sortType, PositionedObjectList<Text> textList, List<IDrawableBatch> batches, Camera camera)
        {
            #region If DistanceAlongForwardVector store old values

            // If the objects are using SortType.DistanceAlongForwardVector
            // then store the old positions, then rotate the objects by the matrix that
            // moves the forward vector to the Z = -1 vector (the camera's inverse rotation
            // matrix)
            if (sortType == SortType.DistanceAlongForwardVector)
            {
                Matrix inverseRotationMatrix = camera.RotationMatrix;
                Matrix.Invert(ref inverseRotationMatrix, out inverseRotationMatrix);

                int temporaryCount = spriteList.Count;

                for (int i = 0; i < temporaryCount; i++)
                {
                    spriteList[i].mOldPosition = spriteList[i].Position;

                    spriteList[i].Position -= camera.Position;
                    Vector3.Transform(ref spriteList[i].Position,
                        ref inverseRotationMatrix, out spriteList[i].Position);
                }

                temporaryCount = textList.Count;

                for (int i = 0; i < temporaryCount; i++)
                {
                    textList[i].mOldPosition = textList[i].Position;

                    textList[i].Position -= camera.Position;
                    Vector3.Transform(ref textList[i].Position,
                        ref inverseRotationMatrix, out textList[i].Position);
                }

                temporaryCount = batches.Count;

                for (int i = 0; i < temporaryCount; i++)
                {


                }

            }
            #endregion
        }

        private static void DrawOrderedByDistanceFromCamera(Camera camera, Section section)
        {
            if (SpriteManager.OrderedByDistanceFromCameraSprites.Count != 0 ||
                SpriteManager.WritableDrawableBatchesList.Count != 0 ||
                TextManager.mDrawnTexts.Count != 0)
            {
                Renderer.CurrentLayer = null;
                // Draw
                DrawMixed(
                    SpriteManager.OrderedByDistanceFromCameraSprites,
                    SpriteManager.OrderedSortType, TextManager.mDrawnTexts,
                    SpriteManager.WritableDrawableBatchesList,
                    false, camera, section);
            }
        }
    }
}
