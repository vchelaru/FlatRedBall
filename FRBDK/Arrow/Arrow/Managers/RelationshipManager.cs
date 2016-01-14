using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArrowDataConversion;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Scene;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Arrow.Managers
{
    public class RelationshipManager : Singleton<RelationshipManager>
    {
        #region Fields

        Dictionary<Type, GeneralSaveConverter> mConvertersForTypes;

        #endregion



        public RelationshipManager()
        {
            mConvertersForTypes = new Dictionary<Type, GeneralSaveConverter>();
            mConvertersForTypes.Add(typeof(SpriteSave), new SpriteSaveConverter());
            mConvertersForTypes.Add(typeof(CircleSave), new CircleSaveConverter());
            mConvertersForTypes.Add(typeof(AxisAlignedRectangleSave), new AxisAlignedRectangleSaveConverter());
            mConvertersForTypes.Add(typeof(ArrowElementInstance), new ArrowElementInstanceToNosConverter());


        }

        public GeneralSaveConverter ConverterFor(object instance)
        {
            if (instance == null)
            {
                return null;
            }
            else
            {
                return mConvertersForTypes[instance.GetType()];
            }
        }

        public ElementRuntime ElementRuntimeForArrowInstance(object instance, ElementRuntime container)
        {
            // todo:  Add verification because we assume the current IElement contains a NOS for the instance

            string name = (string)LateBinder.GetInstance(instance.GetType()).GetValue(instance, "Name");

            ElementRuntime contained = container.GetContainedElementRuntime(name);

            return contained;
        }

        public object InstanceForElementRuntime(ElementRuntime elementRuntime)
        {
            if (elementRuntime == null)
            {
                return null;
            }

            if (elementRuntime.AssociatedNamedObjectSave == null)
            {
                throw new Exception("The ElementRuntime does not have an associated NamedObject and it should");
            }

            string nameToFind = elementRuntime.AssociatedNamedObjectSave.InstanceName;

            IElement containerIElement = ObjectFinder.Self.GetElementContaining(elementRuntime.AssociatedNamedObjectSave);

            string containerNameStripped = null;
            if (containerIElement is EntitySave)
            {
                containerNameStripped = containerIElement.Name.Substring("Entities/".Length);
            }
            else //Implied: if (containerIElement is ScreenSave)
            {
                containerNameStripped = containerIElement.Name.Substring("Screens/".Length);
            }

            ArrowElementSave container = ArrowState.Self.CurrentArrowProject.Elements.FirstOrDefault(
                item => item.Name == containerNameStripped);

            return container.AllInstances.FirstOrDefault(item =>
                LateBinder.GetInstance(item.GetType()).GetValue(item, "Name") as string == nameToFind);
        }
    }
}
