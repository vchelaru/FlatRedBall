using System;

namespace ChickenClicker.Screens
{
    public partial class MenuScreen
    {
        void CustomInitialize()
        {
            SetupButtonClicks();
        }

        private void SetupButtonClicks()
        {
            Forms.StartGameButton.Click += StartGame;
            Forms.OptionsMenuButton.Click += OptionsMenu;
        }

        private void StartGame(object sender, EventArgs e)
        {
            MoveToScreen(nameof(GameScreen));
        }

        private void OptionsMenu(object sender, EventArgs e)
        {
            MoveToScreen(nameof(OptionsScreen));
        }


        void CustomActivity(bool firstTimeCalled)
        {

        }

        void CustomDestroy()
        {

        }

        static void CustomLoadStaticContent(string contentManagerName)
        {

        }

    }
}
