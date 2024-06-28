#pragma warning disable
#if ANDROID || IOS || DESKTOP_GL
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif
#define SUPPORTS_GLUEVIEW_2
using Color = Microsoft.Xna.Framework.Color;
using System.Linq;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall;
using System;
using System.Collections.Generic;
using System.Text;
namespace Beefball.Entities
{
    public partial class PlayerBall : FlatRedBall.PositionedObject, FlatRedBall.Graphics.IDestroyable, FlatRedBall.Entities.IEntity, FlatRedBall.Math.Geometry.ICollidable
    {
        // This is made static so that static lazy-loaded content can access it.
        public static string ContentManagerName { get; set; }
        #if DEBUG
        private double mLastTimeCalledActivity=-1;
        #endif
        #if DEBUG
        public static bool HasBeenLoadedWithGlobalContentManager { get; private set; }= false;
        #endif
        public class VariableState
        {
            public string Name;
            public float X;
            public float Y;
            public float Z;
            public float MovementSpeed;
            public float Drag;
            public float DashFrequency;
            public float DashSpeed;
            public float CooldownCircleRadius;
            public Microsoft.Xna.Framework.Color CircleInstanceColor;
            public Microsoft.Xna.Framework.Color CooldownCircleColor;
            public static Dictionary<string, VariableState> AllStates = new Dictionary<string, VariableState>
            {
            }
            ;
        }
        private VariableState mCurrentState = null;
        public Entities.PlayerBall.VariableState CurrentState
        {
            get
            {
                return mCurrentState;
            }
            set
            {
                mCurrentState = value;
                if (value != null)
                {
                    if (this.Parent == null)
                    {
                        X = value.X;
                    }
                    else
                    {
                        RelativeX = value.X;
                    }
                    if (this.Parent == null)
                    {
                        Y = value.Y;
                    }
                    else
                    {
                        RelativeY = value.Y;
                    }
                    if (this.Parent == null)
                    {
                        Z = value.Z;
                    }
                    else
                    {
                        RelativeZ = value.Z;
                    }
                    MovementSpeed = value.MovementSpeed;
                    Drag = value.Drag;
                    DashFrequency = value.DashFrequency;
                    DashSpeed = value.DashSpeed;
                    CooldownCircleRadius = value.CooldownCircleRadius;
                    CircleInstanceColor = value.CircleInstanceColor;
                    CooldownCircleColor = value.CooldownCircleColor;
                }
            }
        }
        public class DashCategory
        {
            public string Name;
            public float CooldownCircleRadius;
            public static DashCategory Tired = new DashCategory()
            {
                Name = "Tired",
                CooldownCircleRadius = 0f,
            }
            ;
            public static DashCategory Rested = new DashCategory()
            {
                Name = "Rested",
                CooldownCircleRadius = 16f,
            }
            ;
            public static Dictionary<string, DashCategory> AllStates = new Dictionary<string, DashCategory>
            {
                {"Tired", Tired},
                {"Rested", Rested},
            }
            ;
        }
        private DashCategory mCurrentDashCategoryState = null;
        public Entities.PlayerBall.DashCategory CurrentDashCategoryState
        {
            get
            {
                return mCurrentDashCategoryState;
            }
            set
            {
                mCurrentDashCategoryState = value;
                if (value != null)
                {
                    CooldownCircleRadius = value.CooldownCircleRadius;
                }
            }
        }
        static object mLockObject = new object();
        static System.Collections.Generic.List<string> mRegisteredUnloads = new System.Collections.Generic.List<string>();
        static System.Collections.Generic.List<string> LoadedContentManagers = new System.Collections.Generic.List<string>();
        
        private global::FlatRedBall.Math.Geometry.Circle mCircleInstance;
        public global::FlatRedBall.Math.Geometry.Circle CircleInstance
        {
            get
            {
                return mCircleInstance;
            }
            private set
            {
                mCircleInstance = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.Circle mCooldownCircle;
        public global::FlatRedBall.Math.Geometry.Circle CooldownCircle
        {
            get
            {
                return mCooldownCircle;
            }
            private set
            {
                mCooldownCircle = value;
            }
        }
        private float mMovementSpeed = 300f;
        public virtual float MovementSpeed
        {
            set
            {
                mMovementSpeed = value;
            }
            get
            {
                return mMovementSpeed;
            }
        }
        private float mDashFrequency = 2f;
        public virtual float DashFrequency
        {
            set
            {
                mDashFrequency = value;
            }
            get
            {
                return mDashFrequency;
            }
        }
        private float mDashSpeed = 600f;
        public virtual float DashSpeed
        {
            set
            {
                mDashSpeed = value;
            }
            get
            {
                return mDashSpeed;
            }
        }
        public virtual float CooldownCircleRadius
        {
            get
            {
                return CooldownCircle.Radius;
            }
            set
            {
                CooldownCircle.Radius = value;
            }
        }
        #if XNA3 || SILVERLIGHT
        public virtual Microsoft.Xna.Framework.Graphics.Color CircleInstanceColor
        #else
        public virtual Microsoft.Xna.Framework.Color CircleInstanceColor
        #endif
        {
            get
            {
                return CircleInstance.Color;
            }
            set
            {
                CircleInstance.Color = value;
            }
        }
        #if XNA3 || SILVERLIGHT
        public virtual Microsoft.Xna.Framework.Graphics.Color CooldownCircleColor
        #else
        public virtual Microsoft.Xna.Framework.Color CooldownCircleColor
        #endif
        {
            get
            {
                return CooldownCircle.Color;
            }
            set
            {
                CooldownCircle.Color = value;
            }
        }
        private FlatRedBall.Math.Geometry.ShapeCollection mGeneratedCollision;
        public FlatRedBall.Math.Geometry.ShapeCollection Collision
        {
            get
            {
                return mGeneratedCollision;
            }
        }
        public HashSet<string> ItemsCollidedAgainst { get; private set;} = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; private set;} = new HashSet<string>();
        public HashSet<object> ObjectsCollidedAgainst { get; private set;} = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; private set;} = new HashSet<object>();
        protected FlatRedBall.Graphics.Layer LayerProvidedByContainer = null;
        public PlayerBall () 
        	: this(FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, true)
        {
        }
        public PlayerBall (string contentManagerName) 
        	: this(contentManagerName, true)
        {
        }
        public PlayerBall (string contentManagerName, bool addToManagers) 
        	: base()
        {
            ContentManagerName = contentManagerName;
            InitializeEntity(addToManagers);
        }
        protected virtual void InitializeEntity (bool addToManagers) 
        {
            LoadStaticContent(ContentManagerName);
            mCircleInstance = new global::FlatRedBall.Math.Geometry.Circle();
            mCircleInstance.Name = "CircleInstance";
            mCircleInstance.CreationSource = "Glue";
            mCooldownCircle = new global::FlatRedBall.Math.Geometry.Circle();
            mCooldownCircle.Name = "CooldownCircle";
            mCooldownCircle.CreationSource = "Glue";
            
            PostInitialize();
            if (addToManagers)
            {
                AddToManagers(null);
            }
        }
        public virtual void ReAddToManagers (FlatRedBall.Graphics.Layer layerToAddTo) 
        {
            LayerProvidedByContainer = layerToAddTo;
            FlatRedBall.SpriteManager.AddPositionedObject(this);
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(mCircleInstance, LayerProvidedByContainer);
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(mCooldownCircle, LayerProvidedByContainer);
        }
        public virtual void AddToManagers (FlatRedBall.Graphics.Layer layerToAddTo) 
        {
            LayerProvidedByContainer = layerToAddTo;
            FlatRedBall.SpriteManager.AddPositionedObject(this);
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(mCircleInstance, LayerProvidedByContainer);
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(mCooldownCircle, LayerProvidedByContainer);
            AddToManagersBottomUp(layerToAddTo);
            CustomInitialize();
        }
        public virtual void Activity () 
        {
            #if DEBUG
            if(TimeManager.TimeFactor > 0 && mLastTimeCalledActivity > 0 && mLastTimeCalledActivity == FlatRedBall.TimeManager.CurrentScreenTime)
            {
                throw new System.Exception("Activity was called twice in the same frame. This can cause objects to move 2x as fast.");
            }
            mLastTimeCalledActivity = FlatRedBall.TimeManager.CurrentScreenTime;
            #endif
            
            CustomActivity();
        }
        public virtual void ActivityEditMode () 
        {
            CustomActivityEditMode();
        }
        public virtual void Destroy () 
        {
            FlatRedBall.SpriteManager.RemovePositionedObject(this);
            
            if (CircleInstance != null)
            {
                FlatRedBall.Math.Geometry.ShapeManager.RemoveOneWay(CircleInstance);
            }
            if (CooldownCircle != null)
            {
                FlatRedBall.Math.Geometry.ShapeManager.RemoveOneWay(CooldownCircle);
            }
            mGeneratedCollision.RemoveFromManagers(clearThis: true);
            CustomDestroy();
        }
        public virtual void PostInitialize () 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (mCircleInstance.Parent == null)
            {
                mCircleInstance.CopyAbsoluteToRelative();
                mCircleInstance.AttachTo(this, false);
            }
            CircleInstance.Radius = 16f;
            if (mCooldownCircle.Parent == null)
            {
                mCooldownCircle.CopyAbsoluteToRelative();
                mCooldownCircle.AttachTo(this, false);
            }
            CooldownCircle.Radius = 16f;
            mGeneratedCollision = new FlatRedBall.Math.Geometry.ShapeCollection();
            Collision.Circles.AddOneWay(mCircleInstance);
            Collision.Circles.AddOneWay(mCooldownCircle);
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public virtual void AddToManagersBottomUp (FlatRedBall.Graphics.Layer layerToAddTo) 
        {
            AssignCustomVariables(false);
        }
        public virtual void RemoveFromManagers () 
        {
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(this);
            if (CircleInstance != null)
            {
                FlatRedBall.Math.Geometry.ShapeManager.RemoveOneWay(CircleInstance);
            }
            if (CooldownCircle != null)
            {
                FlatRedBall.Math.Geometry.ShapeManager.RemoveOneWay(CooldownCircle);
            }
            mGeneratedCollision.RemoveFromManagers(clearThis: false);
        }
        public virtual void AssignCustomVariables (bool callOnContainedElements) 
        {
            if (callOnContainedElements)
            {
            }
            CircleInstance.Radius = 16f;
            CooldownCircle.Radius = 16f;
            MovementSpeed = 300f;
            Drag = 1f;
            DashFrequency = 2f;
            DashSpeed = 600f;
            CooldownCircleRadius = 16f;
            CircleInstanceColor = Microsoft.Xna.Framework.Color.White;
            CooldownCircleColor = Microsoft.Xna.Framework.Color.White;
        }
        public virtual void ConvertToManuallyUpdated () 
        {
            this.ForceUpdateDependenciesDeep();
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(this);
        }
        public static void LoadStaticContent (string contentManagerName) 
        {
            if (LoadedContentManagers.Contains(contentManagerName))
            {
                return;
            }
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (string.IsNullOrEmpty(contentManagerName))
            {
                throw new System.ArgumentException("contentManagerName cannot be empty or null");
            }
            ContentManagerName = contentManagerName;
            #if DEBUG
            if (contentManagerName == FlatRedBall.FlatRedBallServices.GlobalContentManager)
            {
                HasBeenLoadedWithGlobalContentManager = true;
            }
            else if (HasBeenLoadedWithGlobalContentManager)
            {
                throw new System.Exception( "PlayerBall has been loaded with a Global content manager, then loaded with a non-global.  This can lead to a lot of bugs");
            }
            #endif
            bool registerUnload = false;
            if (LoadedContentManagers.Contains(contentManagerName) == false)
            {
                LoadedContentManagers.Add(contentManagerName);
                lock (mLockObject)
                {
                    if (!mRegisteredUnloads.Contains(ContentManagerName) && ContentManagerName != FlatRedBall.FlatRedBallServices.GlobalContentManager)
                    {
                        FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName).AddUnloadMethod("PlayerBallStaticUnload", UnloadStaticContent);
                        mRegisteredUnloads.Add(ContentManagerName);
                    }
                }
            }
            if (registerUnload && ContentManagerName != FlatRedBall.FlatRedBallServices.GlobalContentManager)
            {
                lock (mLockObject)
                {
                    if (!mRegisteredUnloads.Contains(ContentManagerName) && ContentManagerName != FlatRedBall.FlatRedBallServices.GlobalContentManager)
                    {
                        FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName).AddUnloadMethod("PlayerBallStaticUnload", UnloadStaticContent);
                        mRegisteredUnloads.Add(ContentManagerName);
                    }
                }
            }
            CustomLoadStaticContent(contentManagerName);
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public static void UnloadStaticContent () 
        {
            if (LoadedContentManagers.Count != 0)
            {
                LoadedContentManagers.RemoveAt(0);
                mRegisteredUnloads.RemoveAt(0);
            }
            if (LoadedContentManagers.Count == 0)
            {
            }
        }
        public FlatRedBall.Instructions.Instruction InterpolateToState (DashCategory stateToInterpolateTo, double secondsToTake) 
        {
            if (stateToInterpolateTo == DashCategory.Tired)
            {
                CooldownCircle.RadiusVelocity =  (0f - CooldownCircleRadius) / (float)secondsToTake;
            }
            else if (stateToInterpolateTo == DashCategory.Rested)
            {
                CooldownCircle.RadiusVelocity =  (16f - CooldownCircleRadius) / (float)secondsToTake;
            }
            var instruction = new FlatRedBall.Instructions.DelegateInstruction<DashCategory>(StopStateInterpolation, stateToInterpolateTo);
            instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + secondsToTake;
            this.Instructions.Add(instruction);
            return instruction;
        }
        public void StopStateInterpolation (DashCategory stateToStop) 
        {
            if (stateToStop == DashCategory.Tired)
            {
                CooldownCircle.RadiusVelocity =  0;
            }
            else if (stateToStop == DashCategory.Rested)
            {
                CooldownCircle.RadiusVelocity =  0;
            }
            CurrentDashCategoryState = stateToStop;
        }
        public void InterpolateBetween (DashCategory firstState, DashCategory secondState, float interpolationValue) 
        {
            #if DEBUG
            if (float.IsNaN(interpolationValue))
            {
                throw new System.Exception("interpolationValue cannot be NaN");
            }
            #endif
            bool setCooldownCircleRadius = true;
            float CooldownCircleRadiusFirstValue= 0;
            float CooldownCircleRadiusSecondValue= 0;
            if (firstState == DashCategory.Tired)
            {
                CooldownCircleRadiusFirstValue = 0f;
            }
            else if (firstState == DashCategory.Rested)
            {
                CooldownCircleRadiusFirstValue = 16f;
            }
            if (secondState == DashCategory.Tired)
            {
                CooldownCircleRadiusSecondValue = 0f;
            }
            else if (secondState == DashCategory.Rested)
            {
                CooldownCircleRadiusSecondValue = 16f;
            }
            if (setCooldownCircleRadius)
            {
                CooldownCircleRadius = CooldownCircleRadiusFirstValue * (1 - interpolationValue) + CooldownCircleRadiusSecondValue * interpolationValue;
            }
            if (interpolationValue < 1)
            {
                mCurrentDashCategoryState = firstState;
            }
            else
            {
                mCurrentDashCategoryState = secondState;
            }
        }
        public static void PreloadStateContent (DashCategory state, string contentManagerName) 
        {
            ContentManagerName = contentManagerName;
            if (state == DashCategory.Tired)
            {
            }
            else if (state == DashCategory.Rested)
            {
            }
        }
        [System.Obsolete("Use GetFile instead")]
        public static object GetStaticMember (string memberName) 
        {
            return null;
        }
        public static object GetFile (string memberName) 
        {
            return null;
        }
        object GetMember (string memberName) 
        {
            return null;
        }
        public static void Reload (object whatToReload) 
        {
        }
        protected bool mIsPaused;
        public override void Pause (FlatRedBall.Instructions.InstructionList instructions) 
        {
            base.Pause(instructions);
            mIsPaused = true;
        }
        public virtual void SetToIgnorePausing () 
        {
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(this);
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(CircleInstance);
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(CooldownCircle);
        }
        public virtual void MoveToLayer (FlatRedBall.Graphics.Layer layerToMoveTo) 
        {
            var layerToRemoveFrom = LayerProvidedByContainer;
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(CircleInstance);
            }
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(CircleInstance, layerToMoveTo);
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(CooldownCircle);
            }
            FlatRedBall.Math.Geometry.ShapeManager.AddToLayer(CooldownCircle, layerToMoveTo);
            LayerProvidedByContainer = layerToMoveTo;
        }
        partial void CustomActivityEditMode();
    }
}
