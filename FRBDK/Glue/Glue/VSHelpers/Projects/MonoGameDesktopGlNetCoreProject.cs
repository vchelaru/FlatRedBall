using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using Mono.Cecil;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class MonoGameDesktopGlNetCoreProject : MonoGameDesktopGlBaseProject
    {
        const string FlatRedBallDll = "FlatRedBallDesktopGL.dll";

        public override string NeededVisualStudioVersion
        {
            get { return "14.0"; }

        }

        public MonoGameDesktopGlNetCoreProject(Project project) : base(project)
        {
        }

        protected override void FindSyntaxVersion()
        {
            // do nothing until we get the code working to properly read version from SyntaxVersion
            return;

            var referenceItem = this.EvaluatedItems
                .FirstOrDefault(item =>
                    item.ItemType == "Reference" && item.EvaluatedInclude.EndsWith(FlatRedBallDll));

            var nugetItem = this.EvaluatedItems
                .FirstOrDefault(item =>
                    item.ItemType == "PackageReference" && item.EvaluatedInclude == "FlatRedBallDesktopGLNet6" );


            FilePath dllLocation = null;

            if(nugetItem != null)
            {
                var globalPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

                var versionItem = nugetItem.GetMetadata("Version");

                if (string.IsNullOrEmpty(globalPackagesPath))
                {
                    // Default paths for different operating systems
    #if WINDOWS
                    globalPackagesPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    #elif MACOS
                    globalPackagesPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".nuget", "packages");
    #else
                    globalPackagesPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".nuget", "packages");
    #endif
                }
                // need to load the nuget location

                if(!string.IsNullOrEmpty( versionItem?.EvaluatedValue))
                {
                    // Nuget packages can be referenced with leading 0's like
                    // 2024.02.01.366
                    // But nuget saves it using the Version object so the folder
                    // is actually
                    // 2024.2.1.366
                    var version = Version.Parse(versionItem.EvaluatedValue);

                    dllLocation = Path.Combine(globalPackagesPath, "FlatRedBallDesktopGLNet6", version.ToString(), "lib", "net6.0", "FlatRedBallDesktopGLNet6.dll");

                }

            }
            else
            {

            }

            if(dllLocation?.Exists() == true)
            {
                var module = ModuleDefinition.ReadModule(dllLocation.FullPath);

                //var type = 
                var frbServicesType = module?.Types.FirstOrDefault(item => item.FullName == "FlatRedBall.FlatRedBallServices");

                CustomAttribute syntaxVersionAttribute = null;

                if(frbServicesType != null)
                {
                    if(TryGetCustomAttribute(frbServicesType, "FlatRedBall.SyntaxVersionAttribute", out syntaxVersionAttribute))
                    {
                        // todo - finish here...
                    }
                }


            }
        }
        public static bool TryGetCustomAttribute(TypeDefinition type,
                string attributeType, out CustomAttribute result)
        {
            result = null;
            if (!type.HasCustomAttributes)
                return false;

            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                if (attribute.AttributeType.FullName != attributeType)
                    continue;

                result = attribute;
                return true;
            }

            return false;
        }
    }
}
