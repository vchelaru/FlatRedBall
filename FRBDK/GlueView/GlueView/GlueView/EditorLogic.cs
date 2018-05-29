using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Glue;
using GlueView.Gui;
using GlueView.SaveClasses;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Glue.Elements;
using GlueView.Forms;
using FlatRedBall.Gui;
using GlueView.Plugin;
using GlueView.Facades;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Parsing;
using System.Reflection;
using InteractiveInterface;
using GlueWcfServices;
using System.ServiceModel;
using GlueView.Wcf;

namespace GlueView
{
    public static class EditorLogic
    {
        #region Fields

        static GlueViewSettings glueViewSettings = new GlueViewSettings();
        static bool ignoreCollapseChange = false;
        #endregion

        #region Properties

        public static bool FlickeringOn
        {
            get;
            set;
        }

        static string SettingsSaveFileName
        {
            get
            {
                return FileManager.GetDirectory(GluxManager.CurrentGlueFile) + "GlueViewSettings.gvwx";
            }
        }

        #endregion

        #region Event Handlers

        public static void OnGluxLoaded()
        {
            // todo todo todo
            // This needs to move to plugins eventually
            LocalizationControl.Self.PopulateFromLocalizationManager();
            
            string essFileName = SettingsSaveFileName;

            if (File.Exists(essFileName))
            {   
                try
                {
                    glueViewSettings = GlueViewSettings.Load(essFileName);
                }
                catch
                {
                    // do nothing? It's just a settings file
                }
            }

            if(glueViewSettings?.CollapsedPlugins != null)
            {
                ignoreCollapseChange = true;
                CollapsibleFormHelper.Self.SetCollapsedItems(glueViewSettings.CollapsedPlugins);
                ignoreCollapseChange = false;

            }

            InteractiveConnection.Initialize();

            WcfManager.Self.Initialize();
        }


        static void OnElementRemoved(FlatRedBall.Glue.SaveClasses.IElement obj)
        {
            if (obj != null)
            {
                PluginManager.BeforeElementRemoved();
                GlueViewState.Self.CurrentElement = null;
            }
        }


        static void BeforeElementLoaded(FlatRedBall.Glue.SaveClasses.IElement obj)
        {
            // I think we need to set this *before* plugins react to it, no?
            GlueViewState.Self.CurrentElement = obj;


            // We used to set this *after* the Camera settings
            // but I think we want to do it before so that camera
            // settings can be saved.
            PluginManager.ReactToElementLoad();

            // This used to set the camera explicitly here, but we have a camera controller that does that for
            // us now, so we can get rid of it here:
            
        }

        static void OnElementHighlighted(ElementRuntime elementRuntime)
        {
            PluginManager.ReactToElementHighlight();
        }

        #endregion

        public static void Initialize()
        {
            // This may be annoying but it's very useful
            FlickeringOn = true;

            GluxManager.GluxLoaded += new Action(OnGluxLoaded);
            GluxManager.ElementRemoved += new Action<FlatRedBall.Glue.SaveClasses.IElement>(OnElementRemoved);
            GluxManager.BeforeElementLoaded += new Action<FlatRedBall.Glue.SaveClasses.IElement>(BeforeElementLoaded);
            GluxManager.ElementHighlighted += new Action<ElementRuntime>(OnElementHighlighted);
            GluxManager.ScriptReceived += GlueViewCommands.Self.ScriptingCommands.ApplyScript;

            FacadeContainer.Self.GlueState = GlueViewState.Self;
            FacadeContainer.Self.ProjectValues = GlueViewState.Self;

            TypeManager.LoadAdditionalTypes(Assembly.GetExecutingAssembly(), "FlatRedBall.Glue.StateInterpolation.");
        }

        internal static void HandleToolItemCollapsedOrExpanded()
        {
            // Can't save settings if no Glue file is open
            if(!ignoreCollapseChange && GlueViewState.Self.CurrentGlueProject != null)
            {
                var collapsedItems = CollapsibleFormHelper.Self.GetCollapsedItems();

                glueViewSettings.CollapsedPlugins = collapsedItems;

                glueViewSettings.Save(SettingsSaveFileName);
            }
        }

        public static void Activity()
        {
            if (FlickeringOn)
            {
                SpriteManager.ShuffleInternalLists();
                TextManager.ShuffleInternalLists();
            }

            
            Cursor cursor = GuiManager.Cursor;

            if (cursor.PrimaryPush)
            {
                PluginManager.ReactToCursorPush();
            }

            if(cursor.ScreenXChange != 0 || cursor.ScreenYChange != 0)
            {
                PluginManager.ReactToCursorMove();

                if (cursor.PrimaryDown)
                {
                    PluginManager.ReactToCursorDrag();
                }
            }

            if (cursor.PrimaryClick)
            {
                PluginManager.ReactToCursorClick();
            }

            if (cursor.SecondaryClick)
            {
                PluginManager.ReactToCursorRightClick();
            }

			if (cursor.ZVelocity != 0)
			{
				PluginManager.ReactToCursorMiddleScroll();
			}


            if (FlatRedBall.Input.InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
            }


			PluginManager.ReactToUpdate();
           
        }
        
        private static bool IsFileNamedObject2D(NamedObjectSave nos)
        {
            bool is2D = false;

            if (!string.IsNullOrEmpty(nos.SourceFile) && FileManager.GetExtension(nos.SourceFile) == "scnx")
            {
                string fullFileName = ElementRuntime.ContentDirectory + nos.SourceFile;

                // This will already be 
                Scene scene = FlatRedBallServices.Load<Scene>(fullFileName, GluxManager.ContentManagerName);

                foreach (Sprite sprite in scene.Sprites)
                {
                    if (sprite.PixelSize == .5f)
                    {
                        is2D = true;
                    }
                }

                float epsilon = .3f;
                foreach (SpriteFrame spriteFrame in scene.SpriteFrames)
                {
                    if (spriteFrame.PixelSize == .5f ||
                        System.Math.Abs(spriteFrame.Texture.Width * spriteFrame.TextureBorderWidth - spriteFrame.SpriteBorderWidth) < epsilon)
                    {
                        is2D = true;
                    }
                }

                foreach (Text text in scene.Texts)
                {
                    if (text.Scale == text.Font.LineHeightInPixels / 2.0f)
                    {
                        is2D = true;
                    }
                }
            }
            return is2D;
        }
    }
}
