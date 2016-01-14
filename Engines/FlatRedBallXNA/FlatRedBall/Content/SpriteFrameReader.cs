using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Content.SpriteFrame;

namespace FlatRedBall.Content
{
    public class SpriteFrameReader : ContentTypeReader<FlatRedBall.ManagedSpriteGroups.SpriteFrame>
    {
        protected override FlatRedBall.ManagedSpriteGroups.SpriteFrame Read(ContentReader input, FlatRedBall.ManagedSpriteGroups.SpriteFrame existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }
            /*
            Sprite parentSprite = input.ReadObject<Sprite>();
            FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides borderSides = (FlatRedBall.ManagedSpriteGroups.SpriteFrame.BorderSides)(input.ReadInt32());

            existingInstance = new FlatRedBall.ManagedSpriteGroups.SpriteFrame(parentSprite.Texture, borderSides);

            existingInstance.SpriteBorderWidth = input.ReadSingle();
            existingInstance.TextureBorderWidth = input.ReadSingle();

            // Set the values according to the parent Sprite.
            existingInstance.ScaleX = parentSprite.ScaleX;
            existingInstance.ScaleY = parentSprite.ScaleY;

            existingInstance.Position = parentSprite.Position;
            existingInstance.RelativePosition = parentSprite.RelativePosition;

            existingInstance.KeepTrackOfReal = parentSprite.KeepTrackOfReal;
            existingInstance.RotationMatrix = parentSprite.RotationMatrix;
            existingInstance.RelativeRotationMatrix = parentSprite.RelativeRotationMatrix;

            existingInstance.ParentRotationChangesPosition = parentSprite.ParentRotationChangesPosition;
            existingInstance.ParentRotationChangesRotation = parentSprite.ParentRotationChangesRotation;

            existingInstance.Name = parentSprite.Name;
            */
            existingInstance = ObjectReader.ReadObject<SpriteFrameSave>(input).ToSpriteFrame("");
            return existingInstance;
        }
    }
}
