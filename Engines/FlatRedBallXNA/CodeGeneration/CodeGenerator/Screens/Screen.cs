#region Using

using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Model;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics;

using PolygonSaveList = FlatRedBall.Content.Polygon.PolygonSaveList;

#endregion

namespace CodeGenerator.Screens
{
    public class Screen
    {
        #region Members

        protected Camera mCamera;
        protected Layer mLayer;

        protected List<Screen> mPopups = new List<Screen>();

        private string mContentManagerName;


        // The following are objects which belong to the screen.
        // These are removed by the Screen when it is Destroyed
        protected SpriteList mSprites = new SpriteList();
        protected List<SpriteGrid> mSpriteGrids = new List<SpriteGrid>();
        protected PositionedObjectList<SpriteFrame> mSpriteFrames = new PositionedObjectList<SpriteFrame>();

        protected PositionedObjectList<Text> mTexts = new PositionedObjectList<Text>();
        protected PositionedObjectList<Polygon> mPolygons = new PositionedObjectList<Polygon>();
        protected PositionedObjectList<AxisAlignedRectangle> mAxisAlignedRectangles = new PositionedObjectList<AxisAlignedRectangle>();
        protected PositionedObjectList<Circle> mCircles = new PositionedObjectList<Circle>();
        protected PositionedObjectList<Line> mLines = new PositionedObjectList<Line>();

        protected PositionedModelList mPositionedModels = new PositionedModelList();
        protected List<Layer> mLayers = new List<Layer>();
        protected List<IDrawableBatch> mDrawableBatches = new List<IDrawableBatch>();
        // End of objects which belong to the Screen.

        // These variables control the flow from one Screen to the next.


        protected Scene mLastLoadedScene;
        private bool mIsActivityFinished;
        private string mNextScreen;

        private bool mManageSpriteGrids;

        // Loading a Screen can take a considerable amount
        // of time - certainly more than 1/60 of a frame.  Since
        // the XNA Game class runs at a fixed frame rate by default
        // the long loading time can cause a buildup of frames.  Once
        // the Screen class finishes loading the Game class will spin quickly
        // calling Update over and over.

        // To prevent this, Screens can tell the ScreenManager
        // to set the Game's IsFixedFrameRate to false when the
        // Screen loads and back to true after.  This occurs only
        // if this field is set to true;
        protected bool mTurnOffFixedFrameRateDuringLoading = false;

        #endregion

        #region Properties

        public string ContentManagerName
        {
            get { return mContentManagerName; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the activity is finished for a particular screen.
        /// </summary>
        /// <remarks>
        /// If activity is finished, then the ScreenManager or parent
        /// screen (if the screen is a popup) knows to destroy the screen
        /// and loads the NextScreen class.</remarks>
        #endregion
        public bool IsActivityFinished
        {
            get { return mIsActivityFinished; }
            set { mIsActivityFinished = value; }

        }


        public Layer Layer
        {
            get { return mLayer; }
        }


        public bool ManageSpriteGrids
        {
            get { return mManageSpriteGrids; }
            set { mManageSpriteGrids = value; }
        }

        #region XML Docs
        /// <summary>
        /// The fully qualified path of the Screen-inheriting class that this screen is 
        /// to link to.
        /// </summary>
        /// <remarks>
        /// This property is read by the ScreenManager when IsActivityFinished is
        /// set to true.  Therefore, this must always be set to some value before
        /// or in the same frame as when IsActivityFinished is set to true.
        /// </remarks>
        #endregion
        public string NextScreen
        {
            get { return mNextScreen; }
            set { mNextScreen = value; }
        }


        public bool TurnOffFixedFrameRateDuringLoading
        {
            get { return mTurnOffFixedFrameRateDuringLoading; }
            set { mTurnOffFixedFrameRateDuringLoading = value; }
        }

        protected bool UnloadsContentManagerWhenDestroyed
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Loads a screen, initializes internal fields, and stores the textures which 
        /// belong to the screen.
        /// </summary>
        /// <!--Derived class should not change the argument list.  The ScreenManager
        /// depends on the arguments for the constructor being the same for all derived
        /// classes.-->
        /// <remarks>
        /// When a screen is unloaded, it unloads all textures that it introduced 
        /// to the TextureManager.  It does not unload textures which were already 
        /// cached in the TextureManager before the screen was loaded.
        /// This method gets all textures that are cached after the 
        /// LoadScn method, then removes all
        /// textures that existed before the scene was loaded.
        /// 
        /// Therefore, any texture that is used in the screen should 
        /// exist in the .scn.  This keeps the programmer
        /// from having to manually track textures and remove them when a 
        /// screen unloads.  The exception is objects which dynamically select 
        /// their texture according to conditions and user inputs.  These
        /// textures should be assigned to the screen with the AssignTextureToScreen
        /// method.
        /// </remarks>
        /// <param name="scnFileName">The .scn file name relative to the .exe.</param>
        /// <param name="istFileName">The .ist file name relative to the .exe.</param>
        /// <param name="sprMan">Reference to the SpriteManager used for scene loading and later texture removal.</param>
        /// <param name="random">Reference to the Random instance.</param>
        /// <param name="Music">Reference to the Music instance used to play songs.</param>
        #endregion
        public Screen(string scnFileName, string contentManagerName)
        {
            UnloadsContentManagerWhenDestroyed = true;
            mContentManagerName = contentManagerName;
            mManageSpriteGrids = true;
            IsActivityFinished = false;

            mLayer = ScreenManager.NextScreenLayer;

            #region load the .scnx
            // If no .scnx file is specified, don't call LoadScene.
            if (!string.IsNullOrEmpty(scnFileName))
            {
                LoadScene(scnFileName, mLayer);
            }
            #endregion
        }

        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Manages contained SpriteFrames or SpriteGrids.
        /// </summary>
        #endregion
        public virtual void Activity(bool firstTimeCalled)
        {
            if (mManageSpriteGrids)
            {
                foreach (SpriteGrid sg in mSpriteGrids)
                    sg.Manage();
            }

            for (int i = mPopups.Count - 1; i > -1; i--)
            {
                mPopups[i].Activity(false);


                if (mPopups[i].IsActivityFinished)
                {
                    string nextPopup = mPopups[i].NextScreen;

                    mPopups[i].Destroy();
                    mPopups.RemoveAt(i);

                    if (nextPopup != string.Empty && nextPopup != null)
                    {
                        LoadPopup(nextPopup, false);
                    }
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Method called by the ScreenManager which unloads all textures, 
        /// Remove Sprites, destroys the GUI, Text objects, SpriteGrids, 
        /// and SpriteFrames.
        /// </summary>
        #endregion
        public virtual void Destroy()
        {
            if (mLastLoadedScene != null)
            {
                mLastLoadedScene.Clear();
            }

            // All of the popups should be destroyed as well
            foreach (Screen s in mPopups)
                s.Destroy();

            SpriteManager.RemoveSpriteList(mSprites);

            // It's common for users to forget to add Particle Sprites
            // to the mSprites SpriteList.  This will either create leftover
            // particles when the next screen loads or will throw an assert when
            // the ScreenManager checks if there are any leftover Sprites.  To make
            // things easier we'll just clear the Particle Sprites here.  If you don't
            // want this done (not likely), remove the following line, but only do so if
            // you really know what you're doing!
            SpriteManager.RemoveAllParticleSprites();

            // Destory all SpriteGrids that belong to this Screen
            foreach (SpriteGrid sg in mSpriteGrids)
                sg.Destroy();


            // Destroy all SpriteFrames that belong to this Screen
            while (mSpriteFrames.Count != 0)
                SpriteManager.RemoveSpriteFrame(mSpriteFrames[0]);

            TextManager.RemoveText(mTexts);

            while (mPolygons.Count != 0)
                ShapeManager.Remove(mPolygons[0]);

            while (mLines.Count != 0)
                ShapeManager.Remove(mLines[0]);

            while (mAxisAlignedRectangles.Count != 0)
                ShapeManager.Remove(mAxisAlignedRectangles[0]);

            while (mCircles.Count != 0)
                ShapeManager.Remove(mCircles[0]);

            while (mPositionedModels.Count != 0)
                ModelManager.RemoveModel(mPositionedModels[0]);

            if (UnloadsContentManagerWhenDestroyed)
            {
                FlatRedBallServices.Unload(mContentManagerName);
            }

            if (mLayer != null)
            {
                SpriteManager.RemoveLayer(mLayer);
            }

            for (int i = 0; i < mLayers.Count; i++)
            {
                SpriteManager.RemoveLayer(mLayers[i]);
            }
        }


        public Scene LoadScene(string fileName, Layer layer)
        {
            Scene scene = null;
            if (
#if XBOX360
                FlatRedBallServices.IgnoreExtensionsWhenLoadingContent == false &&
#endif
FlatRedBall.IO.FileManager.GetExtension(fileName) == "scnx")
            {
#if XBOX360
                
                throw new NotImplementedException("Cannot currently load .scnx files on the 360.  Use content manager");
#else
                scene = FlatRedBall.Content.SpriteEditorScene.FromFile(fileName).ToScene(mContentManagerName);
#endif
            }
            else
            {
                // There is either no extension on the fileName or the extensions are being ignored
                // by the engine.  
                scene = FlatRedBallServices.Load<Scene>(fileName, mContentManagerName);
            }

            if (scene != null)
            {
                scene.AddToManagers(layer);

                mSprites.AddRange(scene.Sprites);
                mSpriteGrids.AddRange(scene.SpriteGrids);
                mSpriteFrames.AddRange(scene.SpriteFrames);
                mPositionedModels.AddRange(scene.PositionedModels);

                SpriteManager.SortTexturesSecondary();
            }

            mLastLoadedScene = scene;

            return scene;
        }

        public PositionedObjectList<Polygon> LoadPolygonList(string name, bool addToShapeManager, bool makeVisible)
        {
            PolygonSaveList psl = PolygonSaveList.FromFile(name);

            PositionedObjectList<Polygon> loadedPolygons = psl.ToPolygonList();

            if (addToShapeManager)
            {
                foreach (Polygon polygon in loadedPolygons)
                {
                    ShapeManager.AddPolygon(polygon);
                }
            }

            foreach (Polygon polygon in loadedPolygons)
            {
                polygon.Visible = makeVisible;
            }

            mPolygons.AddRange(loadedPolygons);

            return loadedPolygons;

        }


        public Screen LoadPopup(string popupToLoad, Layer layerToLoadPopupOn)
        {
            Screen loadedScreen = ScreenManager.LoadScreen(popupToLoad, layerToLoadPopupOn);
            mPopups.Add(loadedScreen);
            return loadedScreen;
        }

        public Screen LoadPopup(string popupToLoad, bool useNewLayer)
        {
            Screen loadedScreen = ScreenManager.LoadScreen(popupToLoad, useNewLayer);
            mPopups.Add(loadedScreen);
            return loadedScreen;
        }


        #region XML Docs
        /// <summary>Tells the screen that we are done and wish to move to the
        /// supplied screen</summary>
        /// <param>Fully Qualified Type of the screen to move to</param>
        #endregion
        public void MoveToScreen(string screenClass)
        {
            IsActivityFinished = true;
            NextScreen = screenClass;
        }


        public void Shift(float x, float y, float z)
        {
            mSprites.Shift(x, y, z);

            foreach (SpriteGrid sg in mSpriteGrids)
                sg.Shift(x, y, z);

            mSpriteFrames.Shift(x, y, z);

            mTexts.Shift(x, y, z);

            mPolygons.Shift(x, y, z);

            mAxisAlignedRectangles.Shift(x, y, z);

            mCircles.Shift(x, y, z);
        }

        #endregion

        #region Protected Methods



        #endregion

        #endregion

    }
}
