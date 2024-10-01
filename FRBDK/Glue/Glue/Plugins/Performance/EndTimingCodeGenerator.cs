using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;

namespace PluginTestbed.PerformanceMeasurement
{
    public class EndTimingCodeGenerator : ElementComponentCodeGenerator
    {
        public bool Active
        {
            get;
            set;
        }

        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.GlueElement element)
        {
            if (Active)
            {
                codeBlock.Line("TimeManager.SumTimeSection(@\"" + element.Name + "\");");
            }

            return codeBlock;
        }
    }
}
