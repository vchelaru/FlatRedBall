using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using NAudioPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudioPlugin.CodeGenerators
{
    class ElementCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.BeforeStandardGenerated;
        public ElementCodeGenerator()
        {

        }
        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
            foreach(var rfs in element.ReferencedFiles)
            {
                var rfsAti = rfs.GetAssetTypeInfo();

                var instanceName = rfs.GetInstanceName();

                if (rfsAti == AssetTypeInfoManager.NAudioSongAti)
                {
                    //codeBlock.Line($"{instanceName}.Stop();")
                }
                else if(rfsAti == AssetTypeInfoManager.NAudioSoundEffectAti)
                {
                    //codeBlock.Line($"{instanceName}.Stop();")
                }
            }
            return codeBlock;
        }
    }
}
