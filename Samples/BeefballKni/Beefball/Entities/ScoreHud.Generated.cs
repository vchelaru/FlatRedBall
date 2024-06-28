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
    public partial class ScoreHud : FlatRedBall.PositionedObject, FlatRedBall.Graphics.IDestroyable, FlatRedBall.Entities.IEntity
    {
        // This is made static so that static lazy-loaded content can access it.
        public static string ContentManagerName { get; set; }
        #if DEBUG
        private double mLastTimeCalledActivity=-1;
        #endif
        #if DEBUG
        public static bool HasBeenLoadedWithGlobalContentManager { get; private set; }= false;
        #endif
        static object mLockObject = new object();
        static System.Collections.Generic.List<string> mRegisteredUnloads = new System.Collections.Generic.List<string>();
        static System.Collections.Generic.List<string> LoadedContentManagers = new System.Collections.Generic.List<string>();
        
        private global::FlatRedBall.Graphics.Text Team1Score;
        private global::FlatRedBall.Graphics.Text Team2Score;
        private global::FlatRedBall.Graphics.Text Team1ScoreLabel;
        private global::FlatRedBall.Graphics.Text Team2ScoreLabel;
        public virtual int Score1
        {
            get
            {
                return int.Parse(Team1Score.DisplayText);
            }
            set
            {
                Team1Score.DisplayText = value.ToString();
            }
        }
        public virtual int Score2
        {
            get
            {
                return int.Parse(Team2Score.DisplayText);
            }
            set
            {
                Team2Score.DisplayText = value.ToString();
            }
        }
        protected FlatRedBall.Graphics.Layer LayerProvidedByContainer = null;
        public ScoreHud () 
        	: this(FlatRedBall.Screens.ScreenManager.CurrentScreen.ContentManagerName, true)
        {
        }
        public ScoreHud (string contentManagerName) 
        	: this(contentManagerName, true)
        {
        }
        public ScoreHud (string contentManagerName, bool addToManagers) 
        	: base()
        {
            ContentManagerName = contentManagerName;
            InitializeEntity(addToManagers);
        }
        protected virtual void InitializeEntity (bool addToManagers) 
        {
            LoadStaticContent(ContentManagerName);
            Team1Score = new global::FlatRedBall.Graphics.Text();
            Team1Score.Name = "Team1Score";
            Team1Score.CreationSource = "Glue";
            Team2Score = new global::FlatRedBall.Graphics.Text();
            Team2Score.Name = "Team2Score";
            Team2Score.CreationSource = "Glue";
            Team1ScoreLabel = new global::FlatRedBall.Graphics.Text();
            Team1ScoreLabel.Name = "Team1ScoreLabel";
            Team1ScoreLabel.CreationSource = "Glue";
            Team2ScoreLabel = new global::FlatRedBall.Graphics.Text();
            Team2ScoreLabel.Name = "Team2ScoreLabel";
            Team2ScoreLabel.CreationSource = "Glue";
            
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
            FlatRedBall.Graphics.TextManager.AddToLayer(Team1Score, LayerProvidedByContainer);
            if (Team1Score.Font != null)
            {
                Team1Score.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team2Score, LayerProvidedByContainer);
            if (Team2Score.Font != null)
            {
                Team2Score.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team1ScoreLabel, LayerProvidedByContainer);
            if (Team1ScoreLabel.Font != null)
            {
                Team1ScoreLabel.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team2ScoreLabel, LayerProvidedByContainer);
            if (Team2ScoreLabel.Font != null)
            {
                Team2ScoreLabel.SetPixelPerfectScale(LayerProvidedByContainer);
            }
        }
        public virtual void AddToManagers (FlatRedBall.Graphics.Layer layerToAddTo) 
        {
            LayerProvidedByContainer = layerToAddTo;
            FlatRedBall.SpriteManager.AddPositionedObject(this);
            FlatRedBall.Graphics.TextManager.AddToLayer(Team1Score, LayerProvidedByContainer);
            if (Team1Score.Font != null)
            {
                Team1Score.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team2Score, LayerProvidedByContainer);
            if (Team2Score.Font != null)
            {
                Team2Score.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team1ScoreLabel, LayerProvidedByContainer);
            if (Team1ScoreLabel.Font != null)
            {
                Team1ScoreLabel.SetPixelPerfectScale(LayerProvidedByContainer);
            }
            FlatRedBall.Graphics.TextManager.AddToLayer(Team2ScoreLabel, LayerProvidedByContainer);
            if (Team2ScoreLabel.Font != null)
            {
                Team2ScoreLabel.SetPixelPerfectScale(LayerProvidedByContainer);
            }
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
            
            if (Team1Score != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team1Score);
            }
            if (Team2Score != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team2Score);
            }
            if (Team1ScoreLabel != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team1ScoreLabel);
            }
            if (Team2ScoreLabel != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team2ScoreLabel);
            }
            CustomDestroy();
        }
        public virtual void PostInitialize () 
        {
            bool oldShapeManagerSuppressAdd = FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue;
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = true;
            if (Team1Score.Parent == null)
            {
                Team1Score.CopyAbsoluteToRelative();
                Team1Score.AttachTo(this, false);
            }
            Team1Score.DisplayText = "99";
            if (Team1Score.Parent == null)
            {
                Team1Score.X = -150f;
            }
            else
            {
                Team1Score.RelativeX = -150f;
            }
            if (Team1Score.Parent == null)
            {
                Team1Score.Y = 270f;
            }
            else
            {
                Team1Score.RelativeY = 270f;
            }
            if (Team2Score.Parent == null)
            {
                Team2Score.CopyAbsoluteToRelative();
                Team2Score.AttachTo(this, false);
            }
            Team2Score.DisplayText = "99";
            if (Team2Score.Parent == null)
            {
                Team2Score.X = 180f;
            }
            else
            {
                Team2Score.RelativeX = 180f;
            }
            if (Team2Score.Parent == null)
            {
                Team2Score.Y = 270f;
            }
            else
            {
                Team2Score.RelativeY = 270f;
            }
            if (Team1ScoreLabel.Parent == null)
            {
                Team1ScoreLabel.CopyAbsoluteToRelative();
                Team1ScoreLabel.AttachTo(this, false);
            }
            Team1ScoreLabel.DisplayText = "Team 1:";
            if (Team1ScoreLabel.Parent == null)
            {
                Team1ScoreLabel.X = -205f;
            }
            else
            {
                Team1ScoreLabel.RelativeX = -205f;
            }
            if (Team1ScoreLabel.Parent == null)
            {
                Team1ScoreLabel.Y = 270f;
            }
            else
            {
                Team1ScoreLabel.RelativeY = 270f;
            }
            if (Team2ScoreLabel.Parent == null)
            {
                Team2ScoreLabel.CopyAbsoluteToRelative();
                Team2ScoreLabel.AttachTo(this, false);
            }
            Team2ScoreLabel.DisplayText = "Team 2:";
            if (Team2ScoreLabel.Parent == null)
            {
                Team2ScoreLabel.X = 124f;
            }
            else
            {
                Team2ScoreLabel.RelativeX = 124f;
            }
            if (Team2ScoreLabel.Parent == null)
            {
                Team2ScoreLabel.Y = 270f;
            }
            else
            {
                Team2ScoreLabel.RelativeY = 270f;
            }
            FlatRedBall.Math.Geometry.ShapeManager.SuppressAddingOnVisibilityTrue = oldShapeManagerSuppressAdd;
        }
        public virtual void AddToManagersBottomUp (FlatRedBall.Graphics.Layer layerToAddTo) 
        {
            AssignCustomVariables(false);
        }
        public virtual void RemoveFromManagers () 
        {
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(this);
            if (Team1Score != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team1Score);
            }
            if (Team2Score != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team2Score);
            }
            if (Team1ScoreLabel != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team1ScoreLabel);
            }
            if (Team2ScoreLabel != null)
            {
                FlatRedBall.Graphics.TextManager.RemoveTextOneWay(Team2ScoreLabel);
            }
        }
        public virtual void AssignCustomVariables (bool callOnContainedElements) 
        {
            if (callOnContainedElements)
            {
            }
            Team1Score.DisplayText = "99";
            if (Team1Score.Parent == null)
            {
                Team1Score.X = -150f;
            }
            else
            {
                Team1Score.RelativeX = -150f;
            }
            if (Team1Score.Parent == null)
            {
                Team1Score.Y = 270f;
            }
            else
            {
                Team1Score.RelativeY = 270f;
            }
            Team2Score.DisplayText = "99";
            if (Team2Score.Parent == null)
            {
                Team2Score.X = 180f;
            }
            else
            {
                Team2Score.RelativeX = 180f;
            }
            if (Team2Score.Parent == null)
            {
                Team2Score.Y = 270f;
            }
            else
            {
                Team2Score.RelativeY = 270f;
            }
            Team1ScoreLabel.DisplayText = "Team 1:";
            if (Team1ScoreLabel.Parent == null)
            {
                Team1ScoreLabel.X = -205f;
            }
            else
            {
                Team1ScoreLabel.RelativeX = -205f;
            }
            if (Team1ScoreLabel.Parent == null)
            {
                Team1ScoreLabel.Y = 270f;
            }
            else
            {
                Team1ScoreLabel.RelativeY = 270f;
            }
            Team2ScoreLabel.DisplayText = "Team 2:";
            if (Team2ScoreLabel.Parent == null)
            {
                Team2ScoreLabel.X = 124f;
            }
            else
            {
                Team2ScoreLabel.RelativeX = 124f;
            }
            if (Team2ScoreLabel.Parent == null)
            {
                Team2ScoreLabel.Y = 270f;
            }
            else
            {
                Team2ScoreLabel.RelativeY = 270f;
            }
            Score1 = 0;
            Score2 = 0;
        }
        public virtual void ConvertToManuallyUpdated () 
        {
            this.ForceUpdateDependenciesDeep();
            FlatRedBall.SpriteManager.ConvertToManuallyUpdated(this);
            FlatRedBall.Graphics.TextManager.ConvertToManuallyUpdated(Team1Score);
            FlatRedBall.Graphics.TextManager.ConvertToManuallyUpdated(Team2Score);
            FlatRedBall.Graphics.TextManager.ConvertToManuallyUpdated(Team1ScoreLabel);
            FlatRedBall.Graphics.TextManager.ConvertToManuallyUpdated(Team2ScoreLabel);
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
                throw new System.Exception( "ScoreHud has been loaded with a Global content manager, then loaded with a non-global.  This can lead to a lot of bugs");
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
                        FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName).AddUnloadMethod("ScoreHudStaticUnload", UnloadStaticContent);
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
                        FlatRedBall.FlatRedBallServices.GetContentManagerByName(ContentManagerName).AddUnloadMethod("ScoreHudStaticUnload", UnloadStaticContent);
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
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(Team1Score);
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(Team2Score);
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(Team1ScoreLabel);
            FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(Team2ScoreLabel);
        }
        public virtual void MoveToLayer (FlatRedBall.Graphics.Layer layerToMoveTo) 
        {
            var layerToRemoveFrom = LayerProvidedByContainer;
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(Team1Score);
            }
            if (layerToMoveTo != null || !TextManager.AutomaticallyUpdatedTexts.Contains(Team1Score))
            {
                FlatRedBall.Graphics.TextManager.AddToLayer(Team1Score, layerToMoveTo);
            }
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(Team2Score);
            }
            if (layerToMoveTo != null || !TextManager.AutomaticallyUpdatedTexts.Contains(Team2Score))
            {
                FlatRedBall.Graphics.TextManager.AddToLayer(Team2Score, layerToMoveTo);
            }
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(Team1ScoreLabel);
            }
            if (layerToMoveTo != null || !TextManager.AutomaticallyUpdatedTexts.Contains(Team1ScoreLabel))
            {
                FlatRedBall.Graphics.TextManager.AddToLayer(Team1ScoreLabel, layerToMoveTo);
            }
            if (layerToRemoveFrom != null)
            {
                layerToRemoveFrom.Remove(Team2ScoreLabel);
            }
            if (layerToMoveTo != null || !TextManager.AutomaticallyUpdatedTexts.Contains(Team2ScoreLabel))
            {
                FlatRedBall.Graphics.TextManager.AddToLayer(Team2ScoreLabel, layerToMoveTo);
            }
            LayerProvidedByContainer = layerToMoveTo;
        }
        partial void CustomActivityEditMode();
    }
}
