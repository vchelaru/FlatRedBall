using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.Controls.ProjectSync
{
    public class BuildItemViewModel
    {
        public BuildItem BuildItem { get; set; }
        public ProjectBase Owner { get; set; }

        public string DisplayString
        {
            get
            {
                if (BuildItem != null)
                {
                    string originalForwardSlash = BuildItem.Include.Replace('\\', '/');
                    string removeDotDotSlash = FileManager.RemoveDotDotSlash(originalForwardSlash);

                    if (originalForwardSlash != removeDotDotSlash)
                    {
                        return "Include name may not work on all IDEs: " + BuildItem.Include;
                    }
                    else
                    {
                        return BuildItem.Include;
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

        public static bool IsOrphaned(BuildItem buildItem, ProjectBase owner)
        {
            bool considerBuildItem = (buildItem.Name == "Compile" || buildItem.Name == "Content" || buildItem.Name == "None");

            if(!considerBuildItem)
            {
                if (owner is VisualStudioProject)
                {
                    var asVisualStudioProject = owner as VisualStudioProject;

                    if (!considerBuildItem && buildItem.Name == asVisualStudioProject.DefaultContentAction)
                    {
                        considerBuildItem = true;
                    }
                }
            }

            if (considerBuildItem)
            {
                // characters like '%' are encoded, so we have to decode them:
                string relativeName = System.Web.HttpUtility.UrlDecode( buildItem.Include);
                string fullName = owner.MakeAbsolute(relativeName);
                return !FileManager.FileExists(fullName) && buildItem.Name != "ProjectReference";
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
