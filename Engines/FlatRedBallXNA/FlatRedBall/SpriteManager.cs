#if ANDROID
#define THREAD_POOL_IS_SLOW
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Reflection;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.ManagedSpriteGroups;

using SpriteGrid = FlatRedBall.ManagedSpriteGroups.SpriteGrid;
using System.Diagnostics;
using FlatRedBall.Instructions;
using FlatRedBall.Performance.Measurement;

#if UWP
using Windows.System.Threading;
using Windows.Foundation;
#endif


namespace FlatRedBall
{
    #region Updater Class

    /// <summary>
    /// Contains logic for updating objects.  This is used to separate 
    /// updates into different threads
    /// </summary>
    class Updater
    {
        public SpriteList AutomaticallyUpdatedSprites;

        public int SpriteIndex;
        public int SpriteCount;



#if UWP
        IAsyncAction mAction = null;
#else
        ManualResetEvent mManualResetEvent;

#endif

        public Updater()
        {
#if !UWP
            mManualResetEvent = new ManualResetEvent(false);
#endif
        }

        public void Reset()
        {
            
#if UWP
            mAction = null;
#else
            mManualResetEvent.Reset();

#endif
        }

        public void Wait()
        {
#if UWP
            mAction.AsTask().Wait();
#else
            mManualResetEvent.WaitOne();
#endif
        }

        internal void TimedActivity()
        {
            Reset();
#if UWP
            mAction = Windows.System.Threading.ThreadPool.RunAsync(TimedActivityInternal);
#else
            ThreadPool.QueueUserWorkItem(TimedActivityInternal);
#endif
        }
        void TimedActivityInternal(object unusedState)
        {

            float secondDifference = TimeManager.SecondDifference;
            float secondDifferenceSquaredDividedByTwo = TimeManager.SecondDifferenceSquaredDividedByTwo;
            float lastSecondDifference = TimeManager.LastSecondDifference;
            double currentTime = TimeManager.CurrentTime;

            int lastIndex = SpriteIndex + SpriteCount;
            for (int i = SpriteIndex; i < lastIndex; i++)
            {
                Sprite s = AutomaticallyUpdatedSprites[i];
                s.TimedActivity(
                    secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }
#if !UWP
            mManualResetEvent.Set();
#endif
        }

        internal void AnimateSelf()
        {
            Reset();
#if UWP
            mAction = Windows.System.Threading.ThreadPool.RunAsync(AnimateSelfInternal);
#else
            ThreadPool.QueueUserWorkItem(AnimateSelfInternal);
#endif
        }
        void AnimateSelfInternal(object unusedState)
        {
            float secondDifference = TimeManager.SecondDifference;
            float secondDifferenceSquaredDividedByTwo = TimeManager.SecondDifferenceSquaredDividedByTwo;
            float lastSecondDifference = TimeManager.LastSecondDifference;
            double currentTime = TimeManager.CurrentTime;

            int lastIndex = SpriteIndex + SpriteCount;

            for (int i = SpriteIndex; i < lastIndex; i++)
            {
                Sprite s = AutomaticallyUpdatedSprites[i];
                s.AnimateSelf(currentTime);
            }
#if !UWP
            mManualResetEvent.Set();
#endif
        }

        internal void ExecuteInstructions()
        {
            Reset();
#if UWP
            mAction = Windows.System.Threading.ThreadPool.RunAsync(ExecuteInstructionsInternal);
#else
            ThreadPool.QueueUserWorkItem(ExecuteInstructionsInternal);
#endif
        }
        void ExecuteInstructionsInternal(object unusedState)
        {
            double currentTime = TimeManager.CurrentTime;

            int lastIndex = SpriteIndex + SpriteCount;
            
            for (int i = lastIndex - 1; i > SpriteIndex - 1; i--)
            {// loop through the sprites
                if (i < AutomaticallyUpdatedSprites.Count)
                {
                    AutomaticallyUpdatedSprites[i].ExecuteInstructions(currentTime);
                }
            }
#if !UWP
            mManualResetEvent.Set();
#endif
        }

        internal void UpdateDependencies()
        {
            Reset();
#if UWP
            mAction = Windows.System.Threading.ThreadPool.RunAsync(UpdateDependenciesInternal);
#else
            ThreadPool.QueueUserWorkItem(UpdateDependenciesInternal);
#endif
        }
        void UpdateDependenciesInternal(object unusedState)
        {
            double currentTime = TimeManager.CurrentTime;
            int lastIndex = SpriteIndex + SpriteCount;

            for (int i = SpriteIndex; i < lastIndex; i++)
            {
                Sprite s = AutomaticallyUpdatedSprites[i];
                s.UpdateDependencies(currentTime);

#if SILVERLIGHT
                s.UpdateVertices(SpriteManager.Camera);

#endif

            }
#if !UWP
            mManualResetEvent.Set();
#endif
        }

    }
    #endregion

    /// <summary>
    /// Static manager class which handles the management of Sprites, SpriteFrames, Cameras, and
    /// other PositionedObjects.
    /// </summary>
    /// <remarks>
    /// The SpriteManager is commonly used in FlatRedBall examples to create Sprites.  Perhaps the most
    /// common line of code is:
    /// 
    ///     Sprite sprite = SpriteManager.AddSprite("redball.bmp");
    /// 
    /// Originally the SpriteManager was the only manager class.  For this reason it handled
    /// some of the functionality which may have normally belonged to classes unrelated to Sprites.
    /// For example, Cameras are added and managed through the SpriteManager although they are a general
    /// rendering class which might belong in the Renderer or FlatRedBallServices class.
    /// </remarks>
    public static class SpriteManager
    {
        #region Fields

        static List<Updater> mUpdaters = new List<Updater>();

        static PositionedObjectList<Camera> mCameras;
        static int mCurrentCameraIndex = 0;

        static SpriteList mOrderedByDistanceFromCameraSprites;
        static ReadOnlyCollection<Sprite> mOrderedSpritesReadOnly;

        // This needs to be internal so that the SpriteSave class can test whether
        // the Sprite that it is being created from is ordered or zbuffered.  Access
        // to the actual SpriteList instead of the ReadOnlyCollection allows for a faster
        // contains test:  if(sprite.ListBelongingTo.Contains(SpriteManager.mZBufferedSprites)
        static internal SpriteList mZBufferedSprites;
        static ReadOnlyCollection<Sprite> mZBufferedSpritesReadOnly;

        // for speed, made internal
        internal static SpriteList mAutomaticallyUpdatedSprites;
        static ReadOnlyCollection<Sprite> mAutomaticallyUpdatedSpritesReadOnly;

        static SpriteList mManuallyUpdatedSprites;
        static ReadOnlyCollection<Sprite> mManuallyUpdatedSpritesReadOnly;

        static SortType mOrderedSortType = SortType.Z;
        static SortType mZBufferedSortType = SortType.Texture;

        static PositionedObjectList<SpriteFrame> mSpriteFrames;
        static ReadOnlyCollection<SpriteFrame> mSpriteFramesReadOnly;

        static List<IDrawableBatch> mDrawableBatches;
        static internal List<IDrawableBatch> mZBufferedDrawableBatches = new List<IDrawableBatch>();
        static ReadOnlyCollection<IDrawableBatch> mDrawableBatchesReadOnlyCollection;

        // Most FlatRedBall objects have their own managers.  Users may define
        // their own PositionedObjects which they will want managed for them.  Rather
        // than documenting all of the steps required to manage a PositionedObject, the
        // SpriteManager can simply manage these.  This makes things more convenient and
        // standardized.
        internal static PositionedObjectList<PositionedObject> mManagedPositionedObjects;
        static ReadOnlyCollection<PositionedObject> mManagedPositionedObjectsReadOnly;


        #region Particle Sprite Fields

        static EmitterList mEmitters;
        //static ListBuffer<Emitter> mEmitterBuffer;

        // Do not Edit this value directly, Use MaxParticleCount
        static int mMaxParticleCount;

        static int mAutoIncrementParticleCountValue = 0;

        #region XML Docs
        /// <summary>
        /// SpriteList filled with valid Sprites at the start of execution.
        /// </summary>
        /// <remarks>
        /// Particle Sprites are defined as Sprites which have a short lifespan and
        /// are created frequently.  Common uses are smoke, bullets, and explosions.
        /// </remarks>
        #endregion
        static SpriteList mParticleSprites; // no need to buffer this (yet)
        static int mParticleCount;
        static Sprite mFirstEmpty;

        // Give the Emitter access to this array.
        internal static SpriteList mRemoveWhenAlphaIs0;

        static MethodInfo mRemoveSpriteMethodInfo = typeof(SpriteManager).GetMethod("RemoveSprite", new Type[] { typeof(Sprite) });

        /// <summary>
        /// List of particles which should be removed on a timer. This is internal, so it can only be used by emitters.
        /// </summary>
        /// <remarks>
        /// As of March 19, 2023 Emitters
        /// are not used very often. Therefore,
        /// this list is almost always empty. It
        /// will remain here for old games but it's
        /// unlikely new games will use this unless the
        /// FRB Emitter object gets revived.
        /// </remarks>
        internal static List<TimedRemovalRecord> mTimedRemovalList = new List<TimedRemovalRecord>(100);

        #endregion

        static List<Layer> mLayers;
        static ReadOnlyCollection<Layer> mLayersReadOnly;

        static Layer mTopLayer;

        static Layer mUnderAllDrawnLayer;



        #endregion

        #region Properties

        #region Public Properties

        #region XML Docs
        /// <summary>
        /// A read-only collection of Sprites which are automatically managed
        /// every frame.
        /// </summary>
        /// <remarks>
        /// Sprites in this list have all of their behavioral properties
        /// applied automatically.  Examples of these properties include
        /// Velocity, Acceleration, rotational velocity, and color rates.
        /// This manage requires overhead which can become a significant portion
        /// of frame time, so moving Sprites to be manually updated can improve performance.
        /// 
        /// Most methods which create Sprites will place the created Sprite in this list.
        /// 
        /// <seealso cref="SpriteManager.ConvertToAutomaticallyUpdated"/>
        /// <seealso cref="SpriteManager.ConvertToManuallyUpdated(FlatRedBall.Sprite)"/>
        /// </remarks>
        #endregion
        static public ReadOnlyCollection<Sprite> AutomaticallyUpdatedSprites
        {
            get { return mAutomaticallyUpdatedSpritesReadOnly; }
        }

        public static ReadOnlyCollection<Sprite> ManuallyUpdatedSprites => mManuallyUpdatedSpritesReadOnly;

        /// <summary>
        /// Gets the default Camera.
        /// </summary>
        /// <remarks>
        /// If your application is only using one Camera, then this Camera can be
        /// used for all logic.  This Camera is automatically created by the engine,
        /// so single-camera applications do not need to instantiate their own Camera.
        /// </remarks>
        static public Camera Camera
        {
            get
            {
                // This may happen if we're running in Glue.
                if (mCameras == null || mCameras.Count == 0)
                {
                    return null;
                }
#if DEBUG
                if (mCurrentCameraIndex >= mCameras.Count)
                {
                    Debug.Assert(false, "Invalid camera index " + mCurrentCameraIndex + " for current camera.  Only " + mCameras.Count + " exist.");
                    return null;
                }
#endif
                return mCameras[mCurrentCameraIndex];
            }
        }

        /// <summary>
        /// The List of all Cameras used by FlatRedBall.  Any Camera in this List is managed and
        /// renders according to its settings.
        /// </summary>
        /// <remarks>
        /// Adding a Camera to this list will result in the Camera being managed by the engine and
        /// rendered.  There is no AddCamera method - simply adding the Camera to this list is sufficient
        /// for adding it to the engine.
        /// </remarks>
        public static PositionedObjectList<Camera> Cameras => mCameras; 

        #region XML Docs
        /// <summary>
        /// A read-only collection of IDrawableBatches.
        /// </summary>
        /// <remarks>
        /// <seealso cref="SpriteManager.AddDrawableBatch"/>
        /// <seealso cref="SpriteManager.RemoveDrawableBatch"/>
        /// </remarks>
        #endregion
        public static ReadOnlyCollection<IDrawableBatch> DrawableBatches
        {
            get { return mDrawableBatchesReadOnlyCollection; }
        }

        internal static List<IDrawableBatch> WritableDrawableBatchesList
        {
            get { return mDrawableBatches; }
        }


        public static ReadOnlyCollection<PositionedObject> ManagedPositionedObjects
        {
            get { return mManagedPositionedObjectsReadOnly; }
        }


        static public int LayerCount
        {
            get { return mLayers.Count; }
        }


        static public int ManuallyUpdatedSpriteCount
        {
            get { return mManuallyUpdatedSprites.Count; }
        }


        static public int ParticleCount
        {
            get { return mParticleCount; }
        }


        static public int MaxParticleCount
        {
            get { return mMaxParticleCount; }
            set
            {
                UpdateMaxParticleCount(value);
            }
        }

        /// <summary>
        /// When not set to 0 and the number of particles on screen exceed what is available, then
        /// the MaxParticleCount will be incremented by this amount.  Otherwise, it will throw
        /// an out of particles exception.
        /// </summary>
        public static int AutoIncrementParticleCountValue
        {
            get { return mAutoIncrementParticleCountValue; }
            set
            {
                mAutoIncrementParticleCountValue = value;
            }
        }

        /// <summary>
        /// Read-only collection of SpriteFrames managed by the SpriteManager.
        /// </summary>
        public static ReadOnlyCollection<SpriteFrame> SpriteFrames
        {
            get { return mSpriteFramesReadOnly; }
        }

        /// <summary>
        /// A read-only, ordered list of layers. This does not contain camera-specific layers.
        /// Layers with a higher index will be drawn on top of layers with a lower index. Layers
        /// will appear in this list in the order that they are added.
        /// </summary>
        /// <seealso cref="SpriteManager.MoveLayerAboveLayer(Layer, Layer)"/>
        /// <seealso cref="SpriteManager.MoveToBack(Layer)"/>
        /// <seealso cref="SpriteManager.MoveToFront(Layer)"/>
        public static ReadOnlyCollection<Layer> Layers => mLayersReadOnly; 
        


        /// <summary>
        /// A layer which is drawn on top of all other layers. 
        /// This is drawn on every camera, obeying the camera's DestinationRectangle.
        /// </summary>
        public static Layer TopLayer => mTopLayer; 
        

        /// <summary>
        /// A layer which is drawn underneath all other layers, and under unlayered objects. 
        /// This is drawn on every camera, obeying the camera's DestinationRectangle.
        /// </summary>
        public static Layer UnderAllDrawnLayer
        {
            get
            {
                return mUnderAllDrawnLayer;
            }
        }

        /// <summary>
        /// Gets and sets the sorting type used on Sprites, Text objects, and DrawableBatches
        /// in the world (not on layers). For sorting visual objects on layers, see the Layer's
        /// SortType property.
        /// </summary>
        public static SortType OrderedSortType
        {
            get { return mOrderedSortType; }
            set { mOrderedSortType = value; }
        }

        public static SortType ZBufferedSortType
        {
            get { return mZBufferedSortType; }
            set { mZBufferedSortType = value; }
        }

        public static ReadOnlyCollection<Sprite> OrderedSprites
        {
            get { return mOrderedSpritesReadOnly; }
        }


        public static ReadOnlyCollection<Sprite> ZBufferedSprites
        {
            get { return mZBufferedSpritesReadOnly; }
        }

        // needed for testing - I don't like that I have to do this but unit tests must be done!
        public static int NumberOfTimedRemovalObjects
        {
            get
            {
                return mTimedRemovalList.Count;
            }
        }

        #endregion

        #region Internal Properties

        internal static List<Updater> Updaters
        {
            get
            {
                return mUpdaters;
            }
        }

        static internal SpriteList OrderedByDistanceFromCameraSprites
        {
            get { return mOrderedByDistanceFromCameraSprites; }
        }

        static internal SpriteList ZBufferedSpritesWriteable
        {
            get { return mZBufferedSprites; }
        }

        //        static internal SpriteList AutomaticallyUpdatedSprites
        //      {
        //        get { return mAutomaticallyUpdatedSprites; }
        //  }

        static internal EmitterList Emitters
        {
            get { return mEmitters; }
        }

        static internal List<Layer> LayersWriteable
        {
            get { return mLayers; }
        }

        #endregion

        #endregion

        #region Methods
 
        #region Constructor / Initialize

        /// <summary>
        /// Creates a new SpriteManager.
        /// </summary>
        static SpriteManager()
        {
            #region Create all lists and name them
            mOrderedByDistanceFromCameraSprites = new SpriteList();
            mOrderedByDistanceFromCameraSprites.Name = "Sprites ordered by distance from Camera";
            mOrderedSpritesReadOnly = new ReadOnlyCollection<Sprite>(mOrderedByDistanceFromCameraSprites);

            mZBufferedSprites = new SpriteList();
            mZBufferedSprites.Name = "Sprites drawn using the ZBuffer";
            mZBufferedSpritesReadOnly = new ReadOnlyCollection<Sprite>(mZBufferedSprites);

            mAutomaticallyUpdatedSprites = new SpriteList();
            mAutomaticallyUpdatedSprites.Name = "Sprites automatically updated by the SpriteManager";
            mAutomaticallyUpdatedSpritesReadOnly = new ReadOnlyCollection<Sprite>(mAutomaticallyUpdatedSprites);

            mManuallyUpdatedSprites = new SpriteList();
            mManuallyUpdatedSprites.Name = "Sprites which must be manually updated.";
            mManuallyUpdatedSpritesReadOnly = new ReadOnlyCollection<Sprite>(mManuallyUpdatedSprites);

            mSpriteFrames = new PositionedObjectList<SpriteFrame>();
            mSpriteFrames.Name = "Internal SpriteManager SpriteFrame List";
            mSpriteFramesReadOnly = new ReadOnlyCollection<SpriteFrame>(mSpriteFrames);

            mDrawableBatches = new List<IDrawableBatch>();
            mDrawableBatchesReadOnlyCollection = new System.Collections.ObjectModel.ReadOnlyCollection<IDrawableBatch>(mDrawableBatches);

            mManagedPositionedObjects = new PositionedObjectList<PositionedObject>();
            mManagedPositionedObjects.Name = "PositionedObjects managed by the SpriteManager";
            mManagedPositionedObjectsReadOnly = new ReadOnlyCollection<PositionedObject>(mManagedPositionedObjects);

            mEmitters = new EmitterList();
            mEmitters.Name = "Internal SpriteManager EmitterList";

            mParticleCount = 0;
            mParticleSprites = new SpriteList();
            mParticleSprites.Name = "Particle Sprites";

            mRemoveWhenAlphaIs0 = new SpriteList();
            mRemoveWhenAlphaIs0.Name = "List holding Sprites to remove when Alpha is 0";

            mLayers = new List<Layer>();
            mLayersReadOnly = new ReadOnlyCollection<Layer>(mLayers);

#if DEBUG
            // These are high-traffic lists, so let's make them backed by a hash set to go faster
            mOrderedByDistanceFromCameraSprites.AddInternalHashSet();
            mZBufferedSprites.AddInternalHashSet();
            mAutomaticallyUpdatedSprites.AddInternalHashSet();
            mManuallyUpdatedSprites.AddInternalHashSet();
            mManagedPositionedObjects.AddInternalHashSet();
#endif


#endregion

            int numberOfUpdaters = 1;

            SetNumberOfThreadsToUse(numberOfUpdaters);

            MaxParticleCount = 2900;

            mTopLayer = new Layer();
            mTopLayer.Name = "Top Layer";
            mUnderAllDrawnLayer = new Layer();
            mUnderAllDrawnLayer.Name = "Under All Layer";


        }

        internal static void SetNumberOfThreadsToUse(int numberOfUpdaters)
        {

            mUpdaters.Clear();

            for (int i = 0; i < numberOfUpdaters; i++)
            {
                Updater updater = new Updater();
                updater.AutomaticallyUpdatedSprites = mAutomaticallyUpdatedSprites;
                mUpdaters.Add(updater);
            }
        }


        public static void Initialize()
        {
            mCameras = new PositionedObjectList<Camera>();

            Camera defaultCamera = new Camera(FlatRedBallServices.GlobalContentManager);
            defaultCamera.BackgroundColor = Color.Black;
            defaultCamera.Name = "Default Camera";

            mCameras.Add(defaultCamera);
        }


        #endregion

        #region Add Methods

        #region AddLayer, AddToLayer

        public static void AddLayer(Layer layerToAdd)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            mLayers.Add(layerToAdd);
        }

        static public Layer AddLayer()
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            Layer layer = new Layer();
            mLayers.Add(layer);
            return layer;
        }

        /// <summary>
        /// Adds the argument Sprite to the argument Layer. If the Sprite is not already
        /// managed by the SpriteManager, the Sprite will also be added to the internal list
        /// for management. This method can be called multiple times to add a single Sprite to
        /// multiple Layers.
        /// 
        /// If the layerToAddTo argument is null then the Sprite is added as a regular un-layered Sprite.
        /// </summary>
        /// <param name="spriteToAdd">The Sprite to add.</param>
        /// <param name="layerToAddTo">The Layer to add to. If null, the Sprite will be added as an un-layered Sprite.</param>
        /// <exception cref="InvalidOperationException">Thrown if this is not called on the primary thread.</exception>
        public static void AddToLayer(Sprite spriteToAdd, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            #region Layer is not null, so actually add it to the argument layer
            if (layerToAddTo != null)
            {

                layerToAddTo.Add(spriteToAdd);


                if (spriteToAdd.ListsBelongingTo.Contains(mOrderedByDistanceFromCameraSprites))
                {
                    mOrderedByDistanceFromCameraSprites.Remove(spriteToAdd);
                }

                if (spriteToAdd.ListsBelongingTo.Contains(mZBufferedSprites))
                {
                    mZBufferedSprites.Remove(spriteToAdd);
                }


                if (spriteToAdd.ListsBelongingTo.Contains(mManuallyUpdatedSprites) == false &&
                    spriteToAdd.ListsBelongingTo.Contains(mAutomaticallyUpdatedSprites) == false
                    )
                {
                    // The Sprite is neither manually updated or automatically updated.  That means
                    // it was instantiated outside of the SpriteManager.  Add it here as an
                    // automatically updated Sprite.

                    mAutomaticallyUpdatedSprites.Add(spriteToAdd);

                    spriteToAdd.mAutomaticallyUpdated = true;
                }
            }
            #endregion

            #region else, the layer is null, so do a regular add
            else
            {
                AddSprite(spriteToAdd);
            }
            #endregion
        }

        public static void AddToLayerZBuffered(Sprite spriteToAdd, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (layerToAddTo != null)
            {
                spriteToAdd.mOrdered = false;
                AddToLayer(spriteToAdd, layerToAddTo);
            }
            else
            {
                AddZBufferedSprite(spriteToAdd);
            }

        }

        /// <summary>
        /// Adds the argument batchToAdd to the argument layerToAddTo.
        /// If layerToAddTo is null, then this is the same as calling AddDrawableBatch(batchToAdd).
        /// If batchToAdd has already been added to the SpriteManager as an un-layered IDrawableBatch,
        /// this method will remove the IDrawableBatch from the unlayered list, and add it to the layer (so it draws only one time).
        /// Calling this method multiple times with multiple layers, however, will result in batchToAdd being part of multiple layers.
        /// </summary>
        /// <param name="batchToAdd">The IDrawableBatch to add.</param>
        /// <param name="layerToAddTo">The layer to add to, which can be null.</param>
        public static void AddToLayer(IDrawableBatch batchToAdd, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (layerToAddTo == null)
            {
                AddDrawableBatch(batchToAdd);
            }
            else
            {
                layerToAddTo.Add(batchToAdd);


                if (mDrawableBatches.Contains(batchToAdd))
                {
                    mDrawableBatches.Remove(batchToAdd);
                }
            }
        }

        public static void AddToLayerAllowDuplicateAdds(IDrawableBatch batchToAdd, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            layerToAddTo.Add(batchToAdd);
        }

        public static void AddToLayer(SpriteFrame spriteFrame, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            #region Make this SpriteFrame managed

            if (!mSpriteFrames.Contains(spriteFrame))
            {
                mSpriteFrames.Add(spriteFrame);
                spriteFrame.RefreshAttachments();
            }

            #endregion

            bool isNewlyAddedSpriteFrameOnNullLayer = 
                layerToAddTo == spriteFrame.mLayerBelongingTo && layerToAddTo == null && spriteFrame.mCenter != null &&
                !spriteFrame.mCenter.ListsBelongingTo.Contains(mOrderedByDistanceFromCameraSprites);

            if (isNewlyAddedSpriteFrameOnNullLayer)
            {                
                // This is a new SpriteFrame
                if (spriteFrame.mCenter != null)
                {
                    AddManualSprite(spriteFrame.mCenter);
                }
                if (spriteFrame.mTopLeft != null)
                {
                    AddManualSprite(spriteFrame.mTopLeft);
                }
                if (spriteFrame.mTop != null)
                {
                    AddManualSprite(spriteFrame.mTop);
                }
                if (spriteFrame.mTopRight != null)
                {
                    AddManualSprite(spriteFrame.mTopRight);
                }
                if (spriteFrame.mRight != null)
                {
                    AddManualSprite(spriteFrame.mRight);
                }
                if (spriteFrame.mBottomRight != null)
                {
                    AddManualSprite(spriteFrame.mBottomRight);
                }
                if (spriteFrame.mBottom != null)
                {
                    AddManualSprite(spriteFrame.mBottom);
                }
                if (spriteFrame.mBottomLeft != null)
                {
                    AddManualSprite(spriteFrame.mBottomLeft);
                }
                if(spriteFrame.mLeft != null)
                {
                    AddManualSprite(spriteFrame.mLeft);
                }                                                                                
            }

            else if (spriteFrame.mLayerBelongingTo != layerToAddTo)
            {
                if (spriteFrame.mLayerBelongingTo != null)
                {
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame);
                }
                #region Add to the new Layer

                if (layerToAddTo != null)
                {
                    if (spriteFrame.mCenter != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mCenter, layerToAddTo);
                        // Maybe we can think of a faster way to add them as manual, but this is the safest
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mCenter);
                    }
                    if (spriteFrame.mTop != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mTop, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTop);
                    }
                    if (spriteFrame.mBottom != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mBottom, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottom);
                    }
                    if (spriteFrame.mLeft != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mLeft);
                    }
                    if (spriteFrame.mRight != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mRight);
                    }
                    if (spriteFrame.mTopLeft != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mTopLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTopLeft);
                    }
                    if (spriteFrame.mTopRight != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mTopRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTopRight);
                    }
                    if (spriteFrame.mBottomLeft != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mBottomLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottomLeft);
                    }
                    if (spriteFrame.mBottomRight != null)
                    {
                        SpriteManager.AddToLayer(spriteFrame.mBottomRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottomRight);
                    }
                }
                else
                {
                    throw new NotImplementedException("Cannot currently remove SpriteFrames from layers.  Want this fixed?  Complain on the forums at www.flatredball.com");

                }

                #endregion

                spriteFrame.mLayerBelongingTo = layerToAddTo;
            }
        }

        public static void AddToLayerZBuffered(SpriteFrame spriteFrame, Layer layerToAddTo)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            #region Make this SpriteFrame managed

            if (!mSpriteFrames.Contains(spriteFrame))
            {
                mSpriteFrames.Add(spriteFrame);
                spriteFrame.RefreshAttachments();
            }

            #endregion


            bool isNewlyAddedSpriteFrameOnNullLayer =
                layerToAddTo == spriteFrame.mLayerBelongingTo && layerToAddTo == null && spriteFrame.mCenter != null &&
                !spriteFrame.mCenter.ListsBelongingTo.Contains(mOrderedByDistanceFromCameraSprites);

            if (isNewlyAddedSpriteFrameOnNullLayer)
            {
                // This is a new SpriteFrame

                AddManualZBufferedSprite(spriteFrame.mCenter);

                AddManualZBufferedSprite(spriteFrame.mTopLeft);
                AddManualZBufferedSprite(spriteFrame.mTop);
                AddManualZBufferedSprite(spriteFrame.mTopRight);
                AddManualZBufferedSprite(spriteFrame.mRight);
                AddManualZBufferedSprite(spriteFrame.mBottomRight);
                AddManualZBufferedSprite(spriteFrame.mBottom);
                AddManualZBufferedSprite(spriteFrame.mBottomLeft);
                AddManualZBufferedSprite(spriteFrame.mLeft);
            }

            else if (spriteFrame.mLayerBelongingTo != layerToAddTo)
            {
                if (spriteFrame.mLayerBelongingTo != null)
                {
                    spriteFrame.mLayerBelongingTo.Remove(spriteFrame);
                }
                #region Add to the new Layer

                if (layerToAddTo != null)
                {
                    if (spriteFrame.mCenter != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mCenter, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mCenter);
                    }
                    if (spriteFrame.mTop != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mTop, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTop);
                    }
                    if (spriteFrame.mBottom != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mBottom, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottom);
                    }
                    if (spriteFrame.mLeft != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mLeft);
                    }
                    if (spriteFrame.mRight != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mRight);
                    }
                    if (spriteFrame.mTopLeft != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mTopLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTopLeft);
                    }
                    if (spriteFrame.mTopRight != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mTopRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mTopRight);
                    }
                    if (spriteFrame.mBottomLeft != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mBottomLeft, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottomLeft);
                    }
                    if (spriteFrame.mBottomRight != null)
                    {
                        SpriteManager.AddToLayerZBuffered(spriteFrame.mBottomRight, layerToAddTo);
                        SpriteManager.ConvertToManuallyUpdated(spriteFrame.mBottomRight);
                    }
                }
                else
                {
                    throw new NotImplementedException("Cannot currently remove SpriteFrames from layers.  Want this fixed?  Complain on the forums at www.flatredball.com");

                }

                #endregion

                spriteFrame.mLayerBelongingTo = layerToAddTo;
            }


        }

        static public void AddToLayer<T>(AttachableList<T> listToAdd, Layer layerToAddTo) where T : Sprite
        {
            for (int i = 0; i < listToAdd.Count; i++)
            {
                AddToLayer(listToAdd[i] as Sprite, layerToAddTo);
            }
        }

        #endregion

        #region AddSprite, AddManualSprite, AddParticleSprite

        public static Sprite AddSprite(string texture)
        {
            return AddSprite(texture, FlatRedBallServices.GlobalContentManager);
        }

        public static Sprite AddSprite(AnimationChainList animationChainList)
        {
            Sprite s = new Sprite();
            s.AnimationChains = animationChainList;
            s.Animate = true;
            s.CurrentChainIndex = 0;
            s.CurrentFrameIndex = 0;
            s.SetAnimationChain(animationChainList[0], 0);

            AddSprite(s);
            return s;
        }

        public static Sprite AddSprite(AnimationChain animationChain)
        {
            Sprite s = new Sprite();
            s.SetAnimationChain(animationChain);
            s.Animate = true;
            s.CurrentChainIndex = 0;
            s.CurrentFrameIndex = 0;

            AddSprite(s);
            return s;

        }

        public static Sprite AddSprite(string texture, string contentManagerName)
        {

            Texture2D spriteTexture = FlatRedBallServices.Load<Texture2D>(texture, contentManagerName);

            return AddSprite(spriteTexture);
        }

        public static Sprite AddSprite(string texture, string contentManagerName, Layer layer)
        {
            Texture2D spriteTexture = FlatRedBallServices.Load<Texture2D>(texture, contentManagerName);
            return AddSprite(spriteTexture, layer);
        }

        public static Sprite AddManualSprite(string texture)
        {
            return AddManualSprite(texture, FlatRedBallServices.GlobalContentManager);
        }

        public static Sprite AddManualSprite(string texture, string contentManagerName)
        {
            Texture2D spriteTexture = FlatRedBallServices.Load<Texture2D>(texture);
            return AddManualSprite(spriteTexture);
        }

        public static Sprite AddSprite(Texture2D texture)
        {
            Sprite newSprite = new Sprite();
            newSprite.Texture = texture;
            AddSprite(newSprite);
            return newSprite;
        }

        public static Sprite AddZBufferedParticleSprite(Texture2D texture)
        {
            Sprite newParticleSprite = CreateParticleSprite(texture);

#if DEBUG
            if (mAutomaticallyUpdatedSprites.Contains(newParticleSprite))
            {
                throw new InvalidOperationException(
                    "This Sprite has already been added to the engine.  This exception is thrown to prevent bugs that can occur from a double-add.");
            }
#endif
            AddZBufferedSprite(newParticleSprite);

            return newParticleSprite;
        }



        public static Sprite AddZBufferedSprite(Texture2D texture)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            Sprite newSprite = new Sprite();
            newSprite.mOrdered = false;
            newSprite.Texture = texture;
#if DEBUG
            if (mAutomaticallyUpdatedSprites.Contains(newSprite))
            {
                throw new InvalidOperationException(
                    "This Sprite has already been added to the engine.  This exception is thrown to prevent bugs that can occur from a double-add.");
            }
#endif
            mAutomaticallyUpdatedSprites.Add(newSprite);

            newSprite.mAutomaticallyUpdated = true;

            mZBufferedSprites.Add(newSprite);

            return newSprite;
        }

        public static Sprite AddZBufferedSprite(Sprite sprite)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            sprite.mOrdered = false;
            sprite.mAutomaticallyUpdated = true;
            mAutomaticallyUpdatedSprites.Add(sprite);
            mZBufferedSprites.Add(sprite);

            return sprite;
        }

        public static Sprite AddManualSprite(Texture2D texture)
        {
            Sprite newSprite = new Sprite();
            newSprite.Texture = texture;

            AddManualSprite(newSprite);
            return newSprite;
        }

        public static Sprite AddManualZBufferedSprite(Texture2D texture)
        {
            Sprite newSprite = new Sprite();
            newSprite.Texture = texture;

            return AddManualZBufferedSprite(newSprite);
        }

        public static Sprite AddManualZBufferedParticleSprite(Texture2D texture)
        {
            Sprite newSprite = CreateParticleSprite(texture);

            AddManualZBufferedSprite(newSprite);

            return newSprite;
        }

        public static Sprite AddManualZBufferedSprite(Sprite sprite)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            mManuallyUpdatedSprites.Add(sprite);

            sprite.mAutomaticallyUpdated = false;
            if (sprite.mVerticesForDrawing == null)
                sprite.mVerticesForDrawing = new VertexPositionColorTexture[4];

            mZBufferedSprites.Add(sprite);

            ManualUpdate(sprite);

            return sprite;
        }

        public static void AddSprite(Sprite spriteToAdd)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (spriteToAdd != null)
            {
                // Vic says:  This fixes a bug in Glue that I discovered while working on
                // Udder Chaos.  Sometimes objects are removed after they cycle.  If this happens
                // then the removed Sprite's JustCycled will stay true.  This can cause Sprites to
                // be removed immediately after they are added IF the Sprite is recycled.  
                spriteToAdd.mJustCycled = false;
                spriteToAdd.mJustChangedFrame = false;

#if DEBUG
                if (mAutomaticallyUpdatedSprites.Contains(spriteToAdd))
                {
                    throw new InvalidOperationException(
                        "This Sprite has already been added to the engine.  This exception is thrown to prevent bugs that can occur from a double-add.");
                }
#endif

                mAutomaticallyUpdatedSprites.Add(spriteToAdd);

                spriteToAdd.mAutomaticallyUpdated = true;

                if (mOrderedSortType == SortType.Z)
                {
                    mOrderedByDistanceFromCameraSprites.AddSortedZAscending(spriteToAdd);
                }
                else
                {
                    mOrderedByDistanceFromCameraSprites.Add(spriteToAdd);
                }

            }
        }

        public static void AddManualSprite(Sprite spriteToAdd, Layer layer = null)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            mManuallyUpdatedSprites.Add(spriteToAdd);

            spriteToAdd.mAutomaticallyUpdated = false;
            if (spriteToAdd.mVerticesForDrawing == null)
                spriteToAdd.mVerticesForDrawing = new VertexPositionColorTexture[4];

            if(layer == null)
            {
                mOrderedByDistanceFromCameraSprites.Add(spriteToAdd);
            }
            else
            {
                layer.Add(spriteToAdd);
            }

            ManualUpdate(spriteToAdd);
        }

        public static Sprite AddSprite(Texture2D texture, Layer layer)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (layer == null)
            {
                return AddSprite(texture);
            }
            else
            {
                // there's some code duplication here, but there has to be
                // because the Sprite won't be added to the 
                // mOrderedByDistanceFromCameraSprites SpriteList
                Sprite newSprite = new Sprite();
                newSprite.Texture = texture;

#if DEBUG
                if (mAutomaticallyUpdatedSprites.Contains(newSprite))
                {
                    throw new InvalidOperationException(
                        "This Sprite has already been added to the engine.  This exception is thrown to prevent bugs that can occur from a double-add.");
                }
#endif
                mAutomaticallyUpdatedSprites.Add(newSprite);

                newSprite.mAutomaticallyUpdated = true;

                layer.Add(newSprite); // make this threaded

                return newSprite;
            }
        }
        public static Sprite AddManagedInvisibleSprite()
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            Sprite sprite = new Sprite();
            mAutomaticallyUpdatedSprites.Add(sprite);

            // We do this in case the user is going to handle the drawing
            // through a DrawableBatch.
            sprite.mVerticesForDrawing = new VertexPositionColorTexture[4];

            return sprite;


        }

        public static void AddManagedInvisibleSprite(Sprite sprite)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (sprite.ListsBelongingTo.Contains(mAutomaticallyUpdatedSprites) == false)
            {
                mAutomaticallyUpdatedSprites.Add(sprite);
                sprite.mAutomaticallyUpdated = true;
            }
        }

        public static void AddSpriteAtTime(Sprite spriteToAdd, double timeToAdd)
        {
            FlatRedBall.Instructions.StaticMethodInstruction staticMethodInstruction =
                new FlatRedBall.Instructions.StaticMethodInstruction(
                    typeof(SpriteManager).GetMethod("AddSprite", new Type[1] { typeof(Sprite) }),
                    new object[] { spriteToAdd }, timeToAdd);

            Instructions.InstructionManager.Instructions.Add(staticMethodInstruction);
        }

        #endregion

        #region Add Particle Sprite

        /// <summary>
        /// Creates a new particle sprite and adds it to the SpriteManager for management. The sprite will have
        /// its AnimationChains assigned to the argument animations property. This sprite is given a TextureScale of 1.
        /// </summary>
        /// <param name="animations">The animations to use by the new particle Sprite.</param>
        /// <param name="animationName">The name of the animation to show. This name must be contained in the argument animations.</param>
        /// <returns>The newly-created Sprite.</returns>
        public static Sprite AddParticleSprite(AnimationChainList animations, string animationName = null)
        {
            var newParticleSprite = CreateParticleSprite(null);

            newParticleSprite.AnimationChains = animations;
            newParticleSprite.TextureScale = 1;
            if(!string.IsNullOrEmpty(animationName))
            {
                newParticleSprite.CurrentChainName = animationName;
                newParticleSprite.TimeIntoAnimation = 0;
                newParticleSprite.Animate = true;
            }
            AddSprite(newParticleSprite);
            return newParticleSprite;
        }

        public static Sprite AddParticleSprite(Texture2D texture)
        {
            Sprite newParticleSprite = CreateParticleSprite(texture);
            AddSprite(newParticleSprite);
            return newParticleSprite;
        }

        /// <summary>
        /// Returns a particle Sprite which is not managed internally by the engine. This is the most
        /// efficient type of Sprite in FlatRedBall because it is pooled and the engine does not internally
        /// update the sprite. 
        /// </summary>
        /// <param name="texture">The texture to assign to the sprite.</param>
        /// <returns>The sprite, which may be returned from a pool of sprites.</returns>
        public static Sprite AddManualParticleSprite(Texture2D texture)
        {
            Sprite newParticleSprite = CreateParticleSprite(texture);
            AddManualSprite(newParticleSprite);
            return newParticleSprite;
        }

        #endregion

        #region AddSpriteFrame

        static public SpriteFrame AddSpriteFrame(Texture2D texture2D, SpriteFrame.BorderSides borderSides)
        {
            SpriteFrame spriteFrame = new SpriteFrame(texture2D, borderSides);
            AddSpriteFrame(spriteFrame);
            return spriteFrame;
        }

        static public void AddSpriteFrame(SpriteFrame spriteFrameToAdd)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            if (!mSpriteFrames.Contains(spriteFrameToAdd))
            {
                mSpriteFrames.Add(spriteFrameToAdd);
            }

            if (spriteFrameToAdd.LayerBelongingTo == null)
            {

                AddManualSprite(spriteFrameToAdd.mCenter);

                if (spriteFrameToAdd.mTopLeft != null)
                {
                    AddManualSprite(spriteFrameToAdd.mTopLeft);
                }
                if (spriteFrameToAdd.mTop != null)
                {
                    AddManualSprite(spriteFrameToAdd.mTop);
                }
                if (spriteFrameToAdd.mTopRight != null)
                {
                    AddManualSprite(spriteFrameToAdd.mTopRight);
                }
                if (spriteFrameToAdd.mRight != null)
                {
                    AddManualSprite(spriteFrameToAdd.mRight);
                }
                if (spriteFrameToAdd.mBottomRight != null)
                {
                    AddManualSprite(spriteFrameToAdd.mBottomRight);
                }
                if (spriteFrameToAdd.mBottom != null)
                {
                    AddManualSprite(spriteFrameToAdd.mBottom);
                }
                if (spriteFrameToAdd.mBottomLeft != null)
                {
                    AddManualSprite(spriteFrameToAdd.mBottomLeft);
                }
                if (spriteFrameToAdd.mLeft != null)
                {
                    AddManualSprite(spriteFrameToAdd.mLeft);
                }
            }
            else
            {
                AddToLayer(spriteFrameToAdd.mCenter, spriteFrameToAdd.LayerBelongingTo);
                ConvertToManuallyUpdated(spriteFrameToAdd.mCenter);

                if (spriteFrameToAdd.mTopLeft != null)
                {
                    AddToLayer(spriteFrameToAdd.mTopLeft, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mTopLeft);
                }
                if (spriteFrameToAdd.mTop != null)
                {
                    AddToLayer(spriteFrameToAdd.mTop, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mTop);
                }
                if (spriteFrameToAdd.mTopRight != null)
                {
                    AddToLayer(spriteFrameToAdd.mTopRight, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mTopRight);
                }
                if (spriteFrameToAdd.mRight != null)
                {
                    AddToLayer(spriteFrameToAdd.mRight, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mRight);
                }
                if (spriteFrameToAdd.mBottomRight != null)
                {
                    AddToLayer(spriteFrameToAdd.mBottomRight, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mBottomRight);
                }
                if (spriteFrameToAdd.mBottom != null)
                {
                    AddToLayer(spriteFrameToAdd.mBottom, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mBottom);
                }
                if (spriteFrameToAdd.mBottomLeft != null)
                {
                    AddToLayer(spriteFrameToAdd.mBottomLeft, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mBottomLeft);
                }
                if (spriteFrameToAdd.mLeft != null)
                {
                    AddToLayer(spriteFrameToAdd.mLeft, spriteFrameToAdd.LayerBelongingTo);
                    ConvertToManuallyUpdated(spriteFrameToAdd.mLeft);
                }

            }

            spriteFrameToAdd.RefreshAttachments();
        }

        #endregion

        #region Add Scene

        [Obsolete("This is now obsolete.  Use Scene.AddToManagers.  This will be removed from FlatRedBall in the near future")]
        static public void AddScene(Scene sceneToAdd)
        {
            AddScene(sceneToAdd, null);

        }

        [Obsolete("This is now obsolete.  Use Scene.AddToManagers.  This will be removed from FlatRedBall in the near future")]
        static public void AddScene(Scene sceneToAdd, Layer layer)
        {
            #region Add the Sprites
            if (layer == null)
            {
                for (int i = 0; i < sceneToAdd.Sprites.Count; i++)
                {
                    Sprite sprite = sceneToAdd.Sprites[i];

                    AddSprite(sprite);
                }
            }
            else
            {
                for (int i = 0; i < sceneToAdd.Sprites.Count; i++)
                {
                    Sprite sprite = sceneToAdd.Sprites[i];
                    AddToLayer(sprite, layer);
                }
            }
            #endregion

            #region Add the SpriteGrids
            for (int i = 0; i < sceneToAdd.SpriteGrids.Count; i++)
            {
                SpriteGrid spriteGrid = sceneToAdd.SpriteGrids[i];
                spriteGrid.Layer = layer;

                spriteGrid.PopulateGrid();
                spriteGrid.RefreshPaint();
                spriteGrid.Manage();
            }
            #endregion

            #region Add the SpriteFrames
            for (int i = 0; i < sceneToAdd.SpriteFrames.Count; i++)
            {
                SpriteManager.AddSpriteFrame(sceneToAdd.SpriteFrames[i]);
                if (layer != null)
                {
                    SpriteManager.AddToLayer(sceneToAdd.SpriteFrames[i], layer);
                }
            }
            #endregion
        }

        [Obsolete("This is now obsolete.  Use Scene.AddToManagers.  This will be removed from FlatRedBall in the near future")]
        static public Scene AddSceneCopy(Scene sceneToAdd, string contentManagerName)
        {
            return AddSceneCopy(sceneToAdd, contentManagerName, null);
        }

        [Obsolete("This is now obsolete.  Use Scene.AddToManagers.  This will be removed from FlatRedBall in the near future")]
        static public Scene AddSceneCopy(Scene sceneToAdd, string contentManagerName, Layer layer)
        {
            Scene scene = sceneToAdd.Clone();
            AddScene(scene, layer);
            return scene;
        }

        #endregion

        #region Add Emitter

        static public Emitter AddEmitter(string textureToUse, string contentManagerName)
        {
            Emitter emitter = new Emitter();
            emitter.EmissionSettings.Texture = FlatRedBallServices.Load<Texture2D>(textureToUse, contentManagerName);
            //emitter.Texture = FlatRedBallServices.Load<Texture2D>(textureToUse, contentManagerName);

            AddEmitter(emitter);

            return emitter;
        }

        public static void AddEmitter(Emitter emitterToManage)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            mEmitters.Add(emitterToManage);
        }

        public static void AddEmitter(Emitter emitterToManage, Layer layerToEmitOn)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
            // This should behave like AddToLayer which means it can be called
            // after the regular Add method and not have issues
            if (!emitterToManage.ListsBelongingTo.Contains(mEmitters))
            {
                mEmitters.Add(emitterToManage);
            }
            emitterToManage.LayerToEmitOn = layerToEmitOn;
        }

        #endregion

        #region AddPositionedObject
        public static void AddPositionedObject(PositionedObject positionedObjectToManage)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }
#endif
#if DEBUG
            if (mManagedPositionedObjects.Contains(positionedObjectToManage))
            {
                throw new InvalidOperationException("This PositionedObject is already part of the SpriteManager");
            }
#endif

            mManagedPositionedObjects.Add(positionedObjectToManage);
            //mManagedPositionedObjects.Add(positionedObjectToManage);
        }
        #endregion

        #region Add DrawableBatch

        /// <summary>
        /// Adds the argument IDrawableBatch to the engine to be rendered in order of its Z value.
        /// </summary>
        /// <remarks>
        /// This method must be called on the main thread.
        /// </remarks>
        /// <param name="drawableBatch">The IDrawableBatch to draw</param>
        public static void AddDrawableBatch(IDrawableBatch drawableBatch)
        {
#if DEBUG
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }

            if(mDrawableBatches.Contains(drawableBatch))
            {
                throw new InvalidOperationException("This IDrawableBatch has already been added to the SpriteManager");
            }
#endif
            mDrawableBatches.Add(drawableBatch);
        }
        
        public static void AddZBufferedDrawableBatch(IDrawableBatch drawableBatch)
        {
#if DEBUG

            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added on the primary thread");
            }

            if (mZBufferedDrawableBatches.Contains(drawableBatch))
            {
                throw new InvalidOperationException("This IDrawableBatch has already been added to the SpriteManager as a Z-buffered IrawableBatch");
            }
#endif

            mZBufferedDrawableBatches.Add(drawableBatch);
        }

        #endregion

        #endregion

        #region Public Methods

        public static bool AreAnySpritesReferencingDisposedAssets()
        {
            foreach (Sprite sprite in mAutomaticallyUpdatedSprites)
            {
                if (sprite.Texture != null && sprite.Texture.IsDisposed)
                {
                    return true;
                }

                if (sprite.AnimationChains.Count != 0)
                {
                    foreach (AnimationChain ac in sprite.AnimationChains)
                    {
                        foreach (AnimationFrame af in ac)
                        {
                            if (af.Texture != null && af.Texture.IsDisposed)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the first object which is added twice to the SpriteManager, or null if none are found.
        /// </summary>
        /// <returns>The first duplicate object or null.</returns>
        public static PositionedObject GetDuplicateMembership()
        {
            if (mCameras.GetFirstDuplicatePositionedObject() != null)
            {
                return mCameras.GetFirstDuplicatePositionedObject();
            }

            if (mOrderedByDistanceFromCameraSprites.GetFirstDuplicatePositionedObject() != null)
            {
                return mOrderedByDistanceFromCameraSprites.GetFirstDuplicatePositionedObject();
            }

            if (mZBufferedSprites.GetFirstDuplicatePositionedObject() != null)
            {
                return mZBufferedSprites.GetFirstDuplicatePositionedObject();
            }

            if (mAutomaticallyUpdatedSprites.GetFirstDuplicatePositionedObject() != null)
            {
                return mAutomaticallyUpdatedSprites.GetFirstDuplicatePositionedObject();
            }

            if (mManuallyUpdatedSprites.GetFirstDuplicatePositionedObject() != null)
            {
                return mManuallyUpdatedSprites.GetFirstDuplicatePositionedObject();
            }

            if (mSpriteFrames.GetFirstDuplicatePositionedObject() != null)
            {
                return mSpriteFrames.GetFirstDuplicatePositionedObject();
            }

            if (mManagedPositionedObjects.GetFirstDuplicatePositionedObject() != null)
            {
                return mManagedPositionedObjects.GetFirstDuplicatePositionedObject();
            }

            if (mEmitters.GetFirstDuplicatePositionedObject() != null)
            {
                return mEmitters.GetFirstDuplicatePositionedObject();
            }

            return null;

        }

        #region Convert to Manually/Automatically Updated and Distance/ZBuffer drawn Sprite
        /// <summary>
        /// Converts the argument Sprite from an anutomatically updated Sprite to a manually updated Sprite.
        /// </summary>
        /// <param name="spriteToConvert"></param>
        public static void ConvertToManuallyUpdated(Sprite spriteToConvert)
        {
            // I think we want to wrap the entire function in this if-check
            // so that we don't double-add objects.  This solves a bug discovered
            // in May 2013 where a Sprite that was both part of a Scene and a POList
            // in a Screen was being double-converted, causing a crash.
            //if (mAutomaticallyUpdatedSprites.Contains(spriteToConvert))
            //{
            //    mAutomaticallyUpdatedSprites.Remove(spriteToConvert);
            //}

            if (spriteToConvert.ListsBelongingTo.Contains(mAutomaticallyUpdatedSprites))
            {
                mAutomaticallyUpdatedSprites.Remove(spriteToConvert);
#if DEBUG

                if (spriteToConvert.ListsBelongingTo.Contains(mManuallyUpdatedSprites))
                {
                    throw new InvalidOperationException("This Sprite is already part of the manually updated list.  " +
                        "This exception is being thrown to prevent accumulation errors from occurring.");
                }
#endif
                mManuallyUpdatedSprites.Add(spriteToConvert);

                spriteToConvert.mAutomaticallyUpdated = false;

                // Let's save a little on the GC by protecting the "new" in an if-statement
                if (spriteToConvert.mVerticesForDrawing == null)
                {
                    spriteToConvert.mVerticesForDrawing = new VertexPositionColorTexture[4];
                }
                ManualUpdate(spriteToConvert);
            }
        }

        public static void ConvertToManuallyUpdated(SpriteList spriteList)
        {
            for (int i = spriteList.Count - 1; i > -1; i--)
            {
                ConvertToManuallyUpdated(spriteList[i]);
            }
        }

        public static void ConvertToManuallyUpdated(PositionedObject positionedObject)
        {
            if (positionedObject.ListsBelongingTo.Contains(mManagedPositionedObjects))
            {
                mManagedPositionedObjects.Remove(positionedObject);
            }
        }

        public static void ConvertToManuallyUpdated(SpriteFrame spriteFrame)
        {
            if (mSpriteFrames.Contains(spriteFrame))
            {
                mSpriteFrames.Remove(spriteFrame);
            }
        }

        public static void ConvertToManagedInvisible(Sprite sprite)
        {
            bool isAutomaticallyUpdated = false;

            for (int i = sprite.ListsBelongingTo.Count - 1; i > -1; i--)
            {
                IAttachableRemovable list = sprite.ListsBelongingTo[i];

                // Test automatically updated membership before removing from lists
                if (list == mAutomaticallyUpdatedSprites)
                {
                    isAutomaticallyUpdated = true;
                }

                if (list == mManuallyUpdatedSprites)
                {
                    mManuallyUpdatedSprites.Remove(sprite);
                }

                if (list == mZBufferedSprites ||
                    list == mOrderedByDistanceFromCameraSprites)
                {
                    list.Remove(sprite);
                    continue;
                }

                for (int layerIndex = 0; layerIndex < mLayers.Count; layerIndex++)
                {
                    if (list == mLayers[layerIndex].mSprites)
                    {
                        list.Remove(sprite);
                        // The Sprite may be part of multiple layers, so don't continue here
                    }
                }

                for (int cameraIndex = 0; cameraIndex < mCameras.Count; cameraIndex++)
                {
                    Camera camera = mCameras[cameraIndex];

                    for (int layerIndex = 0; layerIndex < camera.Layers.Count; layerIndex++)
                    {
                        Layer layer = camera.Layers[layerIndex];

                        if (list == layer.mSprites)
                        {
                            list.Remove(sprite);
                            // The Sprite may be part of multiple layers, so don't continue here
                        }
                    }
                }
            }

            if (isAutomaticallyUpdated == false)
            {
                mAutomaticallyUpdatedSprites.Add(sprite);
            }

            sprite.mVerticesForDrawing = new VertexPositionColorTexture[4];

        }

        public static void ConvertToManagedVisible(Sprite sprite)
        {
#if DEBUG
            if (sprite.ListsBelongingTo.Contains(mOrderedByDistanceFromCameraSprites))
            {
                throw new InvalidOperationException("The Sprite is already a visible Sprite");
            }
#endif

            mOrderedByDistanceFromCameraSprites.Add(sprite);

            if (!mAutomaticallyUpdatedSprites.Contains(sprite))
            {
                mAutomaticallyUpdatedSprites.Add(sprite);
            }
        }

        /// <summary>
        /// Converts the argument Sprite from a manually updated Sprite to an automatically updated Sprite.
        /// </summary>
        /// <param name="spriteToConvert">The sprite to convert.</param>
        public static void ConvertToAutomaticallyUpdated(Sprite spriteToConvert)
        {
            if (mManuallyUpdatedSprites.Contains(spriteToConvert))
            {
                mManuallyUpdatedSprites.Remove(spriteToConvert);
            }
            mAutomaticallyUpdatedSprites.Add(spriteToConvert);
            spriteToConvert.mAutomaticallyUpdated = true;
        }

        public static void ConvertToAutomaticallyUpdated(PositionedObject positionedObject)
        {
#if DEBUG
            if (positionedObject.ListsBelongingTo.Contains(mManagedPositionedObjects))
            {
                throw new InvalidOperationException("The object " + positionedObject.Name + " is already automatically updated");
            }
#endif
            mManagedPositionedObjects.Add(positionedObject);
        }

        static public void ConvertToZBufferedSprite(Sprite spriteToConvert)
        {
#if DEBUG
            if (spriteToConvert.ListsBelongingTo.Contains(mOrderedByDistanceFromCameraSprites) == false)
            {
                throw new System.InvalidOperationException("Argument Sprite is not currently being drawn by distance from Camera");
            }
#endif
            spriteToConvert.mOrdered = false;
            mOrderedByDistanceFromCameraSprites.Remove(spriteToConvert);
            mZBufferedSprites.Add(spriteToConvert);

        }

        static public void ConvertToOrderedSprite(Sprite spriteToConvert)
        {
#if DEBUG
            if (spriteToConvert.ListsBelongingTo.Contains(mZBufferedSprites) == false)
            {
                throw new InvalidOperationException("Argument Sprite is not currently a ZBuffered Sprite");
            }
#endif
            spriteToConvert.mOrdered = true;
            mZBufferedSprites.Remove(spriteToConvert);
            mOrderedByDistanceFromCameraSprites.Add(spriteToConvert);
        }

        #endregion

        #region IsAutomaticallyUpdated and other Is<Some State or Type> Methods

        static public bool IsAutomaticallyUpdated(Sprite sprite)
        {
            return mAutomaticallyUpdatedSprites.Contains(sprite);
        }

        #region XML Docs
        /// <summary>
        /// Returns whether the SpriteManager is holding a reference to the argment sprite.
        /// </summary>
        /// <param name="sprite">The sprite to check references for.</param>
        /// <returns>Whether the argument Sprite is referenced by the SpriteManager.</returns>
        #endregion
        static public bool IsManaging(Sprite sprite)
        {
            return mAutomaticallyUpdatedSprites.Contains(sprite) ||
                mManuallyUpdatedSprites.Contains(sprite);
        }

        static public bool IsManuallyUpdated(Sprite sprite)
        {
            return mManuallyUpdatedSprites.Contains(sprite);
        }

        #endregion

        public static bool IsRelativeToCamera(Sprite spriteToTest)
        {
            foreach (Layer layer in mLayers)
            {
                if (layer.RelativeToCamera &&
                    layer.Sprites.Contains(spriteToTest))
                {
                    return true;
                }
            }
            return false;
        }

        #region ManualUpdate Methods

        static public void ManualUpdate()
        {
            ManualUpdate(mManuallyUpdatedSprites);
        }

        static public void ManualUpdate(SpriteList spritesToUpdate)
        {
            for (int i = 0; i < spritesToUpdate.Count; i++)
            {
                ManualUpdate(spritesToUpdate[i]);
            }
        }

        static public void ManualUpdate(Sprite spriteToUpdate)
        {
            // Look at the following diagram to know the vertex numbers
            //    0--------------1
            //    |              |
            //    |              |
            //    |              |
            //    |              |
            //    |              |
            //    3--------------2
            ///////////////////////////////////////////////////////////

            if (spriteToUpdate.mVerticesForDrawing == null)
            {
                // 1/28/2012
                // Not sure why
                // we were returning
                // here - we should instantiate
                // the objects.
                //return;
                // May 26, 2013
                // Not sure why we're
                // forcing this to false.
                // We shouldn't assume that
                // if the user calls this function
                // that they want the Sprite to be a
                // manually updated Sprite.  This actually
                // gets called when you ForceUpdateDependenciesDeep,
                // which can screw a lot of things up.
                //spriteToUpdate.mAutomaticallyUpdated = false;
                spriteToUpdate.mVerticesForDrawing = new VertexPositionColorTexture[4];
            }

            spriteToUpdate.UpdateVertices();

            spriteToUpdate.mVerticesForDrawing[0].Position = spriteToUpdate.mVertices[0].Position;
            spriteToUpdate.mVerticesForDrawing[1].Position = spriteToUpdate.mVertices[1].Position;
            spriteToUpdate.mVerticesForDrawing[2].Position = spriteToUpdate.mVertices[2].Position;
            spriteToUpdate.mVerticesForDrawing[3].Position = spriteToUpdate.mVertices[3].Position;


    #if MONOGAME
            if (spriteToUpdate.Texture != null && spriteToUpdate.ColorOperation == ColorOperation.Texture)
            {
                // In this case we'll just use the Alpha for all components (since it's premultiplied)
                spriteToUpdate.mVerticesForDrawing[0].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[0].Color.W)) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.W)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.W)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.W)) << 24);


                spriteToUpdate.mVerticesForDrawing[1].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[1].Color.W)) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.W)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.W)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.W)) << 24);

                spriteToUpdate.mVerticesForDrawing[2].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[2].Color.W)) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.W)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.W)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.W)) << 24);

                spriteToUpdate.mVerticesForDrawing[3].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[3].Color.W)) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.W)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.W)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.W)) << 24);
            }
            else
    #endif
            {

                spriteToUpdate.mVerticesForDrawing[0].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[0].Color.X)) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.Y)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.Z)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[0].Color.W)) << 24);


                spriteToUpdate.mVerticesForDrawing[1].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[1].Color.X)) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.Y)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.Z)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[1].Color.W)) << 24);

                spriteToUpdate.mVerticesForDrawing[2].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[2].Color.X)) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.Y)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.Z)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[2].Color.W)) << 24);

                spriteToUpdate.mVerticesForDrawing[3].Color.PackedValue =
                    ((uint)(255 * spriteToUpdate.mVertices[3].Color.X)) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.Y)) << 8) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.Z)) << 16) +
                    (((uint)(255 * spriteToUpdate.mVertices[3].Color.W)) << 24);
            }

            if (!spriteToUpdate.FlipHorizontal && !spriteToUpdate.FlipVertical)
            {
                spriteToUpdate.mVerticesForDrawing[0].TextureCoordinate = spriteToUpdate.mVertices[0].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[1].TextureCoordinate = spriteToUpdate.mVertices[1].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[2].TextureCoordinate = spriteToUpdate.mVertices[2].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[3].TextureCoordinate = spriteToUpdate.mVertices[3].TextureCoordinate;
            }
            else if (spriteToUpdate.FlipVertical && spriteToUpdate.FlipHorizontal)
            {
                spriteToUpdate.mVerticesForDrawing[0].TextureCoordinate = spriteToUpdate.mVertices[2].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[1].TextureCoordinate = spriteToUpdate.mVertices[3].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[2].TextureCoordinate = spriteToUpdate.mVertices[0].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[3].TextureCoordinate = spriteToUpdate.mVertices[1].TextureCoordinate;

            }
            else if (spriteToUpdate.FlipVertical)
            {
                spriteToUpdate.mVerticesForDrawing[0].TextureCoordinate = spriteToUpdate.mVertices[3].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[1].TextureCoordinate = spriteToUpdate.mVertices[2].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[2].TextureCoordinate = spriteToUpdate.mVertices[1].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[3].TextureCoordinate = spriteToUpdate.mVertices[0].TextureCoordinate;
            }
            else if (spriteToUpdate.FlipHorizontal)
            {
                spriteToUpdate.mVerticesForDrawing[0].TextureCoordinate = spriteToUpdate.mVertices[1].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[1].TextureCoordinate = spriteToUpdate.mVertices[0].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[2].TextureCoordinate = spriteToUpdate.mVertices[3].TextureCoordinate;
                spriteToUpdate.mVerticesForDrawing[3].TextureCoordinate = spriteToUpdate.mVertices[2].TextureCoordinate;

            }

        }

        #endregion

        #region MoveToFront/Back (Layer reordering)

        /// <summary>
        /// Moves the argument layer to the back (to index 0), so that all other layers
        /// draw on top of the argument layer.
        /// </summary>
        /// <param name="layer"></param>
        public static void MoveToBack(Layer layer)
        {
            mLayers.Remove(layer);
            mLayers.Insert(0, layer);
        }

        /// <summary>
        /// Moves the argument layer so it appears in front (drawn after) all other layers.
        /// </summary>
        /// <param name="layer">The layer to move.</param>
        public static void MoveToFront(Layer layer)
        {
            // Last layers appear on top (front)
            mLayers.Remove(layer);
            mLayers.Add(layer);
        }

        /// <summary>
        /// Reorders the argument layerToMove so that it is drawn immediately after the relativeTo layer.
        /// </summary>
        /// <param name="layerToMove">Which layer to move.</param>
        /// <param name="relativeTo">The layer to move in front of.</param>
        public static void MoveLayerAboveLayer(Layer layerToMove, Layer relativeTo)
        {
            int index = mLayers.IndexOf(relativeTo);

            mLayers.Remove(layerToMove);
            mLayers.Insert(index + 1, layerToMove);
        }

        #endregion

        #region Remove Methods

        public static void RemoveAllParticleSprites()
        {
            // This can be a forward loop because the Sprites in
            // mSparticleSprites are never removed from that list.
            for (int i = 0; i < mParticleSprites.Count; i++)
            {
                if (mParticleSprites[i].mEmpty == false)
                    RemoveSprite(mParticleSprites[i]);
            }
        }

        public static void RemoveEmitter(Emitter emitterToRemove)
        {
            emitterToRemove.RemoveSelfFromListsBelongingTo();
        }

        public static void RemoveEmitterOneWay(Emitter emitterToRemove)
        {
            mEmitters.Remove(emitterToRemove);
        }


        public static void RemoveScene(Scene sceneToRemove, bool emptyArgumentScene)
        {
            if (emptyArgumentScene == false)
            {
                sceneToRemove.Sprites.MakeOneWay();
                sceneToRemove.SpriteFrames.MakeOneWay();
            }

            RemoveSpriteList(sceneToRemove.Sprites);

            for (int i = 0; i < sceneToRemove.SpriteGrids.Count; i++)
            {
                sceneToRemove.SpriteGrids[i].RemoveSprites();
            }

            for (int i = sceneToRemove.SpriteFrames.Count - 1; i > -1; i--)
            {
                RemoveSpriteFrame(sceneToRemove.SpriteFrames[i]);
            }

            if (emptyArgumentScene)
                sceneToRemove.SpriteGrids.Clear();
            else
            {
                sceneToRemove.Sprites.MakeTwoWay();
                sceneToRemove.SpriteFrames.MakeTwoWay();
            }


        }

        public static void RemoveSprite(Sprite spriteToRemove)
        {
#if DEBUG
            if (spriteToRemove == null)
                throw new ArgumentNullException(nameof(spriteToRemove));
#endif

            spriteToRemove.OnRemove();

            // September 24, 2017
            // This used to be called
            // when removing Sprites, but
            // if an entity inherits from a
            // sprite, we don't want all of the
            // entity's children to be detached when
            // it's removed from managers. I know this
            // changes old behavior from FRB but I think
            // it's bad to destroy relationships under an
            // object when it is removed from managers, so
            // I'm going to comment it out:
            //spriteToRemove.ClearRelationships();

            int indexOfParticleArray = spriteToRemove.ListsBelongingTo.IndexOf(mParticleSprites);

            if (indexOfParticleArray != -1)
            {
                // July 10, 2022 - why lock here?
                // This has a cost, and could be expensive
                // for a large number of sprites. This should
                // never be on a secondary thread.
                //lock(typeof(SpriteManager))
                {
                    // this needs to be in the if statement or else it would always evaluate to false
                    for (int i = spriteToRemove.ListsBelongingTo.Count - 1; i > -1; i--)
                    {
                        if (i != indexOfParticleArray)
                            spriteToRemove.ListsBelongingTo[i].Remove(spriteToRemove);
                    }

                    // Only decrement the particle count and perform logic
                    // if the Sprite being removed is actually an active particle.
                    // It's possible for the user to get a reference to a particle
                    // and remove it multiple times.  In this case we could get negative
                    // particle counts if this if statement isn't in place.
                    if (spriteToRemove.mEmpty == false)
                    {
                        mParticleCount--;
                        spriteToRemove.mEmpty = true;
                        if (mFirstEmpty == null || mFirstEmpty.mParticleIndex > spriteToRemove.mParticleIndex)
                            mFirstEmpty = spriteToRemove;
                    }

                    // This particle sprite could have a timed removal.  If so, we want to clear out
                    // the timed removal so that when it's recycled (if it's recycled) it's not removed
                    // sooner than it should be
                    for (int i = 0; i < mTimedRemovalList.Count; i++)
                    {
                        if (mTimedRemovalList[i].SpriteToRemove == spriteToRemove)
                        {
                            mTimedRemovalList.RemoveAt(i);
                            break;
                        }
                    }

                    spriteToRemove.Initialize(false);
                }

            }
            else
            {
                spriteToRemove.RemoveSelfFromListsBelongingTo();
            }
        }

        public static void RemoveSpriteAtTime(Sprite spriteToRemove, double secondsPastCurrentTime)
        {

            spriteToRemove.Instructions.Add(new FlatRedBall.Instructions.StaticMethodInstruction(
                mRemoveSpriteMethodInfo, new object[] { spriteToRemove },
                TimeManager.CurrentTime + secondsPastCurrentTime));


        }

        /// <summary>
        /// Removes the argument sprite from all SpriteManager lists and all
        /// layers, but keeps the Sprite attached to its parent objects
        /// (such as a Glue entity).
        /// </summary>
        /// <param name="spriteToRemove">The Sprite to remove</param>
        public static void RemoveSpriteOneWay(Sprite spriteToRemove)
        {
            mOrderedByDistanceFromCameraSprites.Remove(spriteToRemove);
            mAutomaticallyUpdatedSprites.Remove(spriteToRemove);
            mZBufferedSprites.Remove(spriteToRemove);
            mManuallyUpdatedSprites.Remove(spriteToRemove);

            UnderAllDrawnLayer.Remove(spriteToRemove);
            TopLayer.Remove(spriteToRemove);

            foreach (Layer sl in mLayers)
                sl.Remove(spriteToRemove);
        }

        public static void RemoveSpriteFrame(SpriteFrame spriteFrameToRemove)
        {

            RemoveSprite(spriteFrameToRemove.mCenter);

            // These could be null depending on the bordersides, so do null checks
            if(spriteFrameToRemove.mTopLeft != null)
                RemoveSprite(spriteFrameToRemove.mTopLeft);

            if(spriteFrameToRemove.mTop != null)
                RemoveSprite(spriteFrameToRemove.mTop);
            
            if(spriteFrameToRemove.mTopRight != null)
                RemoveSprite(spriteFrameToRemove.mTopRight);
            
            if(spriteFrameToRemove.mRight != null)
                RemoveSprite(spriteFrameToRemove.mRight);
            
            if(spriteFrameToRemove.mBottomRight != null)
                RemoveSprite(spriteFrameToRemove.mBottomRight);
            
            if(spriteFrameToRemove.mBottom != null)
                RemoveSprite(spriteFrameToRemove.mBottom);

            if(spriteFrameToRemove.mBottomLeft != null)
                RemoveSprite(spriteFrameToRemove.mBottomLeft);
            
            if(spriteFrameToRemove.mLeft != null)
                RemoveSprite(spriteFrameToRemove.mLeft);

            while (spriteFrameToRemove.ListsBelongingTo.Count > 0)
            {
                spriteFrameToRemove.ListsBelongingTo[0].Remove(spriteFrameToRemove);
            }
        }


        public static void RemoveSpriteList<T>(IList<T> listToRemove) where T : Sprite
        {
            // backwards loop so we don't miss any Sprites
            for (int i = listToRemove.Count - 1; i > -1; i--)
            {
                RemoveSprite(listToRemove[i]);
            }
        }


        public static void RemoveLayer(Layer layerToRemove)
        {
            // This seems bad - why do we do this?
            // This function is not honest if we leave
            // this in:
            //RemoveSpriteList(layerToRemove.Sprites);
            //TextManager.RemoveText(layerToRemove.mTexts);
            // It's also incomplete beause there's IDB's and SpriteFrames
            mLayers.Remove(layerToRemove);
        }

        /// <summary>
        /// Removes the argument objectToRemove from all SpriteManager lists (for rendering and automatic updates) as well as
        /// from any two-way lists that the object may belong to (such as PositionedObjectLists in custom code and lists in Glue).
        /// </summary>
        /// <param name="objectToRemove">The object to remove.</param>
        public static void RemovePositionedObject(PositionedObject objectToRemove)
        {
            objectToRemove.RemoveSelfFromListsBelongingTo();
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument DrawableBatch from the internal list and calls its Destroy method.
        /// </summary>
        /// <param name="drawableBatch">The DrawableBatch to remove.</param>
        #endregion
        public static void RemoveDrawableBatch(IDrawableBatch drawableBatch)
        {
            drawableBatch.Destroy();

            if (mZBufferedDrawableBatches.Contains(drawableBatch))
            {
                mZBufferedDrawableBatches.Remove(drawableBatch);
            }
            else
            {
                mDrawableBatches.Remove(drawableBatch);
            }

            for (int cameraIndex = 0; cameraIndex < mCameras.Count; cameraIndex++)
            {
                Camera camera = mCameras[cameraIndex];

                for (int layerIndex = 0; layerIndex < camera.Layers.Count; layerIndex++)
                {
                    Layer layer = camera.Layers[layerIndex];

                    layer.Remove(drawableBatch);
                }
            }

            foreach (Layer layer in mLayers)
            {
                layer.Remove(drawableBatch);
            }
        }

        public static void RemoveDrawableBatchOneWay(IDrawableBatch drawableBatch)
        {
            // Don't destroy
            //drawableBatch.Destroy();
            if (mZBufferedDrawableBatches.Contains(drawableBatch))
            {
                mZBufferedDrawableBatches.Remove(drawableBatch);
            }
            else
            {
                mDrawableBatches.Remove(drawableBatch);
            }

            for (int cameraIndex = 0; cameraIndex < mCameras.Count; cameraIndex++)
            {
                Camera camera = mCameras[cameraIndex];

                for (int layerIndex = 0; layerIndex < camera.Layers.Count; layerIndex++)
                {
                    Layer layer = camera.Layers[layerIndex];

                    layer.Remove(drawableBatch);
                }
            }

            foreach (Layer layer in mLayers)
            {
                layer.Remove(drawableBatch);
            }
        }

            //        mOrderedByDistanceFromCameraSprites.Remove(spriteToRemove);
            //mAutomaticallyUpdatedSprites.Remove(spriteToRemove);
            //mZBufferedSprites.Remove(spriteToRemove);

            //foreach (Layer sl in mLayers)
            //    sl.Remove(spriteToRemove);

        #region XML Docs
        /// <summary>
        /// Removes the Sprite from the SpriteManager but preserves emitters, attachment and children references. 
        /// </summary>
        /// <remarks>
        /// Although the removed Sprite will preserve its parent and children, the parent will 
        /// no longer see this Sprite as its child, and the children will no longer see this 
        /// Sprite as their parent.  The preservation of relationships is not a functional one.  Usually
        /// this method is used to keep relationships alive on a removed Sprite for reattachment at
        /// a later time.
        /// </remarks>
        /// <param name="spriteToRemove">The Sprite to remove.</param>
        #endregion
        public static void RemoveSpritePreserveRelationships(Sprite spriteToRemove)
        {
            if (spriteToRemove.ListsBelongingTo.Contains(mParticleSprites) == false)
            {
                while (spriteToRemove.ListsBelongingTo.Count > 0)
                {

                    spriteToRemove.ListsBelongingTo[0].Remove(spriteToRemove);
                    //continue;
                }
            }
            else
            {
                spriteToRemove.Initialize();

                spriteToRemove.mEmpty = true;
                while (spriteToRemove.ListsBelongingTo.Count > 0)
                {
                    SpriteList sa = spriteToRemove.ListsBelongingTo[0] as SpriteList;
                    if (sa != null)
                    {
                        sa.Remove(spriteToRemove);
                        continue;
                    }

                    IAttachableRemovable poa = spriteToRemove.ListsBelongingTo[0];
                    if (poa != null)
                    {
                        poa.Remove(spriteToRemove);
                        continue;
                    }


                }
                mParticleCount--;
                mParticleSprites.Add(spriteToRemove);
                spriteToRemove.mEmpty = true;
                if (mFirstEmpty == null)
                    mFirstEmpty = spriteToRemove;
            }

            // I don't know why this was here before, but the relationships should be preserved.
            //foreach (PositionedObject po in spriteToRemove.Children)
            //    po.Detach();

        }


        #endregion

        public static void BringToFront(Sprite sprite)
        {
#if DEBUG
            if (OrderedSortType != SortType.None)
            {
                throw new InvalidOperationException("You can only call BringToFront if the " +
                    "OrderedSortType is SortType.None");
            }
#endif

            mOrderedByDistanceFromCameraSprites.Remove(sprite);
            mOrderedByDistanceFromCameraSprites.Add(sprite);

        }

        public static void ShuffleInternalLists()
        {
            mOrderedByDistanceFromCameraSprites.Shuffle();
        }


        #region XML Docs
        /// <summary>
        /// Finds all contained Sprites which reference the argument oldTexture and replaces
        /// the reference with newTexture.
        /// </summary>
        /// <remarks>
        /// This method will preserve all sizes of Sprites even if they use a non-zero PixelSize.
        /// </remarks>
        /// <param name="oldTexture">The old Texture2D to replace.</param>
        /// <param name="newTexture">The new Texture2D to use as a replacement.</param>
        #endregion
        public static void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            float oldScaleX = 0;
            float oldScaleY = 0;

            foreach (Sprite sprite in mAutomaticallyUpdatedSprites)
            {
                if (sprite.Texture == oldTexture)
                {
                    // This could change the PixelSize.  The user may have set the PixelSize,
                    // but then overrode it, so let's preserve it.
                    oldScaleX = sprite.ScaleX;
                    oldScaleY = sprite.ScaleY;

                    sprite.Texture = newTexture;

                    sprite.ScaleX = oldScaleX;
                    sprite.ScaleY = oldScaleY;
                }
            }

            foreach (Sprite sprite in mManuallyUpdatedSprites)
            {
                if (sprite.Texture == oldTexture)
                {
                    // This could change the PixelSize.  The user may have set the PixelSize,
                    // but then overrode it, so let's preserve it.
                    oldScaleX = sprite.ScaleX;
                    oldScaleY = sprite.ScaleY;

                    sprite.Texture = newTexture;

                    sprite.ScaleX = oldScaleX;
                    sprite.ScaleY = oldScaleY;
                }
            }


            foreach (SpriteFrame spriteFrame in mSpriteFrames)
            {
                if (spriteFrame.Texture == oldTexture)
                {
                    spriteFrame.Texture = newTexture;
                }
            }

            for (int i = 0; i < Emitters.Count; i++)
            {
                Emitter emitter = Emitters[i];

                if (emitter.EmissionSettings.Texture == oldTexture)
                {
                    emitter.EmissionSettings.Texture = newTexture;
                }

                //if (emitter.Texture == oldTexture)
                //{
                //    emitter.Texture = newTexture;
                //}
            }

        }

        #region Sort Methods

        static public void SortTexturesSecondary()
        {
            //Monitor.Enter(mOrderedByDistanceFromCameraSprites);
            mOrderedByDistanceFromCameraSprites.SortTextureOnZBreaks();
            mZBufferedSprites.SortTextureInsertion();

            for (int i = 0; i < SpriteManager.Layers.Count; i++)
            {
                SpriteManager.Layers[i].mSprites.SortTextureInsertion();
                SpriteManager.Layers[i].mZBufferedSprites.SortTextureInsertion();
            }

            //Monitor.Exit(mOrderedByDistanceFromCameraSprites);
        }

        static public void SortYSpritesSecondary()
        {
            //Monitor.Enter(mOrderedByDistanceFromCameraSprites);
            mOrderedByDistanceFromCameraSprites.SortYInsertionDescendingOnZBreaks();
            //Monitor.Exit(mOrderedByDistanceFromCameraSprites);
        }

        #endregion

        static public new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Automatically Managed Sprite Count: ").Append(mAutomaticallyUpdatedSprites.Count);

            return stringBuilder.ToString();
        }

        public static void ClearParticleTextures()
        {
            for (int i = 0; i < mParticleSprites.Count; i++)
            {
                mParticleSprites[i].Texture = null;
            }
        }

        #endregion

        #region Internal methods

        public static void Update()
        {
            Update(null);
        }

        public static void Update(Section section )
        {

            if (section != null)
            {
                Section.GetAndStartContextAndTime("Sprite Instructions");
            }
            float secondDifference = TimeManager.SecondDifference;
            float secondDifferenceSquaredDividedByTwo = TimeManager.SecondDifferenceSquaredDividedByTwo;
            float lastSecondDifference = TimeManager.LastSecondDifference;
            double currentTime = TimeManager.CurrentTime;
            //ExecuteInstructions<Sprite>(mAutomaticallyUpdatedSprites, currentTime);
            // This lets us take advantage of multiple cores:
            // Update January 7, 2013
            // Instructions can modify
            // the list.  I thought this
            // was okay, but it can cause
            // problems if one thread updates
            // while another is accessing an index.
            ExecuteInstructions<Sprite>(mAutomaticallyUpdatedSprites, currentTime);

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("All other instructions");
            }

            //for (int i = 0; i < mUpdaters.Count; i++)
            //{
            //    mUpdaters[i].ExecuteInstructions();
            //}
            //for (int i = 0; i < mUpdaters.Count; i++)
            //{
            //    mUpdaters[i].Wait();
            //}
            ExecuteInstructions<Camera>(mCameras, currentTime);
            ExecuteInstructions<PositionedObject>(mManagedPositionedObjects, currentTime);
            ExecuteInstructions<Emitter>(mEmitters, currentTime);
            ExecuteInstructions<SpriteFrame>(mSpriteFrames, currentTime);

            RefreshUpdaterIndexes();

            //for (int i = 0; i < mAutomaticallyUpdatedSprites.Count; i++)
            //{
            //    var c = mAutomaticallyUpdatedSprites[i];
            //    c.TimedActivity(
            //        secondDifference,
            //        secondDifferenceSquaredDividedByTwo,
            //        lastSecondDifference);
            //}
            //for (int i = 0; i < mAutomaticallyUpdatedSprites.Count; i++)
            //{
            //    var c = mAutomaticallyUpdatedSprites[i];
            //    c.TimedActivity(
            //        secondDifference,
            //        secondDifferenceSquaredDividedByTwo,
            //        lastSecondDifference);
            //}

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Sprite Timed Activity");
            }

            bool useThreads = mUpdaters.Count > 1;

		#if THREAD_POOL_IS_SLOW
            useThreads = false;
        #endif
            int spriteCount = mAutomaticallyUpdatedSprites.Count;

            if (useThreads)
            {

                // This lets us take advantage of multiple cores:
                for (int i = 0; i < mUpdaters.Count; i++)
                {
                    mUpdaters[i].TimedActivity();
                }
                for (int i = 0; i < mUpdaters.Count; i++)
                {
                    mUpdaters[i].Wait();
                }
            }
            else
            {
                for (int i = 0; i < spriteCount; i++)
                {
                    Sprite s = AutomaticallyUpdatedSprites[i];
                    s.TimedActivity(
                        secondDifference,
                        secondDifferenceSquaredDividedByTwo,
                        lastSecondDifference);
                }

            }

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Sprite Animation");
            }

            // Animation needs to occur before CustomBehavior.
            // One common behavior is to remove a Sprite when JustCycled
            // is true.  The test for this often occurs in CustomBehaviors.
            // The test for this should occur between Animation and Drawing so
            // that the Sprite is removed before the animation cycles.


            if (useThreads)
            {

                for (int i = 0; i < mUpdaters.Count; i++)
                {
                    mUpdaters[i].AnimateSelf();
                }
                for (int i = 0; i < mUpdaters.Count; i++)
                {
                    mUpdaters[i].Wait();
                }
            }
            else
            {
                for (int i = 0; i < spriteCount; i++)
                {
                    Sprite s = AutomaticallyUpdatedSprites[i];
                    s.AnimateSelf(currentTime);
                }

            }

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Other type timed activity");
            }

            #region Timed Activity



            for (int i = 0; i < mCameras.Count; i++)
            {
                Camera c = mCameras[i];
                c.TimedActivity(
                    secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }

            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                mSpriteFrames[i].TimedActivity(
                    secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }

            for (int i = 0; i < mManagedPositionedObjects.Count; i++)
            {
                mManagedPositionedObjects[i].TimedActivity(
                    secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }

            for (int i = 0; i < mEmitters.Count; i++)
            {
                mEmitters[i].TimedActivity(
                    secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }


            #endregion

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Sprite timed removal");
            }

            //Monitor.Exit(mAutomaticallyUpdatedSprites);
            
            // todo:  SpriteFrames need to be animated

            for (int i = mRemoveWhenAlphaIs0.Count - 1; i > -1; i--)
            {
                if (mRemoveWhenAlphaIs0[i].Alpha <= 0)
                {
                    RemoveSprite(mRemoveWhenAlphaIs0[i]);
                }
            }
            while (mTimedRemovalList.Count != 0 && mTimedRemovalList[0].TimeToRemove <= TimeManager.CurrentTime)
            {
                var timedRemoval = mTimedRemovalList[0];
                mTimedRemovalList.RemoveAt(0);
                RemoveSprite(timedRemoval.SpriteToRemove);
            }

            if (section != null)
            {
                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("SpriteFrame management");
            }

            //Monitor.Enter(mAutomaticallyUpdatedSprites);
            // We are phasing this out:
		#if !ANDROID
            for (int i = mAutomaticallyUpdatedSprites.Count - 1; i > -1; i--)
            {
                if (i < mAutomaticallyUpdatedSprites.Count)
                {
                    Sprite s = mAutomaticallyUpdatedSprites[i];
                    s.OnCustomBehavior();
                }
            }


#endif


            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                mSpriteFrames[i].Manage();
            }

            if (section != null)
            {
                Section.EndContextAndTime();
            }

        }

        private static void RefreshUpdaterIndexes()
        {
            float ratio = 1 / (float)mUpdaters.Count;
            int spriteCount = (int)(ratio * mAutomaticallyUpdatedSprites.Count);
            for (int i = 0; i < mUpdaters.Count; i++)
            {
                int start = spriteCount * i;
                mUpdaters[i].SpriteIndex = start;
                if (i == mUpdaters.Count - 1)
                {
                    mUpdaters[i].SpriteCount = mAutomaticallyUpdatedSprites.Count - start;
                }
                else
                {
                    mUpdaters[i].SpriteCount = spriteCount;
                }
            }
        }


        static internal void Animate(double currentTime, SpriteList spritesToAnimate)
        {
            for (int i = 0; i < spritesToAnimate.Count; i++)
            {
                Sprite s = spritesToAnimate[i];
                s.AnimateSelf(currentTime);
            }
        }


        

        static internal void Pause(InstructionList unpauseInstructions)
        {
            // To prevent the removal of sprites
            FlatRedBall.Instructions.Pause.TimedRemovalUnpause unpause = new FlatRedBall.Instructions.Pause.TimedRemovalUnpause(mTimedRemovalList);
            mTimedRemovalList = new List<TimedRemovalRecord>();
            unpauseInstructions.Add((Instruction)unpause);

            // Pause all of the items that are managed by the SpriteManager
            for(int i = 0; i < mCameras.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mCameras[i]))
                {
                    mCameras[i].Pause(unpauseInstructions);
                }
            }

            for(int i = 0; i < mAutomaticallyUpdatedSprites.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mAutomaticallyUpdatedSprites[i]))
                {
                    mAutomaticallyUpdatedSprites[i].Pause(unpauseInstructions);
                }
            }

            for (int i = 0; i < mManuallyUpdatedSprites.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mManuallyUpdatedSprites[i]))
                {
                    mManuallyUpdatedSprites[i].Pause(unpauseInstructions);
                }
            }

            for (int i = 0; i < mSpriteFrames.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mSpriteFrames[i]))
                {
                    mSpriteFrames[i].Pause(unpauseInstructions);
                }
            }

            for (int i = 0; i < mManagedPositionedObjects.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mManagedPositionedObjects[i]))
                {
                    mManagedPositionedObjects[i].Pause(unpauseInstructions);
                }
            }

            for (int i = 0; i < mEmitters.Count; i++)
            {
                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(mEmitters[i]))
                {
                    mEmitters[i].Pause(unpauseInstructions);
                }
            }

            // Layers don't have a Pause method currently, but they may
            // need one in the future.  In that case, I'm keeping the following
            // lines here:
            //foreach (Layer layer in mLayers)
            //{
            //    layer.Pause(unpauseInstructions);
            //}

            //mTopLayer.Pause(unpauseInstructions);
        }


        static internal void RefreshTextureReferences()
        {
            foreach (Sprite s in mAutomaticallyUpdatedSprites)
            {
                s.Texture = FlatRedBallServices.Load<Texture2D>(s.Texture.Name);
            }

            foreach (Sprite s in mManuallyUpdatedSprites)
            {
                s.Texture = FlatRedBallServices.Load<Texture2D>(s.Texture.Name);
            }
        }

        #region XML Docs
        /// <summary>
        /// Updates the dependencies of all contained automatically updated Sprites, Cameras, and SpriteFrames.
        /// </summary>
        #endregion

        static internal void UpdateDependencies()
        {
            //FlushBuffers();
            RefreshUpdaterIndexes();

            // Properties like currentTime and count are cached in local
            // variables to eliminate the calling of properties which results
            // in method calls on the 360.  Also, loops are inlined here rather
            // than in their own methods to (slighly) reduce method calls and copying
            // on the stack.
            double currentTime = TimeManager.CurrentTime;

            int count = 0;
            int i = 0;
#if DEBUG
            try
            {
#endif

                bool useThreading = mUpdaters.Count > 1;

			#if THREAD_POOL_IS_SLOW
                useThreading = false;    
            #endif

                if (useThreading)
                {

                    for (i = 0; i < mUpdaters.Count; i++)
                    {
                        mUpdaters[i].UpdateDependencies();
                    }
                    for (i = 0; i < mUpdaters.Count; i++)
                    {
                        mUpdaters[i].Wait();
                    }
                }
                else
                {
                    count = mAutomaticallyUpdatedSprites.Count;
                    for (i = 0; i < count; i++)
                    {
                        Sprite s = AutomaticallyUpdatedSprites[i];
                        s.UpdateDependencies(currentTime);

                    }
                }

#if DEBUG
            }
            catch(Exception e)
            {
                throw new Exception("Error looping through AutomaticallyUpdatedSprites.  i is " + i + ", cached count is " + count + 
                    " .  Actual count is " + mAutomaticallyUpdatedSprites.Count + " : " + e.Message, e);
            }
#endif



#if DEBUG
            try
            {
#endif
            count = mSpriteFrames.Count;
            for (i = 0; i < count; i++)
            {
                mSpriteFrames[i].UpdateDependencies(currentTime);
            }
#if DEBUG
            }
            catch (Exception e)
            {
                throw new Exception("Error looping through SpriteFrames.  i is " + i + ", count is " + count + " : " + e.Message, e);
            }
#endif

#if DEBUG
            try
            {
#endif
            count = mCameras.Count;
            for (i = 0; i < count; i++)
            {
                mCameras[i].UpdateDependencies(currentTime);
            }
#if DEBUG
            }
            catch (Exception e)
            {
                throw new Exception("Error looping through Cameras.  i is " + i + ", count is " + count + " : " + e.Message, e);
            }
#endif



#if DEBUG
            try
            {
#endif
            count = mManagedPositionedObjects.Count;
            for (i = 0; i < count; i++)
            {
                mManagedPositionedObjects[i].UpdateDependencies(currentTime);
            }
#if DEBUG
            }
            catch (Exception e)
            {
                throw new Exception("Error looping through ManagedPositionedObjects.  i is " + i + ", count is " + count + " : " + e.Message, e);
            }
#endif

#if DEBUG
            try
            {
#endif
            count = mEmitters.Count;
            for (i = 0; i < count; i++)
            {
                mEmitters[i].UpdateDependencies(currentTime);
            }

#if DEBUG
            }
            catch (Exception e)
            {
                throw new Exception("Error looping through Emitters.  i is " + i + ", count is " + count + " : " + e.Message, e);
            }
#endif


        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Returns a new Sprite instance from the particle pool, but does not add it to the SpriteManager.
        /// </summary>
        /// <param name="texture">The texture to assign on the sprite.</param>
        /// <returns>The new sprite.</returns>
        public static Sprite CreateParticleSprite(Texture2D texture)
        {
            if (texture != null && texture.IsDisposed)
            {
                throw new ObjectDisposedException("Cannot create particle with disposed texture");
            }

            while (true)
            {
                if (mFirstEmpty != null)
                {
                    //lock (mFirstEmpty)
                    {
                        Sprite spriteToReturn = mFirstEmpty;
                        spriteToReturn.Texture = texture;
                        spriteToReturn.TimeCreated = TimeManager.CurrentTime;
                        spriteToReturn.mEmpty = false;
                        spriteToReturn.Instructions.Clear();

                        mParticleCount++;

                        int i = mFirstEmpty.mParticleIndex + 1; ;

                        int particleSpriteListCount = mParticleSprites.Count;

                        for (; i < particleSpriteListCount; i++)
                        {
                            if (mParticleSprites[i].mEmpty == true)
                            { mFirstEmpty = mParticleSprites[i]; break; }
                        }
                        if (i >= particleSpriteListCount)
                            mFirstEmpty = null;
                        return spriteToReturn;
                    }
                }
                else
                {
                    if (mAutoIncrementParticleCountValue == 0)
                    {
                        throw new System.IndexOutOfRangeException("The SpriteManager ran out of available particles.  Your maximum number of particles is set to " +
                            mParticleSprites.Count + ".");
                    }
                    else
                    {
                        MaxParticleCount += mAutoIncrementParticleCountValue;
                    }
                }
            }
        }


        static private void UpdateMaxParticleCount(int Count)
        {

            //int maxBefore = mMaxParticleCount;

            mMaxParticleCount = Count;


            if (mParticleSprites.Count < Count)
            {

                for (int i = mParticleSprites.Count; i < Count; i++)
                {
                    Sprite tempSprite = new Sprite();
                    tempSprite.mEmpty = true;
                    tempSprite.mParticleIndex = i;

                    // We can skip contains checks and a few other checks by 
                    // directly accessing internal objects:
                    //mParticleSprites.Add(tempSprite);
                    tempSprite.ListsBelongingTo.Add(mParticleSprites);
                    mParticleSprites.mInternalList.Add(tempSprite);
                }

				//26 October 2011 - Niall Muldoon
				//find the first empty one instead of using:
                //mFirstEmpty = mParticleSprites[maxBefore];
                for (int i = 0; i < mParticleSprites.Count; i++)
                {
                    if (mParticleSprites[i].mEmpty)
                    {
                        mFirstEmpty = mParticleSprites[i];
                        break;
                    }
                }
            }
            else if (mParticleSprites.Count > Count)
            {
                while (mParticleSprites.Count > Count)
                {
                    mParticleSprites[mParticleSprites.Count - 1].RemoveSelfFromListsBelongingTo();
                }
            }

            if (mMaxParticleCount < 0)
            {
                mFirstEmpty = null;
            }

        }


        static private void ExecuteInstructions<T>(PositionedObjectList<T> list, double currentTime) where T : PositionedObject
        {





            //Monitor.Enter(list);
            for (int i = list.Count - 1; i > -1; i--)
            {// loop through the sprites
                if (i < list.Count)
                {
                    list[i].ExecuteInstructions(currentTime);
                }
            }
            //Monitor.Exit(list);
        }


        static private void UpdateDependencies<T>(PositionedObjectList<T> spriteList, double currentTime) where T : PositionedObject
        {
            for (int i = spriteList.Count - 1; i > -1; i--)
            {// loop through the sprites
                spriteList[i].UpdateDependencies(currentTime);
            }
        }

        #endregion

        #endregion
    }
}


