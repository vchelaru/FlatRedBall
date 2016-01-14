using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class IosMonogameProject : CombinedEmbeddedContentProject
    {

        public IosMonogameProject(Project project)
            : base(project)
        {
            // This is temporary - eventually we need a better solution for this:
            // Update October 1, 2012
            // I wanted to integrate this
            // into Glue, but it seems like
            // something that is more plugin-worthy.
            // I'm going to continue to make these ignored
            // and we'll add them later through a plugin.
            // Turns out we don't want to ignore MP3s on iOS.
            // We just need to make an additional XNB which is
            // going to be handled by a plugin
            //this.ExtensionsToIgnore.Add("mp3");
            this.ExtensionsToIgnore.Add("wav");


        }

        public override bool AllowContentCompile
        {
            get { return false; }
        }

        public override string ProjectId
        {
            get { return "iOS MonoGame"; }
        }

        public override string DefaultContentAction { get { return "BundleResource"; } }

        public override BuildItemMembershipType DefaultContentBuildType
        {
            get
            {
                return BuildItemMembershipType.BundleResource;
            }
        }

        public override string PrecompilerDirective { get { return "IOS"; } }


        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                {
                    "FlatRedBalliOS/FlatRedBalliOS.dll",
                    "FlatRedBalliOS/FlatRedBalliOS.dll.mdb",
                    "FlatRedBalliOS/MonoGame.Framework.dll",
                    "FlatRedBalliOS/MonoGame.Framework.dll.mdb",
                    "FlatRedBalliOS/Lidgren.Network.dll",
                    "FlatRedBalliOS/Lidgren.Network.dll.mdb"
                };


            }
        }

        public override string FolderName
        {
            get
            {
                return "iOS";
            }
        }

        public override string ProcessInclude(string path)
        {
            var returnValue = base.ProcessInclude(path);

            return returnValue.ToLowerInvariant();
        }

        public override string ProcessLink(string path)
        {
            var returnValue = base.ProcessLink(path);
            // iOS is case-sensitive
            return returnValue.ToLower();
        }
        // Is this valid?
        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }

        public override List<string> GetErrors()
        {
            List<string> toReturn = new List<string>();

            foreach (var buildItem in this)
            {
                var link = buildItem.GetLink();

                if (link != null && link.Contains("..\\"))
                {
                    toReturn.Add("The item " + buildItem.Include + " has a link " + link + ".  Android projects do not support ..\\ in the link.");
                }

                if(buildItem.Include.StartsWith("Content\\"))
                {
                    string message = 
                        "The item " + buildItem.Include + " has its \"include\" value starting with " + 
                        "\"Content\" (upper-case C). Other content files will be added with a lower-case " +
                        "\"content\", and this can confuse Xamarin Studio.";
                    toReturn.Add(message);
                }
            }
            return toReturn;
        }
    }
}
