using System;
using FlatRedBall;

using FlatRedBall.Graphics.Particle;
using FlatRedBall.Gui;

using ParticleEditor.GUI;
using EditorObjects.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions;
using System.Reflection;
using System.Collections.Generic;

namespace ParticleEditor
{
	/// <summary>
	/// Summary description for GuiObjects.
	/// </summary>
	public class GuiData
    {
        #region Fields

        public static GuiMessages Messages = new GuiMessages();

		private static ActivityWindow mActivityWindow;

		private static ParticleEditor.GUI.ToolsWindow mToolsWindow;

		private static EmitterListBoxWindow mEmitterListBoxWindow;

        private static EmitterPropertyGrid mEmitterPropertyGrid;

        private static CameraPropertyGrid mCameraPropertyGrid;

        private static EditorPropertiesGrid mEditorPropertiesGrid;

        private static ScenePropertyGrid mScenePropertyGrid;

        private static EmissionSettingsPropertyGrid mEmissionSettingsPropertyGrid;

        private static CameraBoundsPropertyGrid mCameraBoundsPropertyGrid;

        static Menu mMenuStrip;

        #endregion

        #region Properties

        public static ActivityWindow ActivityWindow
        {
            get { return mActivityWindow; }
        }

        public static CameraBoundsPropertyGrid CameraBoundsPropertyGrid
        {
            get
            {
                return mCameraBoundsPropertyGrid;
            }
        }

        public static CameraPropertyGrid EditorCameraPropertyGrid
        {
            get { return mCameraPropertyGrid; }
        }

        public static EditorPropertiesGrid EditorPropertiesGrid
        {
            get { return mEditorPropertiesGrid; }
        }

        public static EmitterListBoxWindow EmitterListBoxWindow
        {
            get { return mEmitterListBoxWindow; }
        }

        public static EmitterPropertyGrid EmitterPropertyGrid
        {
            get { return mEmitterPropertyGrid; }
        }

        public static ScenePropertyGrid ScenePropertyGrid
        {
            get { return mScenePropertyGrid; }
        }

        public static ParticleEditor.GUI.ToolsWindow ToolsWindow
        {
            get { return mToolsWindow; }
        }

        public static EmissionSettingsPropertyGrid EmissionSettingsPropertyGrid
        {
            get { return mEmissionSettingsPropertyGrid; }
        }

        #endregion

        #region Event Methods

        private static void ShowEmissionSettingsPropertyGrid(Window callingWindow)
        {
            GuiData.EmissionSettingsPropertyGrid.Visible = true;
        }

        private static void MouseOverEmissionSettingsGrid(Window callingWindow)
        {
            CollapseItem item = (callingWindow as CollapseListBox).GetItemAtCursor();

            if (item != null)
            {
                GuiManager.ToolTipText = item.Text;
            }
        }

        #endregion

        #region Methods

        public static void Initialize()
		{
            try
            {
                Window.KeepWindowsInScreen = false;
            }
            catch
            {
                throw new Exception("A");
            }

                        try
            {
            mActivityWindow = new ActivityWindow();
            }
                        catch
                        {
                            throw new Exception("B");
                        }
                                    try
            {

            #region Menu Strip

            mMenuStrip = new Menu();

            #endregion

                        }
                        catch
                        {
                            throw new Exception("B");
                        }
                                    try
            {
            mEmitterListBoxWindow = new EmitterListBoxWindow(Messages);

                }
                        catch
                        {
                            throw new Exception("B");
                        }
                                    

			#region tools window

			mToolsWindow = new ParticleEditor.GUI.ToolsWindow(GuiManager.Cursor);

		//	copyEmitter.onClick += new FrbGuiMessage(messages.copyEmitter);
			#endregion



            CreatePropertyGrids();



            Window.KeepWindowsInScreen = true;
		}

        public static void ShowCameraBounds()
        {
            mCameraBoundsPropertyGrid.Visible = true;
            GuiManager.BringToFront(mCameraBoundsPropertyGrid);
        }

        public static void Update()
        {
            mActivityWindow.Update();

            mEmitterListBoxWindow.Update();

            mEmitterPropertyGrid.UpdateDisplayedProperties();

            mCameraPropertyGrid.UpdateDisplayedProperties();

            mCameraBoundsPropertyGrid.UpdateDisplayedProperties();

            #region ScenePropertyGrid

            if (mScenePropertyGrid.SelectedObject != EditorData.Scene)
            {
                mScenePropertyGrid.SelectedObject = EditorData.Scene;
            }
            else
            {
                mScenePropertyGrid.UpdateDisplayedProperties();
            }

            #endregion

            #region EmissionSettingsPropertyGrid


            if (mEmitterPropertyGrid.SelectedObject != null &&
                mEmissionSettingsPropertyGrid.SelectedObject != (GuiData.mEmitterPropertyGrid.SelectedObject as Emitter).EmissionSettings)
            {
                mEmissionSettingsPropertyGrid.SelectedObject = GuiData.mEmitterPropertyGrid.SelectedObject.EmissionSettings;
            }
            mEmissionSettingsPropertyGrid.UpdateDisplayedProperties();
            #endregion
        }

        static void CreatePropertyGrids()
        {            
            PropertyGrid.SetPropertyGridTypeAssociation(typeof(FlatRedBall.Instructions.InstructionBlueprint), typeof(InstructionBlueprintPropertyGrid<Sprite>));


            #region EmitterPropertyGrid
            mEmitterPropertyGrid = new EmitterPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mEmitterPropertyGrid);
            EmitterPropertyGrid.ContentManagerName = AppState.Self.PermanentContentManager;
            EmitterPropertyGrid.UndoInstructions = UndoManager.Instructions;

            #region Reset EmissionSettings Button
            Button settingsButton = EmitterPropertyGrid.GetUIElementForMember("EmissionSettings") as Button;

            Type t = typeof(Window);
            FieldInfo f = t.GetField("Click", BindingFlags.Default | BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            f.SetValue(settingsButton, null);

            settingsButton.Click += ShowEmissionSettingsPropertyGrid;
            #endregion
            #endregion

            #region Editor CameraPropertyGrid
            mCameraPropertyGrid = new CameraPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mCameraPropertyGrid);
            mCameraPropertyGrid.SelectedObject = SpriteManager.Camera;
            mCameraPropertyGrid.MakeFieldOfViewAndAspectRatioReadOnly();
            mCameraPropertyGrid.Visible = false;
            #endregion

            #region Bounds CameraPropertyGrid

            mCameraBoundsPropertyGrid = new CameraBoundsPropertyGrid(
                new Camera(AppState.Self.PermanentContentManager));

            mCameraBoundsPropertyGrid.Visible = false;

            #endregion

            #region EditorPropertiesGrid
            mEditorPropertiesGrid = new EditorPropertiesGrid();
            mEditorPropertiesGrid.Visible = false;
            #endregion

            #region ScenePropertyGrid
            mScenePropertyGrid = new ScenePropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mScenePropertyGrid);
            mScenePropertyGrid.HasCloseButton = true;
            mScenePropertyGrid.Visible = false;
            mScenePropertyGrid.ShowPropertyGridOnStrongSelect = true;
            #endregion

            #region EmissionSettingsPropertyGrid


            List<String> filterList = new List<String>();
            filterList.Add("ordered");
            filterList.Add("JustCycled");
            filterList.Add("TimeUntilNextFrame");
            filterList.Add("TimeCreated");
            filterList.Add("state");
            filterList.Add("Name");

            InstructionBlueprintPropertyGrid<Sprite>.MemberFilter.Add(typeof(Sprite), filterList);


            mEmissionSettingsPropertyGrid = new EmissionSettingsPropertyGrid(GuiManager.Cursor);
            GuiManager.AddWindow(mEmissionSettingsPropertyGrid);
            mEmissionSettingsPropertyGrid.HasCloseButton = true;
            mEmissionSettingsPropertyGrid.Visible = false;
            mEmissionSettingsPropertyGrid.ContentManagerName = AppState.Self.PermanentContentManager;
            mEmissionSettingsPropertyGrid.UndoInstructions = UndoManager.Instructions;
            mEmissionSettingsPropertyGrid.InstructionDisplayWindow.ListDisplayWindow.ListBox.CursorOver += MouseOverEmissionSettingsGrid;

            #endregion
        }

        #endregion
    }
}
