using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects;

public class AndroidMonoGameNet8Project : CombinedEmbeddedContentProject
{
    public AndroidMonoGameNet8Project(Project project) : base(project) {  }

    public override string FolderName => "Android"; 

    public override string ProjectId => "Android"; 

    public override bool AllowContentCompile => false; 
    public override string DefaultContentAction => "AndroidAsset"; 
    public override BuildItemMembershipType DefaultContentBuildType => BuildItemMembershipType.AndroidAsset;

    protected override bool NeedCopyToOutput => false;

    public override string NeededVisualStudioVersion => "17.8"; 

    public override string PrecompilerDirective => "ANDROID";

    public override string ProcessLink(string path)
    {
        var returnValue = base.ProcessLink(path);
        if (returnValue != null)
        {

            // Android is case-sensitive
            // v55 handles this
            //returnValue = returnValue.ToLowerInvariant();

            //if (returnValue.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || returnValue.StartsWith(@"assets\", StringComparison.OrdinalIgnoreCase))
            //{
            //    // Assets folder is capitalized in FRB Android projects:
            //    returnValue = "A" + returnValue[1..];
            //}

            if (returnValue.Contains("/", StringComparison.OrdinalIgnoreCase))
            {
                returnValue = returnValue.Replace("/", @"\");
            }
        }

        return returnValue;
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
