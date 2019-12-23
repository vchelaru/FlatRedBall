using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Parsing;

namespace PluginTestbed.MoveToLayerPlugin
{
        
    [Export(typeof(PluginBase))]
    class MoveToLayerContainer : PluginBase
    {
        List<ElementComponentCodeGenerator> mGeneratorList = new List<ElementComponentCodeGenerator>();

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands
        {
            get;
            set;
        }

        public void CodeGenerationStart(FlatRedBall.Glue.SaveClasses.IElement element)
        {
        }

        public IEnumerable<FlatRedBall.Glue.CodeGeneration.ElementComponentCodeGenerator> CodeGeneratorList
        {
            get { return mGeneratorList; }
        }

        public override string FriendlyName
        {
            get { return "Move to Layer Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(1,1); }
        }
        
        public override void StartUp()
        {
            mGeneratorList.Add(new MoveToLayerComponentGenerator());

            foreach(var generator in mGeneratorList)
            {
                CodeWriter.CodeGenerators.Add(generator);
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            foreach (var generator in mGeneratorList)
            {
                if (CodeWriter.CodeGenerators.Contains(generator))
                {
                    CodeWriter.CodeGenerators.Remove(generator);
                }
            }

            return true;

        }
    }
}
