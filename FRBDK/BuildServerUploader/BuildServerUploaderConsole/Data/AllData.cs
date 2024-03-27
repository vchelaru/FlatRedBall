using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BuildServerUploaderConsole.Data
{
    public static class AllData
    {

        public static List<EngineData> Engines { get; private set; } = new List<EngineData>();

        static AllData()
        {
            {   // XNA 4.0
                //var engine = new EngineData();
                //engine.Name = "XNA 4.0";
                //engine.RelativeToLibrariesDebugFolder = @"Xna4Pc\Debug";
                //engine.RelativeToLibrariesReleaseFolder = @"Xna4Pc\Release";
                //engine.TemplateCsProjFolder = @"FlatRedBallXna4Template\FlatRedBallXna4Template\FlatRedBallXna4Template\";

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\FlatRedBall.Forms.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\FlatRedBall.Forms.pdb");

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\GumCoreXnaPc.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\GumCoreXnaPc.pdb");

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\StateInterpolation.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Debug\StateInterpolation.pdb");

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Debug\Xna4.0\FlatRedBall.xml");

                ////engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Debug\Xna4.0\FlatRedBall.Content.dll");



                //engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.dll");
                //engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\x86\Release\Xna4.0\FlatRedBall.xml");
                //// do we care about this:?
                //// It's not part of .forms so...going to comment it out.
                ////engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.Content\bin\x86\Release\Xna4.0\FlatRedBall.Content.dll");

                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\FlatRedBall.Forms.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\GumCoreXnaPc.dll");
                //engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\x86\Release\StateInterpolation.dll");


                //Engines.Add(engine);
            }

            // Android (old Xamarin)
            //{
            //    var engine = new EngineData();

            //    engine.RelativeToLibrariesDebugFolder = @"Android\Debug";
            //    engine.RelativeToLibrariesReleaseFolder = @"Android\Release";
            //    engine.TemplateCsProjFolder = @"FlatRedBallAndroidTemplate\FlatRedBallAndroidTemplate\";

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Debug\FlatRedBallAndroid.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Debug\StateInterpolation.Android.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Debug\StateInterpolation.Android.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\FlatRedBall.Forms.Android.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\FlatRedBall.Forms.Android.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\GumCoreAndroid.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Debug\GumCoreAndroid.pdb");


            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.dll");
            //    // I don't think we have a .pdb for release projects
            //    //Add(@"FlatRedBallXNA\FlatRedBall\bin\Android\Release\FlatRedBallAndroid.pdb", @"Android\Release");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Release\StateInterpolation.Android.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\Android\Release\StateInterpolation.Android.pdb");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\FlatRedBall.Forms.Android.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\FlatRedBall.Forms.Android.pdb");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\GumCoreAndroid.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms\bin\Android\Release\GumCoreAndroid.pdb");

            //    Engines.Add(engine);
            //}

            // Android (.NET 8)
            {
                var engine = new EngineData();
                engine.Name = "Android .NET 8.0";

                engine.RelativeToLibrariesDebugFolder = @"Android\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Android\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallAndroidMonoGameTemplate\FlatRedBallAndroidMonoGameTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallAndroid\bin\Debug\net8.0-android\FlatRedBallAndroid.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallAndroid\bin\Debug\net8.0-android\FlatRedBallAndroid.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.AndroidMonoGame\bin\Debug\net8.0-android\StateInterpolation.AndroidMonoGame.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.AndroidMonoGame\bin\Debug\net8.0-android\StateInterpolation.AndroidMonoGame.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Debug\net8.0-android\FlatRedBall.Forms.AndroidMonoGame.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Debug\net8.0-android\FlatRedBall.Forms.AndroidMonoGame.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Debug\net8.0-android\GumCoreAndroid.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Debug\net8.0-android\GumCoreAndroid.pdb");



                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallAndroid\bin\Release\net8.0-android\FlatRedBallAndroid.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallAndroid\bin\Release\net8.0-android\FlatRedBallAndroid.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.AndroidMonoGame\bin\Release\net8.0-android\StateInterpolation.AndroidMonoGame.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.AndroidMonoGame\bin\Release\net8.0-android\StateInterpolation.AndroidMonoGame.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Release\net8.0-android\FlatRedBall.Forms.AndroidMonoGame.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Release\net8.0-android\FlatRedBall.Forms.AndroidMonoGame.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Release\net8.0-android\GumCoreAndroid.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.AndroidMonoGame\bin\Release\net8.0-android\GumCoreAndroid.pdb");


                Engines.Add(engine);
            }

            // iOS (old Xamarin)
            //{
            //    var engine = new EngineData();

            //    engine.RelativeToLibrariesDebugFolder = @"iOS\Debug";
            //    engine.RelativeToLibrariesReleaseFolder = @"iOS\Release";
            //    engine.TemplateCsProjFolder = @"FlatRedBalliOSTemplate\FlatRedBalliOSTemplate\";

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Debug\FlatRedBalliOS.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Debug\StateInterpolation.iOS.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Debug\StateInterpolation.iOS.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\FlatRedBall.Forms.iOS.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\FlatRedBall.Forms.iOS.pdb");

            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\GumCoreiOS.dll");
            //    engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Debug\GumCoreiOS.pdb");


            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall\bin\iOS\Release\FlatRedBalliOS.dll");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Release\StateInterpolation.iOS.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\bin\iOS\Release\StateInterpolation.iOS.pdb");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\FlatRedBall.Forms.iOS.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\FlatRedBall.Forms.iOS.pdb");

            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\GumCoreiOS.dll");
            //    engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOS\bin\iOS\Release\GumCoreiOS.pdb");

            //    Engines.Add(engine);
            //}

            // iOS .NET 8
            {
                var engine = new EngineData();
                engine.Name = "iOS .NET 8.0";

                engine.RelativeToLibrariesDebugFolder = @"iOS\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"iOS\Release";
                engine.TemplateCsProjFolder = @"FlatRedBalliOSMonoGameTemplate\FlatRedBalliOSMonoGameTemplate\";

                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBalliOS\bin\Debug\net8.0-ios\FlatRedBalliOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBalliOS\bin\Debug\net8.0-ios\FlatRedBalliOS.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.iOSMonoGame\bin\Debug\net8.0-ios\StateInterpolation.iOSMonoGame.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.iOSMonoGame\bin\Debug\net8.0-ios\StateInterpolation.iOSMonoGame.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Debug\net8.0-ios\FlatRedBall.Forms.iOSMonoGame.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Debug\net8.0-ios\FlatRedBall.Forms.iOSMonoGame.pdb");

                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Debug\net8.0-ios\GumCoreiOS.dll");
                engine.DebugFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Debug\net8.0-ios\GumCoreiOS.pdb");




                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBalliOS\bin\Release\net8.0-ios\FlatRedBalliOS.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBalliOS\bin\Release\net8.0-ios\FlatRedBalliOS.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.iOSMonoGame\bin\Release\net8.0-ios\StateInterpolation.iOSMonoGame.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.iOSMonoGame\bin\Release\net8.0-ios\StateInterpolation.iOSMonoGame.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Release\net8.0-ios\FlatRedBall.Forms.iOSMonoGame.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Release\net8.0-ios\FlatRedBall.Forms.iOSMonoGame.pdb");

                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Release\net8.0-ios\GumCoreiOS.dll");
                engine.ReleaseFiles.Add(@"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.iOSMonoGame\bin\Release\net8.0-ios\GumCoreiOS.pdb");


                Engines.Add(engine);
            }

            // Desktop GL
            {
                var engine = new EngineData();
                engine.Name = "MonoGame DesktopGL .NET 4.X";

                engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallDesktopGlTemplate\FlatRedBallDesktopGlTemplate\";

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
                engine.Name = "MonoGame DesktopGL .NET 6.0";

                engine.EngineCSProjLocation = @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj";

                engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallDesktopGlNet6Template\FlatRedBallDesktopGlNet6Template\";
                                       
                // This is the built folder when building FlatRedBall.Forms sln
                // All files below (DebugFiles and ReleaseFiles) should be contained
                // in that output folder because the project should reference those files
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

                engine.DebugFiles.Add($@"Gum\SvgPlugin\SkiaInGumShared\bin\Debug\net6.0\SkiaInGum.dll");
                engine.DebugFiles.Add($@"Gum\SvgPlugin\SkiaInGumShared\bin\Debug\net6.0\SkiaInGum.pdb");


                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($@"Gum\SvgPlugin\SkiaInGumShared\bin\Release\net6.0\SkiaInGum.dll");
                engine.ReleaseFiles.Add($@"Gum\SvgPlugin\SkiaInGumShared\bin\Release\net6.0\SkiaInGum.pdb");


                Engines.Add(engine);

            }
            // FNA Desktop
            {
                var engine = new EngineData();
                engine.Name = "FNA DesktopGL .NET 7.0";

                engine.EngineCSProjLocation = @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.FNA\FlatRedBall.FNA.csproj";

                engine.RelativeToLibrariesDebugFolder = @"FNA\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"FNA\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallDesktopFnaTemplate\FlatRedBallDesktopFnaTemplate\";

                // This is the built folder when building FlatRedBall.Forms sln
                // All files below (DebugFiles and ReleaseFiles) should be contained
                // in that output folder because the project should reference those files
                var debugBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\bin\Debug\net7.0\";
                var releaseBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\bin\Release\net7.0\";

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.FNA.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.FNA.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.FNA.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.FNA.deps.json");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.FNA.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}FNA.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FNA.dll.config");
                engine.DebugFiles.Add($"{debugBinFolder}FNA.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}GumCore.FNA.dll");
                engine.DebugFiles.Add($"{debugBinFolder}GumCore.FNA.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.FNA.dll");
                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.FNA.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.FNA.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.FNA.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.FNA.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.FNA.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FNA.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FNA.dll.config");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FNA.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.FNA.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.FNA.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.FNA.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.FNA.pdb");

                Engines.Add(engine);
            }

        }
    }
}
