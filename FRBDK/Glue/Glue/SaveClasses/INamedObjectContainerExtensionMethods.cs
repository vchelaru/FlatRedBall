using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class INamedObjectContainerExtensionMethods
    {
        public static void UpdateCustomProperties(this INamedObjectContainer container)
        {
            if (container == null)
            {
                throw new ArgumentException("Argument container is null", "container");
            }

            container.UpdateCustomProperties(container.NamedObjects);

        }

        /// <summary>
        /// Calls UpdateCustomProperties on all contained NamedObjects.  This is done recurisvely.
        /// </summary>
        /// <param name="container">The INamedObjectContainer (which is going to be an IElement at the time of this writing)</param>
        /// <param name="namedObjectList">The List of NamedObjectSaves - so that this can be called recursively.</param>
        private static void UpdateCustomProperties(this INamedObjectContainer container, List<NamedObjectSave> namedObjectList)
        {
            for (int i = 0; i < namedObjectList.Count; i++)
            {
                namedObjectList[i].UpdateCustomProperties();

                container.UpdateCustomProperties(namedObjectList[i].ContainedObjects);
            }
        }


    }
}
