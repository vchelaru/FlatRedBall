using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorReportingPlugin.ViewModels
{
    public class ContentPipelineMismatchViewModel : ErrorViewModel
    {
        public override string UniqueId => Details;

        ReferencedFileSave rfsUsingContentPipeline;
        ReferencedFileSave rfsNotUsingContentPipeline;

        public ContentPipelineMismatchViewModel(ReferencedFileSave rfsUsingContentPipeline, ReferencedFileSave rfsNotUsingContentPipeline)
        { 
            this.rfsNotUsingContentPipeline = rfsNotUsingContentPipeline;
            this.rfsUsingContentPipeline = rfsUsingContentPipeline;

            this.Details = "The two files " + rfsUsingContentPipeline + " and " + rfsNotUsingContentPipeline +
                $" have the same file name.\n{rfsUsingContentPipeline} is using the content pipeline." +
                $"\n{rfsNotUsingContentPipeline} is not." +
                $"\nThis can cause runtime errors.";
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentReferencedFileSave = rfsNotUsingContentPipeline;
        }

        public override bool GetIfIsFixed()
        {
            if(ObjectFinder.Self.GetElementContaining(rfsUsingContentPipeline) == null)
            {
                return true;
            }
            if(ObjectFinder.Self.GetElementContaining(rfsNotUsingContentPipeline) == null)
            {
                return true;
            }
            if(rfsUsingContentPipeline.UseContentPipeline == false)
            {
                return true;
            }
            if(rfsNotUsingContentPipeline.UseContentPipeline == true)
            {
                return true;
            }
            if(rfsNotUsingContentPipeline.LoadedAtRuntime == false || rfsUsingContentPipeline.LoadedAtRuntime == false)
            {
                return true;
            }
            return base.GetIfIsFixed();
        }
    }
}
