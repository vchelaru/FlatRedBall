#if ANDROID

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace AdMobFrb
{
    [Activity(Label = "AdMobFrb"
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

            // FRB needs access to the activity to load fonts from the content:
            g.Services.AddService<Activity>(this);


            var view = g.Services.GetService<View>();



            MonoGameAdMob.IAdManager ads = new MonoGameAdMob.AdMobAdapter(this, "ca-app-pub-3940256099942544/6300978111");
            ads.ShowBannerAd(MonoGameAdMob.BannerPosition.BottomBanner, 46);

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
            // e.Handled defaults to true, surprisingly. Need to explicitly set
            // e.Handled to false if we dont handle, it so that functionality like
            // changing the volume still works.
            bool handled = false;

            if ((e.Event.Source & InputSourceType.Gamepad) == InputSourceType.Gamepad)
            {
                if (e.Event.Action == KeyEventActions.Down)
                {
                    FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyDown(e.KeyCode, e.Event);
                    handled = true;
                }
                if (e.Event.Action == KeyEventActions.Up)
                {
                    FlatRedBallAndroid.Input.AndroidGamePadManager.OnKeyUp(e.KeyCode, e.Event);
                    handled = true;
                }
            }

            e.Handled = handled;
        }

    }
}


#endif
