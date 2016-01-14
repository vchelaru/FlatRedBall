#if false
#define SUPPORTS_LIGHTS
#endif

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;

using FlatRedBall.Content.Scene;
using FlatRedBall.Content.SpriteGrid;
using FlatRedBall.Content.SpriteFrame;

using Microsoft.Xna.Framework;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content
{
    [ContentTypeWriter]
    public class SceneFileWriter : ContentTypeWriter<SpriteEditorSceneContent>
    {
        protected override void Write(ContentWriter output, SpriteEditorSceneContent value)
        {
            if (ObjectReader.UseReflection)
            {
                ObjectWriter.WriteObject<SpriteEditorSceneContent>(output, value);
            }
            else
            {
                WriteSceneFile(output, value);

            }
        }

        private void WriteSceneFile(ContentWriter output, SpriteEditorSceneContent value)
        {
            output.Write(value.SpriteList.Count);
            for (int i = 0; i < value.SpriteList.Count; i++)
                ObjectWriter.WriteObject(output, value.SpriteList[i]);
            output.Write(value.DynamicSpriteList.Count);
            for (int i = 0; i < value.DynamicSpriteList.Count; i++)
                ObjectWriter.WriteObject(output, value.DynamicSpriteList[i]);
            output.Write(value.Snapping);
            output.Write(value.PixelSize);
            output.Write(value.SpriteGridList.Count);
            for (int i = 0; i < value.SpriteGridList.Count; i++)
                ObjectWriter.WriteObject(output, value.SpriteGridList[i]);
            
            output.Write(value.SpriteFrameSaveList.Count);
            for (int i = 0; i < value.SpriteFrameSaveList.Count; i++)
                ObjectWriter.WriteObject(output, value.SpriteFrameSaveList[i]);
            output.Write(value.TextSaveList.Count);
            for (int i = 0; i < value.TextSaveList.Count; i++)
                ObjectWriter.WriteObject(output, value.TextSaveList[i]);
            if (value.SpriteEditorSceneProperties != null)
                output.Write(value.SpriteEditorSceneProperties);
            else
                output.Write("");
            output.Write(value.AssetsRelativeToSceneFile);
            output.Write(System.Convert.ToInt32(value.CoordinateSystem));

        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            //return "FlatRedBall.Scene, FlatRedBall.dll";
#if Target360
            return "FlatRedBall.Content.Scene, FlatRedBall360";
#else
            return typeof(FlatRedBall.Scene).AssemblyQualifiedName;

#endif
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            //return "FlatRedBall.Content.SceneReader, FlatRedBall.dll";
            //return "FlatRedBall.Content.SceneReader, FlatRedBall";
#if Target360
            return "FlatRedBall.Content.SceneReader, FlatRedBall360";
#else
            return typeof(FlatRedBall.Content.SceneReader).AssemblyQualifiedName;
#endif
        }
    }
}
