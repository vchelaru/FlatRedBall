{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Managers;
using FlatRedBall.Math;
using FlatRedBall.Screens;
using FlatRedBall.Utilities;
using GlueControl.Models;

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

        public float GuidesGridSpacing
        {
            get => Guides.GridSpacing;
            set => Guides.GridSpacing = value;
        }

        List<ISelectionMarker> SelectedMarkers = new List<ISelectionMarker>();
        SelectionMarker HighlightMarker;

        INameable itemOver;
        public INameable ItemOver => itemOver;

        PositionedObject ItemGrabbed;
        ResizeSide SideGrabbed = ResizeSide.None;

        List<INameable> itemsSelected = new List<INameable>();
        public IEnumerable<INameable> ItemsSelected => itemsSelected;
        INameable ItemSelected => itemsSelected.Count > 0 ? itemsSelected[0] : null;

        public GlueElement CurrentGlueElement { get; private set; }
        public NamedObjectSave CurrentNamedObjectSave { get; private set; }

        public ElementEditingMode ElementEditingMode { get; set; }

        const float SelectedItemExtraPadding = 2;

        CopyPasteManager CopyPasteManager = new CopyPasteManager();

        public static EditingManager Self { get; private set; }

        #endregion

        #region Delegates/Events

        public Action<INameable, string, object> PropertyChanged;
        public Action<INameable> ObjectSelected;

        #endregion

        #region Constructor

        public EditingManager()
        {
            Self = this;
            HighlightMarker = new SelectionMarker(null);
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

                var itemOverBefore = itemOver;
                var itemSelectedBefore = ItemSelected;

                itemOver = SelectionLogic.GetInstanceOver(itemsSelected, SelectedMarkers, GuiManager.Cursor.PrimaryDoublePush, ElementEditingMode);
                var didChangeItemOver = itemOverBefore != itemOver;

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
                itemsSelected.Clear();
                itemOver = null;
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
            HighlightMarker.Owner = itemOver;
            HighlightMarker.Update(SideGrabbed);

            UpdateSelectedMarkers();
        }

        private void UpdateSelectedMarkers()
        {
            Vector3 moveVector = Vector3.Zero;
            for (int i = 0; i < itemsSelected.Count; i++)
            {
                var marker = SelectedMarkers[i];
                var item = itemsSelected[i];

                marker.Update(SideGrabbed);
                if (item == ItemGrabbed)
                {
                    moveVector = marker.LastUpdateMovement;
                }
            }

            if (moveVector.X != 0 || moveVector.Y != 0)
            {
                foreach (var item in itemsSelected)
                {
                    if (item != ItemGrabbed && item is PositionedObject itemAsPositionedObject)
                    {
                        if (itemAsPositionedObject.Parent == null)
                        {
                            itemAsPositionedObject.X += moveVector.X;
                            itemAsPositionedObject.Y += moveVector.Y;
                        }
                        else
                        {
                            itemAsPositionedObject.RelativeX += moveVector.X;
                            itemAsPositionedObject.RelativeY += moveVector.Y;
                        }
                    }
                }
            }
        }

        private void AddAndDestroyMarkersAccordingToItemsSelected()
        {
            var desiredMarkerCount = itemsSelected.Count;

            for (int i = SelectedMarkers.Count - 1; i > -1; i--)
            {
                var marker = SelectedMarkers[i];
                var hasSelectedItem = itemsSelected.Contains(marker.Owner);

                if (!hasSelectedItem)
                {
                    marker.Destroy();
                    SelectedMarkers.RemoveAt(i);
                }
            }

            for (int i = 0; i < itemsSelected.Count; i++)
            {
                var owner = itemsSelected[i];
                var hasMarker = SelectedMarkers.Any(item => item.Owner == owner);

                if (!hasMarker)
                {
                    var newMarker = CreateNewSelectionMarker(owner);
                    if (SelectedMarkers.Count > 0)
                    {
                        newMarker.FadingSeed = SelectedMarkers[0].FadingSeed;
                    }
                    SelectedMarkers.Add(newMarker);
                }
            }
        }

        private ISelectionMarker CreateNewSelectionMarker(INameable owner)
        {
            ISelectionMarker newMarker = null;
            if (owner is FlatRedBall.TileCollisions.TileShapeCollection)
            {
                newMarker = new TileShapeCollectionMarker(owner, CurrentNamedObjectSave);
            }
            else
            {
                newMarker = new SelectionMarker(owner);
            }
            newMarker.MakePersistent();
            newMarker.Name = "Selection Marker";
            newMarker.CanMoveItem = true;
            newMarker.PropertyChanged += (item, variable, value) => PropertyChanged(item, variable, value);
            return newMarker;
        }

        ISelectionMarker MarkerFor(INameable item)
        {
            var index = itemsSelected.IndexOf(item);
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
                ItemGrabbed = itemOver as PositionedObject;
                if (ItemGrabbed == null)
                {
                    SideGrabbed = ResizeSide.None;
                }

                var clickedOnSelectedItem = itemsSelected.Contains(itemOver);

                if (!clickedOnSelectedItem)
                {
                    var isCtrlDown = InputManager.Keyboard.IsCtrlDown;

                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null)
                    {
                        nos = CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == itemOver.Name);
                    }

                    if (nos != null)
                    {
                        Select(nos, addToExistingSelection: isCtrlDown, playBump: true);
                    }
                    else
                    {
                        // this should't happen, but for now we tolerate it until the current is sent
                        Select(nos, addToExistingSelection: isCtrlDown, playBump: true);
                    }
                }
                if (ItemGrabbed != null)
                {
                    foreach (var item in itemsSelected)
                    {
                        var marker = MarkerFor(item);

                        marker.CanMoveItem = item == ItemGrabbed;

                        marker.HandleCursorPushed();
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
                foreach (var item in itemsSelected)
                {
                    var marker = MarkerFor(item);

                    marker.HandleCursorRelease();
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
                for (int i = itemsSelected.Count - 1; i > -1; i--)
                {
                    InstanceLogic.Self.DeleteInstanceByGame(itemsSelected[i]);
                }
                itemsSelected.Clear();
                AddAndDestroyMarkersAccordingToItemsSelected();
            }

            DoNudgeHotkeyLogic();

            CopyPasteManager.DoHotkeyLogic(itemsSelected, ItemGrabbed);

            CameraLogic.DoHotkeyLogic();

        }

        private void DoNudgeHotkeyLogic()
        {
            var keyboard = InputManager.Keyboard;

            var isShiftDown = keyboard.IsShiftDown;
            var shiftAmount = isShiftDown ? 8 : 1;
            if (keyboard.IsCtrlDown == false)
            {
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    foreach (var item in itemsSelected)
                    {
                        if (item is PositionedObject asPositionedObject)
                        {
                            if (asPositionedObject.Parent != null)
                            {
                                asPositionedObject.RelativeY += shiftAmount;
                            }
                            else
                            {
                                asPositionedObject.Y += shiftAmount;
                            }
                            PropertyChanged(item, nameof(asPositionedObject.Y), asPositionedObject.Y);
                        }
                    }
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    foreach (var item in itemsSelected)
                    {
                        if (item is PositionedObject asPositionedObject)
                        {
                            if (asPositionedObject.Parent != null)
                            {
                                asPositionedObject.RelativeY -= shiftAmount;
                            }
                            else
                            {
                                asPositionedObject.Y -= shiftAmount;
                            }
                            PropertyChanged(item, nameof(asPositionedObject.Y), asPositionedObject.Y);
                        }
                    }
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Left))
                {
                    foreach (var item in itemsSelected)
                    {
                        if (item is PositionedObject asPositionedObject)
                        {
                            if (asPositionedObject.Parent != null)
                            {
                                asPositionedObject.RelativeX -= shiftAmount;
                            }
                            else
                            {
                                asPositionedObject.X -= shiftAmount;
                            }
                            PropertyChanged(item, nameof(asPositionedObject.X), asPositionedObject.X);
                        }
                    }
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Right))
                {
                    foreach (var item in itemsSelected)
                    {
                        if (item is PositionedObject asPositionedObject)
                        {
                            if (asPositionedObject.Parent != null)
                            {
                                asPositionedObject.RelativeX += shiftAmount;
                            }
                            else
                            {
                                asPositionedObject.X += shiftAmount;
                            }
                            PropertyChanged(item, nameof(asPositionedObject.X), asPositionedObject.X);
                        }
                    }
                }
            }
        }

        public void UpdateDependencies()
        {

        }

        public void SetCurrentGlueElement(GlueElement glueElement)
        {
            var oldGlueElement = CurrentGlueElement;

            CurrentGlueElement = glueElement;

            if (CurrentNamedObjectSave != null && oldGlueElement?.AllNamedObjects.Contains(CurrentNamedObjectSave) == true)
            {
                var nameToFind = CurrentNamedObjectSave.InstanceName;
                // Note - this will fail if the this is being called as a result of a rename. Therefore, the caller is responsbile
                // for re-selecting the NOS
                CurrentNamedObjectSave = glueElement?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == nameToFind);
            }
        }

        #region Selection

        internal void Select(NamedObjectSave namedObject, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false)
        {
            CurrentNamedObjectSave = namedObject;

            Select(namedObject?.InstanceName, addToExistingSelection, playBump, focusCameraOnObject);
        }

        internal void Select(string objectName, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false)
        {
            INameable foundObject = null;

            if (!string.IsNullOrEmpty(objectName))
            {
                foundObject = SelectionLogic.GetAvailableObjects(ElementEditingMode)
                    ?.FirstOrDefault(item => item.Name == objectName);


                if (foundObject == null)
                {
                    var screen = ScreenManager.CurrentScreen;
                    var instance = screen.GetInstance($"{objectName}", screen);

                    foundObject = instance as INameable;
                }
            }


            if (!addToExistingSelection)
            {
                itemsSelected.Clear();
            }

            if (itemsSelected.Contains(foundObject) == false)
            {
                if (foundObject != null)
                {
                    itemsSelected.Add(foundObject);
                }

                AddAndDestroyMarkersAccordingToItemsSelected();

                if (playBump)
                {
                    MarkerFor(ItemSelected)?.PlayBumpAnimation(SelectedItemExtraPadding, isSynchronized: false);
                }

                // do this right away so the handles don't pop out of existance when changing selection
                UpdateMarkers(didChangeItemOver: true);

            }

            if (focusCameraOnObject && foundObject is PositionedObject positionedObject)
            {
                Camera.Main.X = positionedObject.X;
                Camera.Main.Y = positionedObject.Y;
            }
        }

        public void RefreshSelectionAfterScreenLoad(bool playBump)
        {
            var names = itemsSelected.Select(item => item.Name).ToArray();

            itemsSelected.Clear();

            if (names.Length > 0)
            {
                var allnamedObjectSaves = CurrentGlueElement?.AllNamedObjects.ToArray();

                foreach (var name in names)
                {
                    var matchingNos = allnamedObjectSaves.FirstOrDefault(item => item.InstanceName == name);
                    Select(matchingNos, addToExistingSelection: true, playBump);
                }
            }
        }

        public void RaiseObjectSelected()
        {
            if (ObjectSelected != null && ItemSelected != null)
            {
                ObjectSelected(ItemSelected);
            }
        }

        #endregion
    }
}