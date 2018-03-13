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

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll");

                Engines.Add(engine);
            }

            // Android
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"Android\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Android\Release";
                engine.TemplateFolder = @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll");
                // I don't think we have a .pdb for release projects
                //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.pdb", @"Android\Release");
                Engines.Add(engine);
            }

            // iOS
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"iOS\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"iOS\Release";
                engine.TemplateFolder = @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.dll");

                Engines.Add(engine);
            }

            // UWP
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"UWP\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"UWP\Release";
                engine.TemplateFolder = @"FlatRedBallUwpTemplate\FlatRedBallUwpTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\x86\Debug\FlatRedBallUwp.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\x86\Debug\FlatRedBallUwp.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\x86\Release\FlatRedBallUwp.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\FlatRedBallUwp\bin\x86\Release\FlatRedBallUwp.pdb");

                Engines.Add(engine);
            }

            // Desktop GL
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                engine.TemplateFolder = @"FlatRedBallDesktopGlTemplate\FlatRedBallDesktopGlTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Debug\FlatRedBallDesktopGL.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Debug\FlatRedBallDesktopGL.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.pdb");

                Engines.Add(engine);

            }

            {
                // I think we can use the regular desktopgl instead of a linux specific
                //var engine = new EngineData();

                //engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                //engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                //// This template is a copy of the regular DesktopGL template, so it uses FlatRedBallDesktopGlTemplate as an internal folder
                ////engine.TemplateFolder = @"FlatRedBallDesktopGlLinuxTemplate\FlatRedBallDesktopGlLinuxTemplate\";
                //engine.TemplateFolder = @"FlatRedBallDesktopGlLinuxTemplate\FlatRedBallDesktopGlTemplate\";

                //engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Debug\FlatRedBallDesktopGL.dll");
                //engine.DebugFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Debug\FlatRedBallDesktopGL.pdb");

                //engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.dll");
                //engine.ReleaseFiles.Add(@"FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.pdb");

                //Engines.Add(engine);

            }

        }
    }
}
