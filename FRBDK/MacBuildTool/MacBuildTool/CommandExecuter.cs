using System;
using System.Diagnostics;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.Linq;

namespace MacBuildTool
{
	public class CommandExecuter
	{
		#region const fields

		const string mSlnDirectory = "/Users/vchelaru/FlatRedBallProjects/Engines/FlatRedBallXNA/FlatRedBall/";
		const string mBuildDirectory = mSlnDirectory + "bin/Debug/";

		const string mTemplateLocation = "/Users/vchelaru/FlatRedBallProjects/CodeTemplates/FlatRedBalliOSTemplate/";
		const string mTemplateLibraryLocation = 
			mTemplateLocation + "FlatRedBalliOSTemplate/Libraries/iOS/";
	

		const string mRemoteZipLocation =
			"ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/EarlyRelease/FlatRedBalliOSTemplate.zip";

		const string mSingleDllLocation = 
			"ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/DailyBuild/SingleDlls/iOS/";

		#endregion

		
		List<string> mDlls = new List<string> ()
		{
			"FlatRedBalliOS.dll",
			"FlatRedBalliOS.dll.mdb",
			"Lidgren.Network.dll",
			"Lidgren.Network.dll.mdb",
			"MonoGame.Framework.dll",
			"MonoGame.Framework.dll.mdb"
		};

		const string mZippedTemplateFile = 
			mTemplateLocation + "FlatRedBalliOSTemplate.zip";

		public CommandExecuter ()
		{


		}

		public void BuildFrbIos ()
		{
			string processFileName = "/Applications/MonoDevelop.app/Contents/MacOS/mdtool";
			string arguments = "build " + 
				mSlnDirectory + "FlatRedBalliOS.sln";
			var process = Process.Start (processFileName, arguments);

			while (!process.HasExited) 
			{
				System.Threading.Thread.Sleep (100);
			}

			System.Console.WriteLine ("Done compiling");
		}

		public void CopyToTemplates ()
		{

			foreach (string file in mDlls) 
			{
				string sourceFile = mBuildDirectory + file;

				string destinationFile = mTemplateLibraryLocation + file;

				System.IO.File.Copy (sourceFile, destinationFile, true);
			}
			System.Console.WriteLine ("Done copying files");
		}

		public void CreateZipFile()
		{
			// Delete the file if it exists
			if (System.IO.File.Exists (mZippedTemplateFile))
			{
				System.IO.File.Delete (mZippedTemplateFile);
			}

			// Now create it
			var allFiles = FileManager.GetAllFilesInDirectory (mTemplateLocation);

			using (ZipFile zipFile = ZipFile.Create (mZippedTemplateFile))
			{
				zipFile.BeginUpdate();

				foreach (string file in allFiles)
				{
					bool shouldSkip = file.Contains ("/.svn/") ||
						file.Contains ("/.DS_Store");

					if(!shouldSkip)
					{
						string sourceFile = file;
						string destinationFile = FileManager.MakeRelative (file, mTemplateLocation);

						zipFile.Add (sourceFile, destinationFile);

						System.Console.WriteLine("Added to zip: " + destinationFile);
					}
				}
				zipFile.CommitUpdate ();
			}
		}
	
		public void UploadToFtp()
		{
			System.Console.WriteLine ("Starting uploads...");
			string username = "frbadmin";

			string directory = 
				System.IO.Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly ().GetName ().CodeBase) + "/";

			string passwordLocationFile = directory + "pw.txt";
			// chop off "file:"
			passwordLocationFile = passwordLocationFile.Substring (5);
			//string password = "bYs3LKAp4T68Og7NQc797";
			string password = System.IO.File.ReadAllText(passwordLocationFile);


			bool succeeded = false;
			string errorString = null;

			try
			{
				FtpManager.UploadFile (mZippedTemplateFile, mRemoteZipLocation, username, password, false);
				System.Console.WriteLine("Uploaded to " + mRemoteZipLocation);

				foreach (string file in mDlls)
				{

					string sourceFile = mBuildDirectory + file;
					string destinationFile = FileManager.MakeRelative (file, mTemplateLocation);
					string remoteFileLocation = mSingleDllLocation + FileManager.RemoveDirectory(file);

					

					FtpManager.UploadFile(sourceFile, remoteFileLocation, username, password, false);
					System.Console.WriteLine("Uploaded to " + remoteFileLocation);
				}






				succeeded = true;
			}
			catch(Exception exception)
			{
				errorString = exception.ToString();
			}

			if (succeeded)
			{
				System.Console.WriteLine ("Uploads succeeded");
			}
			else
			{
				System.Console.WriteLine("Uploads failed");
				System.Console.Write (errorString);
			}

		}
	
	}
}

