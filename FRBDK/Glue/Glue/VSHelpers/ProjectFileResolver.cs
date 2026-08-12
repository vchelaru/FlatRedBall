using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using System;
using System.IO;
using System.Linq;

namespace FlatRedBall.Glue.VSHelpers
{
    /// <summary>
    /// Turns whatever the user picked - a .gluj, a solution, a .csproj - into the .csproj Glue loads.
    /// Shared by the Load Project dialog and the command line so both accept the same set of files.
    /// </summary>
    public static class ProjectFileResolver
    {
        /// <summary>
        /// The .csproj for <paramref name="selectedFileName"/>, or null if the selection names no
        /// project this can identify.
        /// </summary>
        /// <remarks>
        /// A returned path is not guaranteed to exist: when a Glue project has no .csproj anywhere
        /// near it, the sibling path comes back so the load reports "Could not find the project
        /// &lt;path&gt;" against something recognizable rather than against the .gluj.
        /// </remarks>
        public static string ResolveCsproj(string selectedFileName)
        {
            if (string.IsNullOrEmpty(selectedFileName))
            {
                return null;
            }

            switch (FileManager.GetExtension(selectedFileName))
            {
                case "sln":
                case "slnx":
                    return ResolveFromSolution(selectedFileName);
                case "gluj":
                case "glux":
                    return ResolveFromGlueProject(selectedFileName);
                default:
                    return selectedFileName;
            }
        }

        /// <remarks>
        /// FRB1 keeps the .gluj beside the .csproj, so the sibling answers first and none of the rest
        /// applies. FRB2 puts everything Glue authors under Content/FrbEditor, so the project root is
        /// that folder's grandparent - and only that folder, deliberately: walking up "until a .csproj
        /// turns up" happily leaves the game and opens whatever unrelated project sits above it.
        /// </remarks>
        static string ResolveFromGlueProject(string glueProjectFileName)
        {
            var siblingCsproj = FileManager.RemoveExtension(glueProjectFileName) + ".csproj";

            if (File.Exists(siblingCsproj))
            {
                return siblingCsproj;
            }

            var projectRoot = Frb2ProjectRootFor(glueProjectFileName);

            if (projectRoot != null)
            {
                var baseName = FileManager.RemovePath(FileManager.RemoveExtension(glueProjectFileName));
                var sameName = Path.Combine(projectRoot, baseName + ".csproj");

                if (File.Exists(sameName))
                {
                    return sameName;
                }

                // Renaming a project in Visual Studio leaves the .gluj under its old name, so names
                // that disagree do not mean this is the wrong project. Only when there is exactly one
                // though - choosing between two is a guess the user is better placed to make.
                var csprojFiles = Directory.GetFiles(projectRoot, "*.csproj");

                if (csprojFiles.Length == 1)
                {
                    return csprojFiles[0];
                }
            }

            return siblingCsproj;
        }

        /// <summary>
        /// The directory an FRB2 game's .csproj lives in, given its .gluj - or null when the .gluj is
        /// not in the FRB2 layout at all.
        /// </summary>
        static string Frb2ProjectRootFor(string glueProjectFileName)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(glueProjectFileName));
            var expectedSuffix = Frb2Project.Frb2GlueProjectSubdirectory
                .Trim('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var root = directory;

            for (int i = 0; i < expectedSuffix.Split(Path.DirectorySeparatorChar).Length; i++)
            {
                root = Path.GetDirectoryName(root);

                if (root == null)
                {
                    return null;
                }
            }

            return string.Equals(Path.Combine(root, expectedSuffix), directory, StringComparison.OrdinalIgnoreCase)
                ? root
                : null;
        }

        static string ResolveFromSolution(string solutionFileName)
        {
            var solution = VSSolution.FromFile(solutionFileName);
            var solutionDirectory = FileManager.GetDirectory(solutionFileName);

            var projects = solution.ReferencedProjects
                .Where(item => FileManager.GetExtension(item.Name) == "csproj" ||
                               FileManager.GetExtension(item.Name) == "vsproj")
                .ToArray();

            var solutionName = FileManager.RemovePath(FileManager.RemoveExtension(solutionFileName));

            var found =
                projects.FirstOrDefault(item =>
                    string.Equals(FileManager.RemovePath(FileManager.RemoveExtension(item.Name)), solutionName,
                        StringComparison.OrdinalIgnoreCase))
                // An FRB2 solution lists the engine projects alongside the game, so this only helps
                // the single-project case - where a renamed solution leaves nothing to match on.
                ?? (projects.Length == 1 ? projects[0] : null);

            if (found == null)
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(Path.Combine(solutionDirectory, found.Name));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
