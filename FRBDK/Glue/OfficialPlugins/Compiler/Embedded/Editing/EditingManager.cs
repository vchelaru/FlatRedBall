{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Managers;
using FlatRedBall.Math;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    #region Enums

    public enum ElementEditingMode
    {
        EditingScreen,
        EditingEntity
    }

    #endregion

    class EditingManager : IManager
    {
        #region Fields/Properties

        Guides Guides;

        List<ISelectionMarker> SelectedMarkers = new List<ISelectionMarker>();
        SelectionMarker HighlightMarker;

        PositionedObject ItemOver;
        PositionedObject ItemGrabbed;
        ResizeSide SideGrabbed = ResizeSide.None;

        PositionedObjectList<PositionedObject> ItemsSelected = new PositionedObjectList<PositionedObject>();
        PositionedObject ItemSelected => ItemsSelected.Count > 0 ? ItemsSelected[0] : null;

        public ElementEditingMode ElementEditingMode { get; set; }

        const float SelectedItemExtraPadding = 2;

        CopyPasteManager CopyPasteManager = new CopyPasteManager();

        public static EditingManager Self { get; private set; }

        #endregion

        #region Delegates/Events

        public Action<PositionedObject, string, object> PropertyChanged;
        public Action<PositionedObject> ObjectSelected;

        #endregion

        #region Constructor

        public EditingManager()
        {
            Self = this;
            HighlightMarker = new SelectionMarker();
            HighlightMarker.BrightColor = Color.LightGreen;
            HighlightMarker.MakePersistent();
            HighlightMarker.Name = nameof(HighlightMarker);



            Guides = new Guides();
        }

        #endregion

        public void Update()
        {
#if SupportsEditMode

            var isInEditMode = ScreenManager.IsInEditMode;

            Guides.Visible = isInEditMode;

            if (isInEditMode)
            {
                Guides.UpdateGridLines();
                
                var itemOverBefore = ItemOver;
                var itemSelectedBefore = ItemSelected;

                ItemOver = SelectionLogic.GetInstanceOver(ItemsSelected, SelectedMarkers, GuiManager.Cursor.PrimaryDoublePush, ElementEditingMode);
                var didChangeItemOver = itemOverBefore != ItemOver;

                DoGrabLogic();

                DoReleaseLogic();

                DoHotkeyLogic();

                CameraLogic.DoCursorCameraControllingLogic();

                UpdateMarkers(didChangeItemOver);
            }
            else
            {
                HighlightMarker.Visible = false;
                if (SelectedMarkers.Count > 0)
                {
                    for (int i = SelectedMarkers.Count - 1; i > -1; i--)
                    {
                        SelectedMarkers[i].Destroy();
                    }
                    SelectedMarkers.Clear();
                }
                ItemsSelected.Clear();
                ItemOver = null;
                ItemGrabbed = null;

            }
#endif
        }

        #region Markers

        private void UpdateMarkers(bool didChangeItemOver)
        {
            if (didChangeItemOver)
            {
                HighlightMarker.FadingSeed = TimeManager.CurrentTime;
            }

            HighlightMarker.ExtraPaddingInPixels = 4;
            HighlightMarker.Update(ItemOver, SideGrabbed);

            UpdateSelectedMarkers();
        }

        private void UpdateSelectedMarkers()
        {
            Vector3 moveVector = Vector3.Zero;
            for (int i = 0; i < ItemsSelected.Count; i++)
            {
                var marker = SelectedMarkers[i];
                var item = ItemsSelected[i];

                marker.Update(item, SideGrabbed);
                if (item == ItemGrabbed)
                {
                    moveVector = marker.LastUpdateMovement;
                }
            }

            if (moveVector.X != 0 || moveVector.Y != 0)
            {
                foreach (var item in ItemsSelected)
                {
                    if (item != ItemGrabbed)
                    {
                        if (item.Parent == null)
                        {
                            item.X += moveVector.X;
                            item.Y += moveVector.Y;
                        }
                        else
                        {
                            item.RelativeX += moveVector.X;
                            item.RelativeY += moveVector.Y;
                        }
                    }
                }
            }
        }

        private void UpdateSelectedMarkerCount()
        {
            var desiredMarkerCount = ItemsSelected.Count;
            while (SelectedMarkers.Count > desiredMarkerCount)
            {
                var markerToDestroy = SelectedMarkers.Last();
                markerToDestroy.Destroy();
                SelectedMarkers.RemoveAt(SelectedMarkers.Count - 1);
            }
            while (SelectedMarkers.Count < desiredMarkerCount)
            {
                var newMarker = CreateNewSelectionMarker();
                if (SelectedMarkers.Count > 0)
                {
                    newMarker.FadingSeed = SelectedMarkers[0].FadingSeed;
                }
                SelectedMarkers.Add(newMarker);
            }
        }

        private SelectionMarker CreateNewSelectionMarker()
        {
            var newMarker = new SelectionMarker();
            newMarker.MakePersistent();
            newMarker.Name = "Selection Marker";
            newMarker.CanMoveItem = true;
            newMarker.PropertyChanged += (item, variable, value) => PropertyChanged(item, variable, value);
            return newMarker;
        }

        ISelectionMarker MarkerFor(PositionedObject item)
        {
            var index = ItemsSelected.IndexOf(item);
            if (index >= 0 && index < SelectedMarkers.Count)
            {
                return SelectedMarkers[index];
            }
            return null;
        }

        #endregion

        private void DoGrabLogic()
        {
            var cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                ItemGrabbed = ItemOver;
                if (ItemGrabbed == null)
                {
                    SideGrabbed = ResizeSide.None;
                }

                var clickedOnSelectedItem = ItemsSelected.Contains(ItemOver);

                if (!clickedOnSelectedItem)
                {
                    var isCtrlDown = InputManager.Keyboard.IsCtrlDown;

                    if (!isCtrlDown)
                    {
                        ItemsSelected.Clear();
                    }

                    if (ItemOver != null)
                    {
                        ItemsSelected.Add(ItemOver);
                    }
                    UpdateSelectedMarkerCount();
                    MarkerFor(ItemOver)?.PlayBumpAnimation(SelectedItemExtraPadding,
                        isSynchronized: ItemsSelected.Count > 1);

                }
                if (ItemGrabbed != null)
                {
                    foreach (var item in ItemsSelected)
                    {
                        var marker = MarkerFor(item);

                        marker.CanMoveItem = item == ItemGrabbed;

                        marker.HandleCursorPushed(item);
                    }

                    var markerOver = MarkerFor(ItemGrabbed) as SelectionMarker;
                    SideGrabbed = markerOver?.GetSideOver() ?? ResizeSide.None;
                    ObjectSelected(ItemGrabbed);
                }
            }
        }

        private void DoReleaseLogic()
        {
            var cursor = GuiManager.Cursor;

            ///////Early Out
            if (!cursor.PrimaryClick)
            {
                return;
            }
            //////End Early Out

            if (ItemGrabbed != null)
            {
                foreach (var item in ItemsSelected)
                {
                    var marker = MarkerFor(item);

                    marker.HandleCursorRelease(item);
                }
            }

            ItemGrabbed = null;
            SideGrabbed = ResizeSide.None;
        }

        private void DoHotkeyLogic()
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                for (int i = ItemsSelected.Count - 1; i > -1; i--)
                {
                    InstanceLogic.Self.DeleteInstanceByGame(ItemsSelected[i]);
                }
                ItemsSelected.Clear();
                UpdateSelectedMarkerCount();
            }

            CopyPasteManager.DoHotkeyLogic(ItemsSelected);

            CameraLogic.DoHotkeyLogic();

        }

        void Notify(PositionedObject item, string propertyName, object value) => PropertyChanged(item, propertyName, value);

        public void UpdateDependencies()
        {

        }

        internal void Select(string objectName)
        {
            PositionedObject foundObject = SelectionLogic.GetAvailableObjects(ElementEditingMode)
                ?.FirstOrDefault(item => item.Name == objectName);

            if (ItemsSelected.Contains(foundObject) == false)
            {
                ItemsSelected.Clear();
                if (foundObject != null)
                {
                    ItemsSelected.Add(foundObject);
                }

                UpdateSelectedMarkerCount();
                MarkerFor(ItemSelected)?.PlayBumpAnimation(SelectedItemExtraPadding, isSynchronized: false);

                // do this right away so the handles don't pop out of existance when changing selection
                UpdateMarkers(didChangeItemOver: true);

            }
        }
    }
}
