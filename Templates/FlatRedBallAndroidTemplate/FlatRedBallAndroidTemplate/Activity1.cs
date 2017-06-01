#if ANDROID

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace FlatRedBallAndroidTemplate
{
    [Activity(Label = "FlatRedBallAndroidTemplate"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            var g = new Game1();

            var view = (OpenTK.Platform.Android.AndroidGameView)g.Services.GetService(typeof(View));

            view.KeyPress += HandleKeyPress;
            view.GenericMotion += HandleGenericMotion;
            SetContentView(view);
            g.Run();
        }

        private void HandleGenericMotion(object sender, View.GenericMotionEventArgs e)
        {
            if ((e.Event.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad ||
                (e.Event.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                FlatRedBallAndroid.Input.AndroidGamePadManager.OnGenericMotionEvent(e.Event);
            }
        }

        private void HandleKeyPress(object sender, View.KeyEventArgs e)
        {
            if ((e.Event.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                if (e.Event.Action == KeyEventActions.Down)
                {
                    FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyDown(e.KeyCode, e.Event);
                }
                if (e.Event.Action == KeyEventActions.Up)
                {
                    FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyUp(e.KeyCode, e.Event);
                }
            }
        }

    }
}


#endif
