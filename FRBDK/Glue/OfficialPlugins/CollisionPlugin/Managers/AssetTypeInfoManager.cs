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

            toReturn.ConstructorFunc = (element, namedObjectSave, referencedFileSave) =>
            {
                if(namedObjectSave != null)
                {
                    string objectName = namedObjectSave.FieldName;

                    var codeBlock = new CodeBlockBase();

                    CollisionCodeGenerator.GenerateInitializeCodeFor(namedObjectSave, codeBlock);

                    return codeBlock.ToString();
                }
                else
                {
                    return null;
                }
            };

            toReturn.QualifiedRuntimeTypeName.PlatformFunc = (nosAsObject) =>
            {
                var nos = nosAsObject as NamedObjectSave;

                if(nos == null)
                {
                    return $"FlatRedBall.Math.Collision.CollisionRelationship";
                }
                else
                {
                    bool isFirstList;
                    bool isSecondList;

                    var firstType = GetFirstGenericType(nos, out isFirstList);
                    var secondType = GetSecondGenericType(nos, out isSecondList);

                    var isFirstTileShapeCollection = firstType == "FlatRedBall.TileCollisions.TileShapeCollection";
                    var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

                    var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
                    var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

                    // todo - single vs. shape collection
                    // todo - list vs. shape collection

                    string relationshipType;
                    if (isFirstList == false && isSecondList == false)
                    {

                        if (isSecondTileShapeCollection)
                        {
                            relationshipType =
                                "FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship";
                        }
                        else if(isSecondShapeCollection)
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
                    else if(isFirstList && isSecondList)
                    {
                        relationshipType = "FlatRedBall.Math.Collision.ListVsListRelationship";
                    }
                    else if(isFirstList)
                    {
                        if(isSecondTileShapeCollection)
                        {
                            relationshipType = "FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship";
                        }
                        else if(isSecondShapeCollection)
                        {
                            relationshipType = "FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship";
                        }
                        else
                        {
                            relationshipType = "FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship";
                        }
                    }
                    else if(isSecondList)
                    {
                        relationshipType = "FlatRedBall.Math.Collision.PositionedObjectVsListRelationship";
                    }
                    else
                    {
                        // not handled:
                        relationshipType =
                            "FlatRedBall.Math.Collision.CollisionRelationship";
                    }

                    if(isSecondTileShapeCollection || isSecondShapeCollection)
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
            };

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

        public static string GetFirstGenericType(NamedObjectSave collisionRelationship, out bool isList)
        {
            var firstName = collisionRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

            var container = collisionRelationship.GetContainer();

            string firstType = null;
            isList = false;

            if(container != null)
            {
                var firstObject = container.GetNamedObject(firstName);

                isList = firstObject?.IsList == true;

                if(firstObject != null)
                {
                    if(firstObject.IsList)
                    {
                        firstType = firstObject.SourceClassGenericType?.Replace("\\", ".");
                    }
                    else
                    {
                        firstType = NamedObjectSaveCodeGenerator.GetQualifiedTypeName(firstObject);
                    }
                }
            }


            return firstType;
        }

        public static string GetSecondGenericType(NamedObjectSave collisionRelationship, out bool isList)
        {
            var secondName = collisionRelationship.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));

            var container = collisionRelationship.GetContainer();

            string secondType = null;
            isList = false;

            if(container != null)
            {
                var secondObject = container.GetNamedObject(secondName);

                isList = secondObject?.IsList == true;

                if(secondObject != null)
                {
                    if(secondObject.IsList)
                    {
                        secondType = secondObject.SourceClassGenericType?.Replace("\\", ".");
                    }
                    else
                    {
                        secondType = NamedObjectSaveCodeGenerator.GetQualifiedTypeName(secondObject);
                    }
                }
            }

            return secondType;
        }
    }
}
