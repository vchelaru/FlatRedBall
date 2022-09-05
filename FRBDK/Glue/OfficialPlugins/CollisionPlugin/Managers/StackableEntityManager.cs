using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.CollisionPlugin.Managers
{
    internal class StackableEntityManager : Singleton<StackableEntityManager>
    {
        public const string ImplementsIStackableName = "ImplementsIStackable";

        internal void HandleDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer displayer)
        {
            if (entitySave.IsICollidableRecursive())
            {
                var member = displayer.IncludeCustomPropertyMember(ImplementsIStackableName, typeof(bool));

                member.SetCategory("Inheritance and Interfaces");
            }
        }

        public bool ImplementsIStackable(GlueElement glueElement)
        {
            return glueElement is EntitySave && 
                glueElement.GetPropertyValue(ImplementsIStackableName) is bool asBool &&
                asBool;
        }
    }
}
