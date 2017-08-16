#if ANDROID

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace GlueTestProject
{
    [Activity(Label = "GlueTestProject"
        , MainLauncher = true
        , Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
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


            SetContentView((View)g.Services.GetService(typeof(View)));
            g.Run();
        }
    }
}


#endif
