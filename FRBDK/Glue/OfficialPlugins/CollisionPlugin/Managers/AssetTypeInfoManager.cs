using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Managers
{
    public class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        AssetTypeInfo collisionRelationshipAti;
        public AssetTypeInfo CollisionRelationshipAti
        {
            get
            {
                if(collisionRelationshipAti == null)
                {
                    collisionRelationshipAti = CreateAtiForCollisionRelationship();
                }
                return collisionRelationshipAti;
            }
        }


        private AssetTypeInfo CreateAtiForCollisionRelationship()
        {
            AssetTypeInfo toReturn = new AssetTypeInfo();

            toReturn.FriendlyName = "Collision Relationship";

            // prevents the NamedObjectSaveCodeGenerator from doing anything:
            toReturn.ConstructorFunc = (a, b, c) => "";

            toReturn.QualifiedRuntimeTypeName = new PlatformSpecificType();

            // as a fallback to systems that haven't yet converted over to the func
            toReturn.QualifiedRuntimeTypeName.QualifiedType =
              "FlatRedBall.Math.Collision.CollisionRelationship";

            // June 10 2022
            // In the future
            // the ATI's VariableDefinition
            // will determine all variables that
            // should be displayed. Unfortunately,
            // for now, if an ATI has no variables, 
            // then Glue defaults to reflection. We don't
            // want any reflected values to be shown in the
            // variables tab, so we'll just put one of the mor
            // harmless values here:
            toReturn.VariableDefinitions.Add(new VariableDefinition
            {
                Name = nameof(FlatRedBall.Math.Collision.CollisionRelationship.IsActive),
                Type = "bool",
                DefaultValue = "true",
            });

            toReturn.ConstructorFunc = (element, namedObjectSave, referencedFileSave) =>
            {
                if(namedObjectSave != null)
                {
                    string objectName = namedObjectSave.FieldName;

                    var codeBlock = new CodeBlockBase();

                    CollisionCodeGenerator.GenerateInitializeCodeFor(element as GlueElement, namedObjectSave, codeBlock);

                    return codeBlock.ToString();
                }
                else
                {
                    return null;
                }
            };

            toReturn.QualifiedRuntimeTypeName.PlatformFunc = GetNosSourceClassType;

            toReturn.QualifiedSaveTypeName = null;
            toReturn.Extension = null;
            toReturn.AddToManagersMethod = null;
            toReturn.CustomLoadMethod = null;
            toReturn.DestroyMethod = null;
            toReturn.ShouldBeDisposed = false;
            toReturn.ShouldAttach = false;

            toReturn.CanBeCloned = false;
            toReturn.MustBeAddedToContentPipeline = false;
            toReturn.HasCursorIsOn = false;
            toReturn.HasVisibleProperty = false;
            toReturn.CanIgnorePausing = false;
            toReturn.CanBeObject = true;

            toReturn.HideFromNewFileWindow = true;

            return toReturn;
        }

        string GetNosSourceClassType(object nosAsObject)
        {
            var nos = nosAsObject as NamedObjectSave;

            if(nos == null)
            {
                return $"FlatRedBall.Math.Collision.CollisionRelationship";
            }
            else
            {
                var container = nos.GetContainer();
                var properties = nos.Properties;
                return GetCollisionRelationshipSourceClassType(container, properties);

            }
        }

        public static string GetCollisionRelationshipSourceClassType(GlueElement container, List<PropertySave> properties)
        {
            bool isFirstList;
            bool isSecondList;

            var firstType = GetFirstGenericType(container, properties, out isFirstList);
            var secondType = GetSecondGenericType(container, properties, out isSecondList);

            // qualify this to make code gen and other logic work correctly:
            if (firstType == "TileShapeCollection")
            {
                firstType = "FlatRedBall.TileCollisions.TileShapeCollection";
            }

            if (secondType == "TileShapeCollection")
            {
                secondType = "FlatRedBall.TileCollisions.TileShapeCollection";
            }

            var isFirstTileShapeCollection =
                firstType == "FlatRedBall.TileCollisions.TileShapeCollection";

            var isSecondTileShapeCollection =
                secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

            var firstElement = ObjectFinder.Self.GetElement(firstType);

            var isFirstStackable = firstElement?.Properties.GetValue<bool>("ImplementsIStackable");

            var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
            var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

            var isSecondNull = string.IsNullOrEmpty(secondType);

            // todo - single vs. shape collection
            // todo - list vs. shape collection

            string relationshipType;

            var collisionType = (CollisionType)properties.GetValue<int>(
                nameof(CollisionRelationshipViewModel.CollisionType));

            if (collisionType == CollisionType.PlatformerCloudCollision ||
                collisionType == CollisionType.PlatformerSolidCollision)
            {
                var effectiveFirstType = firstType;
                if (isFirstList)
                {
                    effectiveFirstType = $"FlatRedBall.Math.PositionedObjectList<{firstType}>";
                }
                var effectiveSecondType = secondType;
                if (isSecondList)
                {
                    effectiveSecondType = $"FlatRedBall.Math.PositionedObjectList<{secondType}>";
                }


                relationshipType =
                    $"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{effectiveFirstType}, {effectiveSecondType}>";

                if (isFirstList && isSecondList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsListRelationship<{firstType}, {secondType}>";
                }
                else if (isFirstList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>";
                }
                else if (isSecondList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<{firstType}, {secondType}>";
                }
            }
            else if (collisionType == CollisionType.DelegateCollision || collisionType == CollisionType.StackingCollision)
            {
                if (isFirstList && isSecondList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsListRelationship";
                }
                else if (isFirstList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsSingleRelationship";
                }
                else if (isSecondList)
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateSingleVsListRelationship";
                }
                else
                {
                    relationshipType = $"FlatRedBall.Math.Collision.DelegateCollisionRelationshipBase";
                }
            }
            else if (isFirstList == false && isSecondList == false)
            {

                if (isSecondTileShapeCollection)
                {
                    relationshipType =
                        "FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship";
                }
                else if (isSecondShapeCollection)
                {
                    relationshipType =
                        "FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection";
                }
                else
                {
                    relationshipType =
                    "FlatRedBall.Math.Collision.CollisionRelationship";
                }
            }
            else if (isFirstList && isSecondList)
            {
                relationshipType = "FlatRedBall.Math.Collision.ListVsListRelationship";
            }
            else if (isFirstList)
            {
                if (isSecondTileShapeCollection)
                {
                    relationshipType = "FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship";
                }
                else if (isSecondShapeCollection)
                {
                    relationshipType = "FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship";
                }
                else if (isSecondNull)
                {
                    relationshipType = "FlatRedBall.Math.Collision.AlwaysCollidingListCollisionRelationship";
                }
                else
                {
                    relationshipType = "FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship";
                }
            }
            else if (isSecondList)
            {
                relationshipType = "FlatRedBall.Math.Collision.PositionedObjectVsListRelationship";
            }
            else
            {
                // not handled:
                relationshipType =
                    "FlatRedBall.Math.Collision.CollisionRelationship";
            }

            if (collisionType == CollisionType.PlatformerCloudCollision ||
                collisionType == CollisionType.PlatformerSolidCollision)
            {
                return relationshipType;
            }
            else if ( 
                ( isSecondTileShapeCollection && collisionType != CollisionType.DelegateCollision && collisionType != CollisionType.StackingCollision) || 
                isSecondShapeCollection || 
                isSecondNull)
            {
                // doesn't require 2nd type param:
                return
                    $"{relationshipType}<{firstType}>";
            }
            else
            {
                return
                    $"{relationshipType}<{firstType}, {secondType}>";
            }
        }

        public static string GetFirstGenericType(NamedObjectSave nos, out bool isList)
        {
            return GetFirstGenericType(nos.GetContainer(), nos.Properties, out isList);
        }

        public static string GetFirstGenericType(GlueElement containerGlueElement, List<PropertySave> properties, out bool isList)
        {
            var propertyName = nameof(CollisionRelationshipViewModel.FirstCollisionName);

            return GetGenericTypeForPropertyName(containerGlueElement, properties, out isList, propertyName);
        }

        public static string GetSecondGenericType(NamedObjectSave nos, out bool isList)
        {
            return GetSecondGenericType(nos.GetContainer(), nos.Properties, out isList);
        }

        public static string GetSecondGenericType(GlueElement containerGlueElement, List<PropertySave> properties, out bool isList)
        {
            var propertyName = nameof(CollisionRelationshipViewModel.SecondCollisionName);

            return GetGenericTypeForPropertyName(containerGlueElement, properties, out isList, propertyName);
        }

        private static string GetGenericTypeForPropertyName(GlueElement containerGlueElement, List<PropertySave> properties, out bool isList, string propertyName)
        {
            var objectName = properties.GetValue<string>(propertyName);

            string type = null;
            isList = false;

            if (containerGlueElement != null)
            {
                var namedObject = containerGlueElement.GetNamedObject(objectName);

                isList = namedObject?.IsList == true;

                if (namedObject != null)
                {
                    if (namedObject.IsList)
                    {
                        type = namedObject.SourceClassGenericType?.Replace("\\", ".");
                    }
                    else
                    {
                        type = NamedObjectSaveCodeGenerator.GetQualifiedTypeName(namedObject);
                    }
                }
            }


            return type;
        }
    }
}
