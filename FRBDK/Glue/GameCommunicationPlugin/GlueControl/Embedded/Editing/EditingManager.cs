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
using GlueControl.Managers;


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

    class NameableWrapper : INameable, IStaticPositionable
    {
        public string Name { get; set; }

        IStaticPositionable containedAsPositionable;
        object containedObject;
        public object ContainedObject
        {
            get => containedObject;
            set
            {
                containedObject = value;
                containedAsPositionable = value as IStaticPositionable;
            }
        }

        public float X
        {
            get => containedAsPositionable?.X ?? 0;
            set
            {
                if (containedAsPositionable != null)
                {
                    containedAsPositionable.X = value;
                }
            }
        }
        public float Y
        {
            get => containedAsPositionable?.Y ?? 0;
            set
            {
                if (containedAsPositionable != null)
                {
                    containedAsPositionable.Y = value;
                }
            }
        }
        public float Z
        {
            get => containedAsPositionable?.Z ?? 0;
            set
            {
                if (containedAsPositionable != null)
                {
                    containedAsPositionable.Z = value;
                }
            }
        }
    }

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

        bool showGrid = true;
        public bool ShowGrid
        {
            get => showGrid;
            set
            {
                showGrid = value;
                Guides.Visible = value;
            }
        }

        decimal gridAlpha;
        public decimal GridAlpha
        {
            get => gridAlpha;
            set
            {
                gridAlpha = value;

                var premultValue = (byte)(value * 255);

                Guides.SmallGridLineColor.R = premultValue;
                Guides.SmallGridLineColor.G = premultValue;
                Guides.SmallGridLineColor.B = premultValue;
                Guides.SmallGridLineColor.A = premultValue;

                Guides.RefreshColors();

            }
        }

        public float GuidesGridSpacing
        {
            get => Guides.GridSpacing;
            set => Guides.GridSpacing = value;
        }

        /// <summary>
        /// Whether to draw a rectangle showing how much of the game the player would see. When editing an
        /// entity this is centered on the entity, and when editing a screen it follows the edit mode camera.
        /// </summary>
        public bool ShowScreenBounds { get; set; }
        public bool LockScreenBoundsToWorldSpace { get; set; }

        float snapSize = 8;
        public float SnapSize
        {
            get => snapSize;
            set
            {
                snapSize = value;
                foreach (var marker in SelectedMarkers)
                {
                    if (marker is SelectionMarker selectionMarker)
                    {
                        selectionMarker.PositionSnappingSize = snapSize;
                    }
                }
            }
        }

        float polygonPointSnapSize = 1;
        public float PolygonPointSnapSize
        {
            get => polygonPointSnapSize;
            set
            {
                polygonPointSnapSize = value;
                foreach (var marker in SelectedMarkers)
                {
                    if (marker is SelectionMarker selectionMarker)
                    {
                        selectionMarker.PolygonPointSnapSize = polygonPointSnapSize;
                    }
                }
            }
        }

        bool isSnappingEnabled;
        public bool IsSnappingEnabled
        {
            get => isSnappingEnabled;
            set
            {
                isSnappingEnabled = value;
                foreach (var marker in SelectedMarkers)
                {
                    if (marker is SelectionMarker selectionMarker)
                    {
                        selectionMarker.IsSnappingEnabled = isSnappingEnabled;
                    }
                }
            }
        }

        /// <summary>
        /// The PolygonPointHandles for the primary selected marker, or null if nothing/non-polygon is
        /// selected. Test-only - used by SimulatePolygonPointDragDto (see CommandReceiver.HandleDto) so a
        /// LiveGameProcess-based test can drive a point drag without a real Cursor/mouse.
        /// </summary>
        internal PolygonPointHandles PrimarySelectionPolygonPointHandlesForTesting =>
            (SelectedMarkers.FirstOrDefault() as SelectionMarker)?.PolygonPointHandlesForTesting;

        /// <summary>
        /// The primary selected marker, or null if nothing is selected. Test-only - used by
        /// SimulateResizeDragDto (see CommandReceiver.HandleDto) so a LiveGameProcess-based test can drive
        /// a resize-handle drag without a real Cursor/mouse. See #2087.
        /// </summary>
        internal SelectionMarker PrimarySelectionMarkerForTesting =>
            SelectedMarkers.FirstOrDefault() as SelectionMarker;

        /// <summary>
        /// The primary selected item, if it is directly a Polygon (not a tunneled/wrapped one - see
        /// PolygonPointHandles.EveryFrameUpdate). Test-only, alongside
        /// <see cref="PrimarySelectionPolygonPointHandlesForTesting"/>.
        /// </summary>
        internal FlatRedBall.Math.Geometry.Polygon PrimarySelectionAsPolygonForTesting =>
            itemsSelected.FirstOrDefault() as FlatRedBall.Math.Geometry.Polygon;

        List<ISelectionMarker> SelectedMarkers = new List<ISelectionMarker>();
        SelectionMarker HighlightMarker;
        MeasurementMarker MeasurementMarker;

        List<INameable> itemsOver = new List<INameable>();
        public IEnumerable<INameable> ItemsOver => itemsOver;

        IStaticPositionable itemGrabbed;
        public IStaticPositionable ItemGrabbed => itemGrabbed;

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

        bool wasGameActive;

        // Tracks IsGameOrGlueActive (not FlatRedBallServices.Game.IsActive - see DoGrabLogic) so a
        // click that returns focus to the embedded game can still be detected as "the gate just opened"
        // even when the game is embedded and Game.IsActive is stuck true. See issue #2187.
        bool wasGameOrGlueActive;

        bool wasPushedInWindow;

        public bool IsEmbeddedInActiveGlue => EmbeddedWindowLogic.IsParentGlueFocused;

        // When embedded, the game window is reparented into Glue (SetParent) and never receives normal
        // WM_ACTIVATE/deactivate notifications, so FlatRedBallServices.Game.IsActive gets stuck true -
        // it can't be trusted here. Only IsEmbeddedInActiveGlue (a real foreground-window check) reflects
        // whether Glue is actually the active window. See issue #2154.
        public bool IsGameOrGlueActive => EmbeddedWindowLogic.IsEmbedded
            ? IsEmbeddedInActiveGlue
            : FlatRedBallServices.Game.IsActive;

        #endregion

        #region Delegates/Events

        public Action<List<PropertyChangeArgs>> PropertyChanged;
        public Action<List<INameable>> ObjectSelected;

        #endregion

        #region Constructor

        public EditingManager()
        {
            Self = this;
            HighlightMarker = new SelectionMarker(null);
            HighlightMarker.BrightColor = Color.LightGreen;
            HighlightMarker.MakePersistent();
            HighlightMarker.Name = nameof(HighlightMarker);
            HighlightMarker.CanMoveItem = false;

            MeasurementMarker = new MeasurementMarker();

            Guides = new Guides();
        }

        #endregion

        #region Markers

        private void UpdateMarkers(bool didChangeItemOver)
        {
            // By buffering, we dont' send every command to the game directly. This is important if a group of objects is selected:
            IsBuffering = true;

            if (didChangeItemOver)
            {
                HighlightMarker.FadingSeed = TimeManager.CurrentTime;
            }

            HighlightMarker.ExtraPaddingInPixels = 4;
            // do we want to show multiple highlights? Probably?
            HighlightMarker.Owner = itemsOver.FirstOrDefault();
            HighlightMarker.Update(!wasGameActive && IsGameOrGlueActive);

            UpdateSelectedMarkers();

            IsBuffering = false;
            if (bufferedChangeArgs.Count > 0)
            {
                PropertyChanged(bufferedChangeArgs.ToList());

                bufferedChangeArgs.Clear();
            }
        }

        private void UpdateSelectedMarkers()
        {
            Vector3 moveVector = Vector3.Zero;
            for (int i = 0; i < itemsSelected.Count; i++)
            {
                var marker = SelectedMarkers[i];
                var item = itemsSelected[i];

                marker.Update(!wasGameActive && IsGameOrGlueActive);

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

#if MONOGAME_381

            var itemsSelectedHash = itemsSelected.ToHashSet();
#else
            var itemsSelectedHash = itemsSelected;

#endif

            for (int i = SelectedMarkers.Count - 1; i > -1; i--)
            {
                var marker = SelectedMarkers[i];
                var hasSelectedItem = itemsSelectedHash.Contains(marker.Owner);

                if (!hasSelectedItem)
                {
                    marker.Destroy();
                    SelectedMarkers.RemoveAt(i);
                }
            }

            HashSet<INameable> markerItems = new HashSet<INameable>();
            for (int i = 0; i < SelectedMarkers.Count; i++)
            {
                markerItems.Add(SelectedMarkers[i].Owner);
            }

            for (int i = 0; i < itemsSelected.Count; i++)
            {
                var owner = itemsSelected[i];
                var hasMarker = markerItems.Contains(owner);

                if (!hasMarker)
                {
                    var newMarker = CreateNewSelectionMarker(owner);
                    if (SelectedMarkers.Count > 0)
                    {
                        newMarker.FadingSeed = SelectedMarkers[0].FadingSeed;
                    }
                    SelectedMarkers.Add(newMarker);
                    markerItems.Add(owner);

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
                var selectionMarker = new SelectionMarker(owner);
                selectionMarker.PositionSnappingSize = SnapSize;
                selectionMarker.PolygonPointSnapSize = PolygonPointSnapSize;
                selectionMarker.IsSnappingEnabled = IsSnappingEnabled;
                newMarker = selectionMarker;
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

        #region Update/DoXXXXLogic
        public void Update()
        {
#if SupportsEditMode
            var isInEditMode = ScreenManager.IsInEditMode;

            Guides.Visible = isInEditMode && showGrid;


            if (isInEditMode && !ScreenManager.CurrentScreen.IsActivityFinished)
            {
                Guides.UpdateGridLines();

                itemsOverLastFrame.Clear();
                itemsOverLastFrame.AddRange(itemsOver);
                var itemSelectedBefore = ItemSelected;

                var mouse = FlatRedBall.Input.InputManager.Mouse;

                if(!mouse.ButtonDown(Mouse.MouseButtons.LeftButton) &&
                    !mouse.ButtonDown(Mouse.MouseButtons.RightButton) &&
                    !mouse.ButtonDown(Mouse.MouseButtons.MiddleButton))
                {
                    wasPushedInWindow = false;
                }
                else if(mouse.AnyButtonPushed())
                {
                    wasPushedInWindow = mouse.IsInGameWindow();
                }

                // Vic says - not sure how much should be inside the IsActive check
                if (IsGameOrGlueActive && mouse.IsInGameWindow())
                {
                    if (itemGrabbed == null && ItemsSelected.All(item => item is TileShapeCollection == false))
                    {
                        var gameBecameActive = !wasGameActive && FlatRedBallServices.Game.IsActive;

                        SelectionLogic.DoDragSelectLogic(gameBecameActive);
                    }
                    
                    var isCursorUsingMouse = FlatRedBall.Gui.GuiManager.Cursor.DevicesControllingCursor.Contains(mouse);

                    var isOverWindow = false;
                    if(isCursorUsingMouse && FlatRedBall.Gui.GuiManager.Cursor.WindowOver != null)
                    {
                        isOverWindow = true;
                    }

                    if(!isOverWindow)
                    {
                        SelectionLogic.GetItemsOver(itemsSelected, itemsOver, SelectedMarkers, mouse.ButtonDoublePushed(Mouse.MouseButtons.LeftButton), ElementEditingMode);
                    }
                    else
                    {
                        itemsOver.Clear();
                    }
                }
                else
                {
                    if(!mouse.IsInGameWindow())
                    {
                        itemsOver.Clear();
                    }
                    SelectionLogic.DoInactiveWindowLogic();
                }

                var didChangeItemOver = itemsOverLastFrame.Any(item => !itemsOver.Contains(item)) ||
                    itemsOver.Any(item => !itemsOverLastFrame.Contains(item));

                if (IsGameOrGlueActive)
                {
                    if (mouse.IsInGameWindow())
                    {
                        DoGrabLogic();
                    }

                    DoRectangleSelectLogic();

                    DoReleaseLogic();

                    DoHotkeyLogic();

                    if(mouse.IsInGameWindow() || wasPushedInWindow)
                    {
                        CameraLogic.DoCursorCameraControllingLogic(wasPushedInWindow);
                    }

                    DoForwardBackActivity();
                }

                CameraLogic.DoBackgroundLogic();

                UpdateMarkers(didChangeItemOver);

                // The measurement marker responds to right-drags, but unlike the left-button logic above
                // it isn't inside the IsGameOrGlueActive block, and it doesn't check focus itself. Edit mode
                // sets Cursor.RequiresGameWindowInFocus = false (the game window is reparented into Glue so it
                // never gets normal focus), so without this check a right-drag anywhere on screen would draw
                // the measurement overlay even with Glue behind another window.
                if (IsGameOrGlueActive && (mouse.IsInGameWindow() || wasPushedInWindow))
                {
                    UpdateMeasurementMarker();
                }

                DrawScreenBounds();
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

            wasGameActive = FlatRedBallServices.Game.IsActive;
            wasGameOrGlueActive = IsGameOrGlueActive;

#endif
        }

        private void DrawScreenBounds()
        {
            if (!ShowScreenBounds)
            {
                return;
            }

            // When editing an entity the entity is the only thing on screen, so the bounds are centered on it
            // to show how much of the game the player would see around it. When editing a screen there's no
            // equivalent anchor - the runtime camera position comes from custom code - so the bounds follow
            // the edit mode camera instead.
            var center = Vector3.Zero;

            if (ElementEditingMode == ElementEditingMode.EditingScreen)
            {
                center = LockScreenBoundsToWorldSpace
                    ? Vector3.Zero
                    : new Vector3(Camera.Main.X, Camera.Main.Y, 0);
            }
            else if ((ScreenManager.CurrentScreen as Screens.EntityViewingScreen)?.CurrentEntity is PositionedObject mainEntity)
            {
                center = new Vector3(mainEntity.X, mainEntity.Y, 0);
            }

            EditorVisuals.Rectangle(CameraSetup.Data.ResolutionWidth, CameraSetup.Data.ResolutionHeight, center);
        }

        private void UpdateMeasurementMarker()
        {
            var doesMarkerUseRightClick = false;
            foreach (var item in this.SelectedMarkers)
            {
                if (item.UsesRightMouseButton)
                {
                    doesMarkerUseRightClick = true;
                }
            }

            if (!doesMarkerUseRightClick)
            {
                MeasurementMarker.Update();
            }
        }

        bool shouldPrintCurrentNamedObjectInformation = false;
        private void DoGrabLogic()
        {
            var mouse = FlatRedBall.Input.InputManager.Mouse;

            if (shouldPrintCurrentNamedObjectInformation)
            {
                PrintCurrentNamedObjectsInformation();

            }

            var shouldTreatAsPush = ComputeShouldTreatAsPush(
                mouse.IsInGameWindow(),
                mouse.ButtonPushed(Mouse.MouseButtons.LeftButton),
                mouse.ButtonDown(Mouse.MouseButtons.LeftButton),
                wasGameOrGlueActive,
                IsGameOrGlueActive);

            if (shouldTreatAsPush)
            {
                var itemOver = itemsOver.FirstOrDefault();
                var additiveModifierDown = InputManager.Keyboard.IsCtrlDown;

                PerformClickSelection(itemOver, additiveModifierDown);
            }
        }

        /// <summary>
        /// Core of DoGrabLogic's push-vs-drag-continuation decision, split out so it can be exercised
        /// with synthetic inputs (mirrors EmbeddedWindowLogic.ComputeIsFocused from issue #2183). A real
        /// click that returns OS focus to the embedded game can land on the same frame the game/Glue
        /// activity gate (isGameOrGlueActiveThisFrame) opens, in which case Mouse.ButtonPushed may never
        /// report a fresh down-transition for it - only ButtonDown - so "the gate just opened while the
        /// button is held down" must also count as a push, or that gesture is lost until an entirely
        /// separate click. This used to be computed from FlatRedBallServices.Game.IsActive, which is
        /// stuck true for the whole embedded session (the reparented game window never gets a real
        /// deactivate notification), making the fallback permanently dead. See issue #2187.
        /// </summary>
        internal static bool ComputeShouldTreatAsPush(bool isInGameWindow, bool buttonPushed, bool buttonDown,
            bool wasGameOrGlueActiveLastFrame, bool isGameOrGlueActiveThisFrame)
        {
            if (!isInGameWindow)
            {
                return false;
            }

            var didWindowActivate = !wasGameOrGlueActiveLastFrame && isGameOrGlueActiveThisFrame;

            return buttonPushed || (buttonDown && didWindowActivate);
        }

        /// <summary>
        /// Test-only: mirrors DoGrabLogic's push decision (ComputeShouldTreatAsPush) with synthetic
        /// button state instead of real Mouse.ButtonPushed/ButtonDown, which LiveGameProcess can't fake
        /// (no real hardware mouse in that headless host). wasGameOrGlueActiveLastFrame is likewise
        /// passed in directly rather than read from the live wasGameOrGlueActive field - the real
        /// embedded game process keeps running its own Update() loop between DTO round-trips, which
        /// would otherwise race this test's own "last frame" state and silently clobber it. Driven by
        /// SimulateGrabAcrossFocusGateDto (issue #2187).
        /// </summary>
        internal bool SimulateGrabAcrossFocusGateForTesting(string objectName, bool buttonPushed, bool buttonDown,
            bool wasGameOrGlueActiveLastFrame, bool additiveModifierDown)
        {
            // Mirrors Update(): DoGrabLogic is only ever called from inside `if (IsGameOrGlueActive)`, so
            // that outer gate has to be re-applied here too - ComputeShouldTreatAsPush alone only models
            // DoGrabLogic's own body, not the gate its caller already enforces.
            var shouldTreatAsPush = IsGameOrGlueActive && ComputeShouldTreatAsPush(
                isInGameWindow: true,
                buttonPushed,
                buttonDown,
                wasGameOrGlueActiveLastFrame,
                IsGameOrGlueActive);

            if (shouldTreatAsPush)
            {
                var itemOver = string.IsNullOrEmpty(objectName) ? null : GetObjectByName(objectName);
                PerformClickSelection(itemOver, additiveModifierDown);
            }

            return shouldTreatAsPush;
        }

        /// <summary>
        /// Core click-to-select decision: replaces the selection with itemOver, or when
        /// additiveModifierDown is true, adds itemOver to the existing selection (toggling it off if it was
        /// already selected). Parameterized on itemOver/additiveModifierDown instead of reading
        /// InputManager.Mouse/Keyboard directly, so SimulateClickSelectForTesting (LiveGameProcess tests)
        /// can drive it without a real Cursor/mouse/keyboard.
        /// </summary>
        private void PerformClickSelection(INameable itemOver, bool additiveModifierDown)
        {
            itemGrabbed = itemOver as IStaticPositionable;

            var clickedOnSelectedItem = itemsSelected.Contains(itemOver);

            if (!clickedOnSelectedItem)
            {
                if (itemOver?.Name == null)
                {
                    Select((NamedObjectSave)null, addToExistingSelection: additiveModifierDown, playBump: true);
                }
                else
                {
                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null)
                    {
                        nos = GetNosFromItemName(itemOver.Name);
                    }

                    if (nos != null)
                    {
                        Select(nos, addToExistingSelection: additiveModifierDown, playBump: true);
                    }
                    else
                    {
                        if (!additiveModifierDown)
                        {
                            CurrentNamedObjects.Clear();
                        }
                        // this shouldn't happen, but for now we tolerate it until the current is sent
                        Select(itemOver?.Name, addToExistingSelection: additiveModifierDown, playBump: true);
                    }
                }
            }
            else if (additiveModifierDown)
            {
                NamedObjectSave nos = null;
                if (itemOver?.Name != null)
                {
                    nos = GetNosFromItemName(itemOver.Name);
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
                }

                if (!clickedOnSelectedItem)
                {
                    if (additiveModifierDown)
                    {
                        ObjectSelected(itemsSelected);
                    }
                    else
                    {
                        ObjectSelected(new List<INameable> { itemGrabbed as INameable });
                    }
                }
            }
            else if (!clickedOnSelectedItem)
            {
                ObjectSelected(new List<INameable>());
            }
        }

        /// <summary>
        /// Test-only entry point mirroring DoGrabLogic's click-selection decision - resolves
        /// objectName the same way a real click resolves whatever the ray hit, then runs the identical
        /// add/toggle logic, without needing a real Cursor/mouse or keyboard modifier state. Driven by
        /// SimulateClickSelectDto (see CommandReceiver.HandleDto).
        /// </summary>
        internal void SimulateClickSelectForTesting(string objectName, bool additiveModifierDown)
        {
            INameable itemOver = string.IsNullOrEmpty(objectName) ? null : GetObjectByName(objectName);
            PerformClickSelection(itemOver, additiveModifierDown);
        }

        /// <summary>
        /// Test-only entry point for issue #2154 (Edit Mode processing clicks while the game/Glue window
        /// is unfocused or covered) - same as SimulateClickSelectForTesting, but first checks
        /// IsGameOrGlueActive, the same gate EditingManager.Activity applies before ever reaching
        /// DoGrabLogic for a real click. Returns false (and performs no selection change) when the gate
        /// would have blocked a real click too. Driven by SimulateClickSelectRespectingActivityGateDto.
        /// </summary>
        internal bool SimulateClickSelectRespectingActivityGateForTesting(string objectName, bool additiveModifierDown)
        {
            if (!IsGameOrGlueActive)
            {
                return false;
            }

            SimulateClickSelectForTesting(objectName, additiveModifierDown);
            return true;
        }

        private NamedObjectSave GetNosFromItemName(string itemName)
        {
            NamedObjectSave nos = CurrentGlueElement?.AllNamedObjects.FirstOrDefault(item => item.InstanceName == itemName);
            if (nos == null && CurrentGlueElement != null)
            {
                // This could be a nos defined in a base screen and we're viewing a derived screen.
                var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(CurrentGlueElement);
                foreach (var baseElement in baseElements)
                {
                    nos = baseElement.AllNamedObjects.FirstOrDefault(nos => nos.InstanceName == itemName);
                    if (nos != null)
                    {
                        break;
                    }
                }
            }

            return nos;
        }

        private void DoReleaseLogic()
        {
            var mouse = FlatRedBall.Input.InputManager.Mouse;

            ///////Early Out
            if (!mouse.ButtonReleased(Mouse.MouseButtons.LeftButton))
            {
                return;
            }
            //////End Early Out

            if (itemGrabbed != null)
            {
                foreach (var item in itemsSelected)
                {
                    var marker = MarkerFor(item);
                }

            }

            itemGrabbed = null;
        }

        private void DoHotkeyLogic()
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                var handledByMarkers = false;

                foreach (var marker in SelectedMarkers)
                {
                    if (marker.HandleDelete())
                    {
                        handledByMarkers = true;
                        break;
                    }
                }

                if (!handledByMarkers)
                {
                    InstanceLogic.Self.DeleteInstancesByGame(itemsSelected);
                    itemsSelected.Clear();
                    AddAndDestroyMarkersAccordingToItemsSelected();
                }
            }

            DoGoToDefinitionLogic();

            DoNudgeHotkeyLogic();

            CopyPasteManager.DoHotkeyLogic(itemsSelected, CurrentNamedObjects, itemGrabbed);

            CameraLogic.DoHotkeyLogic();

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Z) && keyboard.IsCtrlDown)
            {
                GlueCommands.Self.Undo();
            }
        }

        private void DoForwardBackActivity()
        {
            var mouse = InputManager.Mouse;
            if (mouse.ButtonPushed(Mouse.MouseButtons.XButton1))
            {
                _=GlueControlManager.Self.SendToGlue(new Dtos.SelectPreviousDto());
            }
            else if (mouse.ButtonPushed(Mouse.MouseButtons.XButton2))
            {
                _=GlueControlManager.Self.SendToGlue(new Dtos.SelectNextDto());
            }
        }

        private void DoGoToDefinitionLogic()
        {
            var keyboard = InputManager.Keyboard;

            if (keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.F12))
            {
                _=GlueControlManager.Self.SendToGlue(new Dtos.GoToDefinitionDto());
            }
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

        public bool IsDraggingRectangle => SelectionLogic.LeftSelect != null;

        private void DoRectangleSelectLogic()
        {
            if (SelectionLogic.PerformedRectangleSelection)
            {

                // let's make a dictionary to make this faster:
                Dictionary<string, NamedObjectSave> namedObjectSaveDictionary = new Dictionary<string, NamedObjectSave>();
                var all = CurrentGlueElement?.AllNamedObjects;
                if (all != null)
                {
                    foreach (var item in all)
                    {
                        namedObjectSaveDictionary.Add(item.InstanceName, item);
                    }
                }

                if (CurrentGlueElement != null)
                {
                    var derivedElements = ObjectFinder.Self.GetAllBaseElementsRecursively(CurrentGlueElement);
                    foreach (var derivedElement in derivedElements)
                    {
                        foreach (var item in derivedElement.AllNamedObjects)
                        {
                            // let the derived have priority, check if it already exists:
                            if (namedObjectSaveDictionary.ContainsKey(item.InstanceName) == false)
                            {
                                namedObjectSaveDictionary.Add(item.InstanceName, item);
                            }
                        }
                    }
                }

                var isFirst = true;

                var cache = SelectionLogic.GetAvailableObjects(ElementEditingMode).ToArray();


                foreach (var itemOver in ItemsOver)
                {
                    NamedObjectSave nos = null;
                    if (itemOver?.Name != null && namedObjectSaveDictionary.ContainsKey(itemOver.Name))
                    {
                        nos = namedObjectSaveDictionary[itemOver.Name];
                    }

                    if (nos != null)
                    {
                        var isEditingLocked =
                            ObjectFinder.Self.GetPropertyValueRecursively<bool>(
                                nos, nameof(nos.IsEditingLocked));
                        if (isEditingLocked == false)
                        {
                            Select(nos, addToExistingSelection: isFirst == false, playBump: false, updateMarkers: false, availablePositionedObjectCache: cache);
                        }
                    }
                    else
                    {
                        // this shouldn't happen, but for now we tolerate it until the current is sent
                        Select(itemOver?.Name, addToExistingSelection: isFirst == false, playBump: false, updateMarkers: false);
                    }


                    isFirst = false;
                }

                AddAndDestroyMarkersAccordingToItemsSelected();


                foreach (var marker in SelectedMarkers)
                {
                    marker.PlayBumpAnimation(SelectedItemExtraPadding, isSynchronized: false);
                }

                UpdateMarkers(didChangeItemOver: true);

                ObjectSelected(ItemsOver.ToList());
            }
        }
        #endregion

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

        public void ReplaceNamedObjectSave(NamedObjectSave nos, string glueElementName, string containerName)
        {
            ///////////////////Early Out///////////////////
            if (CurrentGlueElement?.Name != glueElementName)
            {
                return;
            }
            ////////////////End Early Out//////////////////

            var oldNos = CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == nos.InstanceName);
            var oldContainer = CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(oldNos));

            NamedObjectSave newContainer = null;
            if (!string.IsNullOrEmpty(containerName))
            {
                newContainer = CurrentGlueElement.AllNamedObjects.FirstOrDefault(item => item.InstanceName == containerName);
            }

            // Does index matter? Maybe eventually...
            if (oldContainer != null)
            {
                oldContainer.ContainedObjects.Remove(oldNos);
            }

            if (newContainer != null)
            {
                if (oldNos != null)
                {
                    newContainer.ContainedObjects.Remove(oldNos);
                }
                newContainer.ContainedObjects.Add(nos);
            }
            else
            {
                if (oldNos != null)
                {
                    CurrentGlueElement.NamedObjects.Remove(oldNos);
                }
                CurrentGlueElement.NamedObjects.Add(nos);
            }

            if (CurrentNamedObjects.Contains(oldNos))
            {
                // Index matters here because the order should match the same order of the runtime objects
                var index = CurrentNamedObjects.IndexOf(oldNos);
                CurrentNamedObjects.Remove(oldNos);

                CurrentNamedObjects.Insert(index, nos);
            }
            // Things can change due to a re-load so let's check for names too
            else
            {
                var matchingNameNos = CurrentNamedObjects.FirstOrDefault(item => item.InstanceName == nos.InstanceName);
                if (matchingNameNos != null)
                {
                    var index = CurrentNamedObjects.IndexOf(matchingNameNos);
                    CurrentNamedObjects.Remove(matchingNameNos);

                    CurrentNamedObjects.Insert(index, nos);
                }
            }

            nos.FixAllTypes();
        }


        #region Diagnostics

        private void PrintCurrentNamedObjectsInformation()
        {
            var text = $"{CurrentGlueElement?.AllNamedObjects.Count()} NOSes in current element\n";
            foreach (var nos in CurrentNamedObjects)
            {
                text += nos.InstanceName + "\n";
                foreach (var instruction in nos.InstructionSaves)
                {
                    text += $"  {instruction.Member}={instruction.Value}";
                }
                text += "\n";
            }

            WriteDiagnosticText(text);
        }

        private void PrintAvailableObjects()
        {
            var availableObjects = SelectionLogic.GetAvailableObjects(ElementEditingMode);
            var text = $"Number of available objects: {availableObjects.Count()}\n";
            var sublist = availableObjects.Take(30);
            foreach (var item in sublist)
            {
                if (ItemsSelected.Contains(item))
                {
                    text += "> ";
                }
                text += item.Name + "\n";
            }
            WriteDiagnosticText(text);
        }

        private static void WriteDiagnosticText(string text)
        {
            var position = new Vector3();
            position.X = Camera.Main.AbsoluteLeftXEdge;
            position.Y = Camera.Main.AbsoluteTopYEdge;

            var textInstance = EditorVisuals.Text(text, position);
            textInstance.HorizontalAlignment = FlatRedBall.Graphics.HorizontalAlignment.Left;
            textInstance.VerticalAlignment = FlatRedBall.Graphics.VerticalAlignment.Top;
        }

        #endregion

        #region Selection


        internal void Select(IEnumerable<NamedObjectSave> namedObjects, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false)
        {
            var isFirst = true;
            if (namedObjects.Count() == 0)
            {
                Select((NamedObjectSave)null, false, playBump, focusCameraOnObject, false);
            }
            else
            {
                foreach (var item in namedObjects)
                {
                    Select(item, addToExistingSelection || !isFirst, playBump, focusCameraOnObject, false);
                    isFirst = false;
                }
            }

            AddAndDestroyMarkersAccordingToItemsSelected();

            UpdateMarkers(didChangeItemOver: true);
        }

        internal void Select(NamedObjectSave namedObject, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false, bool updateMarkers = true, PositionedObject[] availablePositionedObjectCache = null)
        {
            if (addToExistingSelection == false)
            {
                CurrentNamedObjects.Clear();
            }

            bool isSelectable = true;
            if (namedObject != null)
            {
                INameable foundObject = GetObjectByName(namedObject?.InstanceName, availablePositionedObjectCache);
                isSelectable = foundObject != null &&
                    SelectionLogic.IsSelectable(foundObject);
            }

            if (isSelectable)
            {
                if (namedObject != null && !CurrentNamedObjects.Contains(namedObject))
                {
                    CurrentNamedObjects.Add(namedObject);
                }

                Select(namedObject?.InstanceName, addToExistingSelection, playBump, focusCameraOnObject, updateMarkers, availablePositionedObjectCache);
            }
            else
            {
                Select((string)null, addToExistingSelection, playBump, focusCameraOnObject, updateMarkers);
            }
        }

        internal void Select(string objectName, bool addToExistingSelection = false, bool playBump = true, bool focusCameraOnObject = false, bool updateMarkers = true, PositionedObject[] availablePositionedObjectCache = null)
        {
            INameable foundObject = string.IsNullOrEmpty(objectName)
                ? null
                : GetObjectByName(objectName, availablePositionedObjectCache);

            //if (!string.IsNullOrEmpty(objectName))
            //{
            //    foundObject = SelectionLogic.GetAvailableObjects(ElementEditingMode)
            //        ?.FirstOrDefault(item => item.Name == objectName);


            //    if (foundObject == null)
            //    {
            //        var screen = ScreenManager.CurrentScreen;
            //        var instance = screen.GetInstance($"{objectName}", screen);

            //        foundObject = instance as INameable;
            //    }
            //}


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

                if (updateMarkers)
                {
                    AddAndDestroyMarkersAccordingToItemsSelected();
                }

                if (playBump)
                {
                    MarkerFor(foundObject)?.PlayBumpAnimation(SelectedItemExtraPadding, isSynchronized: false);
                }

                if (updateMarkers)
                {
                    // do this right away so the handles don't pop out of existance when changing selection
                    UpdateMarkers(didChangeItemOver: true);
                }

            }

            if (focusCameraOnObject && foundObject is PositionedObject positionedObject)
            {
                Camera.Main.X = positionedObject.X;
                Camera.Main.Y = positionedObject.Y;
            }
        }


        public INameable GetObjectByName(string objectName, PositionedObject[] availablePositionedObjectCache = null)
        {
            INameable foundObject = null;
            object foundObjectAsObject = null;
            if (!string.IsNullOrEmpty(objectName))
            {
                if(availablePositionedObjectCache != null)
                {
                    for(int i = 0; i < availablePositionedObjectCache.Length; i++)
                    {
                        if (availablePositionedObjectCache[i].Name ==  objectName)
                        {
                            foundObject = availablePositionedObjectCache[i];
                            break;
                        }
                    }
                }
                else
                {
                    var allAvailableObjects = SelectionLogic.GetAvailableObjects(ElementEditingMode);
                    foundObject = allAvailableObjects?.FirstOrDefault(item => item.Name == objectName);
                }
                if (foundObject == null)
                {
                    foundObject = InstanceLogic.Self.GetCameraByName(objectName);
                }

                if (foundObject == null)
                {
                    var screen = ScreenManager.CurrentScreen;
                    var instance = screen.GetInstance($"{objectName}", screen);

                    foundObject = instance as INameable;
                }



                if (foundObject == null && ScreenManager.CurrentScreen is Screens.EntityViewingScreen entityViewingScreen)
                {
                    try
                    {
                        foundObjectAsObject = FlatRedBall.Instructions.Reflection.LateBinder.GetValueStatic(
                            entityViewingScreen.CurrentEntity, objectName);

                        if (foundObjectAsObject != null)
                        {
                            foundObject = foundObjectAsObject as INameable ?? new NameableWrapper { Name = objectName, ContainedObject = foundObjectAsObject };
                        }
                    }
                    catch
                    {

                    }
                }
            }

            if (foundObject == null)
            {
                var message = $"Tried to get object by name {objectName} but couldn't find anything";
                // This object may not exist. Should we tell Glue? I guess...
                if (foundObjectAsObject != null)
                {
                    message += "\n...but was able to find an object of type {}, but it isn't an INameable so it can't be used in the editor.";
                }
                Managers.GlueCommands.Self.PrintOutput(message);
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
                var allNamedObjectSaves = CurrentGlueElement?.AllNamedObjects.ToArray();
                if (allNamedObjectSaves != null)
                {
                    foreach (var name in names)
                    {
                        var matchingNos = allNamedObjectSaves.FirstOrDefault(item => item.InstanceName == name);
                        Select(matchingNos, addToExistingSelection: true, playBump);
                    }
                }
            }
        }

        public void RaiseObjectSelected()
        {
            if (ObjectSelected != null && ItemsSelected.Count() > 0)
            {
                ObjectSelected(ItemsSelected.ToList());
            }
        }

        #endregion

        #region Variable Assignment (suppression)

        static HashSet<string> VariableAssignmentsToIgnore = new HashSet<string>
        {
            nameof(NamedObjectSave.IsEditingLocked), // this gets copied over by the NOS
            "PartitioningAutomaticManual" // We can't support this in realtime because it's done by codegen and would be hard to change...
        };

        public bool GetIfShouldSuppressVariableAssignment(string variableName, INameable targetInstance)
        {
            var isAnythingGrabbed = itemGrabbed != null;

            ISelectionMarker markerToAsk = null;

            if (isAnythingGrabbed && ItemsSelected.Contains(targetInstance))
            {
                var index = itemsSelected.IndexOf(targetInstance);

                if (index > -1 && index < SelectedMarkers.Count)
                {
                    markerToAsk = SelectedMarkers[index];
                }
            }

            var shouldSuppress = markerToAsk?.ShouldSuppress(variableName) == true;

            if (!shouldSuppress)
            {
                if (VariableAssignmentsToIgnore.Contains(variableName))
                {
                    shouldSuppress = true;
                }
            }

            return shouldSuppress;
        }

        #endregion
    }


    internal class EmbeddedWindowLogic
    {

        static DateTime lastUpdate;

        public static bool IsEmbedded { get; set; }

        static System.Diagnostics.Process glueProcess;
        public static bool IsGlueShowingModalWindow { get; set; }

        /// <summary>
        /// Test-only: when set, IsParentGlueFocused uses these values instead of calling
        /// GetForegroundWindow()/GetWindowThreadProcessId() and inspecting a real "GlueFormsCore"
        /// process. There's no way to spin up a real Glue process (or fake OS focus/window ownership)
        /// from the LiveGameProcess test harness, so this is how issue #2183's fix
        /// (foregroundOwnedByThisGame) gets pinned by a deterministic test. Set via
        /// SetEmbeddedFocusTestOverrideDto.
        /// </summary>
        internal static (bool glueProcessExists, bool foregroundMatchesGlueMainWindow, bool foregroundOwnedByThisGame)? TestOverride;

        /// <summary>
        /// Pure decision logic, split out of IsParentGlueFocused so it can be exercised with synthetic
        /// inputs (see TestOverride) instead of only through real OS focus/process state.
        /// </summary>
        internal static bool ComputeIsFocused(bool isEmbedded, bool isModalWindowOpen, bool glueProcessExists,
            bool foregroundMatchesGlueMainWindow, bool foregroundOwnedByThisGame)
        {
            if (!glueProcessExists)
            {
                return false;
            }

            return isEmbedded
                && (foregroundMatchesGlueMainWindow || foregroundOwnedByThisGame)
                && !isModalWindowOpen;
        }

        public static bool IsParentGlueFocused
        {
            get
            {
                if (TestOverride is { } testOverride)
                {
                    return ComputeIsFocused(IsEmbedded, IsGlueShowingModalWindow,
                        testOverride.glueProcessExists, testOverride.foregroundMatchesGlueMainWindow,
                        testOverride.foregroundOwnedByThisGame);
                }

                if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(5))
                {
                    lastUpdate = DateTime.Now;
                    RefreshGlueProcess();
                }

                var glueProcessExists = glueProcess != null;
                var fg = GetForegroundWindow();
                var foregroundMatchesGlueMainWindow = glueProcessExists && glueProcess.MainWindowHandle == fg;

                // The embedded game window is reparented into Glue via a raw SetParent (no WS_CHILD style
                // fixup), so Windows still lets it take OS foreground/activation on its own when clicked -
                // GetForegroundWindow() then returns the game's own window, not Glue's. When embedded,
                // that IS Glue's UI from the user's perspective, so treat it as focused too (issue #2183 -
                // this was masked for years by the old `|| Game.IsActive` fallback that #2154's fix
                // removed).
                var foregroundOwnedByThisGame = false;
                if (glueProcessExists && !foregroundMatchesGlueMainWindow)
                {
                    GetWindowThreadProcessId(fg, out var fgOwnerPid);
                    foregroundOwnedByThisGame = fgOwnerPid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                }

                return ComputeIsFocused(IsEmbedded, IsGlueShowingModalWindow, glueProcessExists,
                    foregroundMatchesGlueMainWindow, foregroundOwnedByThisGame);
            }
        }

#if WINDOWS


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

#else
        static IntPtr GetForegroundWindow() => IntPtr.Zero;
        static uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId) { processId = 0; return 0; }


#endif
        private static void RefreshGlueProcess()
        {
            glueProcess = null;

            var processes = System.Diagnostics.Process.GetProcesses();
            // see if the foreground window is Glue:
            foreach (var process in processes)
            {
                if (process.ProcessName == "GlueFormsCore")
                {
                    glueProcess = process;
                    break;
                }
            }
        }
    }


}
