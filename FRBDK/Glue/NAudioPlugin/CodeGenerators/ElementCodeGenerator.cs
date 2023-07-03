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

                if (rfsAti?.QualifiedRuntimeTypeName.QualifiedType == AssetTypeInfoManager.NAudioQualifiedType)
                {
                    codeBlock.Line($"{instanceName}.Dispose();");
                }
            }
            return codeBlock;
        }
    }
}
