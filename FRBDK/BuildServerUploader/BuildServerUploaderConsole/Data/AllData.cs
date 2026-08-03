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

        /// <summary>
        /// Every package published by a release, deduplicated. The DesktopGL entries below share a
        /// package set across two templates, so the same csproj is listed more than once in
        /// <see cref="Engines"/>.
        /// </summary>
        public static IEnumerable<PackageData> AllPackages =>
            Engines.SelectMany(engine => engine.Packages)
                   .GroupBy(package => package.PackageId)
                   .Select(group => group.First());

        static PackageData Package(string packageId, string csProjLocation) => new PackageData
        {
            PackageId = packageId,
            CsProjLocation = csProjLocation,
        };

        // Both DesktopGL templates (.NET 6 and .NET 9) build against the same multi-targeted
        // projects, so they ship the same packages.
        static void AddDesktopGlPackages(EngineData engine)
        {
            engine.Packages.Add(Package("FlatRedBallDesktopGLNet6",
                @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallDesktopGLNet6\FlatRedBallDesktopGLNet6.csproj"));
            engine.Packages.Add(Package("FlatRedBall.StateInterpolation.DesktopNet6",
                @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.DesktopGlNet6\StateInterpolation.DesktopNet6.csproj"));
            engine.Packages.Add(Package("FlatRedBall.GumCore.DesktopGlNet6",
                @"Gum\GumCore\GumCoreXnaPc\GumCore.DesktopGlNet6\GumCore.DesktopGlNet6.csproj"));
            engine.Packages.Add(Package("FlatRedBall.Forms.DesktopGlNet6",
                @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\FlatRedBall.Forms.DesktopGlNet6.csproj"));
            engine.Packages.Add(Package("FlatRedBall.SkiaInGum",
                @"FlatRedBall\Engines\SkiaGum\SkiaInGum.csproj"));
        }

        static AllData()
        {
            // Android (.NET 8)
            {
                var engine = new EngineData();
                engine.Name = "Android .NET 8.0";

                engine.Packages.Add(Package("FlatRedBallAndroid",
                    @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBallAndroid\FlatRedBallAndroid.csproj"));

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


                // Not registered: Engine.yml no longer builds Android, because the net8.0-android
                // workload is end-of-life and absent from the build agents. Every file listed above
                // would therefore be missing, and each process that walks AllData.Engines -- copying
                // to the release folder, copying to templates, zipping templates -- would fail on it.
                // Re-enable together with the Android steps in Engine.yml once the project is
                // retargeted off net8.
                // Engines.Add(engine);
            }


            // iOS .NET 8
            {
                var engine = new EngineData();
                engine.Name = "iOS .NET 8.0";

                engine.Packages.Add(Package("FlatRedBalliOS",
                    @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBalliOS\FlatRedBalliOS.csproj"));

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


                // Not registered -- same reason as Android above: the net8.0-ios workload is
                // end-of-life and absent from the build agents, so none of these files get built.
                // Engines.Add(engine);
            }


            // Desktop GL Net 6
            {
                var engine = new EngineData();
                engine.Name = "MonoGame DesktopGL .NET 6.0";

                AddDesktopGlPackages(engine);

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

                // SkiaInGum lives in FRB's own fork of SkiaGum, not in Gum, and it is not copied
                // into the Forms output folder because Forms only references it under the
                // AutoBuild configurations. Pull it from the fork's own bin folder instead.
                engine.DebugFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Debug\net6.0\SkiaInGum.dll");
                engine.DebugFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Debug\net6.0\SkiaInGum.pdb");


                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Release\net6.0\SkiaInGum.dll");
                engine.ReleaseFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Release\net6.0\SkiaInGum.pdb");


                Engines.Add(engine);

            }
            // Desktop GL Net 9
            {
                var engine = new EngineData();
                engine.Name = "MonoGame DesktopGL .NET 9.0+";

                // Same projects and therefore the same packages as the .NET 6 template above -- the
                // DesktopGL projects multi-target net6.0 and net8.0, so one package serves both.
                AddDesktopGlPackages(engine);

                engine.RelativeToLibrariesDebugFolder = @"DesktopGl\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"DesktopGl\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallDesktopGlMonoGameTemplate\FlatRedBallDesktopGlMonoGameTemplate\";

                // This is the built folder when building FlatRedBall.Forms sln
                // All files below (DebugFiles and ReleaseFiles) should be contained
                // in that output folder because the project should reference those files
                var debugBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\bin\Debug\net8.0\";
                var releaseBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.DesktopGlNet6\bin\Release\net8.0\";


                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.DebugFiles.Add($"{debugBinFolder}GumCore.DesktopGlNet6.dll");
                engine.DebugFiles.Add($"{debugBinFolder}GumCore.DesktopGlNet6.pdb");

                engine.DebugFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Debug\net8.0\SkiaInGum.dll");
                engine.DebugFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Debug\net8.0\SkiaInGum.pdb");


                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallDesktopGLNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.DesktopNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.DesktopGlNet6.pdb");

                engine.ReleaseFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Release\net8.0\SkiaInGum.dll");
                engine.ReleaseFiles.Add($@"FlatRedBall\Engines\SkiaGum\bin\Release\net8.0\SkiaInGum.pdb");


                Engines.Add(engine);

            }
            // FNA Desktop (.net 7)
            {
                var engine = new EngineData();
                engine.Name = "FNA DesktopGL .NET 7.0";

                engine.Packages.Add(Package("FlatRedBall.FNA",
                    @"FlatRedBall\Engines\FlatRedBallXNA\FlatRedBall.FNA\FlatRedBall.FNA.csproj"));
                engine.Packages.Add(Package("FlatRedBall.StateInterpolation.FNA",
                    @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.FNA\StateInterpolation.FNA.csproj"));
                engine.Packages.Add(Package("FlatRedBall.GumCore.FNA",
                    @"Gum\GumCore\GumCoreXnaPc\GumCore.FNA\GumCore.FNA.csproj"));
                engine.Packages.Add(Package("FlatRedBall.Forms.FNA",
                    @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\FlatRedBall.Forms.FNA.csproj"));

                engine.RelativeToLibrariesDebugFolder = @"FNA\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"FNA\Release";
                engine.TemplateCsProjFolder = @"FlatRedBallDesktopFnaTemplate\FlatRedBallDesktopFnaTemplate\";

                // This is the built folder when building FlatRedBall.Forms sln
                // All files below (DebugFiles and ReleaseFiles) should be contained
                // in that output folder because the project should reference those files
                var debugBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\bin\Debug\net8.0\";
                var releaseBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.FNA\bin\Release\net8.0\";

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

            // Web
            {
                var engine = new EngineData();
                engine.Name = "Web";

                engine.Packages.Add(Package("FlatRedBallKniWeb",
                    @"FlatRedBall\Engines\FlatRedBallXNA\KniWeb\FlatRedBallKniWeb.csproj"));
                engine.Packages.Add(Package("FlatRedBall.StateInterpolation.Kni.Web",
                    @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\StateInterpolation\StateInterpolation.Kni.Web\StateInterpolation.Kni.Web.csproj"));
                engine.Packages.Add(Package("FlatRedBall.GumCore.Kni.Web",
                    @"Gum\GumCore\GumCoreXnaPc\GumCore.Kni.Web\GumCore.Kni.Web.csproj"));
                engine.Packages.Add(Package("FlatRedBall.Forms.Kni.Web",
                    @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Kni.Web\FlatRedBall.Forms.Kni.Web.csproj"));

                engine.RelativeToLibrariesDebugFolder = @"Web\Debug";
                engine.RelativeToLibrariesReleaseFolder = @"Web\Release";

                engine.TemplateCsProjFolder = @"FlatRedBallWebTemplate\FlatRedBallWebTemplate\";

                var debugBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Kni.Web\bin\Debug\net8.0\";
                var releaseBinFolder = @"FlatRedBall\Engines\Forms\FlatRedBall.Forms\FlatRedBall.Forms.Kni.Web\bin\Debug\net8.0\";

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.Kni.Web.deps.json");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.Kni.Web.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.Kni.Web.pdb");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBall.Forms.Kni.Web.xml");

                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallKniWeb.dll");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallKniWeb.pdb");
                engine.DebugFiles.Add($"{debugBinFolder}FlatRedBallKniWeb.xml");

                engine.DebugFiles.Add($"{debugBinFolder}GumCore.Kni.Web.dll");
                engine.DebugFiles.Add($"{debugBinFolder}GumCore.Kni.Web.pdb");
                engine.DebugFiles.Add($"{debugBinFolder}GumCore.Kni.Web.xml");

                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.Kni.Web.dll");
                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.Kni.Web.pdb");
                engine.DebugFiles.Add($"{debugBinFolder}StateInterpolation.Kni.Web.xml");

                ///

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.Kni.Web.deps.json");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.Kni.Web.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.Kni.Web.pdb");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBall.Forms.Kni.Web.xml");

                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallKniWeb.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallKniWeb.pdb");
                engine.ReleaseFiles.Add($"{releaseBinFolder}FlatRedBallKniWeb.xml");

                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.Kni.Web.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.Kni.Web.pdb");
                engine.ReleaseFiles.Add($"{releaseBinFolder}GumCore.Kni.Web.xml");

                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.Kni.Web.dll");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.Kni.Web.pdb");
                engine.ReleaseFiles.Add($"{releaseBinFolder}StateInterpolation.Kni.Web.xml");

                Engines.Add(engine);
            }

        }
    }
}
