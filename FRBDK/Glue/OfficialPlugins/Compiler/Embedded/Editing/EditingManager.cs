{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Input;
using FlatRedBall.Managers;
using FlatRedBall.Math;
using FlatRedBall.Screens;
using FlatRedBall.TileCollisions;
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

    #region Classes

    class PropertyChangeArgs
    {
        public INameable Nameable { get; set; }
        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
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

        List<INameable> itemsOver = new List<INameable>();
        public IEnumerable<INameable> ItemsOver => itemsOver;

        PositionedObject itemGrabbed;
        public PositionedObject ItemGrabbed => itemGrabbed;
        ResizeSide SideGrabbed = ResizeSide.None;

        List<INameable> itemsSelected = new List<INameable>();
        List<INameable> itemsOverLastFrame = new List<INameable>();
        public IEnumerable<INameable> ItemsSelected => itemsSelected;
        INameable ItemSelected => itemsSelected.Count > 0 ? itemsSelected[0] : null;

        List<PropertyChangeArgs> bufferedChangeArgs = new List<PropertyChangeArgs>();
        bool IsBuffering;

        public GlueElement CurrentGlueElement { get; private set; }
        public List<NamedObjectSave> CurrentNamedObjects { get; private set; } = new List<NamedObjectSave>();

        public ElementEditingMode ElementEditingMode { get; set; }

        const float SelectedItemExtraPadding = 2;

        CopyPasteManager CopyPasteManager = new CopyPasteManager();

        public static EditingManager Self { get; private set; }

        #endregion

        #region Delegates/Events

        public Action<List<PropertyChangeArgs>> PropertyChanged;
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

                itemsOverLastFrame.Clear();
                itemsOverLastFrame.AddRange(itemsOver);
                var itemSelectedBefore = ItemSelected;

                if(itemGrabbed == null && ItemsSelected.All(item => item is TileShapeCollection == false))
                {
                    SelectionLogic.DoDragSelectLogic();
                }

                SelectionLogic.GetInstanceOver(itemsSelected, itemsOver, SelectedMarkers, GuiManager.Cursor.PrimaryDoublePush, ElementEditingMode);

                var didChangeItemOver = itemsOverLastFrame.Any(item => !itemsOver.Contains(item)) ||
                    itemsOver.Any(item => !itemsOverLastFrame.Contains(item));

                DoGrabLogic();

                DoRectangleSelectLogic();

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
                itemsOver.Clear();
                itemGrabbed = null;

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
            // do we want to show multiple highlights? Probably?
            HighlightMarker.Owner = itemsOver.FirstOrDefault();
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
                if (item == itemGrabbed)
                {
                    moveVector = marker.LastUpdateMovement;
                }
            }

            if (moveVector.X != 0 || moveVector.Y != 0)
            {
                foreach (var item in itemsSelected)
                {
                    if (item != itemGrabbed && item is PositionedObject itemAsPositionedObject)
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
                // Assume with tile shape collections that only one object can be selected, so we can FirstOrDefault it:
                newMarker = new TileShapeCollectionMarker(owner, CurrentNamedObjects.FirstOrDefault());
            }
            else
            {
                newMarker = new SelectionMarker(owner);
            }
            newMarker.MakePersistent();
            newMarker.Name = "Selection Marker";
            newMarker.CanMoveItem = true;
            newMarker.PropertyChanged += HandleMarkerPropertyChanged;
            return newMarker;
        }

        private void HandleMarkerPropertyChanged(INameable item, string variable, object value)
        {
            var changeArgs = new PropertyChangeArgs
            {
                Nameable = item,
                PropertyName = variable,
                PropertyValue = value
            };

            if (IsBuffering)
            {
                bufferedChangeArgs.Add(changeArgs);
            }
            else
            {
                PropertyChanged(new List<PropertyChangeArgs> { changeArgs });
            }
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
                var itemOver = itemsOver.FirstOrDefault();
                itemGrabbed = itemOver as PositionedObject;
                if (itemGrabbed == null)
                {
                    SideGrabbed = ResizeSide.None;
                }

                var clickedOnSelectedItem = itemsSelected.Contains(itemOver);

                var isCtrlDown = InputManager.Keyboard.IsCtrlDown;
                if (!clickedOnSelectedItem)
                {
                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null)
                    {
                        nos = CurrentGlueElement?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == itemOver.Name);
                    }

                    if (nos != null)
                    {
                        Select(nos, addToExistingSelection: isCtrlDown, playBump: true);
                    }
                    else
                    {
                        // this shouldn't happen, but for now we tolerate it until the current is sent
                        Select(itemOver?.Name, addToExistingSelection: isCtrlDown, playBump: true);
                    }
                }
                else if (isCtrlDown)
                {
                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null)
                    {
                        nos = CurrentGlueElement?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == itemOver.Name);
                    }
                    if (nos != null)
                    {
                        RemoveFromSelection(nos);
                    }
                }

                if (itemGrabbed != null)
                {
                    foreach (var item in itemsSelected)
                    {
                        var marker = MarkerFor(item);

                        marker.CanMoveItem = item == itemGrabbed;

                        marker.HandleCursorPushed();
                    }

                    var markerOver = MarkerFor(itemGrabbed) as SelectionMarker;
                    SideGrabbed = markerOver?.GetSideOver() ?? ResizeSide.None;
                    ObjectSelected(itemGrabbed);
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

            if (itemGrabbed != null)
            {
                IsBuffering = true;
                foreach (var item in itemsSelected)
                {
                    var marker = MarkerFor(item);

                    marker.HandleCursorRelease();
                }
                IsBuffering = false;
                if (bufferedChangeArgs.Count > 0)
                {
                    PropertyChanged(bufferedChangeArgs.ToList());

                    bufferedChangeArgs.Clear();
                }
            }

            itemGrabbed = null;
            SideGrabbed = ResizeSide.None;
        }

        private void DoHotkeyLogic()
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                InstanceLogic.Self.DeleteInstancesByGame(itemsSelected);
                itemsSelected.Clear();
                AddAndDestroyMarkersAccordingToItemsSelected();
            }

            DoNudgeHotkeyLogic();

            CopyPasteManager.DoHotkeyLogic(itemsSelected, CurrentNamedObjects, itemGrabbed);

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
                            PropertyChanged(new List<PropertyChangeArgs>
                            {
                                new PropertyChangeArgs
                                {
                                    Nameable = item,
                                    PropertyName = nameof(asPositionedObject.Y),
                                    PropertyValue = asPositionedObject.Y
                                }
                            });
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
                            PropertyChanged(new List<PropertyChangeArgs>
                            {
                                new PropertyChangeArgs
                                {
                                    Nameable = item,
                                    PropertyName = nameof(asPositionedObject.Y),
                                    PropertyValue = asPositionedObject.Y
                                }
                            });
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

                            var args = new PropertyChangeArgs
                            {
                                Nameable = item,
                                PropertyName = nameof(asPositionedObject.X),
                                PropertyValue = asPositionedObject.X
                            };

                            PropertyChanged(new List<PropertyChangeArgs>
                            {
                              args
                            });
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
                            PropertyChanged(new List<PropertyChangeArgs>
                            {
                                new PropertyChangeArgs
                                {
                                    Nameable = item,
                                    PropertyName = nameof(asPositionedObject.X),
                                    PropertyValue = asPositionedObject.X
                                }
                            });
                        }
                    }
                }
            }
        }

        private void DoRectangleSelectLogic()
        {
            if (SelectionLogic.PerformedRectangleSelection)
            {
                var isFirst = true;
                foreach (var itemOver in ItemsOver)
                {
                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null)
                    {
                        nos = CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == itemOver.Name);
                    }

                    if (nos != null)
                    {
                        Select(nos, addToExistingSelection: isFirst == false, playBump: true);
                    }
                    else
                    {
                        // this shouldn't happen, but for now we tolerate it until the current is sent
                        Select(itemOver?.Name, addToExistingSelection: isFirst == false, playBump: true);
                    }

                    // This pushes the selection up for the first item so that Glue can match the selection. Eventually Glue will accept a list for multi-select, but not yet...
                    if (isFirst)
                    {
                        ObjectSelected(itemOver);
                    }

                    isFirst = false;
                }
            }
        }

        public void UpdateDependencies()
        {

        }

        public void SetCurrentGlueElement(GlueElement glueElement)
        {
            if (glueElement?.AllNamedObjects.Any(item => item == null) == true)
            {
                throw new ArgumentException($"There are null items in the the glueElement being sent over, there shouldn't be!\n{glueElement}");
            }
            var oldGlueElement = CurrentGlueElement;

            CurrentGlueElement = glueElement;

            var oldNames = CurrentNamedObjects.Where(item => item != null).Select(item => item.InstanceName).ToArray();
            CurrentNamedObjects.Clear();
            var newNamedObjects = glueElement?.AllNamedObjects.Where(item => oldNames.Contains(item.FieldName)).ToArray();
            // Note - this will fail if the this is being called as a result of a rename. Therefore, the caller is responsbile
            // for re-selecting the NOS
            CurrentNamedObjects.AddRange(newNamedObjects);
        }

        #region Selection

        internal void Select(NamedObjectSave namedObject, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false)
        {
            if (addToExistingSelection == false)
            {
                CurrentNamedObjects.Clear();
            }

            bool isSelectable = true;
            if (namedObject != null)
            {
                INameable foundObject = GetObjectByName(namedObject?.InstanceName);
                isSelectable = foundObject != null &&
                    SelectionLogic.IsSelectable(foundObject);
            }

            if (isSelectable)
            {
                if (namedObject != null && !CurrentNamedObjects.Contains(namedObject))
                {
                    CurrentNamedObjects.Add(namedObject);
                }

                Select(namedObject?.InstanceName, addToExistingSelection, playBump, focusCameraOnObject);
            }
            else
            {
                Select((string)null, addToExistingSelection, playBump, focusCameraOnObject);
            }
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
                    MarkerFor(foundObject)?.PlayBumpAnimation(SelectedItemExtraPadding, isSynchronized: false);
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


        private INameable GetObjectByName(string objectName)
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

            return foundObject;

        }
        void RemoveFromSelection(NamedObjectSave namedObject)
        {
            CurrentNamedObjects.Remove(namedObject);

            var foundObject = SelectionLogic.GetAvailableObjects(ElementEditingMode)
                ?.FirstOrDefault(item => item.Name == namedObject.InstanceName);

            if (foundObject != null)
            {
                itemsSelected.Remove(foundObject);

                AddAndDestroyMarkersAccordingToItemsSelected();

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