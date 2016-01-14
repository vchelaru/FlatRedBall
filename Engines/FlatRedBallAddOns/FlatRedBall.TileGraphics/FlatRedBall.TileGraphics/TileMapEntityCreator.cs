using FlatRedBall;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlatformerTest1.TileGraphics
{
    public class TileMapEntityCreator
    {
        public static void CreateEntitiesFromTiles<T>(Scene scene, string nameToMatch, IList<T> listToFill) where T : PositionedObject, new()
        {
            // reverse loop since we'll be removing the Sprite
            for (int i = scene.Sprites.Count - 1; i > -1; i--)
            {
                Sprite sprite = scene.Sprites[i];

                if (sprite.Name == nameToMatch)
                {
                    T t = new T();
                    t.Position = sprite.Position;

                    listToFill.Add(t);
                    t.Name = sprite.Name + listToFill.Count;
                    SpriteManager.RemoveSprite(sprite);
                }
            }

        }
    }
}
