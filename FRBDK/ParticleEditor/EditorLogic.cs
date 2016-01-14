using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Particle;
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Gui;
using EditorObjects;
using EditorObjects.Gui;
using FlatRedBall.Math.Geometry;

#if FRB_MDX
using Microsoft.DirectX.DirectInput;

#else
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using FlatRedBall.Graphics;
#endif

namespace ParticleEditor
{
    public class EditorLogic
    {
        #region Fields


        private ReactiveHud mReactiveHud = new ReactiveHud();

        private Sprite mSceneSpriteOver;

        private PositionedObject mGrabbedObject;

        bool arrayGrabbed = false;

        #endregion

        #region Properties



        public ReactiveHud ReactiveHud
        {
            get { return mReactiveHud; }
        }

        public Sprite SceneSpriteOver
        {
            get { return mSceneSpriteOver; }
        }

        #endregion

        #region Methods

        #region Constructor

        public EditorLogic()
        {
            PositionedObjectMover.AllowZMovement = true;

            Camera.Main.UsePixelCoordinates();
        }

        #endregion

        #region Public Methods

        public void DeleteCurrentEmitter()
        {
            if (AppState.Self.CurrentEmitter != null)
            {


                SpriteManager.RemovePositionedObject(AppState.Self.CurrentEmitter);

                AppState.Self.CurrentEmitter = null;
            }
        }

        public void Update()
        {
            foreach (Emitter e in EditorData.Emitters)
            {
                e.EmissionBoundary.Visible = false;
            }
            if (AppState.Self.CurrentEmitter != null)
            {
                AppState.Self.CurrentEmitter.EmissionBoundary.Visible = AppState.Self.CurrentEmitter.BoundedEmission;
            }

            mReactiveHud.Update();

            GetObjectsOver();

            KeyboardControl();

            MouseCameraControl();

            EditorObjects.CameraMethods.KeyboardCameraControl(SpriteManager.Camera);

            MouseControlOverObjects();
        }

        private static void MouseCameraControl()
        {
            if (GuiData.ToolsWindow.IsDownZ)
            {
                EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);
                SpriteManager.Camera.CameraCullMode = CameraCullMode.UnrotatedDownZ;
                SpriteManager.OrderedSortType = SortType.Z;
            }
            else
            {
                EditorObjects.CameraMethods.MouseCameraControl3D(SpriteManager.Camera);
                SpriteManager.Camera.CameraCullMode = CameraCullMode.None;
                SpriteManager.OrderedSortType = SortType.DistanceAlongForwardVector;
            }
        }

        #endregion

        #region Private Methods

        private void GetObjectsOver()
        {
            Cursor cursor = GuiManager.Cursor;

                if (EditorData.Scene != null)
                {
                    mSceneSpriteOver = cursor.GetSpriteOver(EditorData.Scene.Sprites);
                }
                else
                {
                    mSceneSpriteOver = null;
                }
            
        }


        private void KeyboardControl()
        {
            if (InputManager.ReceivingInput != null) return;

            #region Escape for exit
            if (InputManager.Keyboard.KeyPushed(Keys.Escape))
            {
                OkCancelWindow ocw = GuiManager.ShowOkCancelWindow("Exit ParticleEditor?  Unsaved data will be lost", "Exit?");
                ocw.OkClick += new GuiMessage(GuiData.Messages.ExitOk);
            }
            #endregion

            #region press space to emit current emitter
            if (InputManager.Keyboard.KeyPushed(Keys.Space) && AppState.Self.CurrentEmitter != null)
            {
                AppState.Self.CurrentEmitter.Emit(null);

            }
            #endregion

            #region Ctrl + C for copying emitter
            if ((InputManager.Keyboard.KeyDown(Keys.LeftControl) || InputManager.Keyboard.KeyDown(Keys.RightControl)) && InputManager.Keyboard.KeyPushed(Keys.C))
                EditorData.CopyCurrentEmitter();
            #endregion

            #region pressing C to clear all particles
            else if (InputManager.Keyboard.KeyPushed(Keys.C))
            {
                GuiData.ActivityWindow.ClearAllParticles();
            }
            #endregion

            #region pressing delete to delete the current emitter
            if (InputManager.Keyboard.KeyPushed(Keys.Delete))
            {
                DeleteCurrentEmitter();
            }
            #endregion

            if (InputManager.Keyboard.KeyPushed(Keys.A))
                GuiData.ToolsWindow.attachObject.Press();
            if (InputManager.Keyboard.KeyPushed(Keys.M))
                GuiData.ToolsWindow.moveObject.Press();

        }


        private void MouseControlOverObjects()
        {
            Cursor cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null)
            {
                return;
            }

            GuiManager.Cursor.StaticPosition = false;

            #region moving mEditorLogic.CurrentEmitter and its boundary
            if (AppState.Self.CurrentEmitter != null)
            {
                #region see if we pushed the mouse buttons  on a marker
                if (cursor.PrimaryPush || cursor.SecondaryPush)
                {
                    if (cursor.WindowOver == null)
                    {
                        if (cursor.IsOn(EditorData.EditorLogic.ReactiveHud.CurrentEmitterMarker))
                            mGrabbedObject = AppState.Self.CurrentEmitter;
                        else if (AppState.Self.CurrentEmitter.BoundedEmission)
                        {
                            foreach (AxisAlignedRectangle corner in EditorData.EditorLogic.ReactiveHud.CurrentEmitterBoundaryCorners)
                            {
                                if (cursor.IsOn<AxisAlignedRectangle>(corner))
                                {
                                    mGrabbedObject = corner;
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion

                #region release emitter
                if (cursor.PrimaryClick || cursor.SecondaryClick)
                {
                    mGrabbedObject = null;
                }
                #endregion

                #region move the emitter or its boundary if we have one grabbed
                if (mGrabbedObject != null && GuiData.ToolsWindow.moveObject.IsPressed)
                {
                    PositionedObjectMover.MouseMoveObject(mGrabbedObject, MovementStyle.Hierarchy);
                    if (mGrabbedObject is AxisAlignedRectangle)
                    {
                        for (int i = 0; i < EditorData.EditorLogic.ReactiveHud.CurrentEmitterBoundaryCorners.Count; i++)
                        {
                            if (EditorData.EditorLogic.ReactiveHud.CurrentEmitterBoundaryCorners[i].Equals(mGrabbedObject))
                            {
                                AppState.Self.CurrentEmitter.EmissionBoundary.SetPoint(i, mGrabbedObject.X - AppState.Self.CurrentEmitter.X, mGrabbedObject.Y - AppState.Self.CurrentEmitter.Y);
                                if (i == 0)
                                {
                                    AppState.Self.CurrentEmitter.EmissionBoundary.SetPoint(AppState.Self.CurrentEmitter.EmissionBoundary.Points.Count - 1,
                                        mGrabbedObject.X - AppState.Self.CurrentEmitter.X, mGrabbedObject.Y - AppState.Self.CurrentEmitter.Y);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            #region scnFileArray  logic for moving our scene
            if (EditorData.Scene != null && mGrabbedObject == null)
            {
                #region grabbing a sprite
                if (cursor.PrimaryPush)
                {
                    if (cursor.WindowOver == null)
                    {
                        if (cursor.GetSpriteOver(EditorData.Scene.Sprites) != null)
                            arrayGrabbed = true;
                    }
                }
                #endregion

                #region release sprite
                if (cursor.PrimaryClick)
                    arrayGrabbed = false;

                #endregion

                #region if an array is grabbed, move the entire scene by the sprite's velocity
                if (arrayGrabbed && GuiData.ToolsWindow.moveObject.IsPressed)
                {
                    EditorData.Scene.Shift(
                        new Vector3(
                            cursor.ActualXVelocityAt(0),
                            cursor.ActualYVelocityAt(0),
                            0));
                }
                #endregion
            }
            #endregion

            if (GuiData.ToolsWindow.attachObject.IsPressed && cursor.PrimaryClick)
                TryAttachEmitterToSprite();
        }


        private void TryAttachEmitterToSprite()
        {
            if (SceneSpriteOver != null)
            {
                GuiData.ToolsWindow.attachObject.Unpress();
                GuiData.ToolsWindow.detachObject.Enabled = true;
                AppState.Self.CurrentEmitter.AttachTo(GuiManager.Cursor.GetSpriteOver(EditorData.Scene.Sprites), true);

                GuiData.Messages.updateGUIOnEmitterSelect(); // updates relative
            }
        }

        #endregion


        #endregion
    }
}
