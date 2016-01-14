using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using ArrowDataConversion;
using EditorObjects;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using System.Windows.Input;
using FlatRedBallWpf;

namespace FlatRedBall.Arrow.GlueView
{
    public class EditingManager : Singleton<EditingManager>
    {
        #region Fields

        ElementRuntime mGrabbedElementRuntime;
        ArrowElementInstanceToNosConverter mElementToNos = new ArrowElementInstanceToNosConverter();
        FlatRedBallControl mXnaControl;

        CameraController mCameraController;

        #endregion

        #region Methods

        public void Initialize(FlatRedBallControl xnaControl)
        {
            mXnaControl = xnaControl;

            mCameraController = new CameraController();
        }

        public void Activity()
        {
            FlatRedBall.Gui.Cursor cursor = GuiManager.Cursor;
            ElementRuntime elementRuntime = ArrowState.Self.CurrentContainedElementRuntime;

            mCameraController.Activity();

            TryHandleCursorPush(cursor);

            HandleGrabbedMovement(cursor);

            if (cursor.PrimaryClick)
            {
                if(IsCursorInWindow())
                {
                    RecordChanges();
                    PropertyGridManager.Self.UpdateToSelectedInstance();
                }
                mGrabbedElementRuntime = null;

            }
        }

        private void HandleGrabbedMovement(FlatRedBall.Gui.Cursor cursor)
        {
            // Don't do any movement on a push because the window could be gaining focus
            if (!cursor.PrimaryPush && mGrabbedElementRuntime != null)
            {
                if (mGrabbedElementRuntime.DirectObjectReference != null)
                {
                    if (mGrabbedElementRuntime.DirectObjectReference is PositionedObject)
                    {
                        PositionedObjectMover.MouseMoveObject(mGrabbedElementRuntime.DirectObjectReference as PositionedObject, MovementStyle.Hierarchy);
                    }
                }
                else
                {
                    PositionedObjectMover.MouseMoveObject(mGrabbedElementRuntime, MovementStyle.Hierarchy);
                }
            }
        }

        private bool TryHandleCursorPush(FlatRedBall.Gui.Cursor cursor)
        {
            bool didHandle = false;
            if (cursor.PrimaryPush && cursor.IsInWindow() && mXnaControl.IsPanelFocused)
            {
                var before = mGrabbedElementRuntime;
                mGrabbedElementRuntime = GetElementRuntimeOver(cursor);
                didHandle = true;
                if (before != mGrabbedElementRuntime)
                {
                    ArrowState.Self.CurrentContainedElementRuntime= mGrabbedElementRuntime;
                    // Need to do something here to allow selection to occur
                    //ArrowState.Self.CurrentContainedElementRuntime = mGrabbedElementRuntime;
                }
                else if (mGrabbedElementRuntime == null)
                {
                    ArrowState.Self.CurrentContainedElementRuntime = null;
                }
            }
            return didHandle;
        }

        private ElementRuntime GetElementRuntimeOver(FlatRedBall.Gui.Cursor cursor)
        {
            ElementRuntime toReturn = null;

            var currentElementRuntime = ArrowState.Self.CurrentContainedElementRuntime;

            if (currentElementRuntime != null && currentElementRuntime.HasCursorOver(cursor))
            {
                toReturn = currentElementRuntime;
            }

            if (toReturn == null && ArrowState.Self.CurrentElementRuntime != null)
            {
                foreach (var containedElementRuntime in ArrowState.Self.CurrentElementRuntime.ContainedElements)
                {
                    if (containedElementRuntime != currentElementRuntime && containedElementRuntime.HasCursorOver(cursor))
                    {
                        toReturn = containedElementRuntime;
                        break;
                    }
                }
            }

            if (toReturn == null)
            {


            }

            return toReturn;
        }

        private bool IsCursorInWindow()
        {
            FlatRedBall.Gui.Cursor cursor = GuiManager.Cursor;
            int cursorScreenX = cursor.ScreenX;
            int cursorScreenY = cursor.ScreenY;

            return cursorScreenX > 0 && cursorScreenY > 0 &&
                cursorScreenX < mXnaControl.ActualWidth &&
                cursorScreenY < mXnaControl.ActualHeight;
        }

        private void RecordChanges()
        {
            if (mGrabbedElementRuntime != null)
            {
                var runtime = mGrabbedElementRuntime;

                object instance = ArrowCommands.Self.UpdateInstanceValuesFromRuntime(runtime);

                ArrowCommands.Self.File.SaveProject();
                // Don't generate a new one, this screws up the ElementRuntimes
                // shown by GlueView.  Instead, just modify the existing:
                // ArrowCommands.Self.File.GenerateGlux();

                
                NamedObjectSave currentNos = ArrowState.Self.CurrentNamedObjectSave;


                ArrowCommands.Self.UpdateNosFromArrowInstance(instance, currentNos);

                ArrowCommands.Self.File.SaveGlux();
            }
        }


        internal void HandleKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.C)
                {
                    if (ArrowState.Self.CurrentInstanceVm != null)
                    {
                        ArrowCommands.Self.CopyPaste.AddToClipboard(ArrowState.Self.CurrentInstanceVm);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.V)
                {
                    bool hasPasted = false;
                    hasPasted = ArrowCommands.Self.CopyPaste.TryPasteInstance();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Delete)
            {
                // We only allow deleting instances through the GlueView window:
                if (ArrowState.Self.CurrentInstance != null)
                {
                    ArrowCommands.Self.Delete.DeleteCurrentInstance();
                    e.Handled = true;
                }
            }

        }

        #endregion
    }
}
