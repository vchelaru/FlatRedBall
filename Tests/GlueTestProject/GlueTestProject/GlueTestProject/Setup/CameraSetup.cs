using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall;
using Microsoft.Xna.Framework;

#if !FRB_MDX
using System.Linq;
#endif

namespace GlueTestProject
{
	internal static class CameraSetup
	{
			internal static void SetupCamera (Camera cameraToSetUp, GraphicsDeviceManager graphicsDeviceManager)
			{
				SetupCamera(cameraToSetUp, graphicsDeviceManager, 800, 600);
			}
			internal static void SetupCamera (Camera cameraToSetUp, GraphicsDeviceManager graphicsDeviceManager, int width, int height)
			{
				#if !WINDOWS_PHONE && !WINDOWS_8 && !IOS && !ANDROID
				FlatRedBall.FlatRedBallServices.GraphicsOptions.SetResolution(width, height);
				#endif
				#if WINDOWS_PHONE || WINDOWS_8 || IOS || ANDROID
				if (height > width)
				{
					graphicsDeviceManager.SupportedOrientations = DisplayOrientation.Portrait;
				}
				else
				{
					graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
				}
				#endif
				cameraToSetUp.UsePixelCoordinates();
			}
			internal static void ResetCamera (Camera cameraToReset)
			{
				cameraToReset.X = 0;
				cameraToReset.Y = 0;
				cameraToReset.XVelocity = 0;
				cameraToReset.YVelocity = 0;
			}

	}
}
