using FlatRedBall.Gui;

namespace LevelEditor.Gui
{
    public static class GuiData
    {
        static CameraAndScreenControlWindow _cameraAndScreenControlWindow;
        public static LocalizationWindow LocalizationWindow
        {
            get;
            private set;
        }

        public static void Initialize()
        {
            _cameraAndScreenControlWindow = new CameraAndScreenControlWindow(GuiManager.Cursor);
            GuiManager.AddWindow(_cameraAndScreenControlWindow);

            // Vic asks:  Why is this here?!?  Should be moved to EditorLogic.cs I think.
            FlatRedBall.SpriteManager.AutoIncrementParticleCountValue = 500;

            LocalizationWindow = new LocalizationWindow(GuiManager.Cursor);
            GuiManager.AddWindow(LocalizationWindow);
            LocalizationWindow.X = LocalizationWindow.ScaleX + _cameraAndScreenControlWindow.ScaleX * 2;
            LocalizationWindow.Y = LocalizationWindow.ScaleY + Window.MoveBarHeight;
        }
    }
}
