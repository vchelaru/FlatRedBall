using System;
using System.Collections.Generic;
using System.Text;
using EffectEditor.EffectComponents.HLSLInformation;

namespace EffectEditor.EffectComponents
{
    public static class FRBConstants
    {
        public static List<EffectParameterDefinition> StandardParameters;

        static FRBConstants()
        {
            StandardParameters = new List<EffectParameterDefinition>();

            StandardParameters.Add(new EffectParameterDefinition(
                "World", HlslTypeDefinition.CreateMatrix(HlslType.Float, 4, 4), StorageClass.None));
            StandardParameters.Add(new EffectParameterDefinition(
                "View", HlslTypeDefinition.CreateMatrix(HlslType.Float, 4, 4), StorageClass.None));
            StandardParameters.Add(new EffectParameterDefinition(
                "Projection", HlslTypeDefinition.CreateMatrix(HlslType.Float, 4, 4), StorageClass.None));

            StandardParameters.Add(new EffectParameterDefinition(
                "InvViewProj", HlslTypeDefinition.CreateMatrix(HlslType.Float, 4, 4), StorageClass.Shared));
            StandardParameters.Add(new EffectParameterDefinition(
                "NearClipPlane", new HlslTypeDefinition(HlslType.Float), StorageClass.Shared));
            StandardParameters.Add(new EffectParameterDefinition(
                "FarClipPlane", new HlslTypeDefinition(HlslType.Float), StorageClass.Shared));
            StandardParameters.Add(new EffectParameterDefinition(
                "CameraPosition", HlslTypeDefinition.CreateVector(HlslType.Float, 3), StorageClass.Shared));
        }
    }
}
