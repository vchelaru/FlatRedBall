using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    public class BuildItemViewModel
    {
        public ProjectItem BuildItem { get; set; }
        public ProjectBase Owner { get; set; }

        public string DisplayString
        {
            get
            {
                if (BuildItem != null)
                {
                    string originalForwardSlash = BuildItem.UnevaluatedInclude.Replace('\\', '/');
                    string removeDotDotSlash = FileManager.RemoveDotDotSlash(originalForwardSlash);

                    if (originalForwardSlash != removeDotDotSlash)
                    {
                        return "Include name may not work on all IDEs: " + BuildItem.UnevaluatedInclude;
                    }
                    else
                    {
                        return BuildItem.UnevaluatedInclude;
                    }
                }
                else
                {
                    return "No BuildItem set";
                }
            }
        }

        public bool IsOrphanedReference
        {
            get
            {
                return IsOrphaned(BuildItem, Owner);
            }
        }

        /// <summary>
        /// Returns whether a ProjectItem (a reference in a .csproj) references a file which does not exist. Orphans will cause compile errors
        /// so they should probably be removed.
        /// </summary>
        /// <param name="buildItem">The item to test if an orphan.</param>
        /// <param name="owner">The project which owns the item.</param>
        /// <returns>If the item is an orphan.</returns>
        public static bool IsOrphaned(ProjectItem buildItem, ProjectBase owner)
        {
            bool considerBuildItem = (buildItem.ItemType == "Compile" || buildItem.ItemType == "Content" || buildItem.ItemType == "None");

            if(!considerBuildItem)
            {
                if (owner is VisualStudioProject)
                {
                    var asVisualStudioProject = owner as VisualStudioProject;

                    if (!considerBuildItem && buildItem.ItemType == asVisualStudioProject.DefaultContentAction)
                    {
                        considerBuildItem = true;
                    }
                }
            }

            if (considerBuildItem)
            {
                // characters like '%' are encoded, so we have to decode them:
                string relativeName = System.Web.HttpUtility.UrlDecode( buildItem.UnevaluatedInclude);
                string fullName = owner.MakeAbsolute(relativeName);
                return !FileManager.FileExists(fullName) && buildItem.ItemType != "ProjectReference";
            }
            return false;
        }

        /// <summary>
        /// Removes the build item from the owner ProjectBase if it is an orphan.
        /// </summary>
        /// <returns>Whether any item was removed;</returns>
        public bool CleanSelf()
        {
            bool wasRemoved = false;
            if (IsOrphanedReference)
            {
                Owner.RemoveItem(BuildItem);
                wasRemoved = true;
            }
            return wasRemoved;
        }
        
        public override string ToString()
        {
            return DisplayString;
        }
    }
}
