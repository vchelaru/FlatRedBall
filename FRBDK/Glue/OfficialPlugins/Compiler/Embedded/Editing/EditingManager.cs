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
        SelectionMarker HighlightMarker;
        SelectionMarker SelectedMarker;


        PositionedObject ItemOver;
        PositionedObject ItemGrabbed;
        PositionedObject ItemSelected;

        Vector3 GrabbedPosition;

        public Action<PositionedObject, string, object> PropertyChanged;
        public Action<PositionedObject> ObjectSelected;

        public EditingManager()
        {
            HighlightMarker = new SelectionMarker();
            HighlightMarker.BrightColor = Color.LightGreen;
            HighlightMarker.MakePersistent();

            SelectedMarker = new SelectionMarker();
            SelectedMarker.MakePersistent();

        }

        public void Update()
        {
            var isInEditMode = ScreenManager.IsInEditMode;

            HighlightMarker.Visible = isInEditMode;

            if (isInEditMode)
            {
                ItemOver = SelectionLogic.GetEntityOver(ItemSelected, GuiManager.Cursor.PrimaryDoublePush);

                HighlightMarker.Update();

                DoGrabLogic();

                DoMoveLogic();

                DoReleaseLogic();

                UpdateMarkers();
            }

        }

        private void UpdateMarkers()
        {
            {
                var marker = HighlightMarker;
                var extraPadding = 4;
                var item = ItemOver;

                UpdateMarker(marker, extraPadding, item);
            }

            {
                var marker = SelectedMarker;
                var extraPadding = 2;
                var item = ItemSelected;

                UpdateMarker(marker, extraPadding, item);
            }
        }

        private static void UpdateMarker(SelectionMarker marker, int extraPadding, PositionedObject item)
        {
            marker.Visible = item != null;
            if(item != null)
            {
                SelectionLogic.GetDimensionsFor(item,
                    out float minX, out float maxX,
                    out float minY, out float maxY);

                var newPosition = new Vector3();
                newPosition.X = (maxX + minX) / 2.0f;
                newPosition.Y = (maxY + minY) / 2.0f;
                newPosition.Z = item.Z;

                marker.Position = newPosition;

                marker.ScaleX = extraPadding + (maxX - minX) / 2.0f;
                marker.ScaleY = extraPadding + (maxY - minY) / 2.0f;
            }
        }

        private void DoGrabLogic()
        {
            var cursor = GuiManager.Cursor;


            if (cursor.PrimaryPush)
            {
                ItemGrabbed = ItemOver;
                ItemSelected = ItemOver;
                if(ItemGrabbed != null)
                {
                    GrabbedPosition = ItemGrabbed.Position;
                    ObjectSelected(ItemGrabbed);
                }
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
                if(ItemGrabbed != null)
                {
                    if (ItemGrabbed.X != GrabbedPosition.X)
                    {
                        Notify(nameof(ItemGrabbed.X), ItemGrabbed.X);
                    }
                    if (ItemGrabbed.Y != GrabbedPosition.Y)
                    {
                        Notify(nameof(ItemGrabbed.Y), ItemGrabbed.Y);
                    }
                }

                ItemGrabbed = null;
            }
        }

        void Notify(string propertyName, object value) => PropertyChanged(ItemGrabbed, propertyName, value);

        public void UpdateDependencies()
        {

        }

        internal void Select(string objectName)
        {
            var foundObject = SpriteManager.ManagedPositionedObjects.FirstOrDefault(item => item.Name == objectName);

            ItemSelected = foundObject;
        }
    }
}
