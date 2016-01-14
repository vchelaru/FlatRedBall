using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Glue;
using FlatRedBall.Gui;
using FlatRedBall.IO;
using InteractiveInterface;
using LevelEditor.Gui;

namespace LevelEditor
{
    public static class EditorLogic
    {
        public static bool FlickeringOn
        {
            get;
            set;
        }
        
        public static void OnGluxLoaded()
        {
            GuiData.LocalizationWindow.PopulateFromLocalizationManager();
        } 

        public static void Initialize()
        {
            GluxManager.GluxLoaded += OnGluxLoaded;
        }


        
        public static void Activity()
        {
            if (FlickeringOn)
            {
                SpriteManager.ShuffleInternalLists();
                TextManager.ShuffleInternalLists();
            }

            EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);
            EditorObjects.CameraMethods.KeyboardCameraControl(SpriteManager.Camera);

            SelectEntity();
            MoveEntity();
        }

        private static void MoveEntity()
        {
            var cursor = GuiManager.Cursor;

            if(cursor.ObjectGrabbed == null) return;

            if(!cursor.PrimaryDown)
            {
                var element = cursor.ObjectGrabbed as ElementRuntime;

                if(element == null) return;

                element.AssociatedNamedObjectSave.SetPropertyValue("X", element.X);
                element.AssociatedNamedObjectSave.SetPropertyValue("Y", element.Y);

                string xml;

                FileManager.XmlSerialize(element.AssociatedNamedObjectSave, out xml);
                InteractiveConnection.Callback.UpdateNamedObjectSave(element.ContainerName, xml);

                cursor.ObjectGrabbed = null;
            }else
            {
                cursor.UpdateObjectGrabbedPosition();
            }
        }

        private static void SelectEntity()
        {
            var cursor = GuiManager.Cursor;

            if (cursor.WindowOver != null)
                return;

            if (!cursor.PrimaryPush) return;

            if (GluxManager.CurrentElement == null) return;

            if (GluxManager.CurrentElementHighlighted != null)
            {
                foreach (var item in
                GluxManager.CurrentElement.ContainedElements.Where(item => item.IsMouseOver(cursor) && GluxManager.CurrentElementHighlighted == item).Where(item => InteractiveConnection.Initialized()))
                {
                    InteractiveConnection.Callback.SelectNamedObjectSave(item.ContainerName, item.FieldName);
                    if (item.AssociatedNamedObjectSave.HasCustomVariable("X") && item.AssociatedNamedObjectSave.HasCustomVariable("Y"))
                        cursor.ObjectGrabbed = item;
                    return;
                }

                foreach (var item in
                GluxManager.CurrentElement.ElementsInList.Where(item => item.IsMouseOver(cursor) && GluxManager.CurrentElementHighlighted == item).Where(elementRuntime => InteractiveConnection.Initialized()))
                {
                    InteractiveConnection.Callback.SelectNamedObjectSave(item.ContainerName, item.FieldName);
                    if (item.AssociatedNamedObjectSave.HasCustomVariable("X") && item.AssociatedNamedObjectSave.HasCustomVariable("Y"))
                        cursor.ObjectGrabbed = item;
                    return;
                }
            }
            
            foreach (var item in
                GluxManager.CurrentElement.ContainedElements.Where(item => item.IsMouseOver(cursor)).Where(item => InteractiveConnection.Initialized()))
            {
                InteractiveConnection.Callback.SelectNamedObjectSave(item.ContainerName, item.FieldName);
                if(item.AssociatedNamedObjectSave.HasCustomVariable("X") && item.AssociatedNamedObjectSave.HasCustomVariable("Y"))
                    cursor.ObjectGrabbed = item;
                return;
            }

            foreach (var item in
                GluxManager.CurrentElement.ElementsInList.Where(item => item.IsMouseOver(cursor)).Where(elementRuntime => InteractiveConnection.Initialized()))
            {
                InteractiveConnection.Callback.SelectNamedObjectSave(item.ContainerName, item.FieldName);
                if (item.AssociatedNamedObjectSave.HasCustomVariable("X") && item.AssociatedNamedObjectSave.HasCustomVariable("Y"))
                    cursor.ObjectGrabbed = item;
                return;
            }
        }
    }
}
