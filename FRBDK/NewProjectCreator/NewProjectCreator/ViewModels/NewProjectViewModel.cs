using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProjectCreator.ViewModels
{
    public class NewProjectViewModel
    {
        char[] invalidNamespaceCharacters = new char[] 
            { 
                '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', 
                '(', ')', '-', '=', '+', ';', '\'', ':', '"', '<', 
                ',', '>', '.', '/', '\\', '?', '[', '{', ']', '}', 
                '|', 
                // Spaces are handled separately
            //    ' ' 
            };

        public bool OpenSlnFolderAfterCreation { get; set; }

        public string ProjectName { get; set; }

        public bool UseDifferentNamespace { get; set; }

        public string DifferentNamespace { get; set; }

        public bool CheckForNewVersions { get; set; }

        public PlatformProjectInfo ProjectType { get; set; }

        public string ProjectLocation { get; set; }

        public bool CreateProjectDirectory { get; set; }

        public string CombinedProjectDirectory
        {
            get
            {
                if (!ProjectLocation.EndsWith("\\") && !ProjectLocation.EndsWith("/"))
                {
                    return ProjectLocation+ "\\" + ProjectName;
                }
                else
                {
                    return ProjectLocation + ProjectName;

                }
            }
        }

        public string GetWhyIsntValid()
        {
            string whyIsntValid = null;
            if(UseDifferentNamespace)
            {
                if (string.IsNullOrEmpty(DifferentNamespace))
                {
                    whyIsntValid = "You must enter a non-empty namespace if using a different namespace";
                }
                else if (char.IsDigit(DifferentNamespace[0]))
                {
                    whyIsntValid = "Namespace can't start with a number.";
                }
                else if (DifferentNamespace.Contains(' '))
                {
                    whyIsntValid = "The namespace can't have any spaces.";
                }
                else if (DifferentNamespace.IndexOfAny(invalidNamespaceCharacters) != -1)
                {
                    whyIsntValid = "The namespace can't contain invalid character " + DifferentNamespace[DifferentNamespace.IndexOfAny(invalidNamespaceCharacters)];
                }
            }

                if(string.IsNullOrEmpty(whyIsntValid))
                {
                    whyIsntValid = ProjectCreationHelper.GetWhyProjectNameIsntValid(ProjectName);
                }


            return whyIsntValid;
        }
    }
}
