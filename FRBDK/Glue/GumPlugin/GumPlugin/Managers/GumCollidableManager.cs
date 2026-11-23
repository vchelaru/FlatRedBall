using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;

namespace GumPlugin.Managers
{
    public static class GumCollidableManager
    {
        public const string ImplementsIGumCollidable = "ImplementsIGumCollidable";

        internal static void HandleEntityProperties(EntitySave entitySave, EditorPropertyListBuilder properties)
        {
            // Only show this if this is an ICollidable
            if(entitySave.IsICollidableRecursive())
            {
                properties.Set(ElementPropertyModel.CustomProperty(entitySave, ImplementsIGumCollidable, typeof(bool), "Inheritance and Interfaces"));
            }
        }
    }
}
