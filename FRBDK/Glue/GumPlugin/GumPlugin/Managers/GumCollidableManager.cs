using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumPluginCore.Managers
{
    public static class GumCollidableManager
    {
        public const string ImplementsIGumCollidable = "ImplementsIGumCollidable";

        internal static void HandleDisplayedEntity(EntitySave entitySave, EntitySavePropertyGridDisplayer displayer)
        {
            // Only show this if this is an ICollidable
            if(entitySave.ImplementsICollidable)
            {
                displayer.IncludeCustomPropertyMember(ImplementsIGumCollidable, typeof(bool));
            }
        }
    }
}
