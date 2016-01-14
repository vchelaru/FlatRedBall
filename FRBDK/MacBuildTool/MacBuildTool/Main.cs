using System;

namespace MacBuildTool
{
	class MainClass
	{
		static CommandExecuter mCommandExecuter;

		public static void Main (string[] args)
		{
			mCommandExecuter = new CommandExecuter();


			mCommandExecuter.BuildFrbIos ();
			mCommandExecuter.CopyToTemplates();
			mCommandExecuter.CreateZipFile();
			mCommandExecuter.UploadToFtp();


			Console.WriteLine("----Automated Build Succeeded.  Press ENTER to continue.----");
			// I don't think we need
			// to do this on the mac.
			// Looks like we do.
			System.Console.ReadLine();
			
		}
	}
}
