using CSScriptLibrary;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.ScriptLoading
{
    public class ScriptLoadingLogic
    {
        public Assembly LoadProjectCode(string projectDirectoryFullPath)
        {

            List<string> filesToCompile = new List<string>();

            // preload this:
            var tweenerManager = typeof(global::StateInterpolationPlugin.TweenerManager);

            AddCameraSetup(projectDirectoryFullPath, filesToCompile);

            AddScreensAndEntities(projectDirectoryFullPath, filesToCompile);

            var assembly = CSScript.LoadFiles(filesToCompile.ToArray());
            return assembly;
        }

        private void AddScreensAndEntities(string projectDirectoryFullPath, List<string> filesToCompile)
        {
            string projectName = FileManager.RemovePath(projectDirectoryFullPath);
            // remove trailing slash
            projectName = projectName.Substring(0, projectName.Length - 1);


            var screenDirectory = projectDirectoryFullPath + @"Screens\";
            var allScreenFiles = System.IO.Directory.GetFiles(screenDirectory)
                .Where(item =>item.Contains(".Generated."))
                .ToArray();
            filesToCompile.AddRange(allScreenFiles);

            var entitiesDirectory = projectDirectoryFullPath + @"Entities\";
            var allEntityFiles = System.IO.Directory.GetFiles(entitiesDirectory)
                .Where(item =>item.Contains(".Generated."))
                .ToArray();

            filesToCompile.AddRange(allEntityFiles);

            var targetFile = AddEmptyCustomCodeFor(allScreenFiles, allEntityFiles, projectName);
            filesToCompile.Add(targetFile);
        }

        private string AddEmptyCustomCodeFor(string[] allScreenFiles, string[] allEntityFiles, string projectName)
        {
            var stringBuilder = new StringBuilder();

            foreach(var screenFile in allScreenFiles)
            {
                var fileName = 
                    FileManager.RemovePath(FileManager.RemoveExtension(FileManager.RemoveExtension(screenFile)));

                string emptyScreenContents = CreateEmptyPartialForScreen(projectName, fileName);

                stringBuilder.AppendLine(emptyScreenContents);
            }

            foreach(var entityFile in allEntityFiles)
            {
                var fileName =
                    FileManager.RemovePath(FileManager.RemoveExtension(FileManager.RemoveExtension(entityFile)));

                string emptyEntityContents = CreateEmptyPartialForEntity(projectName, fileName);

                stringBuilder.AppendLine(emptyEntityContents);
            }

            var targetfile = "tempEmptyPartials.cs";

            System.IO.File.WriteAllText(targetfile, stringBuilder.ToString());

            return targetfile;
        }

        private static void AddCameraSetup(string projectDirectoryFullPath, List<string> filesToCompile)
        {
            var cameraSetup = projectDirectoryFullPath + @"Setup\CameraSetup.cs";
            // load the camera setup first:
            filesToCompile.Add(cameraSetup);
        }

        private string CreateEmptyPartialForScreen(string projectName, string screenName)
        {
            return $@"
namespace {projectName}.Screens
{{
	public partial class {screenName}
	{{
		void CustomInitialize(){{}}
		void CustomActivity(bool firstTimeCalled){{}}
		void CustomDestroy(){{}}
        static void CustomLoadStaticContent(string contentManagerName){{}}
	}}
}}
";
        }

        private string CreateEmptyPartialForEntity(string projectName, string entityName)
        {
            return $@"
namespace {projectName}.Entities
{{
	public partial class {entityName}
	{{
		void CustomInitialize(){{}}
		void CustomActivity(){{}}
		void CustomDestroy(){{}}
        static void CustomLoadStaticContent(string contentManagerName){{}}
	}}
}}
";
        }
    }
}
