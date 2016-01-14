using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CollisionPlugin
{
    class CollisionPluginCodeGenerator : ElementComponentCodeGenerator
    {
        string[] typesToConsiderForCollision = new string[] 
        {
            "AxisAlignedRectangle",
            "Circle",
            "Line",
            "Sphere",
            "AxisAlignedCube",
            "Polygon",
            "Capsule2D",
            "ShapeCollection"

        };



        public IEnumerable<string> GetCollisionObjectsFor(NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.Entity)
            {
                IElement nosElement = ObjectFinder.Self.GetIElement(nos.SourceClassType);

                if (nosElement != null)
                {
                    foreach (var item in GetCollisionObjectsShallowFor(nosElement))
                    {
                        yield return nos.InstanceName + "." +  item.InstanceName;
                    }
                }
            }
            else if(nos.SourceType == SourceType.FlatRedBallType && typesToConsiderForCollision.Contains(nos.SourceClassType))
            {
                yield return nos.InstanceName;
            }

        }


        public IEnumerable<NamedObjectSave> GetCollisionObjectsShallowFor(IElement element)
        {
            // For collision we'll consider all shapes
            return element.NamedObjects.Where(item =>
                {
                    return item.SourceType == SourceType.FlatRedBallType &&
                        typesToConsiderForCollision.Contains(item.SourceClassType) &&
                        // it's gotta be public to be a collision shape
                        item.HasPublicProperty;


                });

        }


        public string GetCollideAgainstCodeBetween(NamedObjectSave first, NamedObjectSave second)
        {
            var firstCollisions = GetCollisionObjectsFor(first);
            var secondCollisions = GetCollisionObjectsFor(second);

            string toReturn = "";

            bool isFirst = true;
            foreach (var firstCodeElement in firstCollisions)
            {
                foreach (var secondCodeElement in secondCollisions)
                {
                    if (!isFirst)
                    {
                        toReturn += "\n|| ";
                    }
                    toReturn += firstCodeElement + ".CollideAgainst(" + secondCodeElement + ");";
                }
            }

            return toReturn;

        }
    }
}
