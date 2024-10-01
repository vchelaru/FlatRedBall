using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.CollisionPlugin.ExtensionMethods
{
    public static class NamedObjectExtensionMethods
    {
        public static void SetFirstCollidableObjectName(this NamedObjectSave nos, string name)
        {
            nos.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName),
                    name);
        }

        public static void SetSecondCollidableObjectName(this NamedObjectSave nos, string name)
        {
            nos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName),
                    name);
        }

        public static string GetFirstCollidableObjectName(this NamedObjectSave nos) =>
            nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

        public static string GetSecondCollidableObjectName(this NamedObjectSave nos) =>
            nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));
    }
}
