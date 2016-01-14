using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;

namespace PluginTestbed.StateChains
{
    public class CodeGeneratorStateChain : ElementComponentCodeGenerator
    {
        public StateChainsPlugin ParentPlugin { get; set; }

        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            var collection = ParentPlugin.GlueCommands.TreeNodeCommands.GetProperty<StateChainCollection>(element, StateChainsPlugin.PropertyName);

            if (collection == null) return codeBlock;

            if(collection.StateChains.Count > 0)
            {
                codeBlock.Line("ManageStateChains();");
            }

            return codeBlock;
        }
    }
}
