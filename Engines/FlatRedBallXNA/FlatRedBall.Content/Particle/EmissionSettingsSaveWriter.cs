using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

using FlatRedBall.Graphics.Particle;

namespace FlatRedBall.Content.Particle
{
    [ContentTypeWriter]
    class EmissionSettingsSaveWriter : ContentTypeWriter<EmissionSettingsSave>
    {
        protected override void Write(ContentWriter output, EmissionSettingsSave value)
        {
            output.Write((int)value.VelocityRangeType);
            
            output.Write(value.RadialVelocity);
            output.Write(value.RadialVelocityRange);

            output.Write(value.XVelocity);
            output.Write(value.YVelocity);
            output.Write(value.ZVelocity);
            output.Write(value.XVelocityRange);
            output.Write(value.YVelocityRange);
            output.Write(value.ZVelocityRange);

            output.Write(value.WedgeAngle);
            output.Write(value.WedgeSpread);

            output.Write(value.RotationZ);
            output.Write(value.RotationZVelocity);

            output.Write(value.RotationZRange);
            output.Write(value.RotationZVelocityRange);

            output.Write(value.XAcceleration);
            output.Write(value.YAcceleration);
            output.Write(value.ZAcceleration);
            output.Write(value.XAccelerationRange);
            output.Write(value.YAccelerationRange);
            output.Write(value.ZAccelerationRange);

            output.Write(value.Drag);

            output.Write(value.ScaleX);
            output.Write(value.ScaleY);
            output.Write(value.ScaleXRange);
            output.Write(value.ScaleYRange);

            output.Write(value.ScaleXVelocity);
            output.Write(value.ScaleYVelocity);
            output.Write(value.ScaleXVelocityRange);
            output.Write(value.ScaleYVelocityRange);

            output.Write(value.Fade);
            output.Write(value.TintRed);
            output.Write(value.TintGreen);
            output.Write(value.TintBlue);

            output.Write(value.FadeRate);
            output.Write(value.TintRedRate);
            output.Write(value.TintGreenRate);
            output.Write(value.TintBlueRate);

            output.Write(value.BlendOperation);
            output.Write(value.ColorOperation);

            // TODO:  Animation

        }

        public override string GetRuntimeReader(Microsoft.Xna.Framework.TargetPlatform targetPlatform)
        {
            return typeof(EmissionSettingsReader).AssemblyQualifiedName;
        }

    }
}
