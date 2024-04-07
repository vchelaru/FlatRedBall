using FlatRedBall.Instructions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    internal class InjectFnaNugetAndVersion : ProcessStep
    {
        public bool IsBeta { get; set; }
        public InjectFnaNugetAndVersion(IResults results, bool isBeta) : base("Injects the FNA NuGet package and updates the version number.", results)
        {
            IsBeta = isBeta;
        }

        public override void ExecuteStep()
        {
            var csprojLocation = 
                Path.Combine(DirectoryHelper.CheckoutDirectory, @"FlatRedBall\Engines\FlatRedBallXNA\3rd Party Libraries\FNA\FNA.Core.csproj");

            var contents = File.ReadAllText(csprojLocation);

            var index = contents.IndexOf("</PropertyGroup>");

            var whatToInject = $@"
        <PackageId>FNA_for_FlatRedBall</PackageId>
		<Title>FNA (for FlatRedBall.FNA)</Title>
		<GeneratePackageOnBuild>True</GeneratePackageOnBuild>
		<Description>FNA Core Nuget package created for FlatRedBall. This is NOT an official NuGet package from the FNA team, although you are free to use it in any FNA project.</Description>
		<PackageLicenseExpression>MIT</PackageLicenseExpression>
		<Version>{UpdateAssemblyVersions.GetVersionString(IsBeta)}</Version>
    ";

            contents = contents.Insert(index, whatToInject);

            File.WriteAllText(csprojLocation, contents);
        }
    }
}
