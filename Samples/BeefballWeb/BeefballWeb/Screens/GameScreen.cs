using FlatRedBall.Input;

namespace Beefball.Screens
{
    public partial class GameScreen
    {
        int player1Score = 0;
        int player2Score = 0;

        void CustomInitialize()
        {
            AssignInput();
        }

        private void AssignInput()
        {
            if (InputManager.Xbox360GamePads[0].IsConnected)
            {
                PlayerBall1.MovementInput =
                    InputManager.Xbox360GamePads[0].LeftStick;
                PlayerBall1.BoostInput =
                    InputManager.Xbox360GamePads[0].GetButton(Xbox360GamePad.Button.A);
            }
            else
            {
                PlayerBall1.MovementInput =
                    InputManager.Keyboard.Get2DInput(Microsoft.Xna.Framework.Input.Keys.A,
                    Microsoft.Xna.Framework.Input.Keys.D,
                    Microsoft.Xna.Framework.Input.Keys.W,
                    Microsoft.Xna.Framework.Input.Keys.S);
                PlayerBall1.BoostInput =
                    InputManager.Keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.B);
            }

            if (InputManager.Xbox360GamePads[1].IsConnected)
            {
                PlayerBall2.MovementInput =
                    InputManager.Xbox360GamePads[1].LeftStick;
                PlayerBall2.BoostInput =
                    InputManager.Xbox360GamePads[1].GetButton(Xbox360GamePad.Button.A);
            }
            else
            {
                PlayerBall2.MovementInput =
                    InputManager.Keyboard.Get2DInput(Microsoft.Xna.Framework.Input.Keys.Left,
                    Microsoft.Xna.Framework.Input.Keys.Right,
                    Microsoft.Xna.Framework.Input.Keys.Up,
                    Microsoft.Xna.Framework.Input.Keys.Down);
                PlayerBall2.BoostInput =
                    InputManager.Keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.RightShift);
            }
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
