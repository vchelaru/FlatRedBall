using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ProjectExclusionPlugin
{
    class ProjectMembershipManager : Singleton<ProjectMembershipManager>
    {
        /// <summary>
        /// Removes all ReferencedFileSaves which are marked as excluded from the argument ideProject.
        /// </summary>
        /// <remarks>
        /// <param name="ideProject">The project to check for exclusions.</param>
        /// <param name="glueProject">The current Glue project.</param>
        /// <returns>Whether any removals were made.</returns>
        public bool RemoveAllExcludedFiles(ProjectBase ideProject, GlueProjectSave glueProject)
        {
            bool wasAnythingRemoved = false;

            // gets files which have any exclusions
            foreach(var rfs in glueProject.GetAllReferencedFiles().Where(item=>item.ProjectsToExcludeFrom.Count != 0))
            {
                if(rfs.ProjectsToExcludeFrom.Contains( ideProject.Name))
                {
                    wasAnythingRemoved |= TryRemoveFromProject(rfs, ideProject);
                }
            }

            return wasAnythingRemoved;
        }

        public void IncludeFileInProject(string projectName, ReferencedFileSave rfs)
        {
            foreach (var toChange in AllMatching(rfs))
            {
                toChange.ProjectsToExcludeFrom.RemoveAll(item => item == projectName);
            }

            //It's a little less efficient but we'll just perform a full sync to reuse code:

            var syncedProject = GlueState.Self.GetProjects().FirstOrDefault(item => item.Name == projectName);

            if (syncedProject != null)
            {

                syncedProject.SyncTo(GlueState.Self.CurrentMainProject, false);
            }
        }



        public void ExcludeFileFromProject(string projectName, ReferencedFileSave rfs)
        {
            foreach (var toChange in AllMatching(rfs))
            {
                if (!toChange.ProjectsToExcludeFrom.Contains(projectName))
                {
                    toChange.ProjectsToExcludeFrom.Add(projectName);
                }
            }

            var syncedProject = GlueState.Self.GetProjects().FirstOrDefault(item => item.Name == projectName);

            if (syncedProject != null)
            {
                TryRemoveFromProject(rfs, syncedProject);
            }

        }

        private static bool TryRemoveFromProject(ReferencedFileSave rfs, ProjectBase ideProject)
        {
            bool wasRemoved = false;

            string absolute = GlueCommands.Self.GetAbsoluteFileName(rfs);
            wasRemoved = ideProject.RemoveItem(absolute);
            if (ideProject.ContentProject != null)
            {
                wasRemoved |= ideProject.ContentProject.RemoveItem(absolute);
            }

            return wasRemoved;
        }

        private static IEnumerable<ReferencedFileSave> AllMatching(ReferencedFileSave rfs)
        {
            yield return rfs;
            var matching = Elements.ObjectFinder.Self.GetMatchingReferencedFiles(rfs);
            foreach (var match in matching)
            {
                yield return match;
            }
        }


    }
}
