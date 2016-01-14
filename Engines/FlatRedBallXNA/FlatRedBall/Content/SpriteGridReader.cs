using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.ManagedSpriteGroups;

using Microsoft.Xna.Framework.Content;

using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using FlatRedBall.Content.SpriteGrid;

namespace FlatRedBall.Content
{
    public class SpriteGridReader : ContentTypeReader<FlatRedBall.ManagedSpriteGroups.SpriteGrid>
    {
        protected override FlatRedBall.ManagedSpriteGroups.SpriteGrid Read(ContentReader input, FlatRedBall.ManagedSpriteGroups.SpriteGrid existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            ObjectReader.ReadObject<SpriteGridSave>(input);
            return existingInstance;
        }
    }
}
