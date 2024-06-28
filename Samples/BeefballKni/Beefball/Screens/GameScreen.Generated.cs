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
namespace Beefball.Screens
{
    public partial class GameScreen : FlatRedBall.Screens.Screen
    {
        #if DEBUG
        public static bool HasBeenLoadedWithGlobalContentManager { get; private set; }= false;
        #endif
        
        protected global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.PlayerBall> PlayerBallList = new global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.PlayerBall>();
        private global::Beefball.Entities.PlayerBall PlayerBall1;
        private global::Beefball.Entities.PlayerBall PlayerBall2;
        protected global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.Puck> PuckList = new global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.Puck>();
        private global::Beefball.Entities.Puck Puck1;
        protected global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.Goal> GoalList = new global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.Goal>();
        private global::Beefball.Entities.Goal LeftGoal;
        private global::Beefball.Entities.Goal RightGoal;
        protected global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.ScoreHud> ScoreHudList = new global::FlatRedBall.Math.PositionedObjectList<global::Beefball.Entities.ScoreHud>();
        private global::Beefball.Entities.ScoreHud ScoreHud1;
        protected global::FlatRedBall.Math.Geometry.ShapeCollection mWalls;
        public global::FlatRedBall.Math.Geometry.ShapeCollection Walls
        {
            get
            {
                return mWalls;
            }
            private set
            {
                mWalls = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall1;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall1
        {
            get
            {
                return mWall1;
            }
            private set
            {
                mWall1 = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall2;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall2
        {
            get
            {
                return mWall2;
            }
            private set
            {
                mWall2 = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall3;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall3
        {
            get
            {
                return mWall3;
            }
            private set
            {
                mWall3 = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall4;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall4
        {
            get
            {
                return mWall4;
            }
            private set
            {
                mWall4 = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall5;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall5
        {
            get
            {
                return mWall5;
            }
            private set
            {
                mWall5 = value;
            }
        }
        private global::FlatRedBall.Math.Geometry.AxisAlignedRectangle mWall6;
        public global::FlatRedBall.Math.Geometry.AxisAlignedRectangle Wall6
        {
            get
            {
                return mWall6;
            }
            private set
            {
                mWall6 = value;
            }
        }
        private global::FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship<Entities.PlayerBall> PlayerBallVsWalls;
        private global::FlatRedBall.Math.Collision.ListVsListRelationship<Entities.PlayerBall, Entities.Puck> PlayerBallVsPuck;
        private global::FlatRedBall.Math.Collision.ListVsListRelationship<Entities.PlayerBall, Entities.PlayerBall> PlayerBallVsPlayerBall;
        private global::FlatRedBall.Math.Collision.ListVsListRelationship<Entities.PlayerBall, Entities.Goal> PlayerBallVsGoal;
        private global::FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship<Entities.Puck> PuckVsWalls;
        private global::FlatRedBall.Math.Collision.ListVsListRelationship<Entities.Puck, Entities.Goal> PuckVsGoal;
        public event System.Action<Entities.Puck, Entities.Goal> PuckVsGoalCollided;
        public GameScreen () 
        	: this ("GameScreen")
        {
        }
        public GameScreen (string contentManagerName) 
        	: base (contentManagerName)
        {
            PlayerBallList.Name = "PlayerBallList";
            PuckList.Name = "PuckList";
            GoalList.Name = "GoalList";
            ScoreHudList.Name = "ScoreHudList";
            mWalls = new global::FlatRedBall.Math.Geometry.ShapeCollection();
            mWalls.Name = "Walls";
        }
        public override void Initialize (bool addToManagers) 
        {
            LoadStaticContent(ContentManagerName);
            PlayerBallList?.Clear();
            PlayerBall1 = new global::Beefball.Entities.PlayerBall(ContentManagerName, false);
            PlayerBall1.Name = "PlayerBall1";
            PlayerBall1.CreationSource = "Glue";
            PlayerBall2 = new global::Beefball.Entities.PlayerBall(ContentManagerName, false);
            PlayerBall2.Name = "PlayerBall2";
            PlayerBall2.CreationSource = "Glue";
            PuckList?.Clear();
            Puck1 = new global::Beefball.Entities.Puck(ContentManagerName, false);
            Puck1.Name = "Puck1";
            Puck1.CreationSource = "Glue";
            GoalList?.Clear();
            LeftGoal = new global::Beefball.Entities.Goal(ContentManagerName, false);
            LeftGoal.Name = "LeftGoal";
            LeftGoal.CreationSource = "Glue";
            RightGoal = new global::Beefball.Entities.Goal(ContentManagerName, false);
            RightGoal.Name = "RightGoal";
            RightGoal.CreationSource = "Glue";
            ScoreHudList?.Clear();
            ScoreHud1 = new global::Beefball.Entities.ScoreHud(ContentManagerName, false);
            ScoreHud1.Name = "ScoreHud1";
            ScoreHud1.CreationSource = "Glue";
            mWall1 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall1.Name = "Wall1";
            mWall1.CreationSource = "Glue";
            mWall2 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall2.Name = "Wall2";
            mWall2.CreationSource = "Glue";
            mWall3 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall3.Name = "Wall3";
            mWall3.CreationSource = "Glue";
            mWall4 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall4.Name = "Wall4";
            mWall4.CreationSource = "Glue";
            mWall5 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall5.Name = "Wall5";
            mWall5.CreationSource = "Glue";
            mWall6 = new global::FlatRedBall.Math.Geometry.AxisAlignedRectangle();
            mWall6.Name = "Wall6";
            mWall6.CreationSource = "Glue";
            FlatRedBall.Math.Collision.CollisionManager.Self.BeforeCollision += HandleBeforeCollisionGenerated;
            PlayerBallVsWalls = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerBallList, Walls);
PlayerBallVsWalls.Name = "PlayerBallVsWalls";
PlayerBallVsWalls.SetBounceCollision(0f, 1f, 1f);
PlayerBallVsWalls.CollisionOccurred += (first, second) =>
{
}
;

            PlayerBallVsPuck = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerBallList, PuckList);
PlayerBallVsPuck.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.All;
PlayerBallVsPuck.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.PreventDoubleChecksPerFrame;
PlayerBallVsPuck.Name = "PlayerBallVsPuck";
PlayerBallVsPuck.SetBounceCollision(1f, 0.3f, 1f);
PlayerBallVsPuck.CollisionOccurred += (first, second) =>
{
}
;

            PlayerBallVsPlayerBall = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerBallList, PlayerBallList);
PlayerBallVsPlayerBall.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.All;
PlayerBallVsPlayerBall.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.PreventDoubleChecksPerFrame;
PlayerBallVsPlayerBall.Name = "PlayerBallVsPlayerBall";
PlayerBallVsPlayerBall.SetBounceCollision(1f, 1f, 1f);
PlayerBallVsPlayerBall.CollisionOccurred += (first, second) =>
{
}
;

            PlayerBallVsGoal = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PlayerBallList, GoalList);
PlayerBallVsGoal.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.All;
PlayerBallVsGoal.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.PreventDoubleChecksPerFrame;
PlayerBallVsGoal.Name = "PlayerBallVsGoal";
PlayerBallVsGoal.SetBounceCollision(0f, 1f, 1f);
PlayerBallVsGoal.CollisionOccurred += (first, second) =>
{
}
;

            PuckVsWalls = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PuckList, Walls);
PuckVsWalls.Name = "PuckVsWalls";
PuckVsWalls.SetBounceCollision(0f, 1f, 1f);
PuckVsWalls.CollisionOccurred += (first, second) =>
{
}
;

            PuckVsGoal = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(PuckList, GoalList);
PuckVsGoal.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.All;
PuckVsGoal.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.PreventDoubleChecksPerFrame;
PuckVsGoal.Name = "PuckVsGoal";
PuckVsGoal.CollisionOccurred += (first, second) =>
{
}
;

            
            
            PostInitialize();
            base.Initialize(addToManagers);
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
            PlayerBall1.AddToManagers(mLayer);
            PlayerBall2.AddToManagers(mLayer);
            Puck1.AddToManagers(mLayer);
            LeftGoal.AddToManagers(mLayer);
            RightGoal.AddToManagers(mLayer);
            ScoreHud1.AddToManagers(mLayer);
            mWalls.AddToManagers();
            base.AddToManagers();
            AddToManagersBottomUp();
            BeforeCustomInitialize?.Invoke();
            CustomInitialize();
        }
        public override void Activity (bool firstTimeCalled) 
        {
            if (!IsPaused)
            {
                
                for (int i = PlayerBallList.Count - 1; i > -1; i--)
                {
                    if (i < PlayerBallList.Count)
                    {
                        // We do the extra if-check because activity could destroy any number of entities
                        PlayerBallList[i].Activity();
                    }
                }
                for (int i = PuckList.Count - 1; i > -1; i--)
                {
                    if (i < PuckList.Count)
                    {
                        // We do the extra if-check because activity could destroy any number of entities
                        PuckList[i].Activity();
                    }
                }
                for (int i = GoalList.Count - 1; i > -1; i--)
                {
                    if (i < GoalList.Count)
                    {
                        // We do the extra if-check because activity could destroy any number of entities
                        GoalList[i].Activity();
                    }
                }
                for (int i = ScoreHudList.Count - 1; i > -1; i--)
                {
                    if (i < ScoreHudList.Count)
                    {
                        // We do the extra if-check because activity could destroy any number of entities
                        ScoreHudList[i].Activity();
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
                foreach (var item in FlatRedBall.SpriteManager.ManagedPositionedObjects)
                {
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
            Factories.PlayerBallFactory.Destroy();
            Factories.PuckFactory.Destroy();
            Factories.GoalFactory.Destroy();
            Factories.ScoreHudFactory.Destroy();
            
            for (int i = PlayerBallList.Count - 1; i > -1; i--)
            {
                PlayerBallList[i].Destroy();
            }
            for (int i = PuckList.Count - 1; i > -1; i--)
            {
                PuckList[i].Destroy();
            }
            for (int i = GoalList.Count - 1; i > -1; i--)
            {
                GoalList[i].Destroy();
            }
            for (int i = ScoreHudList.Count - 1; i > -1; i--)
            {
                ScoreHudList[i].Destroy();
            }
            if (Walls != null)
            {
                Walls.RemoveFromManagers(ContentManagerName != "Global");
            }
            PuckVsGoal.CollisionOccurred -= OnPuckVsGoalCollidedTunnel;
            FlatRedBall.Math.Collision.CollisionManager.Self.BeforeCollision -= HandleBeforeCollisionGenerated;
            FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();
            CustomDestroy();
        }
        public virtual void PostInitialize () 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            PuckVsGoal.CollisionOccurred += OnPuckVsGoalCollided;
            PuckVsGoal.CollisionOccurred += OnPuckVsGoalCollidedTunnel;
            if (!PlayerBallList.Contains(PlayerBall1))
            {
                PlayerBallList.Add(PlayerBall1);
            }
            if (PlayerBall1.Parent == null)
            {
                PlayerBall1.X = -180f;
            }
            else
            {
                PlayerBall1.RelativeX = -180f;
            }
            if (!PlayerBallList.Contains(PlayerBall2))
            {
                PlayerBallList.Add(PlayerBall2);
            }
            PlayerBall2.CircleInstanceColor = Microsoft.Xna.Framework.Color.Cyan;
            PlayerBall2.CooldownCircleColor = Microsoft.Xna.Framework.Color.Cyan;
            if (PlayerBall2.Parent == null)
            {
                PlayerBall2.X = 180f;
            }
            else
            {
                PlayerBall2.RelativeX = 180f;
            }
            if (!PuckList.Contains(Puck1))
            {
                PuckList.Add(Puck1);
            }
            if (!GoalList.Contains(LeftGoal))
            {
                GoalList.Add(LeftGoal);
            }
            if (LeftGoal.Parent == null)
            {
                LeftGoal.X = -410f;
            }
            else
            {
                LeftGoal.RelativeX = -410f;
            }
            if (!GoalList.Contains(RightGoal))
            {
                GoalList.Add(RightGoal);
            }
            if (RightGoal.Parent == null)
            {
                RightGoal.X = 410f;
            }
            else
            {
                RightGoal.RelativeX = 410f;
            }
            if (!ScoreHudList.Contains(ScoreHud1))
            {
                ScoreHudList.Add(ScoreHud1);
            }
            if (!Walls.Contains(Wall1))
            {
                Walls.Add(Wall1);
            }
            if (Wall1.Parent == null)
            {
                Wall1.Y = 300f;
            }
            else
            {
                Wall1.RelativeY = 300f;
            }
            Wall1.Width = 800f;
            Wall1.Height = 30f;
            if (!Walls.Contains(Wall2))
            {
                Walls.Add(Wall2);
            }
            if (Wall2.Parent == null)
            {
                Wall2.Y = -300f;
            }
            else
            {
                Wall2.RelativeY = -300f;
            }
            Wall2.Width = 800f;
            Wall2.Height = 30f;
            if (!Walls.Contains(Wall3))
            {
                Walls.Add(Wall3);
            }
            if (Wall3.Parent == null)
            {
                Wall3.X = -400f;
            }
            else
            {
                Wall3.RelativeX = -400f;
            }
            if (Wall3.Parent == null)
            {
                Wall3.Y = 200f;
            }
            else
            {
                Wall3.RelativeY = 200f;
            }
            Wall3.Width = 30f;
            Wall3.Height = 200f;
            if (!Walls.Contains(Wall4))
            {
                Walls.Add(Wall4);
            }
            if (Wall4.Parent == null)
            {
                Wall4.X = 400f;
            }
            else
            {
                Wall4.RelativeX = 400f;
            }
            if (Wall4.Parent == null)
            {
                Wall4.Y = 200f;
            }
            else
            {
                Wall4.RelativeY = 200f;
            }
            Wall4.Width = 30f;
            Wall4.Height = 200f;
            if (!Walls.Contains(Wall5))
            {
                Walls.Add(Wall5);
            }
            if (Wall5.Parent == null)
            {
                Wall5.X = -400f;
            }
            else
            {
                Wall5.RelativeX = -400f;
            }
            if (Wall5.Parent == null)
            {
                Wall5.Y = -200f;
            }
            else
            {
                Wall5.RelativeY = -200f;
            }
            Wall5.Width = 30f;
            Wall5.Height = 200f;
            if (!Walls.Contains(Wall6))
            {
                Walls.Add(Wall6);
            }
            if (Wall6.Parent == null)
            {
                Wall6.X = 400f;
            }
            else
            {
                Wall6.RelativeX = 400f;
            }
            if (Wall6.Parent == null)
            {
                Wall6.Y = -200f;
            }
            else
            {
                Wall6.RelativeY = -200f;
            }
            Wall6.Width = 30f;
            Wall6.Height = 200f;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public virtual void AddToManagersBottomUp () 
        {
            CameraSetup.ResetCamera(SpriteManager.Camera);
            AssignCustomVariables(false);
        }
        public virtual void RemoveFromManagers () 
        {
            for (int i = PlayerBallList.Count - 1; i > -1; i--)
            {
                PlayerBallList[i].Destroy();
            }
            for (int i = PuckList.Count - 1; i > -1; i--)
            {
                PuckList[i].Destroy();
            }
            for (int i = GoalList.Count - 1; i > -1; i--)
            {
                GoalList[i].Destroy();
            }
            for (int i = ScoreHudList.Count - 1; i > -1; i--)
            {
                ScoreHudList[i].Destroy();
            }
            if (Walls != null)
            {
                Walls.RemoveFromManagers(false);
            }
        }
        public virtual void AssignCustomVariables (bool callOnContainedElements) 
        {
            if (callOnContainedElements)
            {
                PlayerBall1.AssignCustomVariables(true);
                PlayerBall2.AssignCustomVariables(true);
                Puck1.AssignCustomVariables(true);
                LeftGoal.AssignCustomVariables(true);
                RightGoal.AssignCustomVariables(true);
                ScoreHud1.AssignCustomVariables(true);
            }
            if (PlayerBall1.Parent == null)
            {
                PlayerBall1.X = -180f;
            }
            else
            {
                PlayerBall1.RelativeX = -180f;
            }
            PlayerBall2.CircleInstanceColor = Microsoft.Xna.Framework.Color.Cyan;
            PlayerBall2.CooldownCircleColor = Microsoft.Xna.Framework.Color.Cyan;
            if (PlayerBall2.Parent == null)
            {
                PlayerBall2.X = 180f;
            }
            else
            {
                PlayerBall2.RelativeX = 180f;
            }
            if (LeftGoal.Parent == null)
            {
                LeftGoal.X = -410f;
            }
            else
            {
                LeftGoal.RelativeX = -410f;
            }
            if (RightGoal.Parent == null)
            {
                RightGoal.X = 410f;
            }
            else
            {
                RightGoal.RelativeX = 410f;
            }
            if (Wall1.Parent == null)
            {
                Wall1.Y = 300f;
            }
            else
            {
                Wall1.RelativeY = 300f;
            }
            Wall1.Width = 800f;
            Wall1.Height = 30f;
            if (Wall2.Parent == null)
            {
                Wall2.Y = -300f;
            }
            else
            {
                Wall2.RelativeY = -300f;
            }
            Wall2.Width = 800f;
            Wall2.Height = 30f;
            if (Wall3.Parent == null)
            {
                Wall3.X = -400f;
            }
            else
            {
                Wall3.RelativeX = -400f;
            }
            if (Wall3.Parent == null)
            {
                Wall3.Y = 200f;
            }
            else
            {
                Wall3.RelativeY = 200f;
            }
            Wall3.Width = 30f;
            Wall3.Height = 200f;
            if (Wall4.Parent == null)
            {
                Wall4.X = 400f;
            }
            else
            {
                Wall4.RelativeX = 400f;
            }
            if (Wall4.Parent == null)
            {
                Wall4.Y = 200f;
            }
            else
            {
                Wall4.RelativeY = 200f;
            }
            Wall4.Width = 30f;
            Wall4.Height = 200f;
            if (Wall5.Parent == null)
            {
                Wall5.X = -400f;
            }
            else
            {
                Wall5.RelativeX = -400f;
            }
            if (Wall5.Parent == null)
            {
                Wall5.Y = -200f;
            }
            else
            {
                Wall5.RelativeY = -200f;
            }
            Wall5.Width = 30f;
            Wall5.Height = 200f;
            if (Wall6.Parent == null)
            {
                Wall6.X = 400f;
            }
            else
            {
                Wall6.RelativeX = 400f;
            }
            if (Wall6.Parent == null)
            {
                Wall6.Y = -200f;
            }
            else
            {
                Wall6.RelativeY = -200f;
            }
            Wall6.Width = 30f;
            Wall6.Height = 200f;
        }
        public virtual void ConvertToManuallyUpdated () 
        {
            for (int i = 0; i < PlayerBallList.Count; i++)
            {
                PlayerBallList[i].ConvertToManuallyUpdated();
            }
            for (int i = 0; i < PuckList.Count; i++)
            {
                PuckList[i].ConvertToManuallyUpdated();
            }
            for (int i = 0; i < GoalList.Count; i++)
            {
                GoalList[i].ConvertToManuallyUpdated();
            }
            for (int i = 0; i < ScoreHudList.Count; i++)
            {
                ScoreHudList[i].ConvertToManuallyUpdated();
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
            //StateInterpolationPlugin.TweenerManager.Self.Pause();
            base.PauseThisScreen();
        }
        public override void UnpauseThisScreen () 
        {
            //StateInterpolationPlugin.TweenerManager.Self.Unpause();
            base.UnpauseThisScreen();
        }
        private void InitializeFactoriesAndSorting () 
        {
            Factories.PlayerBallFactory.Initialize(ContentManagerName);
            Factories.PuckFactory.Initialize(ContentManagerName);
            Factories.GoalFactory.Initialize(ContentManagerName);
            Factories.ScoreHudFactory.Initialize(ContentManagerName);
            Factories.PlayerBallFactory.AddList(PlayerBallList);
            Factories.PuckFactory.AddList(PuckList);
            Factories.GoalFactory.AddList(GoalList);
            Factories.ScoreHudFactory.AddList(ScoreHudList);
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
        void HandleBeforeCollisionGenerated () 
        {
            for (int i = 0; i < PlayerBallList.Count; i++)
            {
                var item = PlayerBallList[i];
                item.LastFrameItemsCollidedAgainst.Clear();
                foreach (var name in item.ItemsCollidedAgainst)
                {
                    item.LastFrameItemsCollidedAgainst.Add(name);
                }
                item.ItemsCollidedAgainst.Clear();
                item.LastFrameObjectsCollidedAgainst.Clear();
                foreach (var name in item.ObjectsCollidedAgainst)
                {
                    item.LastFrameObjectsCollidedAgainst.Add(name);
                }
                item.ObjectsCollidedAgainst.Clear();
            }
            for (int i = 0; i < PuckList.Count; i++)
            {
                var item = PuckList[i];
                item.LastFrameItemsCollidedAgainst.Clear();
                foreach (var name in item.ItemsCollidedAgainst)
                {
                    item.LastFrameItemsCollidedAgainst.Add(name);
                }
                item.ItemsCollidedAgainst.Clear();
                item.LastFrameObjectsCollidedAgainst.Clear();
                foreach (var name in item.ObjectsCollidedAgainst)
                {
                    item.LastFrameObjectsCollidedAgainst.Add(name);
                }
                item.ObjectsCollidedAgainst.Clear();
            }
            for (int i = 0; i < GoalList.Count; i++)
            {
                var item = GoalList[i];
                item.LastFrameItemsCollidedAgainst.Clear();
                foreach (var name in item.ItemsCollidedAgainst)
                {
                    item.LastFrameItemsCollidedAgainst.Add(name);
                }
                item.ItemsCollidedAgainst.Clear();
                item.LastFrameObjectsCollidedAgainst.Clear();
                foreach (var name in item.ObjectsCollidedAgainst)
                {
                    item.LastFrameObjectsCollidedAgainst.Add(name);
                }
                item.ObjectsCollidedAgainst.Clear();
            }
            mWalls.LastFrameItemsCollidedAgainst.Clear();
            foreach (var name in mWalls.ItemsCollidedAgainst)
            {
                mWalls.LastFrameItemsCollidedAgainst.Add(name);
            }
            mWalls.ItemsCollidedAgainst.Clear();
            mWalls.LastFrameObjectsCollidedAgainst.Clear();
            foreach (var name in mWalls.ObjectsCollidedAgainst)
            {
                mWalls.LastFrameObjectsCollidedAgainst.Add(name);
            }
            mWalls.ObjectsCollidedAgainst.Clear();
        }
        partial void CustomActivityEditMode();
    }
}
