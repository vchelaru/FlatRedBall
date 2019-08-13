using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin
{
    public class CollisionCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            //var collisionAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;

            //var collisionRelationships = element.AllNamedObjects
            //    .Where(item => item.GetAssetTypeInfo() == collisionAti &&
            //        item.IsDisabled == false &&
            //        item.DefinedByBase == false &&
            //        item.SetByDerived == false)
            //    .ToArray();

            //foreach (var namedObject in collisionRelationships)
            //{
            //    GenerateInitializeCodeFor(namedObject, codeBlock);
            //}


            return codeBlock;
        }

        public static void GenerateInitializeCodeFor(NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            var firstCollidable = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));


            var secondCollidable = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.SecondCollisionName));

            var collisionType = namedObject.Properties.GetValue<CollisionType>(
                nameof(CollisionRelationshipViewModel.CollisionType));

            var firstMass = namedObject.Properties.GetValue<float>(
                nameof(CollisionRelationshipViewModel.FirstCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var secondMass = namedObject.Properties.GetValue<float>(
                nameof(CollisionRelationshipViewModel.SecondCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var elasticity = namedObject.Properties.GetValue<float>(
                nameof(CollisionRelationshipViewModel.CollisionElasticity))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var firstSubCollision = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));

            var secondSubCollision = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));

            var instanceName = namedObject.InstanceName;

            bool isFirstList;
            var firstType = AssetTypeInfoManager.GetFirstGenericType(namedObject, out isFirstList);

            bool isSecondList;
            var secondType = AssetTypeInfoManager.GetSecondGenericType(namedObject, out isSecondList);

            var isFirstTileShapeCollection = firstType == "FlatRedBall.TileCollisions.TileShapeCollection";
            var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

            var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
            var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

            if (!string.IsNullOrEmpty(firstCollidable) && !string.IsNullOrEmpty(secondCollidable))
            {
                if(isSecondTileShapeCollection)
                {
                    // same method used for both list and non-list
                    codeBlock.Line($"{instanceName} = " +
                        $"FlatRedBall.Math.Collision.CollisionManagerTileShapeCollectionExtensions.CreateTileRelationship(" +
                        $"FlatRedBall.Math.Collision.CollisionManager.Self, " +
                        $"{firstCollidable}, {secondCollidable});");

                }
                //else if(isSecondShapeCollection)
                //{
                //    codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
                //        $"{firstCollidable});");
                //}
                else
                {
                    codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
                        $"{firstCollidable}, {secondCollidable});");
                }

                if(!string.IsNullOrEmpty(firstSubCollision) && 
                    firstSubCollision != CollisionRelationshipViewModel.EntireObject)
                {
                    codeBlock.Line($"{instanceName}.SetFirstSubCollision(item => item.{firstSubCollision});");
                }
                if(!string.IsNullOrEmpty(secondSubCollision) && 
                    secondSubCollision != CollisionRelationshipViewModel.EntireObject)
                {
                    codeBlock.Line($"{instanceName}.SetSecondSubCollision(item => item.{secondSubCollision});");
                }

                codeBlock.Line($"{instanceName}.Name = \"{instanceName}\";");



                switch(collisionType)
                {
                    case CollisionType.NoPhysics:
                        // don't do anything
                        break;
                    case CollisionType.MoveCollision:

                        codeBlock.Line($"{instanceName}.SetMoveCollision({firstMass}, {secondMass});");
                        break;
                    case CollisionType.BounceCollision:
                        //var relationship = new FlatRedBall.Math.Collision.CollisionRelationship();
                        //relationship.SetBounceCollision(firstMass, secondMass, elasticity);
                        codeBlock.Line($"{instanceName}.SetBounceCollision({firstMass}, {secondMass}, {elasticity});");
                        break;
                }

            }
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
			if (element is ScreenSave)
			{
				codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();");
			}
			return codeBlock;
        }
    }
}
