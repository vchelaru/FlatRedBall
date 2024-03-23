using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.VSHelpers.Projects;

public class IosMonogameProject : CombinedEmbeddedContentProject
{

    public IosMonogameProject(Project project)
        : base(project)
    {
    }

    public override bool AllowContentCompile =>  false; 

    public override string ProjectId =>  "iOS MonoGame";

    public override string DefaultContentAction => "BundleResource";

    public override BuildItemMembershipType DefaultContentBuildType=> BuildItemMembershipType.BundleResource;

    public override string PrecompilerDirective => "IOS";

    public override string FolderName => "iOS";

    public override string ProcessInclude(string path)
    {
        var returnValue = base.ProcessInclude(path);

        return returnValue.ToLowerInvariant();
    }

    public override string ProcessLink(string path)
    {
        var returnValue = base.ProcessLink(path);
        // iOS is case-sensitive
        return returnValue.ToLowerInvariant();
    }
    // Is this valid?
    public override string NeededVisualStudioVersion => "10.0"; 

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

            if(buildItem.UnevaluatedInclude.StartsWith("Content\\"))
            {
                string message = 
                    "The item " + buildItem.UnevaluatedInclude + " has its \"include\" value starting with " + 
                    "\"Content\" (upper-case C). Other content files will be added with a lower-case " +
                    "\"content\", and this can confuse Xamarin Studio.";
                toReturn.Add(message);
            }
        }
        return toReturn;
    }
}
