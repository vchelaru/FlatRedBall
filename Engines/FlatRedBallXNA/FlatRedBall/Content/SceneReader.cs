#if false
#define SUPPORTS_LIGHTS
#endif

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Content;
using FlatRedBall.Graphics;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Content.Scene;

namespace FlatRedBall.Content
{
    #region XML Docs
    /// <summary>
    /// Class used to read Scenes from XNB files.  This class is used when 
    /// loading Scenes through the content pipeline.
    /// </summary>
    #endregion
    public class SceneReader : ContentTypeReader<FlatRedBall.Scene>
    {
        protected override FlatRedBall.Scene Read(ContentReader input, FlatRedBall.Scene existingInstance)
        {
         
            if (existingInstance != null)
            {
                return existingInstance;
            }

            SceneSave s = null;

            if (ObjectReader.UseReflection)
            {
                s = ObjectReader.ReadObject<SceneSave>(input);
            }
            else
            {
                s = ReadSpriteEditorScene(input);
            }

            s.FileName = input.AssetName;
            return s.ToScene("");
        }

        private SceneSave ReadSpriteEditorScene(ContentReader input)
        {
            SceneSave newObject = new SceneSave();

            int SpriteListCount = input.ReadInt32();
            for (int i = 0; i < SpriteListCount; i++)
                newObject.SpriteList.Add(ObjectReader.ReadObject<FlatRedBall.Content.Scene.SpriteSave>(input));
            int DynamicSpriteListCount = input.ReadInt32();
            for (int i = 0; i < DynamicSpriteListCount; i++)
                newObject.DynamicSpriteList.Add(ObjectReader.ReadObject<FlatRedBall.Content.Scene.SpriteSave>(input));
            newObject.Snapping = input.ReadBoolean();
            newObject.PixelSize = input.ReadSingle();
            int SpriteGridListCount = input.ReadInt32();
            for (int i = 0; i < SpriteGridListCount; i++)
                newObject.SpriteGridList.Add(ObjectReader.ReadObject<FlatRedBall.Content.SpriteGrid.SpriteGridSave>(input));
            
            int LightSaveListCount = input.ReadInt32();

            for (int i = 0; i < LightSaveListCount; i++)
            {
#if SUPPORTS_LIGHTS
                newObject.LightSaveList.Add(ObjectReader.ReadObject<FlatRedBall.Content.Lighting.LightSave>(input));
#endif
            }
            
            int SpriteFrameSaveListCount = input.ReadInt32();
            for (int i = 0; i < SpriteFrameSaveListCount; i++)
                newObject.SpriteFrameSaveList.Add(ObjectReader.ReadObject<FlatRedBall.Content.SpriteFrame.SpriteFrameSave>(input));
            int TextSaveListCount = input.ReadInt32();
            for (int i = 0; i < TextSaveListCount; i++)
                newObject.TextSaveList.Add(ObjectReader.ReadObject<FlatRedBall.Content.Saves.TextSave>(input));
            newObject.SpriteEditorSceneProperties = input.ReadString();
            newObject.AssetsRelativeToSceneFile = input.ReadBoolean();
            newObject.CoordinateSystem = (FlatRedBall.Math.CoordinateSystem)Enum.ToObject(typeof(FlatRedBall.Math.CoordinateSystem), (int)input.ReadInt32());
            return newObject;

        }
    }
}
