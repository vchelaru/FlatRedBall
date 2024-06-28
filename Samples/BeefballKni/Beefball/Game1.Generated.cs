using System.Linq;
namespace Beefball
{
    public partial class Game1
    {
        partial void GeneratedInitializeEarly () 
        {
        }
        partial void GeneratedInitialize () 
        {
            global::Beefball.GlobalContent.Initialize();
            var args = System.Environment.GetCommandLineArgs();
            bool? changeResize = null;
            var resizeArgs = args.FirstOrDefault(item => item.StartsWith("AllowWindowResizing="));
            if (!string.IsNullOrEmpty(resizeArgs))
            {
                var afterEqual = resizeArgs.Split('=')[1];
                changeResize = bool.Parse(afterEqual);
            }
            if (changeResize != null)
            {
                CameraSetup.Data.AllowWindowResizing = changeResize.Value;
            }
            CameraSetup.SetupCamera(FlatRedBall.Camera.Main, graphics);
            System.Type startScreenType = typeof(global::Beefball.Screens.GameScreen);
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 0)
            {
                var thisAssembly = this.GetType().Assembly;
                // see if any of these are screens:
                foreach (var item in commandLineArgs)
                {
                    var type = thisAssembly.GetType(item);
                    if (type != null)
                    {
                        startScreenType = type;
                        break;
                    }
                }
            }
            if (startScreenType != null)
            {
                FlatRedBall.Screens.ScreenManager.Start(startScreenType);
            }
            // This value is used for parallax. If the game doesn't change its resolution, this this code should solve parallax with zooming cameras.
            global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraWidth = global::Beefball.CameraSetup.Data.ResolutionWidth;
            global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraHeight = global::Beefball.CameraSetup.Data.ResolutionHeight;
        }
        partial void GeneratedUpdate (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
        partial void GeneratedDrawEarly (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
        partial void GeneratedDraw (Microsoft.Xna.Framework.GameTime gameTime) 
        {
        }
    }
}
