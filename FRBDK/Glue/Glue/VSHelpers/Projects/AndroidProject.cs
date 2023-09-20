using System;
using Microsoft.Build.Evaluation;
using System.Collections.Generic;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class AndroidProject : CombinedEmbeddedContentProject
    {
        public AndroidProject(Project project) : base(project)
        {
        }

        public override string FolderName
        {
            get { return "Android"; }
        }

        public override string ProjectId{ get { return "Android"; } }

        public override bool AllowContentCompile { get { return false; } }
        public override string DefaultContentAction { get { return "AndroidAsset"; } }
        public override BuildItemMembershipType DefaultContentBuildType
        {
            get
            {
                return BuildItemMembershipType.AndroidAsset;
            }
        }
        protected override bool NeedCopyToOutput { get { return false; } }

        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }

        public override string PrecompilerDirective { get { return "ANDROID"; } }
        
        public override string ProcessInclude(string path)
        {
            var returnValue = base.ProcessInclude(path);


            // I think we need to replace and consider slashes as this could be part of a file name
            // like assets.png
            returnValue = returnValue.ToLowerInvariant();
            if(returnValue.StartsWith("assets\\") || returnValue.Contains("assets/"))
            {
                returnValue = "Assets\\" + returnValue.Substring("assets/".Length);
            }
            return returnValue;
        }

        public override string ProcessLink(string path)
        {
            var returnValue = base.ProcessLink(path);
            if(returnValue != null)
            {

                // Android is case-sensitive
                returnValue = returnValue.ToLowerInvariant();

                if (returnValue.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || returnValue.StartsWith(@"assets\", StringComparison.OrdinalIgnoreCase))
                {
                    // Assets folder is capitalized in FRB Android projects:
                    returnValue = "A" + returnValue[1..];
                }

                if(returnValue.Contains("/", StringComparison.OrdinalIgnoreCase))
                {
                    returnValue = returnValue.Replace("/", @"\");
                }
            }

            return returnValue;
        }

        public override string ContentDirectory => "Assets/content/";

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               @"Android\FlatRedBall.dll",
                               @"Android\Lidgren.Network.Android.dll",
                               @"Android\MonoGame.Framework.dll"
                           };
            }
        }

        public override List<string> GetErrors()
        {
            List<string> toReturn = new List<string>();

            foreach(var buildItem in EvaluatedItems)
            {
                var link = buildItem.GetLink();

                if(link != null && link.Contains("..\\"))
                {
                    toReturn.Add("The item " + buildItem.UnevaluatedInclude + " has a link " + link + ".  Android projects do not support ..\\ in the link.");
                }
            }
            return toReturn;
        }
    }
}
