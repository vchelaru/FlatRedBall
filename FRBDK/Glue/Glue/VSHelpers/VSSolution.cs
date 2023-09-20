using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers
{
    public class VSSolution
    {
        public List<string> ReferencedProjects
        {
            get;
            private set;
        } = new List<string>();

        public string FullFileName
        {
            get;
            set;
        }

        public static bool AddExistingProjectWithDotNet(FilePath solution, FilePath project, out string output, out string error)
        {
            output = null;
            error = null;
            try
            {
                var process = new Process();
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet.exe",
                    Arguments = $"sln \"{solution.FullPath}\" add \"{project.FullPath}\"",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                process.StartInfo = startInfo;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();

                process.WaitForExit(10000);

                if (string.IsNullOrEmpty(error))
                    return true;
                else
                    return false;

            }
            catch (Exception ex)
            {
                error = (error ?? "") + ex.ToString();

                return false;
            }
        }

        public static bool AddExistingProject(FilePath solution, Guid projectTypeId, Guid projectId, string projectName, FilePath projectPath, List<SharedProject> sharedProjects, List<string> projectConfigurations, List<string> solutionConfigurations, out string error)
        {
            //Read file
            var allLines = File.ReadAllLines(solution.FullPath).ToList();

            //Find Global Section
            int? addLineHere = null;
            for (var i = 0; i < allLines.Count; i++)
            {
                if (allLines[i] == "Global")
                {
                    addLineHere = i;
                    break;
                }
            }

            if (!addLineHere.HasValue)
            {
                error = "Unable to parse solution file";
                return false;
            }

            //Add project before global section
            allLines.Insert(addLineHere.Value, GetProjectText(projectTypeId, projectId, projectName, projectPath.RelativeTo(solution.GetDirectoryContainingThis())));

            //Find SharedMSBuildProjectFiles (Todo: fix case where shared doesn't exist)
            if (sharedProjects != null && sharedProjects.Any())
            {
                addLineHere = null;
                for (var i = 0; i < allLines.Count; i++)
                {
                    if (allLines[i].Trim() == "GlobalSection(SharedMSBuildProjectFiles) = preSolution")
                    {
                        addLineHere = i;
                        break;
                    }
                }

                if (!addLineHere.HasValue)
                {
                    error = "Unable to parse solution file";
                    return false;
                }

                //Add shared projects
                foreach (var sharedProject in sharedProjects)
                {
                    allLines.Insert(addLineHere.Value + 1, GetSharedProjectText(sharedProject.Path.RelativeTo(solution.GetDirectoryContainingThis()), projectId, sharedProject.IsSelf ? 13 : 4)); //13 points to itself, otherwise it's a project referencing a shared project
                }
            }

            //Find Solution Configurations
            if (solutionConfigurations != null && solutionConfigurations.Any())
            {
                addLineHere = null;
                for (var i = 0; i < allLines.Count; i++)
                {
                    if (allLines[i].Trim() == "GlobalSection(SolutionConfigurationPlatforms) = preSolution")
                    {
                        addLineHere = i;
                        break;
                    }
                }

                if (!addLineHere.HasValue)
                {
                    error = "Unable to parse solution file";
                    return false;
                }

                //Gather existing solution configurations
                var existingSolutionConfigurations = new List<string>();
                var currentLine = addLineHere.Value + 1;
                while (allLines[currentLine].Trim() != "EndGlobalSection")
                {
                    existingSolutionConfigurations.Add(allLines[currentLine].Trim());
                    currentLine++;
                }

                //Add ones that don't already exist
                foreach (var solutionConfiguration in solutionConfigurations)
                {
                    if (existingSolutionConfigurations.All(item => !String.Equals(item.Trim(),solutionConfiguration.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        allLines.Insert(addLineHere.Value + 1, GetSolutionConfigurationText(solutionConfiguration));
                    }
                }
            }

            //Find Project Configurations
            if (projectConfigurations != null && projectConfigurations.Any())
            {
                addLineHere = null;
                for (var i = 0; i < allLines.Count; i++)
                {
                    if (allLines[i].Trim() == "GlobalSection(ProjectConfigurationPlatforms) = postSolution")
                    {
                        addLineHere = i;
                        break;
                    }
                }

                if (!addLineHere.HasValue)
                {
                    error = "Unable to parse solution file";
                    return false;
                }

                foreach (var projectConfiguration in projectConfigurations)
                {
                    allLines.Insert(addLineHere.Value + 1, GetProjectConfigurationText(projectId, projectConfiguration));
                }
            }

            //Save File
            File.WriteAllLines(solution.FullPath, allLines.ToArray());

            error = null;
            return true;
        }

        private static string GetProjectConfigurationText(Guid projectId, string projectConfiguration)
        {
            return $"\t\t{{{projectId.ToString().ToUpperInvariant()}}}.{projectConfiguration}";
        }

        private static string GetSolutionConfigurationText(string solutionConfiguration)
        {
            return $"\t\t{solutionConfiguration}";
        }

        private static string GetSharedProjectText(string path, Guid projectId, int shareType)
        {
            return $"\t\t{path}*{{{projectId.ToString().ToLowerInvariant()}}}*SharedItemsImports = {shareType}";
        }

        private static string GetProjectText(Guid projectTypeId, Guid projectId, string projectName, string relativePath)
        {
            return $"Project(\"{{{projectTypeId.ToString().ToUpperInvariant()}}}\") = \"{projectName}\", \"{relativePath}\", \"{{{projectId.ToString().ToUpperInvariant()}}}\"\r\nEndProject";
        }

        public static VSSolution FromFile(FilePath fileName)
        {
            VSSolution toReturn = new VSSolution();

            toReturn.FullFileName = fileName.FullPath;

            var lines = System.IO.File.ReadAllLines(fileName.FullPath);

            foreach (var line in lines)
            {
                ParseLine(line, toReturn);
            }

            return toReturn;
        }

        private static void ParseLine(string line, VSSolution solution)
        {
            if (line.StartsWith("Project("))
            {
                ParseProject(line, solution);
            }
        }

        private static void ParseProject(string line, VSSolution solution)
        {
            var splitBySpaces = line.Split('"');

            var projectReference = splitBySpaces[5];

            solution.ReferencedProjects.Add(projectReference);

        }

        public override string ToString()
        {
            return FileManager.RemovePath(FullFileName);
        }

        public class SharedProject
        {
            public FilePath Path { get; set; }
            public bool IsSelf { get; set; }
        }
    }
}
