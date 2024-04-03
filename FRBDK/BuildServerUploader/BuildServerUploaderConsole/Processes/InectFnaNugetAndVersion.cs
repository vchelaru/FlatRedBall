using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildServerUploaderConsole.Processes
{
    internal class InectFnaNugetAndVersion : ProcessStep
    {
        public InectFnaNugetAndVersion(IResults results) : base("Injects the FNA NuGet package and updates the version number.", results)
        {
        }

        public override void ExecuteStep()
        {
            var csprojLocation = 
                Path.Combine(DirectoryHelper.CheckoutDirectory + @"FlatRedBall\Engines\FlatRedBallXNA\3rd Party Libraries\FNA\FNA.Core.csproj");

            var contents = File.ReadAllText(csprojLocation);

            var index = contents.IndexOf("</PropertyGroup>");

            var whatToInject = @"
		<Title>FNA (for FlatRedBall.FNA)</Title>
		<GeneratePackageOnBuild>True</GeneratePackageOnBuild>
		<Description>FNA Core Nuget package created for FlatRedBall. This is NOT an official NuGet package from the FNA team, although you are free to use it in any FNA project.</Description>
		<PackageLicenseExpression>MIT</PackageLicenseExpression>
		<Version>1.0.0</Version>";

            contents = contents.Insert(index, whatToInject);

            File.WriteAllText(csprojLocation, contents);
        }
    }
}
