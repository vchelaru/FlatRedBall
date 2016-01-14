using System.Collections.Generic;

namespace BuildServerUploaderConsole.Processes
{


    class CopyBuiltEnginesToReleaseFolder : ProcessStep
    {

        private static readonly List<CopyInformation> _copyInformation = new List<CopyInformation>();

        static void Add(string file, string category)
        {
            _copyInformation.Add(CopyInformation.CreateEngineCopy(file, category));
        }

        public static List<CopyInformation> CopyInformationList
        {
            get
            {
                if (_copyInformation.Count == 0)
                {
                    // FlatRedBall XNA 3.1 discontinued
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.dll","Xna");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna3.1\FlatRedBall.xml","Xna");
                    //Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna3.1\FlatRedBall.Content.dll", "Xna");

                    // FSB discontinued
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\SilverArcade.SilverSprite.Core.dll", "Silverlight");
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\SilverArcade.SilverSprite.dll", "Silverlight");
                    //Add(@"FlatSilverBall\FlatSilverBall\Bin\Debug\FlatRedBall.dll", "Silverlight");

                    {
                        Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll", @"Xna4Pc\Debug");
                        Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml", @"Xna4Pc\Debug");
                        Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll", @"Xna4Pc\Debug");

                        Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll", @"Xna4Pc\Release");
                        Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml", @"Xna4Pc\Release");
                        Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll", @"Xna4Pc\Release");
                    }
                    // Retired March 15, 2015
                    //Add(@"FlatRedBallMDX\bin\Debug\FlatRedBallMdx.dll","Mdx");
                    //Add(@"FlatRedBallMDX\bin\Debug\FlatRedBallMdx.xml", "Mdx");

                    // Retired November 21, 2014
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.dll", "WindowsPhone");
                    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Windows Phone\Debug\FlatRedBall.xml", "WindowsPhone");


                    Add(@"FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.dll", "Xna4_360");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\Xbox 360\Debug\XNA4\FlatRedBall.xml", "Xna4_360");

                    {
                        Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.dll", @"Android\Debug");
                        Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb", @"Android\Debug");

                        Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll", @"Android\Release");
                        // I don't think we have a .pdb for release projects
                        //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.pdb", @"Android\Release");
                    
                    }

                    Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.dll", @"iOS\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.pdb", @"iOS\Debug");


                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Debug\FlatRedBallW8.dll", @"Windows8\Debug");
                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Debug\FlatRedBallW8.pdb", @"Windows8\Debug");

                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Release\FlatRedBallW8.dll", @"Windows8\Release");
                    Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Release\FlatRedBallW8.pdb", @"Windows8\Release");

                }
                return _copyInformation;
            }
        }



        public CopyBuiltEnginesToReleaseFolder(IResults results)
            : base(
            @"Copy all built engine files (.dll) to release folder", results)
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
