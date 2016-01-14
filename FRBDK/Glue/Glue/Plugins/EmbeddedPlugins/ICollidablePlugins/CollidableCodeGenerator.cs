using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.ICollidablePlugins
{
    public class CollidableCodeGenerator : ElementComponentCodeGenerator
    {

        public override Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, SaveClasses.IElement element)
        {
            EntitySave asEntitySave = element as EntitySave;
            if (asEntitySave != null && asEntitySave.ImplementsICollidable)
            {

                codeBlock.Line("private FlatRedBall.Math.Geometry.ShapeCollection mGeneratedCollision;");
                var propBlock = codeBlock.Property("public FlatRedBall.Math.Geometry.ShapeCollection", "Collision");

                propBlock.Get().Line("return mGeneratedCollision;");

                return codeBlock;
            }
            else
            {
                return base.GenerateFields(codeBlock, element);
            }
        }

        public override ICodeBlock GeneratePostInitialize(ICodeBlock codeBlock, IElement element)
        {
            if (element.IsICollidable())
            {
                codeBlock.Line("mGeneratedCollision = new FlatRedBall.Math.Geometry.ShapeCollection();");

                foreach (var item in element.NamedObjects.Where(item=>item.IncludeInICollidable))
                {
                    string addCall = item.GetAddToShapeCollection();

                    if (!string.IsNullOrEmpty(addCall))
                    { 
                        codeBlock.Line("mGeneratedCollision." + addCall + "(" + item.FieldName + ");");
                    }


                }
            }

            return codeBlock;
        }


        public override void AddInheritedTypesToList(List<string> listToAddTo, IElement element)
        {
            if (element.IsICollidable())
            {

                listToAddTo.Add("FlatRedBall.Math.Geometry.ICollidable");
            }
        }


    }


    public static class CollidableExtensions
    {
        public static bool IsICollidable(this IElement element)
        {
            return element is EntitySave && (element as EntitySave).ImplementsICollidable;
        }

        public static string GetAddToShapeCollection(this NamedObjectSave nos)
        {
            var type = nos.SourceClassType;

            if (nos.SourceType == SourceType.FlatRedBallType || nos.SourceType == SourceType.File)
            {
                bool hasAdd = 
                    type == "AxisAlignedRectangle" ||
                    type == "Circle" ||
                    type == "Line" ||
                    type == "Polygon" ||
                    type == "Sphere" ||
                    type == "AxisAlignedCube" ||
                    type == "Capsule2D";

                if (hasAdd)
                {
                    return type + "s.AddOneWay";
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // Since ICollidables use a ShapeCollection, we need to add all contained elements to the
                // mGeneratedShapeCollection to get proper collision.  We can't add other ICollidables (currently)
                return null;
            }

        }
    }

}
