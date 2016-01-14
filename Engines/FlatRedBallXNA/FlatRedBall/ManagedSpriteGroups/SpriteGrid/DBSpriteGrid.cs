using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall;
using Microsoft.Xna.Framework;

namespace FlatRedBall.ManagedSpriteGroups
{
    internal class DBSpriteGrid : PositionedObject, IDrawableBatch
    {
        private SpriteGrid mSpriteGrid;

        public bool UpdateEveryFrame
        {
            get { return false; }
        }

        public DBSpriteGrid(SpriteGrid spriteGrid)
            : base()
        {
            mSpriteGrid = spriteGrid;
        }

        public void Draw(Camera camera)
        {
            Position = mSpriteGrid.Blueprint.Position;
            //Renderer.DrawZBufferedSprites(camera, mSpriteGrid.mVisibleSprites);
        }

        public void Update()
        {
        }

        public void Destroy()
        {
        }

    }
}