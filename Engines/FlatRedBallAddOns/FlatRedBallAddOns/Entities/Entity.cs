using System;

using FlatRedBall;
using FlatRedBall.Math.Geometry;

namespace SilverStorm.Entities
{
    public class Entity : PositionedObject
    {
        #region Fields

        Circle mCollision;
        Sprite mVisibleRepresentation;

        #endregion

        #region Properties

        public Circle Collision
        {
            // We provide a getter so that our Screen can perform collision between
            // entities.
            get { return mCollision; }
        }

        #endregion

        #region Methods

        #region Constructor

        public Entity(string contentManagerName)
        {
            SpriteManager.AddPositionedObject(this);

            CreateVisibleRepresentation(contentManagerName);

            CreateCollision();
        }

        #endregion

        #region Public Methods

        public void Activity()
        {
            // Do every-frame activity here, like respond to input or control state

        }

        public void Destroy()
        {
            ShapeManager.Remove(mCollision);

            SpriteManager.RemoveSprite(mVisibleRepresentation);

            SpriteManager.RemovePositionedObject(this);
        }

        #endregion

        #region Private Methods

        private void CreateCollision()
        {
            Circle mCollision = new Circle();

            mCollision.AttachTo(this, false);

            // Comment the following if you want the collision to be invisible.
            ShapeManager.AddCircle(mCollision);
        }

        private void CreateVisibleRepresentation(string contentManagerName)
        {
            string assetToUse = "redball.bmp";
            mVisibleRepresentation.AttachTo(this, false);

            mVisibleRepresentation = SpriteManager.AddSprite(assetToUse, contentManagerName);
        }

        #endregion

        #endregion
    }
}
