using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Managers;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Editing
{
    class EditingManager : IManager
    {
        SelectionMarker SelectionMarker;

        PositionedObject ItemOver;
        PositionedObject ItemGrabbed;

        Vector3 GrabbedPosition;

        public Action<PositionedObject, string, object> PropertyChanged;

        public EditingManager()
        {
            SelectionMarker = new SelectionMarker();
        }

        public void Update()
        {
            var isInEditMode = ScreenManager.IsInEditMode;

            SelectionMarker.Visible = isInEditMode;

            if (isInEditMode)
            {
                ItemOver = SelectionLogic.GetEntityOver();

                HighlightItemOver();

                SelectionMarker.Update();

                DoGrabLogic();

                DoMoveLogic();

                DoReleaseLogic();
            }

        }

        private void HighlightItemOver()
        {

            SelectionMarker.Visible = ItemOver != null;
            if (ItemOver != null)
            {
                SelectionLogic.GetDimensionsFor(ItemOver,
                    out float minX, out float maxX,
                    out float minY, out float maxY);

                var newPosition = new Vector3();
                newPosition.X = (maxX + minX) / 2.0f;
                newPosition.Y = (maxY + minY) / 2.0f;
                newPosition.Z = ItemOver.Z;

                SelectionMarker.Position = newPosition;

                SelectionMarker.ScaleX = 2 + (maxX - minX) / 2.0f;
                SelectionMarker.ScaleY = 2 + (maxY - minY) / 2.0f;
            }
        }
        private void DoGrabLogic()
        {
            var cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                ItemGrabbed = ItemOver;
                GrabbedPosition = ItemGrabbed.Position;
            }
        }

        private void DoMoveLogic()
        {
            var cursor = GuiManager.Cursor;

            if (ItemGrabbed != null)
            {
                ItemGrabbed.X += cursor.WorldXChangeAt(ItemGrabbed.Z);
                ItemGrabbed.Y += cursor.WorldYChangeAt(ItemGrabbed.Z);
            }
        }

        private void DoReleaseLogic()
        {
            var cursor = GuiManager.Cursor;

            if (cursor.PrimaryClick)
            {
                if (ItemGrabbed.X != GrabbedPosition.X)
                {
                    Notify(nameof(ItemGrabbed.X), ItemGrabbed.X);
                }
                if (ItemGrabbed.Y != GrabbedPosition.Y)
                {
                    Notify(nameof(ItemGrabbed.Y), ItemGrabbed.Y);
                }

                ItemGrabbed = null;
            }
        }

        void Notify(string propertyName, object value) => PropertyChanged(ItemGrabbed, propertyName, value);

        public void UpdateDependencies()
        {

        }
    }
}
