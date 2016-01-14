using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

using FlatRedBall.Content.Scene;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.SpriteFrame
{
    [ContentTypeWriter]
    class SpriteFrameWriter : ContentTypeWriter<SpriteFrameSaveContent>
    {
        protected override void Write(ContentWriter output, SpriteFrameSaveContent value)
        {/*
            output.WriteObject<SpriteSave>(value.ParentSprite);
            output.Write(value.BorderSides);
            output.Write(value.SpriteBorderWidth);
            output.Write(value.TextureBorderWidth);*/
            ObjectWriter.WriteObject<SpriteFrameSaveContent>(output, value);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(SpriteFrameReader).AssemblyQualifiedName;
        }
    }
}
