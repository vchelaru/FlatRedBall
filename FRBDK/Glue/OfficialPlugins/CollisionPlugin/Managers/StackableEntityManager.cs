using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ICollidablePlugins;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.CollisionPlugin.Managers
{
    internal class StackableEntityManager : Singleton<StackableEntityManager>
    {
        public const string ImplementsIStackableName = "ImplementsIStackable";

        internal void HandleEntityProperties(EntitySave entitySave, EditorPropertyListBuilder properties)
        {
            if (entitySave.IsICollidableRecursive())
            {
                properties.Set(ElementPropertyModel.CustomProperty(entitySave, ImplementsIStackableName, typeof(bool), "Inheritance and Interfaces"));
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
