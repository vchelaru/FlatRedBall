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
        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, GlueElement element)
        {
            // no more need to dispose - this is handled by the content manager
            //foreach(var rfs in element.ReferencedFiles)
            //{
            //    var rfsAti = rfs.GetAssetTypeInfo();

            //    var instanceName = rfs.GetInstanceName();

            //    var isNAudio =
            //        rfsAti?.QualifiedRuntimeTypeName.QualifiedType == AssetTypeInfoManager.NAudioQualifiedType;

            //    if (isNAudio && rfs.DestroyOnUnload)
            //    {
            //        codeBlock.Line($"{instanceName}.Dispose();");
            //    }
            //}
            return codeBlock;
        }
    }
}
