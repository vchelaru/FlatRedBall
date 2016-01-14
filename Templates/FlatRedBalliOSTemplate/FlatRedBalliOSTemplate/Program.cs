
using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace FlatRedBalliOSTemplate
{
	[Register("AppDelegate")]
	class Program : UIApplicationDelegate
	{
		Game1 game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new Game1 ();
			game.Run ();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			UIApplication.Main (args, null, "AppDelegate");
		}
	}    
}


