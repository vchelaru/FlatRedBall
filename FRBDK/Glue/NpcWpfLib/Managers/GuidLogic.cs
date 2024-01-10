using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Npc.Managers
{
    public static class GuidLogic
    {
        public static void ReplaceGuids(string unpackDirectory)
        {
            string oldGuid = GetOldGuid(unpackDirectory);

            // SDK style projects (like .net 6) no longer require a GUID
            if(!string.IsNullOrEmpty(oldGuid))
            {
                string newGuid = Guid.NewGuid().ToString();

                ReplaceGuidInFile(oldGuid, newGuid, unpackDirectory, "csproj");
                ReplaceGuidInFile(oldGuid, newGuid, unpackDirectory, "sln");
            
                ReplaceAssemblyInfoGuids(unpackDirectory, newGuid);
            }

        }

        private static string GetOldGuid(string unpackDirectory)
        {
            string csproj = GetCsproj(unpackDirectory);

            return GetGUID(csproj);

        }

        private static string GetCsproj(string unpackDirectory)
        {
            string extension = "csproj";

            return GetSingleFileByExtension(unpackDirectory, extension);
        }

        private static string GetSingleFileByExtension(string unpackDirectory, string extension)
        {
            List<string> stringList = FileManager.GetAllFilesInDirectory(unpackDirectory, extension);

            // we should have only one:
            if (stringList.Count > 1)
            {
                throw new Exception($"This location has multiple .{extension} files, so GUIDs cannot be replaced");
            }

            if (stringList.Count == 0)
            {
                throw new Exception($"Could not find any .{extension} files");
            }

            var fullFileName = stringList[0];
            return fullFileName;
        }

        private static void ReplaceGuidInFile(string oldGuid, string newGuid, string unpackDirectory, string extension)
        {
            var fileName = GetSingleFileByExtension(unpackDirectory, extension);

            var contents = FileManager.FromFileText(fileName);

            // Just in case cases are mixed/inconsistent:
            contents = contents.Replace(oldGuid.ToLowerInvariant(), newGuid.ToLowerInvariant());
            contents = contents.Replace(oldGuid.ToUpperInvariant(), newGuid.ToUpperInvariant());

            FileManager.SaveText(contents, fileName);
        }

        private static void ReplaceSlnGuids(string newGuid)
        {

            throw new NotImplementedException();
        }

        private static void ReplaceAssemblyInfoGuids(string unpackDirectory, string newGuid)
        {
            var stringList = FileManager.GetAllFilesInDirectory(unpackDirectory, "cs");

            foreach (string s in stringList)
            {
                if (s.Contains("assemblyinfo.cs", StringComparison.OrdinalIgnoreCase))
                {
                    string contents = FileManager.FromFileText(s);


                    string newLine = "[assembly: Guid(\"" + newGuid + "\")]";

                    StringFunctions.ReplaceLine(
                        ref contents, "[assembly: Guid(", newLine);

                    FileManager.SaveText(contents, s);
                }

            }
        }

        private static string GetGUID(string projectLocation)
        {
            string projectContents = FileManager.FromFileText(projectLocation);
            int startIndex = projectContents.IndexOf("<ProjectGuid>{") + "<ProjectGuid>{".Length;
            int endIndex = projectContents.IndexOf("}</ProjectGuid>");
            if(startIndex == -1 || endIndex == -1)
            {
                return null;
            }
            else
            {
                projectContents = projectContents.Substring(startIndex, endIndex - startIndex);
                return projectContents;
            }

        }
    }
}
