using System;
using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.IO;


namespace InstructionEditor.Gui
{
	/// <summary>
	/// Summary description for FileMenu.
	/// </summary>
	public class InstructionEditorMenuStrip : FlatRedBall.Gui.MenuStrip
    {
        #region Event Methods

        void AddSpriteClick(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToLoad();
            fileWindow.SetFileType("graphic and animation");
            fileWindow.OkClick += AddSpriteOk;            
        }

        void AddSpriteOk(Window callingWindow)
        {
            string result = ((FileWindow)callingWindow).Results[0];

            EditorData.AddSprite(result);
        }

        void AddTextClick(Window callingWindow)
        {
            EditorData.AddText();
        }

        void SaveActiveScene(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToSave();
            fileWindow.SetFileType("scnx");
            fileWindow.OkClick += SaveActiveSceneOk;
        }

        void SaveActiveSceneOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];
            EditorData.SaveActiveScene(fileName);
        }

        void SaveInstructionCodeClick(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetToSave();
            fileWindow.SetFileType("txt");
            fileWindow.OkClick += SaveInstructionCodeOk;
        }

        void SaveInstructionCodeOk(Window callingWindow)
        {
            string fileName = ((FileWindow)callingWindow).Results[0];

            

            FileManager.SaveText(
                EditorData.GetStringForInstructions(), 
                fileName);   
        }

        void ShowEditorCameraWindow(Window callingWindow)
        {
            GuiData.EditorCameraPropertyGrid.Visible = true;
        }

        void ShowEditorOptionsWindow(Window callingWindow)
        {
            GuiData.EditorOptionsPropertyGrid.Visible = true;
        }

        void ShowSceneCameraWindow(Window callingWindow)
        {
            GuiData.SceneCameraPropertyGrid.Visible = true;
        }

        void ShowScenePropertyGrid(Window callingWindow)
        {
            GuiData.ScenePropertyGrid.Visible = true;
            GuiManager.BringToFront(GuiData.ScenePropertyGrid);
        }

        void ShowToolsWindow(Window callingWindow)
        {
            GuiData.ToolsWindow.Visible = true;
            GuiManager.BringToFront(GuiData.ToolsWindow);
        }

        void ShowUsedMembersWindow(Window callingWindow)
        {
            GuiData.UsedPropertySelectionWindow.Visible = true;
            GuiManager.BringToFront(GuiData.UsedPropertySelectionWindow);
        }

        #endregion

        #region Methods

        public InstructionEditorMenuStrip() : 
            base(GuiManager.Cursor)
		{
			GuiManager.AddWindow(this);

            #region File

            MenuItem fileMenuItem = AddItem("File");

            fileMenuItem.AddItem("New Set").Click += new GuiMessage(FileMenuMessages.NewSet);
            fileMenuItem.AddItem("---------------");
            fileMenuItem.AddItem("Load Set").Click += new GuiMessage(FileMenuMessages.LoadSetClick);
            fileMenuItem.AddItem("Load Active .scnx").Click += new GuiMessage(FileMenuMessages.LoadActiveSceneClick);
            fileMenuItem.AddItem("Load Inactive .scnx").Click += new GuiMessage(FileMenuMessages.LoadInactiveSceneClick);
            fileMenuItem.AddItem("---------------");
            fileMenuItem.AddItem("Save Set").Click += new GuiMessage(FileMenuMessages.SaveSetClick);
            fileMenuItem.AddItem("Save Active .scnx").Click += SaveActiveScene;
            fileMenuItem.AddItem("Save Instructions Code").Click += SaveInstructionCodeClick;

            #endregion

            #region Add

            MenuItem addMenuItem = AddItem("Add");
            addMenuItem.AddItem("Sprite").Click += AddSpriteClick;
            addMenuItem.AddItem("Text").Click += AddTextClick;

            #endregion

            #region Window

            MenuItem windowMenuItem = AddItem("Window");

            windowMenuItem.AddItem("Editor Camera").Click += ShowEditorCameraWindow;
            windowMenuItem.AddItem("Camera Bounds").Click += ShowSceneCameraWindow;

            windowMenuItem.AddItem("Editor Options").Click += ShowEditorOptionsWindow;
            windowMenuItem.AddItem("Used Members").Click += ShowUsedMembersWindow;

            windowMenuItem.AddItem("Scene Objects").Click += ShowScenePropertyGrid;
            windowMenuItem.AddItem("Tools").Click += ShowToolsWindow;

            #endregion


        }

		
		#endregion

	}
}
