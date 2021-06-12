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

        SelectionMarker HighlightMarker;
        SelectionMarker SelectedMarker;


        PositionedObject ItemOver;
        PositionedObject ItemGrabbed;
        PositionedObject ItemSelected;

        Vector3 GrabbedPosition;

        public ElementEditingMode ElementEditingMode { get; set; }


        #endregion

        #region Properties

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
        }

        #endregion

        public void Update()
        {
            var isInEditMode = ScreenManager.IsInEditMode;

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
            HighlightMarker.Update(ItemOver, extraPadding:4);
            SelectedMarker.Update(ItemSelected, extraPadding:2);
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
