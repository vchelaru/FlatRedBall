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

namespace FlatRedBall.Content.SpriteGrid
{
    [ContentTypeWriter]
    class SpriteGridWriter : ContentTypeWriter<SpriteGridSaveContent>
    {
        protected override void Write(ContentWriter output, SpriteGridSaveContent value)
        {
            ObjectWriter.WriteObject<SpriteGridSaveContent>(output, value);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(SpriteGridReader).AssemblyQualifiedName;
        }
    }
}
