using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class AndroidMonoGameNet8Project : CombinedEmbeddedContentProject
    {
        public AndroidMonoGameNet8Project(Project project) : base(project)
        {
        }

        public override string FolderName
        {
            get { return "Android"; }
        }

        public override string ProjectId { get { return "Android"; } }

        public override bool AllowContentCompile { get { return false; } }
        public override string DefaultContentAction { get { return "AndroidAsset"; } }
        public override BuildItemMembershipType DefaultContentBuildType
        {
            get
            {
                return BuildItemMembershipType.AndroidAsset;
            }
        }

        protected override bool NeedCopyToOutput => false;

        public override string NeededVisualStudioVersion
        {
            get { return "17.8"; }
        }

        public override string PrecompilerDirective => "ANDROID";

        public override string ProcessInclude(string path)
        {
            var returnValue = base.ProcessInclude(path);


            // I think we need to replace and consider slashes as this could be part of a file name
            // like assets.png
            // March 21, 2024
            // Vic asks - are these needed for .NET 8 projects? I don't think they are...
            //returnValue = returnValue.ToLowerInvariant();
            //if (returnValue.StartsWith("assets\\") || returnValue.Contains("assets/"))
            //{
            //    returnValue = "Assets\\" + returnValue.Substring("assets/".Length);
            //}
            return returnValue;
        }

        public override string ContentDirectory => "content/";

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               @"Android\FlatRedBallAndroid.dll",
                           };
            }
        }


        public override List<string> GetErrors()
        {
            List<string> toReturn = new List<string>();

            foreach (var buildItem in EvaluatedItems)
            {
                var link = buildItem.GetLink();

                if (link != null && link.Contains("..\\"))
                {
                    toReturn.Add("The item " + buildItem.UnevaluatedInclude + " has a link " + link + ".  Android projects do not support ..\\ in the link.");
                }
            }
            return toReturn;
        }
    }
}
