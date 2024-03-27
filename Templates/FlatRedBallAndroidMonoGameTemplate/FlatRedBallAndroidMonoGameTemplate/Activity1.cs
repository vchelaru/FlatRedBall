using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace FlatRedBallAndroidMonoGameTemplate
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@drawable/icon",
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
    )]
    public class Activity1 : AndroidGameActivity
    {
        private Game1 _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            _game = new Game1();

            // FRB needs access to the activity to load fonts from the content:
            _game.Services.AddService<Activity>(this);

            _view = _game.Services.GetService(typeof(View)) as View;

            _view.KeyPress += HandleKeyPress;
            _view.GenericMotion += HandleGenericMotion;

            SetContentView(_view);
            _game.Run();
        }


        private void HandleGenericMotion(object sender, View.GenericMotionEventArgs e)
        {
            if ((e.Event.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad ||
                (e.Event.Source & InputSourceType.Joystick) == InputSourceType.Joystick)
            {
                //FlatRedBallAndroid.Input.AndroidGamePadManager.OnGenericMotionEvent(e.Event);
            }
        }

        private void HandleKeyPress(object sender, View.KeyEventArgs e)
        {
            // e.Handled defaults to true, surprisingly. Need to explicitly set
            // e.Handled to false if we dont handle, it so that functionality like
            // changing the volume still works.
            bool handled = false;

            if ((e.Event.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                if (e.Event.Action == KeyEventActions.Down)
                {
                    //FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyDown(e.KeyCode, e.Event);
                    handled = true;
                }
                if (e.Event.Action == KeyEventActions.Up)
                {
                    //FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyUp(e.KeyCode, e.Event);
                    handled = true;
                }
            }

            e.Handled = handled;
        }
    }
}
