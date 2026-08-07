#pragma warning disable
#if ANDROID || IOS || DESKTOP_GL
#define REQUIRES_PRIMARY_THREAD_LOADING
#endif
#define SUPPORTS_GLUEVIEW_2
using Color = Microsoft.Xna.Framework.Color;
using System.Linq;
using FlatRedBall;
using System;
using System.Collections.Generic;
using System.Text;
namespace EditorTest1.Screens
{
    public abstract partial class GameScreen : FlatRedBall.Screens.Screen
    {
        #if DEBUG
        public static bool HasBeenLoadedWithGlobalContentManager { get; private set; }= false;
        #endif
        
        protected global::FlatRedBall.Math.PositionedObjectList<global::EditorTest1.Entities.Entity1> Entity1List = new global::FlatRedBall.Math.PositionedObjectList<global::EditorTest1.Entities.Entity1>();
        private global::EditorTest1.Entities.Entity1 Entity11;
        protected global::FlatRedBall.TileGraphics.LayeredTileMap Map;
        public GameScreen () 
        	: this ("GameScreen")
        {
        }
        public GameScreen (string contentManagerName) 
        	: base (contentManagerName)
        {
            Entity1List.Name = "Entity1List";
            // Not instantiating for FlatRedBall.TileGraphics.LayeredTileMap Map in Screens\GameScreen (Screen) because properties on the object prevent it
        }
        public override void Initialize (bool addToManagers) 
        {
            LoadStaticContent(ContentManagerName);
            Entity1List?.Clear();
            Entity11 = new global::EditorTest1.Entities.Entity1(ContentManagerName, false);
            Entity11.Name = "Entity11";
            Entity11.CreationSource = "Glue";
            // Not instantiating for FlatRedBall.TileGraphics.LayeredTileMap Map in Screens\GameScreen (Screen) because properties on the object prevent it
            
            
            PostInitialize();
            base.Initialize(addToManagers);
            this.Name = "GameScreen";
            if (addToManagers)
            {
                AddToManagers();
            }
        }
        public override void AddToManagers () 
        {
            mAccumulatedPausedTime = TimeManager.CurrentTime;
            mTimeScreenWasCreated = FlatRedBall.TimeManager.CurrentTime;
            InitializeFactoriesAndSorting();
            Entity11.AddToManagers(mLayer);
            var Map_gameplayLayer = Map.MapLayers.FindByName("GameplayLayer");
            if (Map_gameplayLayer != null)
            {
                Map_gameplayLayer.ForceUpdateDependencies();
                // What if the map's Z isn't 0? Add its Z to make up for that
                Map.Z = Map.Z - Map_gameplayLayer.Z;
            }
            CustomBeforeCreateEntitiesFromTiles(Map);
            FlatRedBall.TileEntities.TileEntityInstantiator.CreateEntitiesFrom(Map);
            base.AddToManagers();
            AddToManagersBottomUp();
            BeforeCustomInitialize?.Invoke();
            GlueControl.InstanceLogic.Self.ApplyEditorCommandsToNewEntity(null, GlueControl.CommandReceiver.GameElementTypeToGlueElement(this.GetType().FullName));
            CustomInitialize();
        }
        public override void Activity (bool firstTimeCalled) 
        {
            if (!IsPaused)
            {
                
                for (int i = Entity1List.Count - 1; i > -1; i--)
                {
                    if (i < Entity1List.Count)
                    {
                        // We do the extra if-check because activity could destroy any number of entities
                        Entity1List[i].Activity();
                    }
                }
            }
            else
            {
            }
            base.Activity(firstTimeCalled);
            if (!IsActivityFinished)
            {
                CustomActivity(firstTimeCalled);
            }
        }
        public override void ActivityEditMode () 
        {
            if (FlatRedBall.Screens.ScreenManager.IsInEditMode)
            {
                for (int i = FlatRedBall.SpriteManager.ManagedPositionedObjects.Count - 1; i > -1; i--)
                {
                    var item = FlatRedBall.SpriteManager.ManagedPositionedObjects[i];
                    if (item is FlatRedBall.Entities.IEntity entity)
                    {
                        entity.ActivityEditMode();
                    }
                }
                CustomActivityEditMode();
                base.ActivityEditMode();
            }
        }
        public override void Destroy () 
        {
            base.Destroy();
            Factories.Entity1Factory.Destroy();
            Factories.Entity1Factory.DefaultLayer = null;
            
            for (int i = Entity1List.Count - 1; i > -1; i--)
            {
                Entity1List[i].Destroy();
            }
            FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();
            CustomDestroy();
        }
        public virtual void PostInitialize () 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (!Entity1List.Contains(Entity11))
            {
                Entity1List.Add(Entity11);
            }
            if (Map!= null)
            {
            }
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public virtual void AddToManagersBottomUp () 
        {
            CameraSetup.ResetCamera(SpriteManager.Camera);
            AssignCustomVariables(false);
        }
        public virtual void RemoveFromManagers () 
        {
            for (int i = Entity1List.Count - 1; i > -1; i--)
            {
                Entity1List[i].Destroy();
            }
        }
        public virtual void AssignCustomVariables (bool callOnContainedElements) 
        {
            if (callOnContainedElements)
            {
                Entity11.AssignCustomVariables(true);
            }
            if (Map != null)
            {
            }
            
        }
        public virtual void ConvertToManuallyUpdated () 
        {
            for (int i = 0; i < Entity1List.Count; i++)
            {
                Entity1List[i].ConvertToManuallyUpdated();
            }
            if (Map != null)
            {
            }
        }
        public static void LoadStaticContent (string contentManagerName) 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (string.IsNullOrEmpty(contentManagerName))
            {
                throw new System.ArgumentException("contentManagerName cannot be empty or null");
            }
            #if DEBUG
            if (contentManagerName == FlatRedBall.FlatRedBallServices.GlobalContentManager)
            {
                HasBeenLoadedWithGlobalContentManager = true;
            }
            else if (HasBeenLoadedWithGlobalContentManager)
            {
                throw new System.Exception( "GameScreen has been loaded with a Global content manager, then loaded with a non-global.  This can lead to a lot of bugs");
            }
            #endif
            CustomLoadStaticContent(contentManagerName);
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public override void PauseThisScreen () 
        {
            StateInterpolationPlugin.TweenerManager.Self.Pause();
            base.PauseThisScreen();
        }
        public override void UnpauseThisScreen () 
        {
            StateInterpolationPlugin.TweenerManager.Self.Unpause();
            base.UnpauseThisScreen();
        }
        private void InitializeFactoriesAndSorting () 
        {
            Factories.Entity1Factory.Initialize(ContentManagerName);
            Factories.Entity1Factory.DefaultLayer = this.DefaultLayer;
            Factories.Entity1Factory.AddList(Entity1List);
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
        partial void CustomBeforeCreateEntitiesFromTiles(FlatRedBall.TileGraphics.LayeredTileMap layeredTileMap);
        partial void CustomActivityEditMode();
    }
}
