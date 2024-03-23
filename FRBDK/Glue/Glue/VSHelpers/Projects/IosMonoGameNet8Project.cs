using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects;

public class IosMonoGameNet8Project : CombinedEmbeddedContentProject
{
    public IosMonoGameNet8Project(Project project) : base(project)
    {
    }

    public override bool AllowContentCompile => false;

    public override string ProjectId => "iOS MonoGame";

    public override string DefaultContentAction => "BundleResource";

    public override BuildItemMembershipType DefaultContentBuildType => BuildItemMembershipType.BundleResource;

    public override string PrecompilerDirective => "IOS";

    public override string FolderName => "iOS";

    public override string NeededVisualStudioVersion => "17.8";

    public override List<string> GetErrors()
    {
        List<string> toReturn = new List<string>();

        foreach (var buildItem in EvaluatedItems)
        {
            var link = buildItem.GetLink();

            if (link != null && link.Contains("..\\"))
            {
                toReturn.Add("The item " + buildItem.UnevaluatedInclude + " has a link " + link + ".  iOS projects do not support ..\\ in the link.");
            }
        }
        return toReturn;
    }
}
