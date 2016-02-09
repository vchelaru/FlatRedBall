using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BuildVerifier
{
    class Program
    {
        static void Main()
        {
            const string root = "http://files.flatredball.com/content/FrbXnaTemplates/";

            // February 8, 2016
            // I'm disabling this for now, builds seem to be working well and everything migrated. Maybe we'll bring it back
            // if builds start failing or if there are other problems, but I don't see the need to maintain this now.


            var files = new List<string>
                            {
                                //root + "BackupFolders.txt",
                                root + "DailyBuild/FRBDK.zip",
                                "http://files.flatredball.com/FlatRedBallInstaller.exe",

                                //root + "DailyBuild/SingleDlls/Fsb/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/Fsb/SilverArcade.SilverSprite.Core.dll",
                                //root + "DailyBuild/SingleDlls/Fsb/SilverArcade.SilverSprite.dll",

                                //root + "DailyBuild/SingleDlls/Mdx/FlatRedBallMdx.dll",
                                //root + "DailyBuild/SingleDlls/Mdx/FlatRedBallMdx.xml",

                                //root + "DailyBuild/SingleDlls/MonoDroid/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/MonoDroid/Lidgren.Network.Android.dll",
                                //root + "DailyBuild/SingleDlls/MonoDroid/MonoGame.Framework.dll",

                                //root + "DailyBuild/SingleDlls/WindowsPhone/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/WindowsPhone/FlatRedBall.xml",

                                // No more FRB XNA 3.1
                                //root + "DailyBuild/SingleDlls/Xna/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/Xna/FlatRedBall.xml",
                                //root + "DailyBuild/SingleDlls/Xna/FlatRedBall.Content.dll",

                                //root + "DailyBuild/SingleDlls/Xna4_360/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/Xna4_360/FlatRedBall.xml",

                                //root + "DailyBuild/SingleDlls/Xna4Pc/FlatRedBall.dll",
                                //root + "DailyBuild/SingleDlls/Xna4Pc/FlatRedBall.xml",
                                //root + "DailyBuild/SingleDlls/Xna4Pc/FlatRedBall.Content.dll",

                                //root + "DailyBuild/ZippedTemplates/FlatRedBallMDXTemplate.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatRedBallMonoDroidTemplate.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatRedBallPhoneTemplate.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatRedBallXNATemplate.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatRedBallXna4Template.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatRedBallXna4_360Template.zip",
                                //root + "DailyBuild/ZippedTemplates/FlatSilverBallTemplate.zip",
                                //root + "DailyBuild/ZippedTemplates/GluePluginTemplate.zip"
                            };

            //Add verify files

            foreach (var file in files.Where(file => !VerifyFileExistsAndIsCurrent(file)))
            {
                throw new Exception("File doesn't exist or is out of date: " + file);
            }
        }

        private static bool VerifyFileExistsAndIsCurrent(string file)
        {
            try
            {
                var url = new Uri(file);
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                response.Close();

                var fileSize = response.ContentLength;
                if (fileSize <= 0)
                    return false;

                var lastModified = response.LastModified;
                if (lastModified.Date < new DateTime().Date)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
