using System;
using System.Collections.Generic;
using System.Text;



using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall.Graphics.Particle;

namespace FlatRedBall.Content
{
    public class EmissionSettingsReader : ContentTypeReader<EmissionSettings>
    {
        protected override EmissionSettings Read(ContentReader input, EmissionSettings existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            existingInstance = new EmissionSettings();

            existingInstance.VelocityRangeType = (RangeType)input.ReadInt32();

            existingInstance.RadialVelocity = input.ReadSingle();
            existingInstance.RadialVelocityRange = input.ReadSingle();

            existingInstance.XVelocity = input.ReadSingle();
            existingInstance.YVelocity = input.ReadSingle();
            existingInstance.ZVelocity = input.ReadSingle();
            existingInstance.XVelocityRange = input.ReadSingle();
            existingInstance.YVelocityRange = input.ReadSingle();
            existingInstance.ZVelocityRange = input.ReadSingle();

            existingInstance.WedgeAngle = input.ReadSingle();
            existingInstance.WedgeSpread = input.ReadSingle();

            existingInstance.RotationZ = input.ReadSingle();
            existingInstance.RotationZVelocity = input.ReadSingle();

            existingInstance.RotationZRange = input.ReadSingle();
            existingInstance.RotationZVelocityRange = input.ReadSingle();

            existingInstance.XAcceleration = input.ReadSingle();
            existingInstance.YAcceleration = input.ReadSingle();
            existingInstance.ZAcceleration = input.ReadSingle();

            existingInstance.XAccelerationRange = input.ReadSingle();
            existingInstance.YAccelerationRange = input.ReadSingle();
            existingInstance.ZAccelerationRange = input.ReadSingle();

            existingInstance.Drag = input.ReadSingle();

            existingInstance.ScaleX = input.ReadSingle();
            existingInstance.ScaleY = input.ReadSingle();
            existingInstance.ScaleXRange = input.ReadSingle();
            existingInstance.ScaleYRange = input.ReadSingle();

            existingInstance.ScaleXVelocity = input.ReadSingle();
            existingInstance.ScaleYVelocity = input.ReadSingle();
            existingInstance.ScaleXVelocityRange = input.ReadSingle();
            existingInstance.ScaleYVelocityRange = input.ReadSingle();

            existingInstance.Alpha = (255 - input.ReadSingle()) / 255.0f;
            existingInstance.Red = input.ReadSingle() / 255.0f;
            existingInstance.Green = input.ReadSingle() / 255.0f;
            existingInstance.Blue = input.ReadSingle() / 255.0f;
                             
            existingInstance.AlphaRate = -1 * input.ReadSingle() / 255.0f;
            existingInstance.RedRate = input.ReadSingle() / 255.0f;
            existingInstance.GreenRate = input.ReadSingle() / 255.0f;
            existingInstance.BlueRate = input.ReadSingle() / 255.0f;

            existingInstance.BlendOperation = FlatRedBall.Graphics.GraphicalEnumerations.TranslateBlendOperation(input.ReadString());
            existingInstance.ColorOperation = FlatRedBall.Graphics.GraphicalEnumerations.TranslateColorOperation(input.ReadString());

            return existingInstance;
        }
    }
}
