using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;

namespace FlatRedBall.Content.Scene
{
    public partial class SceneSaveFromRuntimeExtensions
    {
        public static SceneSave FromLayer(Layer layer)
        {
            FlatRedBall.Scene scene = new FlatRedBall.Scene();
            scene.Sprites.AddRange(layer.Sprites);
            scene.Texts.AddRange(layer.Texts);
            scene.Sprites.AddRange(layer.ZBufferedSprites);

            SceneSave toReturn = new SceneSave();
            SpriteEditorScene.SetFromScene( scene, toReturn);

            return toReturn;

        }



    }
}
