using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildServerUploaderConsole.Data
{
    public static class AllData
    {

        public static List<EngineData> Engines { get; private set; } = new List<EngineData>();

        static AllData()
        {
            {   // XNA 4.0
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"Xna4Pc\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Xna4Pc\Release";
                engine.TemplateFolder = @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\";

                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll");

                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll");
                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml");
                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll");

                Engines.Add(engine);
            }

            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"Android\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Android\Release";
                engine.TemplateFolder = @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\";

                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll");
                // I don't think we have a .pdb for release projects
                //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.pdb", @"Android\Release");
                Engines.Add(engine);
            }

            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"iOS\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"iOS\Release";
                engine.TemplateFolder = @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\";

                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.dll");

                Engines.Add(engine);
            }

            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"Windows8\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Windows8\Release";
                engine.TemplateFolder = @"Windows8Template\Windows8Template\";

                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Debug\FlatRedBallW8.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Debug\FlatRedBallW8.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Release\FlatRedBallW8.dll");
                engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBallW8\bin\Release\FlatRedBallW8.pdb");

                Engines.Add(engine);
            }

            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"UWP\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"UWP\Release";
                engine.TemplateFolder = @"FlatRedBallUwpTemplate\FlatRedBallUwpTemplate\";

                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\Debug\FlatRedBallUwp.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\Debug\FlatRedBallUwp.pdb");


                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\Release\FlatRedBallUwp.dll");
                engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\Release\FlatRedBallUwp.pdb");

                Engines.Add(engine);
            }

        }
    }
}
