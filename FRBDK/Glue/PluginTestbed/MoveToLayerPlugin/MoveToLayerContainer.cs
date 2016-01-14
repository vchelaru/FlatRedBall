using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.CodeGeneration;

namespace PluginTestbed.MoveToLayerPlugin
{
        
    [Export(typeof(ICodeGeneratorPlugin))]
    class MoveToLayerContainer : ICodeGeneratorPlugin
    {
        List<ElementComponentCodeGenerator> mGeneratorList = new List<ElementComponentCodeGenerator>();

        bool mIsEnabled = true;

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

        public string FriendlyName
        {
            get { return "Move to Layer Plugin"; }
        }

        public Version Version
        {
            get { return new Version(); }
        }

        public void StartUp()
        {
            mIsEnabled = true;
            if (mGeneratorList.Count == 0)
            {
                mGeneratorList.Add(new MoveToLayerComponentGenerator());
            }
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            mIsEnabled = false;
            return true;
        }
    }
}
