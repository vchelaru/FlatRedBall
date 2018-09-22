using FlatRedBall.Math.Collision;
using FlatRedBall.Math.Geometry;
using FlatRedBall.TileCollisions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Math.Collision
{
    public static class CollisionManagerTileShapeCollectionExtensions
    {
        public static CollidableVsTileShapeCollectionRelationship<FirstCollidableT> CreateTileRelationship<FirstCollidableT>(
            this CollisionManager collisionManager,
            FirstCollidableT collidable, TileShapeCollection tileShapeCollection)
            where FirstCollidableT : PositionedObject, ICollidable
        {
            var relationship = new CollidableVsTileShapeCollectionRelationship<FirstCollidableT>(
                collidable, tileShapeCollection);

            CollisionManager.Self.Relationships.Add(relationship);

            return relationship;
        }

        public static CollidableListVsTileShapeCollectionRelationship<FirstCollidableT> CreateTileRelationship<FirstCollidableT>(
            this CollisionManager collisionManager, 
            PositionedObjectList<FirstCollidableT> collidable, TileShapeCollection tileShapeCollection)
            where FirstCollidableT : PositionedObject, ICollidable
        {
            var relationship = new CollidableListVsTileShapeCollectionRelationship<FirstCollidableT>(
                collidable, tileShapeCollection);

            CollisionManager.Self.Relationships.Add(relationship);

            return relationship;
        }

    }
}
