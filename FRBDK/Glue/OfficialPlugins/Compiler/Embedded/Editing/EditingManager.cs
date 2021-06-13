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

        SelectionMarker SelectedMarker;
        SelectionMarker HighlightMarker;

        PositionedObject ItemOver;
        PositionedObject ItemGrabbed;
        ResizeSide SideGrabbed = ResizeSide.None;

        PositionedObject ItemSelected;

        Vector3 GrabbedPosition;
        Vector2 GrabbedWidthAndHeight;

        public ElementEditingMode ElementEditingMode { get; set; }


        #endregion

        #region Delegates/Events

        public Action<PositionedObject, string, object> PropertyChanged;
        public Action<PositionedObject> ObjectSelected;

        #endregion

        #region Constructor

        public EditingManager()
        {
            HighlightMarker = new SelectionMarker();
            HighlightMarker.BrightColor = Color.LightGreen;
            HighlightMarker.MakePersistent();
            HighlightMarker.Name = nameof(HighlightMarker);

            SelectedMarker = new SelectionMarker();
            SelectedMarker.MakePersistent();
            SelectedMarker.Name = nameof(SelectedMarker);
            SelectedMarker.CanMoveItem = true;
            SelectedMarker.ResizeMode = ResizeMode.EightWay;

            Guides = new Guides();
        }

        #endregion

        public void Update()
        {
            var isInEditMode = ScreenManager.IsInEditMode;

            Guides.Visible = isInEditMode;

            if (isInEditMode)
            {
                var itemBefore = ItemOver;
                ItemOver = SelectionLogic.GetEntityOver(ItemSelected, SelectedMarker, GuiManager.Cursor.PrimaryDoublePush, ElementEditingMode);
                var didChangeItemOver = itemBefore != ItemOver;

                DoGrabLogic();

                DoReleaseLogic();

                DoHotkeyLogic();

                UpdateMarkers(didChangeItemOver);
            }
            else
            {
                HighlightMarker.Visible = false;
                SelectedMarker.Visible = false;

            }

        }

        #region Markers

        private void UpdateMarkers(bool didChangeItemOver)
        {
            if(didChangeItemOver)
            {
                HighlightMarker.FadingSeed = TimeManager.CurrentTime;
            }
            HighlightMarker.Update(ItemOver, SideGrabbed, extraPadding:4);
            SelectedMarker.Update(ItemSelected, SideGrabbed, extraPadding: 2);

            if(ItemSelected is FlatRedBall.Math.Geometry.IScalable)
            {
                SelectedMarker.ResizeMode = ResizeMode.EightWay;
            }
            else
            {
                SelectedMarker.ResizeMode = ResizeMode.None;
            }
        }


        #endregion

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
                    if (ItemGrabbed is FlatRedBall.Math.Geometry.IScalable itemGrabbedAsScalable)
                    {
                        GrabbedWidthAndHeight = new Vector2(itemGrabbedAsScalable.ScaleX * 2, itemGrabbedAsScalable.ScaleY * 2);
                    }
                    SideGrabbed = SelectedMarker.GetSideOver();
                    ObjectSelected(ItemGrabbed);
                }
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
                        var value = ItemGrabbed.Parent == null
                            ? ItemGrabbed.X
                            : ItemGrabbed.RelativeX;
                        Notify(nameof(ItemGrabbed.X), value);
                    }
                    if (ItemGrabbed.Y != GrabbedPosition.Y)
                    {
                        var value = ItemGrabbed.Parent == null
                            ? ItemGrabbed.Y
                            : ItemGrabbed.RelativeY;
                        Notify(nameof(ItemGrabbed.Y), value);
                    }

                    if(ItemGrabbed is FlatRedBall.Math.Geometry.IScalable asScalable)
                    {
                        if(GrabbedWidthAndHeight.X != asScalable.ScaleX * 2)
                        {
                            Notify("Width", asScalable.ScaleX*2);
                        }
                        if(GrabbedWidthAndHeight.Y != asScalable.ScaleY * 2)
                        {
                            Notify("Height", asScalable.ScaleY * 2);
                        }
                    }
                }

                ItemGrabbed = null;
            }
        }

        private void DoHotkeyLogic()
        {
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            if(keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete))
            {
                if(ItemSelected != null)
                {
                    InstanceLogic.Self.DeleteInstanceByGame(ItemSelected);
                    ItemSelected = null;
                }
            }
        }

        void Notify(string propertyName, object value) => PropertyChanged(ItemGrabbed, propertyName, value);

        public void UpdateDependencies()
        {

        }

        internal void Select(string objectName)
        {
            PositionedObject foundObject = null;
            if(ScreenManager.CurrentScreen.GetType().Name == "EntityViewingScreen" && SpriteManager.ManagedPositionedObjects.Count > 0)
            {
                foundObject = SpriteManager.ManagedPositionedObjects[0].Children.FirstOrDefault(item => item.Name == objectName);
            }
            else
            {
                foundObject = SpriteManager.ManagedPositionedObjects.FirstOrDefault(item => item.Name == objectName);

            }

            ItemSelected = foundObject;
        }
    }
}
