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

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\FlatRedBall.Forms.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\FlatRedBall.Forms.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\GumCoreXnaPc.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\GumCoreXnaPc.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\StateInterpolation.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\StateInterpolation.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml");

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll");



                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml");
                // do we care about this:?
                // It's not part of .forms so...going to comment it out.
                //engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\FlatRedBall.Forms.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\GumCoreXnaPc.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\StateInterpolation.dll");


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

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Debug\StateInterpolation.Android.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Debug\StateInterpolation.Android.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\FlatRedBall.Forms.Android.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\FlatRedBall.Forms.Android.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\GumCoreAndroid.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\GumCoreAndroid.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll");
                // I don't think we have a .pdb for release projects
                //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.pdb", @"Android\Release");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Release\StateInterpolation.Android.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Release\StateInterpolation.Android.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\FlatRedBall.Forms.Android.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\FlatRedBall.Forms.Android.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\GumCoreAndroid.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\GumCoreAndroid.pdb");

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

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Debug\StateInterpolation.iOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Debug\StateInterpolation.iOS.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\FlatRedBall.Forms.iOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\FlatRedBall.Forms.iOS.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\GumCoreiOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\GumCoreiOS.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.dll");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Release\StateInterpolation.iOS.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Release\StateInterpolation.iOS.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\FlatRedBall.Forms.iOS.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\FlatRedBall.Forms.iOS.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\GumCoreiOS.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\GumCoreiOS.pdb");

                Engines.Add(engine);
            }

            // UWP
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"UWP\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"UWP\Release";
                engine.TemplateFolder = @"FlatRedBallUwpTemplate\FlatRedBallUwpTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\FlatRedBall.Forms.Uwp.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\FlatRedBall.Forms.Uwp.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\GumCoreUwp.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\GumCoreUwp.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\FlatRedBallUwp.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\FlatRedBallUwp.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\StateInterpolation.Uwp.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\Debug\StateInterpolation.Uwp.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\FlatRedBall.Forms.Uwp.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\FlatRedBall.Forms.Uwp.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\GumCoreUwp.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\GumCoreUwp.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\FlatRedBallUwp.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\FlatRedBallUwp.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\StateInterpolation.Uwp.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Uwp\bin\x86\Release\StateInterpolation.Uwp.pdb");


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

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\DesktopGL\Debug\StateInterpolation.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\DesktopGL\Debug\StateInterpolation.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Debug\FlatRedBall.Forms.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Debug\FlatRedBall.Forms.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Debug\GumCoreXnaPc.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Debug\GumCoreXnaPc.pdb");


                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\DesktopGL\Release\FlatRedBallDesktopGL.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\DesktopGL\Release\StateInterpolation.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\DesktopGL\Release\StateInterpolation.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Release\FlatRedBall.Forms.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Release\FlatRedBall.Forms.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Release\GumCoreXnaPc.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\DesktopGL\Release\GumCoreXnaPc.pdb");


                Engines.Add(engine);

            }

            // Desktop GL Net 6
            {
                var engine = new EngineData();

                engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                engine.TemplateFolder = @"FlatRedBallDesktopGlNet6Template\FlatRedBallDesktopGlNet6Template\";

                var debugBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\bin\Debug\net6.0\";
                var releaseBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\bin\Release\net6.0\";


                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}GumCore.DesktopGlNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}GumCore.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.pdb");

                Engines.Add(engine);

            }


        }
    }
}
