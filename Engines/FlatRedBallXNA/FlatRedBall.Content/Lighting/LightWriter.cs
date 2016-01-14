using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

// TODO: replace this with the type you want to write out.
using TWrite = FlatRedBall.Content.Lighting.LightSaveList;

namespace FlatRedBall.Content.Lighting
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class LightWriter : ContentTypeWriter<TWrite>
    {
        protected override void Write(ContentWriter output, TWrite value)
        {
            // Write the number of Ambient lights
            output.Write(value.AmbientLights.Count);

            for (int i = 0; i < value.AmbientLights.Count; i++)
            {
                AmbientLightSave ambientLightSave = value.AmbientLights[i];

                output.Write(ambientLightSave.DiffuseColorR);
                output.Write(ambientLightSave.DiffuseColorG);
                output.Write(ambientLightSave.DiffuseColorB);

                output.Write(ambientLightSave.Enabled);

                output.Write(ambientLightSave.Name);

                output.Write(ambientLightSave.SpecularColorR);
                output.Write(ambientLightSave.SpecularColorG);
                output.Write(ambientLightSave.SpecularColorB);
            }


            // Write the number of Directional Lights
            output.Write(value.DirectionalLights.Count);

            for (int i = 0; i < value.DirectionalLights.Count; i++)
            {
                DirectionalLightSave directionalLightSave = value.DirectionalLights[i];

                output.Write(directionalLightSave.DiffuseColorR);
                output.Write(directionalLightSave.DiffuseColorG);
                output.Write(directionalLightSave.DiffuseColorB);

                output.Write(directionalLightSave.DirectionX);
                output.Write(directionalLightSave.DirectionY);
                output.Write(directionalLightSave.DirectionZ);

                output.Write(directionalLightSave.Enabled);

                output.Write(directionalLightSave.Name);

                output.Write(directionalLightSave.SpecularColorR);
                output.Write(directionalLightSave.SpecularColorG);
                output.Write(directionalLightSave.SpecularColorB);
            }


            // Write the number of Point Lights
            output.Write(value.PointLights.Count);

            for (int i = 0; i < value.PointLights.Count; i++)
            {
                PointLightSave pointLightSave = value.PointLights[i];

                output.Write(pointLightSave.DiffuseColorR);
                output.Write(pointLightSave.DiffuseColorG);
                output.Write(pointLightSave.DiffuseColorB);

                output.Write(pointLightSave.Enabled);

                output.Write(pointLightSave.Name);

                output.Write(pointLightSave.Range);

                output.Write(pointLightSave.SpecularColorR);
                output.Write(pointLightSave.SpecularColorG);
                output.Write(pointLightSave.SpecularColorB);

                output.Write(pointLightSave.X);
                output.Write(pointLightSave.Y);
                output.Write(pointLightSave.Z);
            }


            // Write the number of Spot Lights
            output.Write(value.SpotLights.Count);

            for (int i = 0; i < value.SpotLights.Count; i++)
            {
                SpotLightSave spotLightSave = value.SpotLights[i];

                output.Write(spotLightSave.DiffuseColorR);
                output.Write(spotLightSave.DiffuseColorG);
                output.Write(spotLightSave.DiffuseColorB);

                output.Write(spotLightSave.Enabled);

                output.Write(spotLightSave.InnerAngle);
                output.Write(spotLightSave.Name);
                output.Write(spotLightSave.OuterAngle);

                output.Write(spotLightSave.Range);

                output.Write(spotLightSave.RotationX);
                output.Write(spotLightSave.RotationY);
                output.Write(spotLightSave.RotationZ);

                output.Write(spotLightSave.SpecularColorR);
                output.Write(spotLightSave.SpecularColorG);
                output.Write(spotLightSave.SpecularColorB);

                output.Write(spotLightSave.X);
                output.Write(spotLightSave.Y);
                output.Write(spotLightSave.Z);
            }
        }

        public override string GetRuntimeType(Microsoft.Xna.Framework.TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Graphics.Lighting.LightCollection).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(Microsoft.Xna.Framework.TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.LightReader).AssemblyQualifiedName;
        }
    }
}
