{CompilerDirectives}

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
        float GrabbedRadius;

        public ElementEditingMode ElementEditingMode { get; set; }

        const float SelectedItemExtraPadding = 2;
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

            Guides = new Guides();
        }

        #endregion

        public void Update()
        {
#if SupportsEditMode

            var isInEditMode = ScreenManager.IsInEditMode;

            Guides.Visible = isInEditMode;
            Guides.UpdateGridLines();

            if (isInEditMode)
            {
                var itemOverBefore = ItemOver;
                var itemSelectedBefore = ItemSelected;

                ItemOver = SelectionLogic.GetEntityOver(ItemSelected, SelectedMarker, GuiManager.Cursor.PrimaryDoublePush, ElementEditingMode);
                var didChangeItemOver = itemOverBefore != ItemOver;

                DoGrabLogic();

                DoReleaseLogic();

                DoHotkeyLogic();

                DoCameraControllingLogic();

                UpdateMarkers(didChangeItemOver);
            }
            else
            {
                HighlightMarker.Visible = false;
                SelectedMarker.Visible = false;

            }
#endif
        }

#region Markers

        private void UpdateMarkers(bool didChangeItemOver)
        {
            if(didChangeItemOver)
            {
                HighlightMarker.FadingSeed = TimeManager.CurrentTime;
            }

            HighlightMarker.ExtraPadding = 4;
            HighlightMarker.Update(ItemOver, SideGrabbed);

            SelectedMarker.Update(ItemSelected, SideGrabbed);

            if(ItemSelected is Sprite asSprite && asSprite.TextureScale > 0)
            {
                SelectedMarker.ResizeMode = ResizeMode.Cardinal;
            }
            else if(ItemSelected is FlatRedBall.Math.Geometry.Circle)
            {
                SelectedMarker.ResizeMode = ResizeMode.Cardinal;
            }
            else if(ItemSelected is FlatRedBall.Math.Geometry.IScalable)
            {
                SelectedMarker.ResizeMode = ResizeMode.EightWay;
            }
            else
            {
                SelectedMarker.ResizeMode = ResizeMode.None;
            }
        }


#endregion

        private void DoCameraControllingLogic()
        {
            var cursor = GuiManager.Cursor;
            if(cursor.MiddleDown)
            {
                var camera = Camera.Main;
                camera.X -= cursor.WorldXChangeAt(0);
                camera.Y -= cursor.WorldYChangeAt(0);
            }
        }

        private void DoGrabLogic()
        {
            var cursor = GuiManager.Cursor;


            if (cursor.PrimaryPush)
            {
                ItemGrabbed = ItemOver;
                if(ItemOver != ItemSelected)
                {
                    ItemSelected = ItemOver;
                    SelectedMarker.PlayBumpAnimation(SelectedItemExtraPadding);
                }
                if(ItemGrabbed != null)
                {
                    GrabbedPosition = ItemGrabbed.Position;
                    if (ItemGrabbed is FlatRedBall.Math.Geometry.IScalable itemGrabbedAsScalable)
                    {
                        GrabbedWidthAndHeight = new Vector2(itemGrabbedAsScalable.ScaleX * 2, itemGrabbedAsScalable.ScaleY * 2);
                    }
                    else if(ItemGrabbed is FlatRedBall.Math.Geometry.Circle circle)
                    {
                        GrabbedRadius = circle.Radius;
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
                        var didChangeWidth = GrabbedWidthAndHeight.X != asScalable.ScaleX * 2;
                        var didChangeHeight = GrabbedWidthAndHeight.Y != asScalable.ScaleY * 2;
                        if(ItemGrabbed is Sprite asSprite && asSprite.TextureScale > 0)
                        {
                            Notify(nameof(asSprite.TextureScale), asSprite.TextureScale);
                        }
                        else
                        {
                            if (didChangeWidth)
                            {
                                Notify("Width", asScalable.ScaleX*2);
                            }
                            if(didChangeWidth)
                            {
                                Notify("Height", asScalable.ScaleY * 2);
                            }
                        }
                    }
                    else if(ItemGrabbed is FlatRedBall.Math.Geometry.Circle circle)
                    {
                        Notify(nameof(circle.Radius), circle.Radius);
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

            const int movePerPush = 16;
            if(keyboard.IsCtrlDown)
            {
                if(keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Up))
                {
                    Camera.Main.Y += movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Down))
                {
                    Camera.Main.Y -= movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Left))
                {
                    Camera.Main.X -= movePerPush;
                }
                if (keyboard.KeyTyped(Microsoft.Xna.Framework.Input.Keys.Right))
                {
                    Camera.Main.X += movePerPush;
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

            if(ItemSelected != foundObject)
            {
                ItemSelected = foundObject;
                SelectedMarker.PlayBumpAnimation(SelectedItemExtraPadding);
            }
        }
    }
}
