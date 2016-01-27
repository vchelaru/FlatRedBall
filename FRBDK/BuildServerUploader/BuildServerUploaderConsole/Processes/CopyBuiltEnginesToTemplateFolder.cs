using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{


    class CopyBuiltEnginesToTemplateFolder : ProcessStep
    {

        private static readonly List<CopyInformation> mCopyInformation = new List<CopyInformation>();

        static void Add(string file, string destination)
        {
            mCopyInformation.Add(CopyInformation.CreateTemplateCopy(file, destination));
        }

        static void AddFrbdk(string file, string destination)
        {
            mCopyInformation.Add(CopyInformation.CreateFrbdkTemplateCopy(file, destination));
        }

        static void AddDirectory(string fileName, string destination)
        {
            mCopyInformation.AddRange(CopyInformation.CopyDirectory(fileName, destination));
        }

        public static List<CopyInformation> CopyInformationList
        {
            get
            {
                if (mCopyInformation.Count == 0)
                {
                    // XNA 3.1
                    // Discontinued November 23 2014
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.dll",
                    //    @"FlatRedBall XNA Template\FlatRedBallXNATemplate\Libraries\XnaPC");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.xml",
                    //    @"FlatRedBall XNA Template\FlatRedBallXNATemplate\Libraries\XnaPc");
                    //Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna3.1\FlatRedBall.Content.dll",
                    //    @"FlatRedBall XNA Template\FlatRedBallXNATemplate\Libraries\XnaPc");

                    // Silverlight discontinued
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\FlatRedBall.dll",
                    //    @"FlatSilverBallTemplate\FlatSilverBallTemplate\FlatSilverBallTemplate\Libraries\Silverlight");

                    // There is no XML file for FSB (yet)
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\SilverArcade.SilverSprite.Core.dll",
                    //    @"FlatSilverBallTemplate\FlatSilverBallTemplate\FlatSilverBallTemplate\Libraries\Silverlight");
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\SilverArcade.SilverSprite.dll",
                    //    @"FlatSilverBallTemplate\FlatSilverBallTemplate\FlatSilverBallTemplate\Libraries\Silverlight");


                    // XNA 4.0
                    /// Debug
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Debug");
                    /// Release
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Release");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Release");
                    Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll",
                        @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4Pc\Release");

                    // MDX
                    // Reetired March 15, 2015
                    //Add(@"FlatRedBallMDX\bin\Debug\FlatRedBallMdx.dll",
                    //    @"FlatRedBall MDX Template\Libraries");
                    //Add(@"FlatRedBallMDX\bin\Debug\FlatRedBallMdx.xml",
                    //    @"FlatRedBall MDX Template\Libraries");

                    // Windows Phone 7
                    // Discontinued November 21, 2014
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.dll",
                    //    @"FlatRedBallPhoneTemplate\FlatRedBallPhoneTemplate\FlatRedBallPhoneTemplate\Libraries\WindowsPhone");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.xml",
                    //    @"FlatRedBallPhoneTemplate\FlatRedBallPhoneTemplate\FlatRedBallPhoneTemplate\Libraries\WindowsPhone");

                    // Xbox 360 XNA 4
                    // Retired December 12, 2015
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.dll",
                    //    @"FlatRedBallXna4_360Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4_360");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.xml",
                    //    @"FlatRedBallXna4_360Template\FlatRedBallXna4Template\FlatRedBallXna4Template\Libraries\Xna4_360");

                    // Windows8
                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Debug\FlatRedBallW8.dll",
                        @"Windows8Template\Windows8Template\Libraries\Windows8\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Release\FlatRedBallW8.dll",
                        @"Windows8Template\Windows8Template\Libraries\Windows8\Release");                    

                    // Android
                    /// Debug
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.dll",
                        @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\Libraries\Android\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb",
                        @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\Libraries\Android\Debug");
                    /// Release
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll",
                        @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\Libraries\Android\Release");
                    // I don't think the PDB exists for a release build, does it?
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb",
                    //    @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\Libraries\Android\Release");

                    // iOS
                    /// Debug
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.dll",
                        @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\Libraries\iOS\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.pdb",
                        @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\Libraries\iOS\Debug");
                    /// Release
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.dll",
                        @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\Libraries\iOS\Release");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.pdb",
                    //    @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\Libraries\iOS\Release");

                    string sourcePrefix = @"Glue\Glue\Bin\Debug\";
                    string destination = @"GluePluginTemplate\GluePluginTemplate\Libraries\XnaPc";
                    // Glue Plugin Template
                    AddFrbdk(sourcePrefix + "FlatRedBall.dll", destination);
                    AddFrbdk(sourcePrefix + "EditorObjectsXna.dll", destination);
                    AddFrbdk(sourcePrefix + "Glue.exe", destination);
                    AddFrbdk(sourcePrefix + "FlatRedBall.PropertyGrid.dll", destination);
                    AddFrbdk(sourcePrefix + "GlueSaveClasses.dll", destination);
                    AddFrbdk(sourcePrefix + "FlatRedBall.Plugin.dll", destination);

                    
                    AddDirectory(sourcePrefix, @"GluePluginTemplate\TestWithGlue\Glue");

                    
                }
                return mCopyInformation;
            }
        }



        public CopyBuiltEnginesToTemplateFolder(IResults results)
            : base(
            @"Copy all built engine files (.dll) to templates", results)
        {

        }

        public override void ExecuteStep()
        {
            List<CopyInformation> copyInformationList =
                CopyInformationList;
 

            foreach (CopyInformation ci in copyInformationList)
            {
                ci.PerformCopy(Results, "Copied to " + ci.DestinationFile);

            }
        }
    }
}
